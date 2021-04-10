module Signer.Minting

open Nethereum.RPC.Eth.DTOs
open Nichelson
open Signer.Ethereum
open Signer.Ethereum.Contract
open Signer.Tezos

let erc20Params (dto: ERC20WrapAskedEventDto) (log: FilterLog) =
    TezosAddress.FromString dto.TezosAddress
    |> Result.map (fun v ->
        { Erc20 = dto.Token
          Amount = dto.Amount
          Owner = v
          EventId =
              { BlockHash = log.BlockHash
                LogIndex = log.LogIndex.Value } })

let erc721Params (dto: ERC721WrapAskedEventDto) (log: FilterLog) =
    TezosAddress.FromString dto.TezosAddress
    |> Result.map (fun v ->
        { TokenId = dto.TokenId
          Owner = v
          Erc721 = dto.Token
          EventId =
              { BlockHash = log.BlockHash
                LogIndex = log.LogIndex.Value } })


let erc20Workflow (signer: TezosSigner) (quorum: Quorum) (log: FilterLog) (dto: ERC20WrapAskedEventDto) =
    asyncResult {
        let! parameters = erc20Params dto log |> AsyncResult.ofResult

        let! packed =
            Multisig.packMintErc20 quorum parameters
            |> AsyncResult.ofResult

        let! signature = signer.Sign packed

        let! address =
            signer.PublicAddress()
            |> AsyncResult.map (fun v -> v.GetBase58())

        return
            Erc20MintingSigned
                { Level = log.BlockNumber.Value
                  TransactionHash = log.TransactionHash
                  Call =
                      { Quorum = quorum
                        Signature = signature.ToBase58()
                        SignerAddress = address
                        Parameters = parameters } }
    }

let erc721Workflow (signer: TezosSigner) (quorum: Quorum) (log: FilterLog) (dto: ERC721WrapAskedEventDto) =
    asyncResult {
        let! parameters = erc721Params dto log |> AsyncResult.ofResult

        let! packed =
            Multisig.packMintErc721 quorum parameters
            |> AsyncResult.ofResult

        let! address =
            signer.PublicAddress()
            |> AsyncResult.map (fun v -> v.GetBase58())

        let! signature = signer.Sign packed

        return
            Erc721MintingSigned
                { Level = log.BlockNumber.Value
                  TransactionHash = log.TransactionHash
                  Call =
                      { Quorum = quorum
                        SignerAddress = address
                        Signature = signature.ToBase58()
                        Parameters = parameters } }
    }



type MinterWorkflow = EthEventLog -> DomainResult<(EventId * DomainEvent)>


let workflow (signer: TezosSigner) (append: _ Append) (target: Quorum): MinterWorkflow =
    let erc20Workflow = erc20Workflow signer target
    let erc721Workflow = erc721Workflow signer target
    let append = AsyncResult.bind append

    fun logEvent ->
        match logEvent.Event with
        | Erc20Wrapped dto -> erc20Workflow logEvent.Log dto
        | Erc721Wrapped dto -> erc721Workflow logEvent.Log dto
        |> append
