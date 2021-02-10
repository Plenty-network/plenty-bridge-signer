[<RequireQualifiedAccess>]
module Signer.Ethereum.Keys

open Nethereum.Signer
open Signer.Core

let fromSpki k =
    let p = Secp256k1.publicKeyFromSpki k
    EthECKey(p, false) 