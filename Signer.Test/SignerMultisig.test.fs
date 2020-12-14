module Signer.Test

open System
open Netezos.Forge.Utils
open Nichelson
open Signer.Multisig
open FsUnit.Xunit
open Xunit


let multisig = "KT1MsooZb43dWi5GpHLeoYw5gyXj9viUuMcE"
let fa2Contract = "KT1LL3X5FcnUji8MVVWdi8bsjDATWqVvDgCB"

let benderContract = "KT1VUNmGa1JYJuNxNS4XDzwpsc9N1gpcCBN2%signer"

let target =
    { MultisigContract = TezosAddress.FromString multisig
      BenderContract = TezosAddress.FromString (benderContract)
      ChainId = "NetXm8tYqnMWky1" }

let mint =
    { Amount = 100L
      Owner = TezosAddress.FromString "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
      TokenId = "contract_on_eth"
      TxId = "txId" }

[<Fact>]
let ``should pack`` () =

    let v = pack target mint

    v
    |> Encoder.byteToHex
    |> should
        equal
           "0x05070707070a00000004a83650210a000000160191d2931b012d854529bb38991cb439283b157c9400070700000508070705080707070700a4010a00000016000046f146853a32c121cfdcd4f446876ae36c4afc580707010000000f636f6e74726163745f6f6e5f6574680100000004747849640a0000001c01e5251ca070e1082433a3445733139b318fa80ca1007369676e6572"

[<Fact>]
let ``Should sign``() =
    let payload = pack target mint
    
    let signature = sign payload
    
    signature.ToBase58() |> should equal "edsigtvcit3FoxW4q7jatc84AJxs77kEnXRkpQwD8if31X6SGU8u4R6QXQBFVw3gUY7C99pW5QNXoCAe4dMHcrrerXjujG2fide"
    
[<Fact>]
let ``Should talk to ledger``() = async {
    let! key = ledgerKey
    
    key |> should equal "edpkuPLUBK5Vm2TcGedc8BZ3DiE9UigpuukwdCqfrdgiQPLhkeX2Ev"
}

[<Fact>]
let ``Should sign with ledger``() = async {
    let payload = pack target mint
    let! ledger = Ledger.Client.get() |> Async.AwaitTask
    let buffer = Array.concat [|Hex.Parse("05"); payload|]
    
    
    let! signature = ledger.Sign(payload) |> Async.AwaitTask
    
    signature.ToBase58() |> should equal "edsigtmn1obykPqt74tbCzypKzuqvJ4ydRNjRRvh5i8VhyceaTLoLb9Bgq8NW4GEUv7LkHu8NHV4wm355VN1V1L3X9Tngnx3MxG"
}
    