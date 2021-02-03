module Signer.Minting

open Nethereum.RPC.Eth.DTOs
open Signer.Ethereum
open Signer.Ethereum.Contract
open Signer.Tezos

type ParamFunction<'a, 'b> = 'a -> FilterLog -> 'b

let erc20Params (dto: ERC20WrapAskedEventDto) (log: FilterLog) =
    { Erc20 = dto.Token
      Amount = dto.Amount
      Owner = dto.TezosAddress
      EventId =
          { BlockHash = log.BlockHash
            LogIndex = log.LogIndex.Value } }

let erc721Params (dto: ERC721WrapAskedEventDto) (log: FilterLog) =
    { TokenId = dto.TokenId
      Owner = dto.TezosAddress
      Erc721 = dto.Token
      EventId =
          { BlockHash = log.BlockHash
            LogIndex = log.LogIndex.Value } }


let erc20Workflow (signer: Signer) (quorum: Quorum) (log: FilterLog) (dto: ERC20WrapAskedEventDto) =
    asyncResult {
        let parameters = erc20Params dto log

        let! packed =
            Multisig.packMintErc20 quorum parameters
            |> AsyncResult.ofResult

        let! signature = signer packed

        return
            Erc20MintingSigned
                { Level = log.BlockNumber.Value
                  Call =
                      { Quorum = quorum
                        Signature = signature.ToBase58()
                        Parameters = parameters } }
    }

let erc721Workflow (signer: Signer) (quorum: Quorum) (log: FilterLog) (dto: ERC721WrapAskedEventDto) =
    asyncResult {
        let parameters = erc721Params dto log

        let! packed =
            Multisig.packMintErc721 quorum parameters
            |> AsyncResult.ofResult

        let! signature = signer packed

        return
            Erc721MintingSigned
                { Level = log.BlockNumber.Value
                  Call =
                      { Quorum = quorum
                        Signature = signature.ToBase58()
                        Parameters = parameters } }
    }



type MinterWorkflow = EthEventLog -> DomainResult<(EventId * DomainEvent)>


let workflow (signer: Signer) (append: _ Append) (target: Quorum): MinterWorkflow =
    let erc20Workflow = erc20Workflow signer target
    let erc721Workflow = erc721Workflow signer target
    let append = AsyncResult.bind append

    fun logEvent ->
        match logEvent.Event with
        | Erc20Wrapped dto -> erc20Workflow logEvent.Log dto
        | Erc721Wrapped dto -> erc721Workflow logEvent.Log dto
        |> append
