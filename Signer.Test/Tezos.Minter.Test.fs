module Signer.``Tezos minter contract test``

open FsUnit.Xunit
open Newtonsoft.Json.Linq
open Signer.Tezos
open Xunit

let sample = """{
  "prim": "Pair",
  "args": [
    {
      "prim": "Pair",
      "args": [
        {
          "int": "500"
        },
        {
          "bytes": "850adb2175bfc9c1d5d56cf948203116b976dff3"
        }
      ]
    },
    {
      "prim": "Pair",
      "args": [
        {
          "int": "5"
        },
        {
          "bytes": "42775d50b7db4768f32d0267b399de8ed7e56700"
        }
      ]
    }
  ]
}"""

[<Fact>]
let ``Should extract parameters`` () =
    let expected =

        { Amount = 500I
          Destination = "0x850adb2175bfc9c1d5d56cf948203116b976dff3"
          Fees = 5I
          Erc20 = "0x42775d50b7db4768f32d0267b399de8ed7e56700" }

    let result =
        FungibleUnwrappedEventDto.fromJson (JToken.Parse(sample))

    result |> should equal expected
