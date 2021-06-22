module Signer.Worker.TransactionFailure

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
open Signer.Unwrap

type TransactionFailureService
    (
        logger: ILogger<TransactionFailureService>,
        web3: Web3,
        ethConfiguration: EthereumConfiguration,
        state: StateLiteDb,
        commandBus: ICommandBus
    ) =

    let mutable startingBlock : bigint = 0I

    let apply (blockLevel: HexBigInteger, events: _ seq) =
        logger.LogInformation(
            "Processing Block {level} containing {count} event(s)",
            blockLevel.Value,
            events |> Seq.length
        )

        let applyOne (event: TransactionFailure) =
            logger.LogDebug(
                "Processing {transactionIndex}:{transactionHash} index:{logIndex}",
                event.Log.TransactionIndex,
                event.Log.TransactionHash,
                event.Log.LogIndex
            )

            commandBus.Post(Unwrap(blockLevel.Value, UnwrapERc20FromExecutionFailure event))


        let rec f elements =
            asyncResult {
                match elements with
                | [] -> return blockLevel.Value
                | head :: tail ->
                    let! _ = applyOne head
                    return! f tail

            }

        f (events |> Seq.toList)

    member _.Check =
        asyncResult {
            let! block =
                web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync
                |> AsyncResult.catch (fun err -> $"Couldn't connect to ethereum node %s{err.Message}")

            startingBlock <- defaultArg (state.GetEthereumErrorLevel()) (bigint ethConfiguration.InitialLevel)
            state.PutEthereumErrorLevel startingBlock
            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)
        }
        |> AsyncResult.catch (fun err -> $"Unexpected check error %s{err.Message}")



    member _.Work() =
        logger.LogInformation("Resume ethereum transaction errors watch at level {level}", startingBlock)

        Watcher.watchForExecutionFailure
            web3
            { Contract = ethConfiguration.LockingContract
              Confirmations = ethConfiguration.Node.Confirmations
              From = startingBlock }
        |> AsyncSeq.iterAsync
            (fun event ->

                async {
                    let! result = apply event

                    match result with
                    | Ok level -> state.PutEthereumErrorLevel(level)
                    | Error err -> return raise (ApplicationException(err))

                })

type IServiceCollection with
    member this.AddTransactionFailure() =
        this.AddSingleton<TransactionFailureService>()
