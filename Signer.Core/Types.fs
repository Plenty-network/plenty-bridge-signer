namespace Signer.Core

open Nichelson
type TezosSignature = Netezos.Keys.Signature

type DomainError = string

type DomainResult<'T> = AsyncResult<'T, DomainError>

type Signer = byte [] -> DomainResult<TezosSignature>
