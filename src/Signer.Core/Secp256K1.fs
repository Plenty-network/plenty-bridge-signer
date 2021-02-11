[<RequireQualifiedAccess>]
module Signer.Core.Secp256k1

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
