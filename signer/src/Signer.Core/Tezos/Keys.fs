[<RequireQualifiedAccess>]
module Signer.Tezos.Keys

open Netezos.Keys
open Nethereum.Signer.Crypto
open Signer
open Signer.Core


module Secp256k1 =
    let keyFromSpki (bytes: byte []) =
        let p = Secp256k1.publicKeyFromSpki bytes
        PubKey.FromBytes(p, ECKind.Secp256k1)

    let signatureFromDer (der: byte []) =
        let parsed =
            ECDSASignature.FromDER(der).MakeCanonical()

        let result = Array.zeroCreate<byte> (64)
        let rBytes = parsed.R.ToByteArrayUnsigned()
        let sBytes = parsed.S.ToByteArrayUnsigned()
        rBytes.CopyTo(result, 32 - rBytes.Length)
        sBytes.CopyTo(result, 64 - sBytes.Length)
        TezosSignature(result, [| 13uy; 115uy; 101uy; 19uy; 63uy |])
