module Signer.``Tezos crypto utils test``

open System.IO
open System.Text
open Xunit
open FsUnit.Xunit
open Signer.Tezos

let getKey =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)
        return Crypto.Secp256k1.keyFromSpki bytes
    }

[<Fact>]
let ``Should import from der x509`` () =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)

        let key = Crypto.Secp256k1.keyFromSpki bytes

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

        let signature = Crypto.Secp256k1.signatureFromDer der

        key.Verify(payload, signature.ToBytes())
        |> should equal true

        key.Verify("J'aime la soupe", signature.ToBase58())
        |> should equal true
    }
