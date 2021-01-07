[<RequireQualifiedAccess>]
module Signer.Tezos.Crypto

open Netezos.Keys
open Nethereum.Signer.Crypto
open Org.BouncyCastle.Asn1
open Org.BouncyCastle.Asn1.Sec
open Org.BouncyCastle.Asn1.X509
open Signer


module Secp256k1 = 

    let keyFromSpki (bytes: byte []) =
        let spki =
            SubjectPublicKeyInfo.GetInstance(Asn1Object.FromByteArray(bytes))

        let curve =
            SecNamedCurves
                .GetByOid(spki.AlgorithmID.Parameters :?> DerObjectIdentifier)
                .Curve

        let p =
            curve.DecodePoint(spki.PublicKeyData.GetBytes())

        PubKey.FromBytes(p.GetEncoded(true), ECKind.Secp256k1)

    let  signatureFromDer (der: byte []) =
        let parsed = ECDSASignature.FromDER(der).MakeCanonical()
        let result = Array.zeroCreate<byte> (64)
        let rBytes = parsed.R.ToByteArrayUnsigned()
        let sBytes = parsed.S.ToByteArrayUnsigned()
        rBytes.CopyTo(result, 32 - rBytes.Length)
        sBytes.CopyTo(result, 64 - sBytes.Length)
        TezosSignature(result, [| 13uy; 115uy; 101uy; 19uy; 63uy |])