module Signer.Worker.Minting

open System
open FSharp.Control
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open Signer
open Signer.Configuration
open Signer.Ethereum
open Signer.State.LiteDB

type MinterService(logger: ILogger<MinterService>,
                   web3: Web3,
                   ethConfiguration: EthereumConfiguration,
                   signer: TezosSigner,
                   state: StateLiteDb,
                   commandBus: ICommandBus) =

    let mutable startingBlock: bigint = 0I

    let apply (blockLevel: HexBigInteger, events: _ seq) =
        logger.LogInformation
            ("Processing Block {level} containing {count} event(s)", blockLevel.Value, events |> Seq.length)

        let applyOne (event: EthEventLog) =
            logger.LogDebug
                ("Processing {transactionIndex}:{transactionHash} index:{logIndex}",
                 event.Log.TransactionIndex,
                 event.Log.TransactionHash,
                 event.Log.LogIndex)

            commandBus.Post(Minting event)


        let rec f elements =
            asyncResult {
                match elements with
                | [] -> return blockLevel.Value
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
                |> AsyncResult.catch (fun err -> $"Couldn't connect to ethereum node %s{err.Message}")

            startingBlock <- defaultArg (state.GetEthereumLevel()) (bigint ethConfiguration.InitialLevel)
            state.PutEthereumLevel startingBlock
            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)

            let! addr =
                signer.PublicAddress()
                |> AsyncResult.catch (fun err -> $"Couldn't get tezos public key %s{err.Message}")

            logger.LogInformation("Using signing tezos address {hash} {key}", addr.Address, addr.GetBase58())
            return ()
        }
        |> AsyncResult.catch (fun err -> $"Unexpected check error %s{err.Message}")



    member this.Work() =
        logger.LogInformation("Resume ethereum watch at level {level}", startingBlock)

        Watcher.watchFor
            web3
            { Contract = ethConfiguration.LockingContract
              Confirmations = ethConfiguration.Node.Confirmations
              From = startingBlock }
        |> AsyncSeq.iterAsync (fun event ->

            async {
                let! result = apply event

                match result with
                | Ok level -> state.PutEthereumLevel(level)
                | Error err -> return raise (ApplicationException(err))

            })

type IServiceCollection with
    member this.AddMinter() = this.AddSingleton<MinterService>()
