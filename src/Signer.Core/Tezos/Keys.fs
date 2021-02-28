[<RequireQualifiedAccess>]
module Signer.Tezos.Keys

open Azure.Security.KeyVault.Keys
open Netezos.Keys
open Nethereum.Signer
open Nethereum.Signer.Crypto
open Signer
open Signer.Core

module Secp256k1 =
    let keyFromSpki (bytes: byte []) =
        let p = Secp256k1.publicKeyFromSpki bytes
        PubKey.FromBytes(p, ECKind.Secp256k1)

    let keyFromJsonWebKey (key: JsonWebKey) =
        let p = Secp256k1.publicKeyFromJsonWebKey key
        PubKey.FromBytes(p, ECKind.Secp256k1)

    let private toTezosSignature (parsed: ECDSASignature) =
        let result = Array.zeroCreate<byte> (64)
        let rBytes = parsed.R.ToByteArrayUnsigned()
        let sBytes = parsed.S.ToByteArrayUnsigned()
        rBytes.CopyTo(result, 32 - rBytes.Length)
        sBytes.CopyTo(result, 64 - sBytes.Length)
        TezosSignature(result, [| 13uy; 115uy; 101uy; 19uy; 63uy |])

    let signatureFromDer (der: byte []) =
        ECDSASignature.FromDER(der).MakeCanonical()
        |> toTezosSignature

    let signatureFromComponents (c: byte array) =
        ECDSASignatureFactory
            .FromComponents(c)
            .MakeCanonical()
        |> toTezosSignature
