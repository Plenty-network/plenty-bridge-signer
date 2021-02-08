module Signer.Unwrap

open FsToolkit.ErrorHandling.Operator.AsyncResult
open Signer.Ethereum.Multisig
open Signer.Tezos
open TzWatch.Domain
open Signer.Tezos.Events

type EthereumAddress = EthereumAddress of string

type UnwrapWorkflow = bigint -> Update -> DomainResult<(EventId * DomainEvent)>

type private Erc20Workflow = bigint -> Erc20UnwrapParameters -> DomainResult<DomainEvent>

type private Erc721Workflow = bigint -> Erc721UnwrapParameters -> DomainResult<DomainEvent>

let private updateIdToString =
    function
    | InternalOperation ({ OpgHash = hash; Counter = counter }, nonce) -> sprintf "%s/%i/%i" hash counter nonce
    | Operation { OpgHash = hash; Counter = counter } -> sprintf "%s/%i" hash counter

let private route (erc20Workflow: Erc20Workflow)
                      (erc721Workflow: Erc721Workflow)
                      level
                      ({ Value = value; UpdateId = id })
                      =

    match value with
    | Erc20Unwrapped dto ->
        let p =
            { Amount = dto.Amount
              Owner = dto.Destination
              ERC20 = dto.Erc20
              OperationId = updateIdToString id }

        erc20Workflow level p
    | Erc721Unwrapped dto ->
        let p =
            { TokenId = dto.TokenId
              Owner = dto.Destination
              ERC721 = dto.Erc721
              OperationId = updateIdToString id }

        erc721Workflow level p
    | _ -> AsyncResult.ofError "Wrong update type"

let erc20Workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) level (p: Erc20UnwrapParameters) =
    asyncResult {
        let call = erc20TransferCall p
        let! r = pack lockingContract p.ERC20 p.OperationId call
        let! signature = signer.Sign(r)

        return
            Erc20UnwrapSigned
                { Level = level
                  Call =
                      { LockingContract = lockingContract
                        Signature = signature
                        Parameters = p } }
    }

let erc721Workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) level (p: Erc721UnwrapParameters) =
    asyncResult {
        let call = erc721SafeTransferCall lockingContract p
        let! hash = pack lockingContract p.ERC721 p.OperationId call
        let! signature = signer.Sign(hash)

        return
            Erc721UnwrapSigned
                { Level = level
                  Call =
                      { LockingContract = lockingContract
                        Signature = signature
                        Parameters = p } }
    }

let workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) (append: _ Append): UnwrapWorkflow =

    let erc20Workflow =
        erc20Workflow signer pack lockingContract

    let erc721Workflow =
        erc721Workflow signer pack lockingContract

    let append = AsyncResult.bind append
    let route = route erc20Workflow erc721Workflow

    fun level update -> route level update |> append
