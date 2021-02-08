namespace Signer.State

open System
open System.Text
open LiteDB
open Signer.EventStore
open Signer.IPFS

module LiteDB =

    let private stateCollection = "state"
    let private ethereumLevelId = BsonValue(1)
    let private tezosLevelId = BsonValue(2)
    let private eventStoreHeadId = BsonValue(0)

    let private toBson<'a> id (value: 'a) =
        let doc = BsonDocument()
        doc.["_id"] <- id
        doc.["value"] <- BsonValue(value)
        doc

    let private toValue f (doc: BsonDocument) =
        if isNull <| box doc then None else Some(f doc.["value"])

    let private find (db: LiteDatabase) f id =
        db.GetCollection(stateCollection).FindById(id)
        |> toValue f

    let private save (db: LiteDatabase) id value =
        db
            .GetCollection(stateCollection)
            .Upsert(toBson id value)

    type StateLiteDb(db: LiteDatabase) =
        member this.PutEthereumLevel(v: bigint) =
            save db ethereumLevelId (v.ToByteArray())
            |> ignore

            ()

        member this.GetEthereumLevel() =
            find db (fun v -> v.AsBinary |> bigint) ethereumLevelId

        member this.PutTezosLevel(v: bigint) =
            save db tezosLevelId (v.ToByteArray()) |> ignore
            ()

        member this.GetTezosLevel() =
            find db (fun v -> v.AsBinary |> bigint) tezosLevelId

        interface EventStoreState with
            member this.PutHead(Cid value) =
                save db eventStoreHeadId (Encoding.UTF8.GetBytes value)
                |> ignore

                ()

            member this.GetHead() =
                find db (fun v -> Cid(Encoding.UTF8.GetString(v.AsBinary))) eventStoreHeadId

        interface IDisposable with
            member this.Dispose() = db.Dispose()
