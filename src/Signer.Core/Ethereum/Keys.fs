[<RequireQualifiedAccess>]
module Signer.Ethereum.Keys

open Azure.Security.KeyVault.Keys
open Nethereum.Signer
open Signer.Core

let fromSpki k =
    let p = Secp256k1.publicKeyFromSpki k
    EthECKey(p, false)
    
let fromJsonWebKey (k:JsonWebKey) =
    let p = Secp256k1.publicKeyFromJsonWebKey k
    EthECKey(p, false)