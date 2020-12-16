module Signer.Core.Mint

open Nethereum.Contracts
open Nichelson
open Signer.Ethereum.Contract
open Signer.Tezos


let processEvent (signer: Signer) (target: MintTarget) (e: EventLog<TransferEventDto>): DomainResult<TezosSignature> =
    asyncResult {
        let mint =
            { Amount = e.Event.Value
              Owner = TezosAddress.FromString e.Event.TezosAddress
              TokenId = e.Log.Address
              TxId = e.Log.TransactionHash }

        let payload = Multisig.pack target mint
        return! signer payload
    }
