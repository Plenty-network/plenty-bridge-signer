namespace Indexer.Service

open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Indexer.State.PostgresqlDB
open Indexer.Watcher
open Indexer.Migration
open Indexer.Worker
open Indexer.Endpoints
open Npgsql
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe.EndpointRouting
open Giraffe

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
    
    let configureServices (hostContext: WebHostBuilderContext) (services: IServiceCollection) =
        services
            .AddState(hostContext.Configuration)
            .AddPgConnection(hostContext.Configuration)
            .AddWatcher(hostContext.Configuration)
            .AddMigration()
            .AddWorker(hostContext.Configuration)
            .AddRouting()
            .AddGiraffe()
        |> ignore    
            
    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .ConfigureServices(configureServices)
                    .Configure(fun app -> app.UseRouting().UseGiraffe(endpoints) |> ignore)
                |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
