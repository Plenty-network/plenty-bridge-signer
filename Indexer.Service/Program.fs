namespace Indexer.Service

open LiteDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Indexer.State.PostgresqlDB
open Indexer.Watcher
open Indexer.Migration
open Indexer.Worker
open Npgsql

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let pgConnectionString = configuration.["Postgresql:ConnectionString"]
            let pgConnection = new NpgsqlConnection(pgConnectionString)
            this.AddSingleton(new StatePG(pgConnection))

    type IServiceCollection with
        member this.AddPgConnection(configuration: IConfiguration) =
            let pgConnectionString = configuration.["Postgresql:ConnectionString"]
            this.AddSingleton(new NpgsqlConnection(pgConnectionString))
    
    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddPgConnection(hostContext.Configuration)
                    .AddWatcher(hostContext.Configuration)
                    .AddMigration()
                    .AddWorker(hostContext.Configuration)
                |> ignore)


    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
