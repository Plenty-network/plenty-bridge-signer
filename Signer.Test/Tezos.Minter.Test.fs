module Signer.``Tezos minter contract test``

open FsUnit.Xunit
open Newtonsoft.Json.Linq
open Signer.Tezos
open Xunit

let sampleERC20 = """ { "prim": "Pair",
                    "args":
                      [ { "prim": "Pair",
                          "args":
                            [ { "int": "100" },
                              { "bytes":
                                  "8178c9c1be2a48dcf9ea8ad7a99577da7a283de5" } ] },
                        { "prim": "Pair",
                          "args":
                            [ { "bytes":
                                  "d368146a3bff47e0fa8cada1ffe00f6738374721" },
                              { "int": "5" } ] } ] }"""

let sampleERC721 = """{ "prim": "Pair",
                    "args":
                      [ { "prim": "Pair",
                          "args":
                            [ { "bytes":
                                  "8178c9c1be2a48dcf9ea8ad7a99577da7a283de5" },
                              { "bytes":
                                  "a71007e73288789d4f430d5b685182fe84189094" } ] },
                        { "int": "1337" } ] }"""

[<Fact>]
let ``Should extract erc20 parameters`` () =
    let expected =

        { Amount = 100I
          Destination = "0x8178c9c1be2a48dcf9ea8ad7a99577da7a283de5"
          Fees = 5I
          Erc20 = "0xd368146a3bff47e0fa8cada1ffe00f6738374721" }

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
