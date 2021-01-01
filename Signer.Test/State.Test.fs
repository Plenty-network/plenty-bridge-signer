module Signer.State.``RocksDB tests``

open System.IO
open FsUnit.Xunit
open RocksDbSharp
open Signer.EventStore
open Signer.IPFS
open Signer.State.RocksDb
open Xunit


let withDb f =
    let temp =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let path = Directory.CreateDirectory(temp)

    use db =
        RocksDb.Open(DbOptions().SetCreateIfMissing(true), path.FullName)

    f (new StateRocksDb(db))
    ()

type ``Watcher state``() =

    [<Fact>]
    let ``should save Ethereum level`` () =
        withDb (fun state ->
            state.PutEthereumLevel(bigint 64)

            let actual = state.GetEthereumLevel()

            actual.IsSome |> should equal true
            actual.Value |> should equal (bigint 64))

type ``Event Store State``() =

    let withDb f =
        withDb ((fun s -> s :> EventStoreState) >> f)

    [<Fact>]
    let ``Should save head`` () =
        withDb (fun state ->
            state.PutHead(Cid "acid")
            let head = state.GetHead()

            match head with
            | Some (Cid v) -> v |> should equal "acid"
            | None -> failwith "not found")

    [<Fact>]
    let ``Should return empty head`` () =
        withDb (fun state ->
            let head = state.GetHead()

            head.IsNone |> should equal true)
