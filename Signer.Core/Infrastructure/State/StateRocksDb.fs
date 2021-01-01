namespace Signer.State

open System
open System.Text
open RocksDbSharp
open Signer.IPFS

module RocksDb =
    let private tezosLevelKey = Encoding.UTF8.GetBytes("tezos_level")
    let private headKey = Encoding.UTF8.GetBytes("head")

    type StateRocksDb(db: RocksDb) =
        member this.PutEthereumLevel(v: bigint) = db.Put(tezosLevelKey, v.ToByteArray())

        member this.GetEthereumLevel() =
            let v = db.Get(tezosLevelKey)
            if v = null then None else Some(bigint(v))

        member this.PutHead(Cid value) =
            db.Put(headKey, Encoding.UTF8.GetBytes(value))

        member this.GetHead() =
            let v = db.Get(headKey)
            if v = null then None else Some(Cid(Encoding.UTF8.GetString(v)))

        interface IDisposable with
            member this.Dispose() = db.Dispose()
        