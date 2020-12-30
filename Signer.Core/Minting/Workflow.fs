module Signer.Minting

open Nethereum.Contracts
open Nichelson
open Signer.Ethereum.Contract
open Signer.Tezos


let private toMintingParameters (e: EventLog<TransferEventDto>): MintingParameters =
    { Amount = e.Event.Value
      Owner = TezosAddress.FromString e.Event.TezosAddress
      TokenId = e.Log.Address
      TxId = e.Log.TransactionHash }

let private packAndSign (signer: Signer) (target: MintingTarget) (mint: MintingParameters) =
    asyncResult {
        let! payload = Multisig.pack target mint |> AsyncResult.ofResult
        let! signature = signer payload
        return (mint, signature)
    }

let private toPayload level (target: MintingTarget) (parameters: MintingParameters, signature: TezosSignature) =
    { Level = level
      Proof =
          { Signature = signature.ToBase58()
            Amount = parameters.Amount
            Owner = TezosAddress.Value(parameters.Owner)
            TokenId = parameters.TokenId
            TxId = parameters.TxId }
      Quorum =
          { QuorumContract = TezosAddress.Value(target.MultisigContract)
            MinterContract = TezosAddress.Value(target.BenderContract)
            ChainId = target.ChainId } }
    |> AsyncResult.ofSuccess

let private toEvent (payload: MintingSigned) =
    MintingSigned payload |> AsyncResult.ofSuccess

let workflow (signer: Signer) (target: MintingTarget) (event: EventLog<TransferEventDto>) =
    let packAndSign = packAndSign signer target

    let toPayload =
        toPayload event.Log.BlockNumber.Value target
        |> AsyncResult.bind

    let toEvent = toEvent |> AsyncResult.bind

    event
    |> toMintingParameters
    |> packAndSign
    |> toPayload
    |> toEvent
