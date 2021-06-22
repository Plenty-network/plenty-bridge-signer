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
    let private ethereumErrorLevelId = BsonValue(3)

    let private toBson<'a> id (value: 'a) =
        let doc = BsonDocument()
        doc.["_id"] <- id
        doc.["value"] <- BsonValue(value)
        doc

    let private toValue f (doc: BsonDocument) =
        if isNull <| box doc then
            None
        else
            Some(f doc.["value"])

    let private find (db: LiteDatabase) f id =
        db.GetCollection(stateCollection).FindById(id)
        |> toValue f

    let private save (db: LiteDatabase) id value =
        db
            .GetCollection(stateCollection)
            .Upsert(toBson id value)

    type StateLiteDb(db: LiteDatabase) =
        member _.PutEthereumLevel(v: bigint) =
            save db ethereumLevelId (v.ToByteArray())
            |> ignore

            ()

        member _.GetEthereumLevel() =
            find db (fun v -> v.AsBinary |> bigint) ethereumLevelId

        member _.PutEthereumErrorLevel(v: bigint) =
            save db ethereumErrorLevelId (v.ToByteArray())
            |> ignore

            ()

        member _.GetEthereumErrorLevel() =
            find db (fun v -> v.AsBinary |> bigint) ethereumErrorLevelId

        member _.PutTezosLevel(v: bigint) =
            save db tezosLevelId (v.ToByteArray()) |> ignore
            ()

        member _.GetTezosLevel() =
            find db (fun v -> v.AsBinary |> bigint) tezosLevelId

        interface EventStoreState with
            member _.PutHead(Cid value) =
                save db eventStoreHeadId (Encoding.UTF8.GetBytes value)
                |> ignore

                ()

            member _.GetHead() =
                find db (fun v -> Cid(Encoding.UTF8.GetString(v.AsBinary))) eventStoreHeadId

        interface IDisposable with
            member _.Dispose() = db.Dispose()
