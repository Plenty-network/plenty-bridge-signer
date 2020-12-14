namespace Signer.Service

open System
open System.Threading
open System.Threading.Tasks
open FSharp.Control
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Nethereum.Web3
open Signer.Service.Ethereum

type Worker(logger: ILogger<Worker>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        let web3 =
            Web3("https://rinkeby.infura.io/v3/fa01913603ac4f058ab8a0bfc0b2ba9a")
        Watcher.watch web3
        |> AsyncSeq.iterAsync (fun e ->
            async {
                logger.LogInformation("event count {event}", e.Count)
                e |> Seq.iter (fun v ->
                    logger.LogInformation("block {event}", v.Log.BlockNumber)
                    logger.LogInformation("event {from} {to} {value}", v.Event.From, v.Event.To, v.Event.Value)
                    
                    )
                do! Async.Sleep(1000)
            })
        |> Async.StartAsTask :> Task // need to convert into the parameter-less task
