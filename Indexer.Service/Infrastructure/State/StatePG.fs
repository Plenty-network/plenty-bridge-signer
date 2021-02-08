namespace Indexer.State

open System
open System.Numerics
open Npgsql
open Dapper

type Item = {
    Key: string
    Value: string
}

module PostgresqlDB =

    let private toValue f (doc: Item) =
        if isNull <| box doc then None else Some (f doc)

    let private find (db: NpgsqlConnection) f id =
        db.QueryFirstOrDefault<Item>("select key,value from progress where key=@key", {| key = id |})
        |> toValue f     

    let private save (db: NpgsqlConnection) id value =
        db.Execute("INSERT INTO progress (key, value) VALUES (@key, @value) ON CONFLICT (key) DO UPDATE SET value=@value", { Key = id; Value = value}) |> ignore

    type StatePG(db: NpgsqlConnection) =

        member this.PutEthereumLevel(v: bigint) =
            save db "ethereumLevelId" (v.ToString()) |> ignore

        member this.GetEthereumLevel() =
            find db (fun v -> v.Value |> BigInteger.Parse) "ethereumLevelId"
            
        member this.PutTezosLevel(v: bigint) =
            save db "tezosLevelId" (v.ToString()) |> ignore

        member this.GetTezosLevel() =
            find db (fun v -> v.Value |> BigInteger.Parse) "tezosLevelId"
            
        interface IDisposable with
            member this.Dispose() = db.Dispose()
