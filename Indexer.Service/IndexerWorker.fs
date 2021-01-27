module Indexer.Worker

open System
open System.Threading
open System.Threading.Tasks
open Indexer.Watcher
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Signer.EventStore
open Signer.IPFS
open Signer.State.LiteDB

[<CLIMutable>]
type IpfsConfiguration = { Endpoint: string; KeyName: string }

type IndexerWorker(logger: ILogger<IndexerWorker>,
                  state: StateLiteDb,
                  ipfsConfiguration: IpfsConfiguration,
                  watcher: WatcherService,
                  lifeTime: IHostApplicationLifetime) =

    let mutable watcherTask: Task<_> option = None
    let cancelToken = new CancellationTokenSource()

    let catch =
        function
        | Choice2Of2 v ->
            logger.LogError("Error in worker {v}", v.ToString())
            Environment.ExitCode <- 1
            lifeTime.StopApplication()
            Async.retn ()
        | _ ->
            logger.LogInformation("Worker gracefully shutdown")
            Async.retn ()


    interface IHostedService with
        member this.StartAsync(ct) =
            asyncResult {
                let watcherWork =
                    watcher.Work  
                    |> Async.Catch
                    |> Async.bind catch

                watcherTask <- Some(Async.StartAsTask(watcherWork, cancellationToken = cancelToken.Token))
                logger.LogInformation("Workers started")
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task

        member this.StopAsync(cancellationToken) =
            async {
                logger.LogInformation("Stopping workers…")
                cancelToken.Cancel()

                match watcherTask with
                | Some t ->
                    Task.WhenAny(t, Task.Delay(Timeout.Infinite, cancellationToken))
                    |> Async.AwaitTask
                    |> ignore
                | _ -> ()
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = cancellationToken)) :> Task


type IServiceCollection with
    member this.AddWorker(conf: IConfiguration) =
        this
            .AddSingleton(conf.GetSection("IPFS").Get<IpfsConfiguration>())
            .AddHostedService<IndexerWorker>()
