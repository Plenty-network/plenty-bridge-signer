[<RequireQualifiedAccess>]
module Signer.Ethereum.Signer

open Nethereum.Signer
open Signer

let memorySigner (key: EthECKey) =
    let messageSigner = EthereumMessageSigner()

    { new EthereumSigner with

        member this.Sign(p) =
            messageSigner.Sign(p, key)
            |> AsyncResult.ofSuccess }
