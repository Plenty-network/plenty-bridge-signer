namespace Signer.Ethereum

open Nethereum.RPC.Eth.DTOs
open Signer.Ethereum.Contract
open FSharp.Control
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open FSharpx.Control


type EthEvent =
    | Erc20Wrapped of ERC20WrapAskedEventDto
    | Erc721Wrapped of ERC721WrapAskedEventDto

type EthEventLog = { Log: FilterLog; Event: EthEvent }

module Watcher =

    type WatchParameters =
        { From: bigint
          Confirmations: int
          Contract: string }

    let watchFor (web3: Web3)
                 ({ From = from
                    Confirmations = confirmations
                    Contract = contract })
                 =
        let contract =
            web3.Eth.GetContract(lockingContractAbi, contract)


        let erc20Wrapped =
            contract.GetEvent<ERC20WrapAskedEventDto>()

        let erc721Wrapped =
            contract.GetEvent<ERC721WrapAskedEventDto>()

        let exec (event: _ Nethereum.Contracts.Event) (filter: NewFilterInput) (ctor: _ -> EthEvent) =
            event.GetAllChanges filter
            |> Async.AwaitTask
            |> Async.map (fun e ->
                e
                |> Seq.map (fun e -> { Log = e.Log; Event = ctor e.Event }))

        let rec loop (lastPolled: bigint) =
            asyncSeq {
                let next = lastPolled + 1I

                let! lastBlock =
                    web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                    |> Async.AwaitTask
                    |> Async.map (fun v -> v.Value)

                let maxBlock = lastBlock - (confirmations |> bigint)

                if maxBlock <= next then
                    do! Async.Sleep 5000
                    yield! loop lastPolled

                (* todo: it is possible to explode infura max response size here (10k). Should think of a better
                 solution *)
                let erc20Filter =
                    erc20Wrapped.CreateFilterInput
                        (BlockParameter(HexBigInteger(next)), BlockParameter(HexBigInteger(maxBlock)))

                let erc721Filter =
                    erc721Wrapped.CreateFilterInput
                        (BlockParameter(HexBigInteger(next)), BlockParameter(HexBigInteger(maxBlock)))

                let r =
                    exec erc20Wrapped erc20Filter Erc20Wrapped

                let r' =
                    exec erc721Wrapped erc721Filter Erc721Wrapped

                let! changes =
                    Async.Parallel [ r; r' ]
                    |> Async.map Seq.concat
                    |> Async.map (Seq.groupBy (fun e -> e.Log.BlockNumber))

                for change in changes do
                    yield change

                yield! loop (maxBlock)
            }

        loop from
