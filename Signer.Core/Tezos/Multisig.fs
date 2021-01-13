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
    { QuorumContract: TezosAddress.T
      MinterContract: TezosAddress.T
      ChainId: string }

[<RequireQualifiedAccess>]
module Multisig =

    let private targetContractParameter = "(or %entry_point
                 (pair %add_token
                    (pair (pair (nat %decimals) (bytes %eth_contract))
                          (pair (string %eth_symbol) (string %name)))
                    (pair (string %symbol) (nat %token_id)))
                 (pair %mint_token
                    (pair (nat %amount) (address %owner))
                    (pair (bytes %token_id) (bytes %tx_id))))"

    let private signedParameters =
        sprintf "(pair (pair chain_id address) (pair %s address))" targetContractParameter

    let private paramsType = ContractParameters signedParameters

    let pack ({ QuorumContract = multisig
                MinterContract = benderContract
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
                             Record [ ("%mint_token", mint) ]
                             AddressArg benderContract ])

            Result.Ok(Encoder.pack value)
        with err -> Result.Error err.Message
