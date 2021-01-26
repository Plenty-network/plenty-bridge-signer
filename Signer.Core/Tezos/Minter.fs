namespace Signer.Tezos

open Newtonsoft.Json.Linq
open Nichelson
open Nichelson.Contract
open TzWatch.Domain

type UnwrappedEventDto =
    { Amount: bigint
      Destination: string
      Fees: bigint
      TokenId: string }

[<RequireQualifiedAccess>]
module Minter =
    let private unwrapType =
        ContractParameters "(pair (pair (nat %amount) (bytes %destination))
                              (pair (nat %fees) (bytes %token_id)))"

    let unwrapValue (call: JToken) =
        let expr =
            Nichelson.Parser.Json.Expression.load call

        let amount: bigint = unwrapType.Extract("%amount", expr)
        let destination: byte array = unwrapType.Extract("%destination", expr)
        let fees: bigint = unwrapType.Extract("%fees", expr)
        let tokenId: byte array = unwrapType.Extract("%token_id", expr)

        { Amount = amount
          Destination = Encoder.byteToHex destination
          Fees = fees
          TokenId = Encoder.byteToHex tokenId }
