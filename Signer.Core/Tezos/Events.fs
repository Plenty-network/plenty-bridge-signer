namespace Signer.Tezos

open Newtonsoft.Json.Linq
open Nichelson
open Nichelson.Contract
open TzWatch.Domain

type FungibleUnwrappedEventDto =
    { Amount: bigint
      Destination: string
      Fees: bigint
      Erc20: string }

type NftUnwrappedEventDto =
    { TokenId: bigint
      Destination: string
      Erc721: string }

[<RequireQualifiedAccess>]
module FungibleUnwrappedEventDto =

    let entryPointName = "unwrap_fungible"

    let private unwrapType =
        ContractParameters "(pair
              (pair (nat %amount) (bytes %destination))
              (pair (bytes %erc_20) (nat %fees)))"

    let fromJson (call: JToken) =
        let expr =
            Nichelson.Parser.Json.Expression.load call

        let amount: bigint = unwrapType.Extract("%amount", expr)
        let destination: byte array = unwrapType.Extract("%destination", expr)
        let fees: bigint = unwrapType.Extract("%fees", expr)
        let erc20: byte array = unwrapType.Extract("%erc_20", expr)

        { Amount = amount
          Destination = Encoder.byteToHex destination
          Fees = fees
          Erc20 = Encoder.byteToHex erc20 }

[<RequireQualifiedAccess>]
module NftUnwrappedEventDto =
    
    let private unwrapType = ContractParameters "(pair (pair (bytes %destination) (bytes %erc_721)) (nat %token_id))"
    let entryPointName = "unwrap_nft"
    
    let fromJson (call: JToken) =
        let expr =
            Nichelson.Parser.Json.Expression.load call

        let tokenId: bigint = unwrapType.Extract("%token_id", expr)
        let destination: byte array = unwrapType.Extract("%destination", expr)
        let erc721: byte array = unwrapType.Extract("%erc_721", expr)

        { TokenId = tokenId
          Destination = Encoder.byteToHex destination
          Erc721 = Encoder.byteToHex erc721 }

module Events =

    let subscription contract confirmations =
        { Contract = (ContractAddress.createUnsafe contract)
          Interests =
              [ EntryPoint FungibleUnwrappedEventDto.entryPointName
                EntryPoint NftUnwrappedEventDto.entryPointName ]
          Confirmations = confirmations }

    let (|FungibleUnwrapped|_|) =
        function
        | EntryPointCall { Entrypoint = "unwrap_fungible" ; Parameters = token } -> Some (FungibleUnwrappedEventDto.fromJson token)
        | _ -> None
        
    let (|NftUnwrapped|_|) =
        function
        | EntryPointCall { Entrypoint = "unwrap_nft" ; Parameters = token } -> Some (NftUnwrappedEventDto.fromJson token)
        | _ -> None