module Signer.Worker.Unwrap

open System
open Amazon.KeyManagementService
open Azure.Identity
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Netezos.Rpc
open Nethereum.Signer
open Nethereum.Web3
open Newtonsoft.Json.Linq
open Signer
open Signer.Configuration
open Signer.EventStore
open Signer.State.LiteDB
open Signer.Tezos
open Signer.Ethereum
open Signer.Unwrap
open TzWatch.Domain
open TzWatch.Sync

type UnwrapService(logger: ILogger<UnwrapService>,
                   web3: Web3,
                   tezosRpc: TezosRpc,
                   ethConfiguration: EthereumConfiguration,
                   tezosConfiguration: TezosConfiguration,
                   signer: EthereumSigner,
                   state: StateLiteDb) =

    let mutable lastBlock: bigint = 2I

    let idToString =
        function
        | Operation { OpgHash = hash; Counter = counter } -> sprintf "Hash:%s Counter:%i" hash counter
        | InternalOperation ({ OpgHash = hash; Counter = counter }, nonce) ->
            sprintf "Hash:%s Counter:%i Nonce:%i" hash counter nonce

    let apply (workflow: UnwrapWorkflow) level (updates: Update seq) =
        if updates |> Seq.length > 0
        then logger.LogInformation("Processing Block {level} containing {nb} event(s)", level, updates |> Seq.length)

        let applyOne (event: Update) =
            logger.LogDebug("Processing {id}", idToString event.UpdateId)
            workflow level event

        let rec f elements =
            asyncResult {
                match elements with
                | [] -> return level
                | head :: tail ->
                    let! _ = applyOne head
                    return! f tail

            }

        f (updates |> Seq.toList)

    member this.Check =
        asyncResult {
            let! blockHead =
                tezosRpc.Blocks.Head.Header.GetAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync
                |> AsyncResult.map (fun v -> JToken.Parse(v.ToString()))
                |> AsyncResult.catch (fun err -> sprintf "Couldn't connect to tezos node %s" err.Message)

            lastBlock <- defaultArg (state.GetTezosLevel()) (bigint tezosConfiguration.InitialLevel)
            state.PutTezosLevel lastBlock
            logger.LogInformation("Connected to tezos node at level {level}", blockHead.Value<int>("level"))

            let! addr =
                signer.PublicAddress()
                |> AsyncResult.catch (fun err -> sprintf "Couldn't get public key %s" err.Message)

            logger.LogInformation("Using signing eth address {hash}", addr)
            return ()
        }
        |> AsyncResult.catch (fun err -> sprintf "Unexpected check error %s" err.Message)

    member this.Work(store: EventStoreIpfs) =
        logger.LogInformation("Resume tezos watch at level {level}", lastBlock)

        let pack = Ethereum.Multisig.transactionHash web3

        let workflow =
            Unwrap.workflow signer pack ethConfiguration.LockingContract store.Append

        let parameters =
            Events.subscription tezosConfiguration.MinterContract (uint tezosConfiguration.Node.Confirmations)


        let poller =
            SyncNode(tezosRpc, tezosConfiguration.Node.ChainId)

        let apply = apply workflow

        Subscription.run
            poller
            ({ Level = Height(int lastBlock + 1)
               YieldEmpty = true })
            parameters
        |> AsyncSeq.iterAsync (fun { BlockHeader = header
                                     Updates = updates } ->
            async {
                if updates |> Seq.length > 0
                then logger.LogDebug("Event from tezos level:{level} Block:{hash}", header.Level, header.Hash)

                let! result = apply header.Level updates

                match result with
                | Ok level -> state.PutTezosLevel(level)
                | Error err -> return raise (ApplicationException(err))
            })




type IServiceCollection with
    member this.AddUnwrap() = this.AddSingleton<UnwrapService>()
