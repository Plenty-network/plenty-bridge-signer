namespace Signer

type TezosSignature = Netezos.Keys.Signature

type DomainError = string

type DomainResult<'T> = AsyncResult<'T, DomainError>

type Signer = byte [] -> DomainResult<TezosSignature>

type FileDescriptor = {
    Name : string
    Content: string
}