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


let target: Quorum =
    { QuorumContract = TezosAddress.FromString multisig
      MinterContract = TezosAddress.FromString(benderContract)
      ChainId = "NetXm8tYqnMWky1" }

let mintErc20 =
    { Erc20 = "0x5592ec0cfb4dbc12d3ab100b257153436a1f0fea"
      Amount = 100I
      Owner = TezosAddress.FromString "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
      EventId =
          { BlockHash = "0xc2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b"
            LogIndex = 5I } }


let key =
    Key.FromBase58("edsk3na5J3BQh5DY8QGt4v8f3JLpGVfax6YaiRqcfLWmYKaRhs65LU")

[<Fact>]
let ``should pack mint fungible`` () =

    let v =
        Multisig.packMintErc20 target mintErc20
        |> Result.map (Encoder.byteToHex)

    match v with
    | Ok v ->
        v
        |> should
            equal
               "0x05070707070a00000004a83650210a000000160191d2931b012d854529bb38991cb439283b157c940007070508050507070a000000145592ec0cfb4dbc12d3ab100b257153436a1f0fea070707070a00000020c2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b000507070a00000016000046f146853a32c121cfdcd4f446876ae36c4afc5800a4010a0000001c01e5251ca070e1082433a3445733139b318fa80ca1007369676e6572"
    | Error err -> failwith err

[<Fact>]
let ``Should pack mint nft`` () =
    let mint =
        { TokenId = 1337I
          Owner = TezosAddress.FromString "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
          Erc721 = "0x5592ec0cfb4dbc12d3ab100b257153436a1f0fea"
          EventId =
              { BlockHash = "0xc2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b"
                LogIndex = 5I } }

    let v =
        Multisig.packMintErc721 target mint
        |> Result.map (Encoder.byteToHex)

    match v with
    | Ok v ->
        v
        |> should
            equal
               "0x05070707070a00000004a83650210a000000160191d2931b012d854529bb38991cb439283b157c940007070508050807070a000000145592ec0cfb4dbc12d3ab100b257153436a1f0fea070707070a00000020c2796cf51a390d3049f55cc97b6584e14e8a4a9c89b934afee27b3ce9c396f7b000507070a00000016000046f146853a32c121cfdcd4f446876ae36c4afc5800b9140a0000001c01e5251ca070e1082433a3445733139b318fa80ca1007369676e6572"
    | Error err -> failwith err

[<Fact>]
let ``Should sign`` () =
    async {
        let signer = Crypto.memorySigner key

        let! signature =
            Multisig.packMintErc20 target mintErc20
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
               "edsigu16VhLaBaPmEJTfze3Jqkpxd23kRP5TZUMZEMkHqd1dTSJVfPvUK3yx6F55XXnTvsZtUKUMM738gzgLPXs8jzEXWiY7SgA"
    }
