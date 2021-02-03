namespace Signer

open Netezos.Keys
open Nichelson


type TezosSignature = Signature

type DomainError = string

type EventId = EventId of string

type DomainResult<'T> = AsyncResult<'T, DomainError>

type Signer = byte [] -> DomainResult<TezosSignature>

type TezosSigner =

    abstract PublicAddress: unit -> PubKey DomainResult

    abstract Sign: byte [] -> TezosSignature DomainResult

type EthereumSigner =

    abstract Sign: byte [] -> string DomainResult

    abstract PublicAddress: unit -> string DomainResult

type EthEventId = { BlockHash: string; LogIndex: bigint }

type Erc20MintingParameters =
    { Erc20: string
      Amount: bigint
      Owner: TezosAddress.T
      EventId: EthEventId }

type Erc721MintingParameters =
    { Erc721: string
      TokenId: bigint
      Owner: TezosAddress.T
      EventId: EthEventId }

type Quorum =
    { QuorumContract: TezosAddress.T
      MinterContract: TezosAddress.T
      ChainId: string }

type QuorumContractCall<'T> =
    { Quorum: Quorum
      Signature: string
      Parameters: 'T }

type ErcMint<'T> =
    { Level: bigint
      Call: QuorumContractCall<'T> }

type BurnProof =
    { Amount: bigint
      Owner: string
      TokenId: string
      OperationId: string
      Signature: string }

type UnwrapSigned =
    { Level: bigint
      Proof: BurnProof
      Quorum: EthQuorum }

and EthQuorum = { LockingContract: string }

type DomainEvent =
    | Erc20MintingSigned of ErcMint<Erc20MintingParameters>
    | Erc721MintingSigned of ErcMint<Erc721MintingParameters>
    | UnwrapSigned of UnwrapSigned

type Append<'e> = 'e -> DomainResult<EventId * 'e>
