module Signer.Pipeline.Ethereum

open System.Threading
open System.Threading.Tasks
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Nethereum.Web3
open Signer.Ethereum


[<CLIMutable>]
type NodeConfiguration = { Endpoint: string; Wait: int }

[<CLIMutable>]
type EthereumConfiguration =
    { Node: NodeConfiguration
      Contract: string }

type EthereumWorker(logger: ILogger<EthereumWorker>, configuration: EthereumConfiguration) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        let ep = configuration.Node.Endpoint
        let web3 = Web3(ep)
        let startingBlock = 7723438I

        Watcher.watchFor
            web3
            { Contract = configuration.Contract
              Wait = configuration.Node.Wait
              From = startingBlock }
        |> AsyncSeq.iter (fun e ->
            ()

            logger.LogInformation
                ("event {hash} {from} {to} {value}", e.Log.TransactionHash, e.Event.From, e.Event.To, e.Event.Value))
        |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task // need to convert into the parameter-less task

type IServiceCollection with
    member this.AddEthereumPipeline(configuration: IConfiguration) =
        let ethereumConfiguration =
            { Contract = configuration.["Ethereum:Contract"]
              Node =
                  { Endpoint = configuration.["Ethereum:Node:Endpoint"]
                    Wait = configuration.GetValue<int>("Ethereum:Node:Wait") } }

        this.AddSingleton<EthereumConfiguration>(ethereumConfiguration)
        |> ignore

        this.AddHostedService<EthereumWorker>() |> ignore
