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

type SignerWorker(logger: ILogger<SignerWorker>, state: StateRocksDb, minter: MinterService) =

    let mutable minterTask: Task<_> option = None
    let cancelToken = new CancellationTokenSource()

    let check (result: AsyncResult<_, string>) =
        async {
            let! result = result

            match result with
            | Ok t -> return t
            | Error err ->
                logger.LogError("Error while starting app {err}", err)
                return raise (ApplicationException(err))
        }
    
    let logHead = function
        | Some (Cid value) -> logger.LogInformation("At Head {v}", value)
        | None -> logger.LogInformation("Starting from scratch")

    interface IHostedService with
        member this.StartAsync(ct) =
            async {
                let! _ = check minter.Check

                let! store =
                    let cidOption = state.GetHead()
                    logHead cidOption
                    check (EventStoreIpfs.Create(IpfsClient("http://localhost:5001"), "bender", cidOption))

                logger.LogInformation("All checks are green")
                let minterWork = minter.Work store
                minterTask <- Some(Async.StartAsTask(minterWork, cancellationToken = cancelToken.Token))
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task

        member this.StopAsync(cancellationToken) =
            async {
                match minterTask with
                | Some t ->
                    cancelToken.Cancel()

                    Task.WhenAny(t, Task.Delay(Timeout.Infinite, cancellationToken))
                    |> Async.AwaitTask
                    |> ignore
                | _ -> ()
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = cancellationToken)) :> Task
