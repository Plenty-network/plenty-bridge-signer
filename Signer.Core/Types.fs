namespace Signer

open System

type TezosSignature = Netezos.Keys.Signature

type DomainError = string

type EventId = EventId of uint64

type DomainResult<'T> = AsyncResult<'T, DomainError>

type Signer = byte [] -> DomainResult<TezosSignature>

type MintingSigned =
    { Level: bigint
      Proof: PressProof
      Quorum: Quorum }

and PressProof =
    { Amount: bigint
      Owner: string
      TokenId: string
      TxId: string
      Signature: string }

and Quorum =
    { QuorumContract: string
      MinterContract: string
      ChainId: string }

type DomainEvent = MintingSigned of MintingSigned

type Append<'e> = 'e -> Async<EventId * 'e>