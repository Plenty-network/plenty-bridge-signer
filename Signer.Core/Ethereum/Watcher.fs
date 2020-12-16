namespace Signer.Ethereum

open Nethereum.RPC.Eth.DTOs
open Signer.Ethereum.Contract
open FSharp.Control
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open FSharpx.Control


module Watcher =

    type WatchParameters =
        { From: bigint
          Wait: int
          Contract: string }

    let watchFor (web3: Web3)
                 ({ From = from
                    Wait = wait
                    Contract = contract })
                 =
        let contract = web3.Eth.GetContract(erc20Abi, contract)
        let transferEvent = contract.GetEvent<TransferEventDto>()

        let rec loop (lastPolled: bigint) =
            asyncSeq {
                let next = lastPolled + 1I
                let! lastBlock =
                    web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                    |> Async.AwaitTask
                    |> Async.map (fun v -> v.Value)
            
                let maxBlock = lastBlock - (wait |> bigint)
                if maxBlock <= next then
                    do! Async.Sleep 1000
                    yield! loop lastPolled

                let filter =
                    transferEvent.CreateFilterInput
                        (BlockParameter(HexBigInteger(next)), BlockParameter(HexBigInteger(maxBlock)))


                let! changes =
                    transferEvent.GetAllChanges filter
                    |> Async.AwaitTask

                for change in changes do
                    yield change

                yield! loop (maxBlock)
            }

        loop from
