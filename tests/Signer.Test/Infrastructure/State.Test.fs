module Signer.State.``LiteDB tests``

open FsUnit.Xunit
open LiteDB
open Signer.EventStore
open Signer.IPFS
open Xunit


let withDb f =
    use db = new LiteDatabase("Filename=:memory:")
    f (new StateLiteDb(db))
    ()

type ``Watcher state``() =

    [<Fact>]
    let ``should save Ethereum level`` () =
        withDb (fun state ->
            state.PutEthereumLevel(bigint 64)

            let actual = state.GetEthereumLevel()

            actual.IsSome |> should equal true
            actual.Value |> should equal (bigint 64))

    [<Fact>]
    let ``should save Tezos level`` () =
        withDb (fun state ->
            state.PutTezosLevel(bigint 64)

            let actual = state.GetTezosLevel()

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
