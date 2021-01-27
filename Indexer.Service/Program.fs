namespace Indexer.Service

open LiteDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Signer.State.LiteDB
open Indexer.Watcher
open Indexer.Worker

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let liteDbPath = configuration.["LiteDB:Path"]
            
            let db = new LiteDatabase(sprintf "Filename=%s;Connection=direct" liteDbPath)
            
            this.AddSingleton(new StateLiteDb(db))

       

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddWatcher(hostContext.Configuration)
                    .AddWorker(hostContext.Configuration)
                |> ignore)


    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
