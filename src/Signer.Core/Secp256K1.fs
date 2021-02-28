[<RequireQualifiedAccess>]
module Signer.Core.Secp256k1

open System
open Azure.Security.KeyVault.Keys
open Nethereum.Signer.Crypto
open Org.BouncyCastle.Asn1
open Org.BouncyCastle.Asn1.Sec
open Org.BouncyCastle.Asn1.X509

let publicKeyFromSpki (bytes: byte []) =
    let spki =
        SubjectPublicKeyInfo.GetInstance(Asn1Object.FromByteArray(bytes))

    let curve =
        SecNamedCurves
            .GetByOid(spki.AlgorithmID.Parameters :?> DerObjectIdentifier)
            .Curve

    let p =
        curve.DecodePoint(spki.PublicKeyData.GetBytes())

    p.GetEncoded(true)

let publicKeyFromJsonWebKey (key: JsonWebKey) =
    let xLen = key.X.Length
    let yLen = key.Y.Length
    let publicKey: byte array = Array.zeroCreate (1 + xLen + yLen)
    publicKey.[0] <- 0x04uy
    let offset = 1
    Buffer.BlockCopy(key.X, 0, publicKey, offset, xLen)
    let offset = offset + xLen
    Buffer.BlockCopy(key.Y, 0, publicKey, offset, yLen)

    ECKey
        .Secp256k1
        .Curve
        .DecodePoint(publicKey)
        .Normalize()
        .GetEncoded(true)
