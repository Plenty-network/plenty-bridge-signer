namespace Signer.Tezos

open Newtonsoft.Json.Linq
open Nichelson
open Nichelson.Contract
open TzWatch.Domain

type ERC20UnwrappedEventDto =
    { Amount: bigint
      Destination: string
      Fees: bigint
      Erc20: string }

type ERC721UnwrappedEventDto =
    { TokenId: bigint
      Destination: string
      Erc721: string }

[<RequireQualifiedAccess>]
module ERC20UnwrappedEventDto =

    let entryPointName = "unwrap_erc20"

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
module ERC721UnwrappedEventDto =

    let private unwrapType =
        ContractParameters "(pair (pair (bytes %destination) (bytes %erc_721)) (nat %token_id))"

    let entryPointName = "unwrap_erc721"

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
              [ EntryPoint ERC20UnwrappedEventDto.entryPointName
                EntryPoint ERC721UnwrappedEventDto.entryPointName ]
          Confirmations = confirmations }

    let (|Erc20Unwrapped|_|) =
        function
        | EntryPointCall { Entrypoint = e; Parameters = token } when e = ERC20UnwrappedEventDto.entryPointName ->
            Some(ERC20UnwrappedEventDto.fromJson token)
        | _ -> None

    let (|Erc721Unwrapped|_|) =
        function
        | EntryPointCall { Entrypoint = e; Parameters = token } when e = ERC721UnwrappedEventDto.entryPointName ->
            Some(ERC721UnwrappedEventDto.fromJson token)
        | _ -> None
