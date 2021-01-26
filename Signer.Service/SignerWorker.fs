namespace Signer.Service

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Signer.EventStore
open Signer.IPFS
open Signer.State.LiteDB
open Signer.Worker.Minting
open Signer.Worker.Publish
open Signer.Worker.Unwrap

[<CLIMutable>]
type IpfsConfiguration = {
    Endpoint: string
    KeyName: string
}

type SignerWorker(logger: ILogger<SignerWorker>,
                  state: StateLiteDb,
                  ipfsConfiguration: IpfsConfiguration,
                  minter: MinterService,
                  publish: PublishService,
                  unwrap: UnwrapService,
                  lifeTime : IHostApplicationLifetime) =

    let mutable minterTask: Task<_> option = None
    let mutable publishTask: Task<_> option = None
    let mutable unwrapTask: Task<_> option = None
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
                    let eventStoreIpfsResultAsync = EventStoreIpfs.Create(IpfsClient(ipfsConfiguration.Endpoint), ipfsConfiguration.KeyName, state)
                    check (eventStoreIpfsResultAsync)

                logger.LogInformation("All checks are green")
                let minterWork = minter.Work store |> Async.Catch |> Async.bind catch
                let publishWork = publish.Work store |> Async.Catch |> Async.bind catch
                let unwrapWork = unwrap.Work store |> Async.Catch |> Async.bind catch
                minterTask <- Some(Async.StartAsTask(minterWork, cancellationToken = cancelToken.Token))
                publishTask <- Some(Async.StartAsTask(publishWork, cancellationToken = cancelToken.Token))
                unwrapTask <- Some(Async.StartAsTask(unwrapWork, cancellationToken = cancelToken.Token))
                logger.LogInformation("Workers started")
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task

        member this.StopAsync(cancellationToken) =
            async {
                logger.LogInformation("Stopping workers…")
                cancelToken.Cancel()
                match minterTask with
                | Some t ->
                    Task.WhenAny(t, Task.Delay(Timeout.Infinite, cancellationToken))
                    |> Async.AwaitTask
                    |> ignore
                | _ -> ()
            }
            |> (fun a -> Async.StartAsTask(a, cancellationToken = cancellationToken)) :> Task
