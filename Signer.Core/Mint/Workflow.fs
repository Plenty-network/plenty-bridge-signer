module Signer.Mint

open Nethereum.Contracts
open Nethereum.RPC.Eth.DTOs
open Nichelson
open Signer.Ethereum.Contract
open Signer.Tezos


type BAT =
    { Signature: string
      Mint: Mint
      Quorum: Quorum }

and Mint =
    { Amount: bigint
      Owner: string
      TokenId: string
      TxId: string }

and Quorum =
    { QuorumContract: string
      MinterContract: string
      ChainId: string }

let toMintParameters (e: EventLog<TransferEventDto>): MintParameters =
    { Amount = e.Event.Value
      Owner = TezosAddress.FromString e.Event.TezosAddress
      TokenId = e.Log.Address
      TxId = e.Log.TransactionHash }

let packAndSign (signer: Signer) (target: MintTarget) (mint: MintParameters) =
    asyncResult {
        let! payload = Multisig.pack target mint |> AsyncResult.ofResult
        let! signature = signer payload
        return (mint, signature)
    }

let toBat (target: MintTarget) (parameters: MintParameters, signature: TezosSignature) =
    { Signature = signature.ToBase58()
      Mint =
          { Amount = parameters.Amount
            Owner = TezosAddress.Value(parameters.Owner)
            TokenId = parameters.TokenId
            TxId = parameters.TxId }
      Quorum =
          { QuorumContract = TezosAddress.Value(target.MultisigContract)
            MinterContract = TezosAddress.Value(target.BenderContract)
            ChainId = target.ChainId } }
    |> AsyncResult.ofSuccess

let toJson bat =
    Json.serialize bat |> AsyncResult.ofSuccess

let toFileDescriptor (log: FilterLog) value =
    { Name = sprintf "%s.json" (log.TransactionHash)
      Content = value }
    |> AsyncResult.ofSuccess

let workflow (signer: Signer) (target: MintTarget) (e: EventLog<TransferEventDto>) =
    let packAndSign = packAndSign signer target
    let toBat = toBat target |> AsyncResult.bind
    let toJson = toJson |> AsyncResult.bind

    let toFileDescriptor =
        toFileDescriptor e.Log |> AsyncResult.bind

    e
    |> toMintParameters
    |> packAndSign
    |> toBat
    |> toJson
    |> toFileDescriptor
