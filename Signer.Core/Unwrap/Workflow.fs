module Signer.Unwrap

open Signer.Ethereum.Multisig
open TzWatch.Domain

type EthereumAddress = EthereumAddress of string


[<RequireQualifiedAccess>]
module EthereumAddress =
    let value (EthereumAddress v) = v


type UnwrapWorkflow = bigint -> Update -> DomainResult<(EventId * DomainEvent)>

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

let private updateIdToString =
    function
    | InternalOperation ({OpgHash=hash;Counter=counter}, nonce) -> sprintf "%s/%i/%i" hash counter nonce
    | Operation { OpgHash = hash; Counter=counter } -> sprintf "%s/%i" hash counter

let workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) (append: _ Append): UnwrapWorkflow =

    fun level update ->
        asyncResult {
            let operationId = (updateIdToString update.UpdateId)
            let! s = toPayload operationId update.Value
            let! packed = pack s
            let! signature = signer.Sign packed

            let proof =
                { Amount = s.Amount
                  Owner = s.Destination
                  TokenId = s.TokenContract
                  OperationId = operationId
                  Signature = signature }

            let event =
                { Level = level
                  Proof = proof
                  Quorum = { LockingContract = lockingContract } }

            return! append (UnwrapSigned event)
        }
