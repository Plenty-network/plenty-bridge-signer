module Signer.Minting

open Nethereum.RPC.Eth.DTOs
open Nichelson
open Signer.Ethereum
open Signer.Ethereum.Contract
open Signer.Tezos

let erc20Params (dto: ERC20WrapAskedEventDto) (log: FilterLog) =
    TezosAddress.FromString dto.TezosAddress
    |> Result.map
        (fun v ->
            { Erc20 = dto.Token
              Amount = dto.Amount
              Owner = v
              EventId =
                  { BlockHash = log.BlockHash
                    LogIndex = log.LogIndex.Value } })

let erc721Params (dto: ERC721WrapAskedEventDto) (log: FilterLog) =
    TezosAddress.FromString dto.TezosAddress
    |> Result.map
        (fun v ->
            { TokenId = dto.TokenId
              Owner = v
              Erc721 = dto.Token
              EventId =
                  { BlockHash = log.BlockHash
                    LogIndex = log.LogIndex.Value } })




let erc20Workflow (signer: TezosSigner) (quorum: Quorum) (log: FilterLog) (dto: ERC20WrapAskedEventDto) =

    let signErc20Minting (parameters: Erc20MintingParameters) =
        asyncResult {
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

    let buildErc20MintingError =
        asyncResult {
            let! address =
                signer.PublicAddress()
                |> AsyncResult.map (fun v -> v.GetBase58())

            return
                Erc20MintingFailed
                    { Level = log.BlockNumber.Value
                      TransactionHash = log.TransactionHash
                      Reason = sprintf "Bad tezos address %s" dto.TezosAddress
                      EventId =
                          { BlockHash = log.BlockHash
                            LogIndex = log.LogIndex.Value }
                      SignerAddress = address
                      Payload =
                          { ERC20 = dto.Token
                            Owner = dto.Owner
                            Amount = dto.Amount } }
        }

    match erc20Params dto log with
    | Ok v -> signErc20Minting v
    | Error _ -> buildErc20MintingError


let erc721Workflow (signer: TezosSigner) (quorum: Quorum) (log: FilterLog) (dto: ERC721WrapAskedEventDto) =

    let signErc721Minting (parameters: Erc721MintingParameters) =
        asyncResult {
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


    let buildErc721MintingError =
        asyncResult {
            let! address =
                signer.PublicAddress()
                |> AsyncResult.map (fun v -> v.GetBase58())

            return
                Erc721MintingFailed
                    { Level = log.BlockNumber.Value
                      TransactionHash = log.TransactionHash
                      Reason = sprintf "Bad tezos address %s" dto.TezosAddress
                      SignerAddress = address
                      EventId =
                          { BlockHash = log.BlockHash
                            LogIndex = log.LogIndex.Value }
                      Payload =
                          { ERC721 = dto.Token
                            Owner = dto.Owner
                            TokenId = dto.TokenId } }
        }


    match erc721Params dto log with
    | Ok v -> signErc721Minting v
    | Error _ -> buildErc721MintingError



type MinterWorkflow = EthEventLog -> DomainResult<DomainEvent>


let workflow (signer: TezosSigner) (target: Quorum) : MinterWorkflow =
    let erc20Workflow = erc20Workflow signer target
    let erc721Workflow = erc721Workflow signer target

    fun { Event = event; Log = log } ->
        match event with
        | Erc20Wrapped dto -> erc20Workflow log dto
        | Erc721Wrapped dto -> erc721Workflow log dto
