[<RequireQualifiedAccess>]
module Signer.Ethereum.Signer

open Nethereum.Signer

let sign (key: EthECKey) payload =
    let signer = EthereumMessageSigner()
    signer.Sign(payload, key)
