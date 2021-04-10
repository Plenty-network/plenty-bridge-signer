module Signer.``Tezos crypto utils test``

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Azure.Identity
open Azure.Security.KeyVault.Keys
open Netezos.Keys
open Xunit
open FsUnit.Xunit
open Signer.Tezos

let getKey =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)
        return Keys.Secp256k1.keyFromSpki bytes
    }

[<Fact>]
let ``Should import public key from der x509`` () =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)

        let key = Keys.Secp256k1.keyFromSpki bytes

        key.Address
        |> should equal "tz2KbPyXNP3pqod2RX5Tdk4vq7BqYuMxjsbc"

        key.GetBase58()
        |> should equal "sppk7ZiakigJcQL9nWJbCbChDaumBbBcNntPWHV6P2gPxFD8Ugc1NYQ"
    }

[<Fact>]
let ``Should import signature from DER`` () =
    async {
        let! key = getKey

        let payload =
            Encoding.UTF8.GetBytes("J'aime la soupe")

        use sigFile = File.OpenRead("./sample/signature.der")
        let! der = sigFile.AsyncRead(int sigFile.Length)

        let signature = Keys.Secp256k1.signatureFromDer der

        key.Verify(payload, signature.ToBytes())
        |> should equal true

        key.Verify("J'aime la soupe", signature.ToBase58())
        |> should equal true
    }


[<Fact(Skip="Requires azure credentials")>]
let ``Should extract tezos address from azure`` () =
    async {
        let client =
            Crypto.azureSigner
                (DefaultAzureCredential())
                (Uri("https://benderlabs-staging-wsig.vault.azure.net/"))
                "tezos-key"

        let! address = client.PublicAddress()

        match address with
        | Ok v ->
            v.GetBase58()
            |> should equal "sppk7ZZjyc5cHXiHjus7cWAemTNCmTkoZEJqn9UmojFLUoDLfSscfEz"
        | Error err -> failwith err
    }


[<Fact(Skip="Requires azure credentials")>]
let ``Should sign using azure`` () =
    async {
        let signer =
            Crypto.azureSigner
                (DefaultAzureCredential())
                (Uri("https://benderlabs-staging-wsig.vault.azure.net/"))
                "tezos-key"


        let! signature = signer.Sign(Encoding.UTF8.GetBytes("Coucou"))

        match signature with
        | Ok v ->
            v
            |> should
                equal
                   "0x9fa1a5f02cd8652ad6dd6a8c11105eb958f7d29baf324276945087ccfa31fece16528416f9363546949a74780c2a3c080a9e16090663e324ec2a4bb162beba781b"
        | Result.Error err -> failwith err
    }
