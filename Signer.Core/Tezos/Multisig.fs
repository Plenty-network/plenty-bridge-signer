namespace Signer.Tezos

open FParsec
open Nichelson
open Nichelson.Contract
open Nichelson.Contract.Arg

type MintingParameters =
    { Amount: bigint
      Owner: TezosAddress.T
      TokenId: string
      TxId: string }

type MintingTarget =
    { MultisigContract: TezosAddress.T
      BenderContract: TezosAddress.T
      ChainId: string }

[<RequireQualifiedAccess>]
module Multisig =

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
        try
            let mint =
                Record [ ("%amount", IntArg(amount))
                         ("%owner", AddressArg owner)
                         ("%token_id", StringArg tokenId)
                         ("%tx_id", StringArg txId) ]

            let value =
                paramsType.Instantiate
                    (Tuple [ StringArg chainId
                             AddressArg multisig
                             IntArg 0I
                             Record [ ("%signer_operation",
                                       Record [ ("%mint_token", mint)
                                                ("%target", AddressArg benderContract) ]) ] ])

            Result.Ok(Encoder.pack value)
        with err -> Result.Error err.Message
