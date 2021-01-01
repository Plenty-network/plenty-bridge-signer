namespace Signer.Service

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open RocksDbSharp
open Signer.State.RocksDb
open Signer.Worker.Minting

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let rocksDbPath = configuration.["RocksDB:Path"]

            let db =
                RocksDb.Open(DbOptions().SetCreateIfMissing(true), rocksDbPath)

            this.AddSingleton(new StateRocksDb(db))

        member this.AddSigner(_: IConfiguration) = this.AddHostedService<SignerWorker>()

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddMinter(hostContext.Configuration)
                    .AddSigner(hostContext.Configuration)
                |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
