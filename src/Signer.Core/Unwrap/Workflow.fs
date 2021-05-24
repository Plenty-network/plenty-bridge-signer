module Signer.Unwrap

open Signer
open FsToolkit.ErrorHandling.Operator.AsyncResult
open Signer.Ethereum.Multisig
open Signer.Tezos
open TzWatch.Domain
open Signer.Tezos.Events

type EthereumAddress = EthereumAddress of string

type UnwrapCommand =
    | UnwrapFromTezosUpdate of Update
    | UnwrapErc20FromWrappingError of ErcMintError<Erc20MintingError>
    | UnwrapErc721FromWrappingError of ErcMintError<Erc721MintingError>

type UnwrapWorkflow = bigint -> UnwrapCommand -> DomainResult<DomainEvent>

type private Erc20Workflow = bigint -> ObservedFact -> Erc20UnwrapParameters -> DomainResult<DomainEvent>

type private Erc721Workflow = bigint -> ObservedFact -> Erc721UnwrapParameters -> DomainResult<DomainEvent>

let private updateIdToString =
    function
    | InternalOperation ({ OpgHash = hash; Counter = counter }, nonce) -> $"%s{hash}/%i{counter}/%i{nonce}"
    | Operation { OpgHash = hash; Counter = counter } -> $"%s{hash}/%i{counter}"

let private route (erc20Workflow: Erc20Workflow) (erc721Workflow: Erc721Workflow) level command =
    match command with
    | UnwrapFromTezosUpdate { Value = value; UpdateId = id } ->

        match value with
        | Erc20Unwrapped dto ->
            let p =
                { Amount = dto.Amount
                  Owner = dto.Destination
                  ERC20 = dto.Erc20
                  OperationId = updateIdToString id }

            erc20Workflow level Burn p
        | Erc721Unwrapped dto ->
            let p =
                { TokenId = dto.TokenId
                  Owner = dto.Destination
                  ERC721 = dto.Erc721
                  OperationId = updateIdToString id }

            erc721Workflow level Burn p
        | _ -> AsyncResult.ofError "Wrong update type"
    | UnwrapErc20FromWrappingError ({ Level = level
                                      Payload = payload
                                      EventId = eventId }) ->
        let p =
            { Amount = payload.Amount
              Owner = payload.Owner
              ERC20 = payload.ERC20
              OperationId = $"revert:%s{eventId.BlockHash}:{eventId.LogIndex}" }

        erc20Workflow level MintingError p
    | UnwrapErc721FromWrappingError ({ Level = level
                                       Payload = payload
                                       EventId = eventId }) ->

        let p =
            { TokenId = payload.TokenId
              Owner = payload.Owner
              ERC721 = payload.ERC721
              OperationId = $"revert:%s{eventId.BlockHash}:{eventId.LogIndex}" }

        erc721Workflow level MintingError p

let erc20Workflow
    (signer: EthereumSigner)
    (pack: EthPack)
    (lockingContract: string)
    level
    fact
    (p: Erc20UnwrapParameters)
    =
    asyncResult {
        let call = erc20TransferCall p

        let! r =
            pack
                { LockingContract = lockingContract
                  ErcContract = p.ERC20
                  OperationId = p.OperationId
                  Data = call }

        let! signature = signer.Sign(r)
        let! address = signer.PublicAddress()

        return
            Erc20UnwrapSigned
                { Level = level
                  ObservedFact = fact
                  Call =
                      { LockingContract = lockingContract
                        Signature = signature
                        SignerAddress = address
                        Parameters = p } }
    }

let erc721Workflow
    (signer: EthereumSigner)
    (pack: EthPack)
    (lockingContract: string)
    level
    fact
    (p: Erc721UnwrapParameters)
    =
    asyncResult {
        let call = erc721SafeTransferCall lockingContract p

        let! hash =
            pack
                { LockingContract = lockingContract
                  ErcContract = p.ERC721
                  OperationId = p.OperationId
                  Data = call }

        let! signature = signer.Sign(hash)
        let! address = signer.PublicAddress()

        return
            Erc721UnwrapSigned
                { Level = level
                  ObservedFact = fact
                  Call =
                      { LockingContract = lockingContract
                        Signature = signature
                        SignerAddress = address
                        Parameters = p } }
    }

let workflow (signer: EthereumSigner) (pack: EthPack) (lockingContract: string) : UnwrapWorkflow =

    let erc20Workflow =
        erc20Workflow signer pack lockingContract

    let erc721Workflow =
        erc721Workflow signer pack lockingContract

    let route = route erc20Workflow erc721Workflow

    route
