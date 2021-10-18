module Signer.Service

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Signer.EventStore
open Signer.IPFS
open Signer.State.LiteDB
open Signer.Worker.Minting
open Signer.Worker.TransactionFailure
open Signer.Worker.Unwrap

type SignerWorker(logger: ILogger<SignerWorker>,
                  state: StateLiteDb,
                  eventStore: EventStoreIpfs,
                  minter: MinterService,
                  transactionFailure: TransactionFailureService,
                  unwrap: UnwrapService,
                  lifeTime: IHostApplicationLifetime) =

    let mutable minterTask: Task<_> option = None
    let mutable publishTask: Task<_> option = None
    let mutable unwrapTask: Task<_> option = None
    let mutable transactionFailureTask: _ Task option = None
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

    let logHead =
        function
        | Some (Cid value) -> logger.LogInformation("At Head {head}", value)
        | None -> logger.LogInformation("Starting from scratch")

    let catch (workerName:string) =
        function
        | Choice2Of2 v ->
            logger.LogError("Error in worker {workerName} {err}", workerName, v.ToString())
            Environment.ExitCode <- 1
            lifeTime.StopApplication()
            Async.retn ()
        | _ ->
            logger.LogInformation("Worker {workerName} gracefully shutdown", workerName)
            Async.retn ()


    interface IHostedService with
        member this.StartAsync(ct) =
            asyncResult {
                let! _ = check minter.Check
                let! _ = check unwrap.Check
                let! _ = check transactionFailure.Check

                let cidOption = (state :> EventStoreState).GetHead()
                logHead cidOption

                let! _ = check (eventStore.GetKey())

                logger.LogInformation("All checks are green")
                eventStore.Publish() |> Async.Start
                let minterWork =
                    minter.Work() |> Async.Catch |> Async.bind (catch "Minter")

                let unwrapWork =
                    unwrap.Work() |> Async.Catch |> Async.bind (catch "Unwrap")
                    
                let transactionFailureWork =
                    transactionFailure.Work() |> Async.Catch |> Async.bind (catch "TransactionFailure")

                minterTask <- Some(Async.StartAsTask(minterWork, cancellationToken = cancelToken.Token))
                unwrapTask <- Some(Async.StartAsTask(unwrapWork, cancellationToken = cancelToken.Token))
                transactionFailureTask <- Some(Async.StartAsTask(transactionFailureWork, cancellationToken = cancelToken.Token))
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


type IServiceCollection with
    member this.AddSigner() = this.AddHostedService<SignerWorker>()
