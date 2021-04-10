module Signer.``Tezos minter contract test``

open FsUnit.Xunit
open Newtonsoft.Json.Linq
open Signer.Tezos
open Xunit

let sampleERC20 = """ {
  "prim": "Pair",
  "args": [
    {
      "bytes": "8710cba8dd88770c868dada31f7471bb832385df"
    },
    {
      "prim": "Pair",
      "args": [
        {
          "int": "9900000000"
        },
        {
          "prim": "Pair",
          "args": [
            {
              "int": "99000000"
            },
            {
              "bytes": "ecb2d6583858aae994f4248f8948e35516cfc9cf"
            }
          ]
        }
      ]
    }
  ]
}"""

let sampleERC721 = """{ "prim": "Pair",
                    "args":
                      [
                        { "bytes":
                                  "a71007e73288789d4f430d5b685182fe84189094" },
                      { "prim": "Pair",
                          "args":
                            [ { "int": "1337" },
                              { "bytes":
                                  "8178c9c1be2a48dcf9ea8ad7a99577da7a283de5" },
                               ] },
                         ] }"""

[<Fact>]
let ``Should extract erc20 parameters`` () =
    let expected =

        { Amount = 9900000000I
          Destination = "0xecb2d6583858aae994f4248f8948e35516cfc9cf"
          Fees = 99000000I
          Erc20 = "0x8710cba8dd88770c868dada31f7471bb832385df" }

    let result =
        ERC20UnwrappedEventDto.fromJson (JToken.Parse(sampleERC20))

    result |> should equal expected

[<Fact>]
let ``Should extract erc721 parameters`` () =
    let expected =

        { TokenId = 1337I
          Destination = "0x8178c9c1be2a48dcf9ea8ad7a99577da7a283de5"
          Erc721 = "0xa71007e73288789d4f430d5b685182fe84189094" }

    let result =
        ERC721UnwrappedEventDto.fromJson (JToken.Parse(sampleERC721))

    result |> should equal expected
