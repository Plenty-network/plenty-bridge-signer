namespace Signer.Tezos

open Nichelson
open Nichelson.Contract
open Nichelson.Contract.Arg

type MintingParameters =
    { Amount: bigint
      Owner: TezosAddress.T
      TokenId: string
      BlockHash: string
      LogIndex: bigint }

type MintingTarget =
    { QuorumContract: TezosAddress.T
      MinterContract: TezosAddress.T
      ChainId: string }

[<RequireQualifiedAccess>]
module Multisig =

    // todo: deals with real add_token params
    let private targetContractParameter = "(or %entry_point
                 (pair %add_token
                    unit unit)
                 (pair %mint_token
                    (pair (nat %amount) (pair %event_id (bytes %block_hash) (nat %log_index)))
                    (pair (address %owner) (bytes %token_id))))"

    let private signedParameters =
        sprintf "(pair (pair chain_id address) (pair %s address))" targetContractParameter

    let private paramsType = ContractParameters signedParameters

    let pack ({ QuorumContract = multisig
                MinterContract = benderContract
                ChainId = chainId })
             ({ Amount = amount
                Owner = owner
                TokenId = tokenId
                BlockHash = txId
                LogIndex = logIndex })
             =
        try
            let mint =
                Record [ ("%amount", IntArg(amount))
                         ("%owner", AddressArg owner)
                         ("%token_id", StringArg tokenId)
                         ("%event_id",
                          Record [ ("%block_hash", StringArg txId)
                                   ("%log_index", IntArg logIndex) ]) ]

            let value =
                paramsType.Instantiate
                    (Tuple [ StringArg chainId
                             AddressArg multisig
                             Record [ ("%mint_token", mint) ]
                             AddressArg benderContract ])

            Result.Ok(Encoder.pack value)
        with err -> Result.Error err.Message
