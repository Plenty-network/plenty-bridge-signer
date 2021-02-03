module Indexer.Watcher

open System
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open Signer
open Signer.Ethereum
open Signer.State.LiteDB

[<CLIMutable>]
type IpfsConfiguration = { Endpoint: string; KeyName: string }

[<CLIMutable>]
type EthNodeConfiguration = { Endpoint: string; Wait: int }


[<CLIMutable>]
type EthereumConfiguration =
    { InitialLevel: bigint
      Node: EthNodeConfiguration
      Contract: string }


[<CLIMutable>]
type TezosNodeConfiguration =
    { ChainId: string
      Endpoint: string
      Timeout: int }

[<CLIMutable>]
type TezosConfiguration =
    { QuorumContract: string
      MinterContract: string
      Node: TezosNodeConfiguration }

type WatcherWorkflow = EthEventLog -> DomainResult<bigint>

let workflow: WatcherWorkflow =
    fun logEvent -> AsyncResult.ofSuccess logEvent.Log.BlockNumber.Value
(*
        let toEvent =
            toEvent logEvent.Log.BlockNumber.Value target
            |> AsyncResult.bind

        logEvent
        |> toMintingParameters
        |> packAndSign
        |> toEvent
        |> append*)


type WatcherService(logger: ILogger<WatcherService>,
                    web3: Web3,
                    ethConfiguration: EthereumConfiguration,
                    tezosConfiguration: TezosConfiguration,
                    state: StateLiteDb) =

    let mutable startingBlock: bigint = 0I

    let apply (workflow: WatcherWorkflow) (blockLevel: HexBigInteger, events: _ seq) =
        logger.LogInformation
            ("Processing Block {level} containing {nb} event(s)", blockLevel.Value, events |> Seq.length)

        let applyOne event =
            logger.LogDebug("Processing {i}:{h}", event.Log.TransactionIndex, event.Log.TransactionHash)
            workflow event

        let rec f elements =
            asyncResult {
                match elements with
                | [] -> return blockLevel.Value
                | [ last ] ->
                    let! _ = applyOne last
                    return blockLevel.Value
                | head :: tail ->
                    let! _ = applyOne head
                    return! f tail

            }

        f (events |> Seq.toList)


    member this.Check =
        asyncResult {
            let! block =
                web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync
                |> AsyncResult.catch (fun err -> sprintf "Couldn't connect to ethereum node %s" err.Message)

            startingBlock <- defaultArg (state.GetEthereumLevel()) ethConfiguration.InitialLevel
            state.PutEthereumLevel startingBlock
            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)
            return ()
        }
        |> AsyncResult.catch (fun err -> sprintf "Unexpected check error %s" err.Message)



    member this.Work =
        //let target =
        //    { QuorumContract = TezosAddress.FromString tezosConfiguration.QuorumContract
        //      MinterContract = TezosAddress.FromString(tezosConfiguration.MinterContract + "%signer")
        //++      ChainId = tezosConfiguration.Node.ChainId }

        logger.LogInformation("Resume ethereum watch at level {}", startingBlock)

        //let workflow =
        //    Minting.workflow signer.Sign store.Append target

        let apply = apply workflow

        Watcher.watchFor
            web3
            { Contract = ethConfiguration.Contract
              Confirmations = ethConfiguration.Node.Wait
              From = startingBlock }
        |> AsyncSeq.iterAsync (fun event ->

            async {
                let! result = apply event

                match result with
                | Ok level -> state.PutEthereumLevel(level)
                | Error err -> return raise (ApplicationException(err))

            })



type IServiceCollection with
    member this.AddWatcher(configuration: IConfiguration) =
        let web3Factory (s: IServiceProvider) =
            let conf = s.GetService<EthereumConfiguration>()
            Web3(conf.Node.Endpoint) :> obj

        this.Add(ServiceDescriptor(typeof<Web3>, web3Factory, ServiceLifetime.Singleton))

        this
            .AddSingleton(configuration
                .GetSection("Tezos")
                .Get<TezosConfiguration>())
            .AddSingleton(configuration
                .GetSection("Ethereum")
                .Get<EthereumConfiguration>())
            .AddSingleton<WatcherService>()
