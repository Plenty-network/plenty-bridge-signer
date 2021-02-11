namespace Signer.Tezos

open Nichelson
open Nichelson.Contract
open Nichelson.Contract.Arg
open Signer



[<RequireQualifiedAccess>]
module Multisig =

    let private targetContractParameter = "(or %entry_point
                 (or (pair %add_fungible_token (bytes %eth_contract) (pair %token_address address nat))
                     (pair %add_nft (bytes %eth_contract) (address %token_contract)))
                 (or (pair %mint_fungible_token
                        (bytes %erc_20)
                        (pair (pair %event_id (bytes %block_hash) (nat %log_index))
                              (pair (address %owner) (nat %amount))))
                     (pair %mint_nft
                        (bytes %erc_721)
                        (pair (pair %event_id (bytes %block_hash) (nat %log_index))
                              (pair (address %owner) (nat %token_id))))))"

    let private signedParameters =
        sprintf "(pair (pair chain_id address) (pair %s address))" targetContractParameter

    let private paramsType = ContractParameters signedParameters

    let packMintErc20 ({ QuorumContract = multisig
                         MinterContract = benderContract
                         ChainId = chainId })
                      ({ Amount = amount
                         Owner = owner
                         Erc20 = erc20
                         EventId = { BlockHash = txId
                                     LogIndex = logIndex } })
                      =
        try
            let mint =
                Record [ ("%amount", IntArg(amount))
                         ("%owner", AddressArg owner)
                         ("%erc_20", StringArg erc20)
                         ("%event_id",
                          Record [ ("%block_hash", StringArg txId)
                                   ("%log_index", IntArg logIndex) ]) ]

            let value =
                paramsType.Instantiate
                    (Tuple [ StringArg chainId
                             AddressArg multisig
                             Record [ ("%mint_fungible_token", mint) ]
                             AddressArg benderContract ])

            Result.Ok(Encoder.pack value)
        with err -> Result.Error err.Message

    let packMintErc721 ({ QuorumContract = multisig
                          MinterContract = benderContract
                          ChainId = chainId })
                       ({ TokenId = tokenId
                          Owner = owner
                          Erc721 = erc721
                          EventId = { BlockHash = txId
                                      LogIndex = logIndex } })
                       =
        try
            let mint =
                Record [ ("%token_id", IntArg(tokenId))
                         ("%owner", AddressArg owner)
                         ("%erc_721", StringArg erc721)
                         ("%event_id",
                          Record [ ("%block_hash", StringArg txId)
                                   ("%log_index", IntArg logIndex) ]) ]

            let value =
                paramsType.Instantiate
                    (Tuple [ StringArg chainId
                             AddressArg multisig
                             Record [ ("%mint_nft", mint) ]
                             AddressArg benderContract ])

            Result.Ok(Encoder.pack value)
        with err -> Result.Error err.Message
