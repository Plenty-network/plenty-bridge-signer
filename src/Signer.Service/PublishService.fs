module Signer.Worker.Publish

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Signer.EventStore

type PublishService(logger: ILogger<PublishService>, store: EventStoreIpfs) =

    member this.Work() =
        let rec work () =
            async {
                logger.LogInformation("Publishing head")

                let! p = store.Publish()

                match p with
                | Ok v ->
                    logger.LogInformation("Head published at {head}", v)
                    do! Async.Sleep(TimeSpan.FromMinutes(5.0))
                    do! work ()
                | Error err ->
                    logger.LogError("Error while publishing {err}", err)
                    do! work ()
            }

        work ()

type IServiceCollection with

    member this.AddPublisher() = this.AddSingleton<PublishService>()
