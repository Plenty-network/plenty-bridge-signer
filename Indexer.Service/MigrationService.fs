module Indexer.Migration

open System
open FSharp.Control
open Indexer
open Indexer.State.PostgresqlDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open Npgsql
open Signer
open Signer.Ethereum
open Signer.State.LiteDB

type MigrationService(logger: ILogger<MigrationService>,
                    connection: NpgsqlConnection) =

    member this.Work =        
        let evolve = Evolve.Evolve(connection, (fun msg -> logger.LogInformation(msg)))
        evolve.Locations <- seq { "db/migrations" }
        evolve.IsEraseDisabled <- true
        evolve.Migrate()

type IServiceCollection with
    member this.AddMigration() =
        this.AddSingleton<MigrationService>()
            
