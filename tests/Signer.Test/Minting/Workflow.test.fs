module Signer.``Minting workflow test``

open Common.Logging
open FsUnit.Xunit
open Netezos.Encoding
open Netezos.Keys
open Nethereum.Hex.HexTypes
open Nethereum.RPC.Eth.DTOs
open Nichelson
open Signer.Ethereum
open Signer.Ethereum.Contract
open Signer.Test
open Xunit
open Signer.Minting


let signerPubKey =
    Key.FromBase58 "edsk3na5J3BQh5DY8QGt4v8f3JLpGVfax6YaiRqcfLWmYKaRhs65LU"

let constantSignature =
    ("edsigtzeLrYsqoRxLiQiFbZwtkvR2aQE8u3be1UoFQqYxJYArp3N6jKbpnPKLK5Co51x34GHSCjKugCnJrb69UmKyEfmGYxpoSQ")

let token =
    "0xc7ad46e0b8a400bb3c915120d284aafba8fc4735"

let filterLog =
    FilterLog
        (BlockHash = "0xc279",
         LogIndex = HexBigInteger(10I),
         BlockNumber = HexBigInteger(10I),
         TransactionHash = "TxHash")

let fakeSigner =
    { new TezosSigner with
        member this.PublicAddress() =

            signerPubKey.PubKey |> AsyncResult.ofSuccess

        member this.Sign bytes =
            let bytes =

                Base58.Parse constantSignature

            Signature(bytes, [||]) |> AsyncResult.retn }

let workflow =
    workflow fakeSigner Factory.target

[<Fact>]
let ``Should build erc20 wrap`` () =

    async {
        let p =
            Erc20Wrapped(ERC20WrapAskedEventDto("owner", token, 100I, "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"))

        let! result = workflow { Log = filterLog; Event = p }

        match result with
        | Ok event ->
            let expectedMint =
                { Level = filterLog.BlockNumber.Value
                  TransactionHash = filterLog.TransactionHash
                  Call =
                      { Quorum = Factory.target
                        Signature = constantSignature
                        SignerAddress = signerPubKey.PubKey.GetBase58()
                        Parameters =
                            { Erc20 = token
                              Amount = 100I
                              Owner = TezosAddress.FromStringUnsafe "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
                              EventId = { BlockHash = "0xc279"; LogIndex = 10I } } } }

            event
            |> should equal (Erc20MintingSigned expectedMint)
        | Error e -> failwith e
    }

[<Fact>]
let ``Should build wrap erc721`` () =
    async {
        let p =
            Erc721Wrapped(ERC721WrapAskedEventDto("owner", token, 1337I, "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"))

        let! result = workflow { Log = filterLog; Event = p }

        match result with
        | Ok (event) ->
            let expectedMint =
                { Level = filterLog.BlockNumber.Value
                  TransactionHash = filterLog.TransactionHash
                  Call =
                      { Quorum = Factory.target
                        Signature = constantSignature
                        SignerAddress = signerPubKey.PubKey.GetBase58()
                        Parameters =
                            { Erc721 = token
                              TokenId = 1337I
                              Owner = TezosAddress.FromStringUnsafe "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
                              EventId = { BlockHash = "0xc279"; LogIndex = 10I } } } }

            event
            |> should equal (Erc721MintingSigned expectedMint)
        | Error e -> failwith e
    }

[<Fact>]
let ``Should build erc20 mint error on bad tezos address`` () =
    async {
        let p =
            Erc20Wrapped(ERC20WrapAskedEventDto("owner", token, 100I, "bad_address"))

        let! result = workflow { Log = filterLog; Event = p }

        match result with
        | Ok (event) ->
            let expectedMint =
                { Level = filterLog.BlockNumber.Value
                  TransactionHash = filterLog.TransactionHash
                  Reason = "Bad tezos address bad_address"
                  SignerAddress = signerPubKey.PubKey.GetBase58()
                  EventId = { BlockHash = "0xc279"; LogIndex = 10I } 
                  Payload =
                      { ERC20 = token
                        Owner = "owner"
                        Amount = 100I } }

            event
            |> should equal (Erc20MintingFailed expectedMint)
        | Error e -> failwith e
    }

[<Fact>]
let ``Should build erc721 mint error on bad tezos address`` () =
    async {
        let p =
            Erc721Wrapped(ERC721WrapAskedEventDto("owner", token, 1337I, "bad_address"))

        let! result = workflow { Log = filterLog; Event = p }

        match result with
        | Ok (event) ->
            let expectedMint =
                { Level = filterLog.BlockNumber.Value
                  TransactionHash = filterLog.TransactionHash
                  Reason = "Bad tezos address bad_address"
                  SignerAddress = signerPubKey.PubKey.GetBase58()
                  EventId = { BlockHash = "0xc279"; LogIndex = 10I } 
                  Payload =
                      { ERC721 = token
                        Owner = "owner"
                        TokenId = 1337I } }

            event
            |> should equal (Erc721MintingFailed expectedMint)
        | Error e -> failwith e
    }
