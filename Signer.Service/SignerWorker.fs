namespace Signer.Service

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Signer.EventStore
open Signer.IPFS
open Signer.State.RocksDb
open Signer.Worker.Minting

type SignerWorker(logger: ILogger<SignerWorker>, state: StateRocksDb, minter: MinterService, lifeTime : IHostApplicationLifetime) =

    let mutable minterTask: Task<_> option = None
    let cancelToken = new CancellationTokenSource()

    let check (result: AsyncResult<_, string>) =
        async {
            let! result = result

            match result with
            | Ok _ -> return result
            | Error err ->
                logger.LogError("Error while starting app: {err}", err)
                Environment.ExitCode <- 1
                lifeTime.StopApplication()
                return result
        }
    
    let logHead = function
        | Some (Cid value) -> logger.LogInformation("At Head {v}", value)
        | None -> logger.LogInformation("Starting from scratch")

    let catch = function
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
                let! _ = check minter.Check

                let! store =
                    let cidOption = (state:>EventStoreState).GetHead()
                    logHead cidOption
                    check (EventStoreIpfs.Create(IpfsClient("http://localhost:5001"), "bender", state))

                logger.LogInformation("All checks are green")
                let minterWork = minter.Work store |> Async.Catch |> Async.bind catch
                minterTask <- Some(Async.StartAsTask(minterWork, cancellationToken = cancelToken.Token))
                logger.LogInformation("Workers started")
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task

        member this.StopAsync(cancellationToken) =
            async {
                logger.LogInformation("Stopping workers…")
                match minterTask with
                | Some t ->
                    cancelToken.Cancel()

                    Task.WhenAny(t, Task.Delay(Timeout.Infinite, cancellationToken))
                    |> Async.AwaitTask
                    |> ignore
                | _ -> ()
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = cancellationToken)) :> Task
