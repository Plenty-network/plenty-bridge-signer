module Signer.``Tezos Multisig test``

open Netezos.Keys
open Nichelson
open FsUnit.Xunit
open Xunit
open Signer.Tezos

let multisig = "KT1MsooZb43dWi5GpHLeoYw5gyXj9viUuMcE"
let fa2Contract = "KT1LL3X5FcnUji8MVVWdi8bsjDATWqVvDgCB"

let benderContract =
    "KT1VUNmGa1JYJuNxNS4XDzwpsc9N1gpcCBN2%signer"


let target: MintingTarget =
    { QuorumContract = TezosAddress.FromString multisig
      MinterContract = TezosAddress.FromString(benderContract)
      ChainId = "NetXm8tYqnMWky1" }

let mint =
    { Amount = 100I
      Owner = TezosAddress.FromString "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
      TokenId = "0x5592ec0cfb4dbc12d3ab100b257153436a1f0fea"
      TxId = "0xc2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b" }


let key =
    Key.FromBase58("edsk3na5J3BQh5DY8QGt4v8f3JLpGVfax6YaiRqcfLWmYKaRhs65LU")

[<Fact>]
let ``should pack`` () =

    let v =
        Multisig.pack target mint
        |> Result.map (Encoder.byteToHex)

    match v with
    | Ok v ->
        v
        |> should
            equal
               "0x05070707070a00000004a83650210a000000160191d2931b012d854529bb38991cb439283b157c9400070705080707070700a4010a00000016000046f146853a32c121cfdcd4f446876ae36c4afc5807070a000000145592ec0cfb4dbc12d3ab100b257153436a1f0fea0a00000020c2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b0a0000001c01e5251ca070e1082433a3445733139b318fa80ca1007369676e6572"
    | Error err -> failwith err

[<Fact>]
let ``Should sign`` () =
    async {
        let signer = Signer.memorySigner key

        let! signature =
            Multisig.pack target mint
            |> AsyncResult.ofResult
            |> AsyncResult.bind signer.Sign
            |> AsyncResult.map (fun s -> s.ToBase58())

        let signature =
            match signature with
            | Ok v -> v
            | Error err -> failwith err

        signature
        |> should
            equal
               "edsigtij4jvh9tTy6cNFUqP2TAprECDQdtqstvdKqyoVKhutWtHu8NtPhhnxqvvzzoRD7Zwdw8w8UxmWuxXHuc4uQGyCcjtKeNw"
    }
