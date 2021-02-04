[<RequireQualifiedAccess>]
module Signer.Ethereum.Crypto

open Nethereum.Signer
open Nichelson
open Signer

let memorySigner (key: EthECKey) =
    let messageSigner = EthereumMessageSigner()
    { new EthereumSigner with

        member this.Sign(p) =
            messageSigner.Sign(p, key)
            |> AsyncResult.ofSuccess

        member this.PublicAddress() =
            key.GetPubKey()
            |> Encoder.byteToHex
            |> AsyncResult.ofSuccess }
