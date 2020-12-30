module Signer.IPFS.``Client Test``

open FsUnit.Xunit
open Newtonsoft.Json.Linq
open Signer.Ethereum.Contract
open Xunit

let client = IpfsClient("http://localhost:5001")

type Dag() =

    [<Fact>]
    let ``Should save dag`` () =
        let link (v: string) =
            let link = JObject()
            link.Add("/", JValue(v))
            link

        async {
            let p =
                JObject.FromObject
                    (TransferEventDto(_from = "other value", _to = "nsta", _value = 10I, _tezosAddress = "ainst"))

            let payload = JObject()
            payload.["parent"] <- link "bafyreicxg4gn5j2krxzsj6ddonsyh4rbzxa57flplvhn25yo6fbt3agshy"
            payload.["data"] <- p

            let! r = client.Dag.PutDag(payload, pin = true)

            match r with
            | Ok v ->
                v
                |> should equal (Cid "bafyreiabpwmujiflpznkgmkorss3jg3ktai6h5l7gbefkokvux3a6iezcu")
            | Error err -> failwith err
        }


type Key() =
    [<Fact>]
    let ``Should list keys`` () =
        async {
            let! r = client.Key.List()

            match r with
            | Ok r ->
                r.Length |> should be (greaterThan 0)
                r.Head.Name |> should equal "self"
            | Error msg -> failwith msg
        }

type Name() =
    [<Fact>]
    let ``Should resolve`` () =
        async {
            let! r = client.Name.Resolve("k51qzi5uqu5dhuc1pto6x98woksrqgwhq6d1lff2hfymxmlk4qd7vqgtf980yl")
            match r with
            | Ok v ->
                v
                |> should
                    equal
                       { Protocol = "ipfs"
                         Path = "bafyreiabpwmujiflpznkgmkorss3jg3ktai6h5l7gbefkokvux3a6iezcu" }
            | Error msg -> failwith msg
        }
