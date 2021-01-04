namespace Signer.Service

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open RocksDbSharp
open Signer.State.RocksDb
open Signer.Worker.Minting
open Signer.Worker.Publish

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let rocksDbPath = configuration.["RocksDB:Path"]

            let db =
                RocksDb.Open(DbOptions().SetCreateIfMissing(true), rocksDbPath)

            this.AddSingleton(new StateRocksDb(db))

        member this.AddSigner(conf: IConfiguration) =
            this
                .AddSingleton({ Endpoint = conf.["IPFS:Endpoint"]
                                KeyName = conf.["IPFS:KeyName"] })
                .AddHostedService<SignerWorker>()

        member this.AddPublisher() = this.AddSingleton<PublishService>()

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddPublisher()
                    .AddMinter(hostContext.Configuration)
                    .AddSigner(hostContext.Configuration)
                |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
