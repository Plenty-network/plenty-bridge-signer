module Signer.Unwrap

open System.Text
open Nichelson
open Signer.Ethereum.Multisig
open TzWatch.Domain

type EthereumAddress = EthereumAddress of string


[<RequireQualifiedAccess>]
module EthereumAddress =
    let value (EthereumAddress v) = v


type UnwrapWorkflow = Update -> DomainResult<(EventId * DomainEvent)>

let private toPayload hash value =
    match value with
    | EntryPointCall { Parameters = token } ->
        let dto = Signer.Tezos.Minter.unwrapValue token

        { TokenContract = dto.TokenId
          Destination = dto.Destination
          Amount = dto.Amount
          OperationId = hash }
        |> AsyncResult.ofSuccess
    | _ -> AsyncResult.ofError "Wrong update type"

let workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) (append: _ Append): UnwrapWorkflow =

    fun (update: Update) ->
        asyncResult {
            let! s = toPayload update.Hash update.Value
            let! packed = pack s
            let! signature = signer.Sign packed

            let proof =
                { Amount = s.Amount
                  Owner = s.Destination
                  TokenId = s.TokenContract
                  OperationId = update.Hash
                  Signature = signature }

            let event =
                { Level = bigint update.Level
                  Proof = proof
                  Quorum = { LockingContract = lockingContract } }

            return! append (UnwrapSigned event)
        }
