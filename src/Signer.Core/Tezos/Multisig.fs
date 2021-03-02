namespace Signer.Tezos

open Nichelson
open Nichelson.Contract
open Nichelson.Contract.Arg
open Signer



[<RequireQualifiedAccess>]
module Multisig =

    let private minterEntrypoints = "(or %entrypoint
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

    let private minterPayload =
        sprintf "(pair (pair chain_id address) (pair %s address))" minterEntrypoints

    let private minterPayloadType = ContractParameters minterPayload

    let private paymentAddressPayloadType =
        ContractParameters "(pair (pair chain_id address) (pair nat (pair address address)))"

    let packMintErc20
        ({ QuorumContract = multisig
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
                minterPayloadType.Instantiate(
                    Tuple [ StringArg chainId
                            AddressArg multisig
                            Record [ ("%mint_fungible_token", mint) ]
                            AddressArg benderContract ]
                )

            Ok(Encoder.pack value)
        with err -> Error err.Message

    let packMintErc721
        ({ QuorumContract = multisig
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
                minterPayloadType.Instantiate(
                    Tuple [ StringArg chainId
                            AddressArg multisig
                            Record [ ("%mint_nft", mint) ]
                            AddressArg benderContract ]
                )

            Ok(Encoder.pack value)
        with err -> Error err.Message

    let packChangePaymentAddress
        { QuorumContract = multisig
          MinterContract = benderContract
          ChainId = chainId }
        ({ Address = address; Counter = counter })
        =
        try 
            let payload = Tuple [StringArg chainId ; AddressArg multisig ; IntArg (bigint counter) ; AddressArg benderContract ; AddressArg address]
            let value = paymentAddressPayloadType.Instantiate payload
            Ok (Encoder.pack value)
        with err -> Error err.Message
