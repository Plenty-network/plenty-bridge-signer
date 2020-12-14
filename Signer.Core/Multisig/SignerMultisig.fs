module Signer.Multisig

open Netezos.Ledger
open Nichelson
open Nichelson.Contract
open Nichelson.Contract.Arg
open Netezos.Keys

type MintParametes =
    { Amount: int64
      Owner: TezosAddress.T
      TokenId: string
      TxId: string }

type MintTarget =
    { MultisigContract: TezosAddress.T
      BenderContract: TezosAddress.T
      ChainId: string }

let private targetContractParameter = "(or
                         (unit %add_token)
                         (pair %mint_token
                            (pair (nat %amount) (address %owner))
                            (pair (string %token_id) (string %tx_id))))"


let private multisigAction =
    sprintf "(or \
               (unit %%change_keys) \
               (pair %%signer_operation \
                  %s
                  (address %%target)))" targetContractParameter

let private signedParameters =
    sprintf "(pair (pair chain_id address) (pair nat %s))" multisigAction

let private paramsType = ContractParameters signedParameters

let pack ({ MultisigContract = multisig
            BenderContract = benderContract
            ChainId = chainId })
         ({ Amount = amount
            Owner = owner
            TokenId = tokenId
            TxId = txId })
         =
    let mint =
        Record [ ("%amount", IntArg amount)
                 ("%owner", AddressArg owner)
                 ("%token_id", StringArg tokenId)
                 ("%tx_id", StringArg txId) ]

    let value =
        paramsType.Instantiate
            (Tuple [ StringArg chainId
                     AddressArg multisig
                     IntArg 0L
                     Record [ ("%signer_operation",
                               Record [ ("%mint_token", mint)
                                        ("%target", AddressArg benderContract) ]) ] ])

    Encoder.pack value

let sign (payload:byte[]) =
    let words = [ ]
    let key = Key.FromMnemonic(Mnemonic.Parse(words), "vespwozi.vztxobwc@tezos.example.org", "")
    key.Sign(payload)
    
let ledgerKey =
    async {
        let! ledger = Ledger.Client.get() |> Async.AwaitTask
        return! ledger.GetPublicKeyAsync(ECKind.NistP256) |> Async.AwaitTask
        // return! ledger.GetPublicKeyAsync() |> Async.AwaitTask
    }