namespace Signer

open Netezos.Keys


type TezosSignature = Signature

type DomainError = string

type EventId = EventId of string

type DomainResult<'T> = AsyncResult<'T, DomainError>

type Signer = byte [] -> DomainResult<TezosSignature>

type TezosSigner =

    abstract PublicAddress: unit -> PubKey DomainResult

    abstract Sign: byte [] -> TezosSignature DomainResult


type PressProof =
    { Amount: bigint
      Owner: string
      TokenId: string
      TxId: string
      Signature: string }

type MintingSigned =
    { Level: bigint
      Proof: PressProof
      Quorum: Quorum }

and Quorum =
    { QuorumContract: string
      MinterContract: string
      ChainId: string }

type UnwrapSigned =
    { Level: bigint
      Proof: PressProof
      Quorum: EthQuorum }

and EthQuorum = { LockingContract: string }

type DomainEvent =
    | MintingSigned of MintingSigned
    | UnwrapSigned of UnwrapSigned

type Append<'e> = 'e -> DomainResult<EventId * 'e>
