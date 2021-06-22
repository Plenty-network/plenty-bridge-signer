namespace Signer.Ethereum

open System.Collections.Generic
open System.Numerics
open Nethereum.ABI
open Nethereum.Contracts
open Nethereum.Hex.HexTypes
open Nethereum.RPC.Eth.DTOs
open Signer.Ethereum.Contract
open FSharp.Control
open Nethereum.Web3
open FSharpx.Control


type EthEvent =
    | Erc20Wrapped of ERC20WrapAskedEventDto
    | Erc721Wrapped of ERC721WrapAskedEventDto


type BaseEthEventLog<'a> = { Log: FilterLog; Event: 'a }

type EthEventLog = BaseEthEventLog<EthEvent>

type FailedUnwrap =
    { TezosTransaction: string
      TokenContract: string
      Owner: string
      Amount: bigint }

type TransactionFailure = BaseEthEventLog<FailedUnwrap>

module Watcher =

    type WatchParameters =
        { From: bigint
          Confirmations: int
          Contract: string }

    let private poll
        (web3: Web3)
        (nextRun: HexBigInteger -> HexBigInteger -> Async<seq<BaseEthEventLog<_>>> list)
        from
        (confirmations: int)
        =


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
                let filters =
                    nextRun (HexBigInteger(next)) (HexBigInteger(maxBlock))

                let! changes =
                    Async.Parallel filters
                    |> Async.map Seq.concat
                    |> Async.map (Seq.groupBy (fun e -> e.Log.BlockNumber))
                    |> Async.map (Seq.sortBy (fun (e, _) -> e.Value))

                for change in changes do
                    yield change

                yield! loop maxBlock
            }

        loop from

    let checkReceipt (web3: Web3) (lockingContract: Contract) hash =
        async {
            let! receipt =
                web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash)
                |> Async.AwaitTask

            let lockingContract =
                web3.Eth.GetContract(lockingContractAbi, "")

            let eventAbi =
                lockingContract
                    .GetEvent(
                        "ExecutionFailure"
                    )
                    .EventABI

            let t =
                eventAbi.GetLogsForEvent(receipt.Logs).Length

            return t = receipt.Logs.Count
        }

    let watchForExecutionFailure
        (web3: Web3)
        ({ From = from
           Confirmations = confirmations
           Contract = contract })
        =


        let contract =
            web3.Eth.GetContract(lockingContractAbi, contract)

        let erc20 = web3.Eth.GetContract(erc20Abi, "")

        let check (t: TransactionFailure) =
            async {
                let! valid = checkReceipt web3 contract t.Log.TransactionHash

                if valid then
                    return Some t
                else
                    return None
            }

        let toExecutionFailure (log: FilterLog, lockContractCall: List<FunctionEncoding.ParameterOutput>) =

            let tokenContract = lockContractCall.[0].Result :?> string

            let transferCall =
                erc20
                    .GetFunction("transfer")
                    .DecodeInput(
                        Nethereum.Hex.HexConvertors.Extensions.HexByteConvertorExtensions.ToHex(
                            lockContractCall.[2].Result :?> byte []
                        )
                    )

            let to_ = transferCall.[0].Result :?> string
            let amount = transferCall.[1].Result :?> bigint
            let tezosTransactionIdentifier = lockContractCall.[3].Result :?> string

            { Event =
                  { TezosTransaction = tezosTransactionIdentifier
                    Owner = to_
                    TokenContract = tokenContract
                    Amount = amount }
              Log = log }



        let transactionFailed =
            contract.GetEvent<LockingContractExecutionFailureDto>()

        let toTransferCalls (e: _ EventLog seq) =
            e
            |> Seq.map
                (fun e ->
                    web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(e.Log.TransactionHash)
                    |> Async.AwaitTask
                    |> Async.map
                        (fun transaction ->
                            (e.Log,
                             contract
                                 .GetFunction("execTransaction")
                                 .DecodeInput(transaction.Input))))
            |> Async.Parallel



        let exec (event: _ Nethereum.Contracts.Event) (filter: NewFilterInput) =
            event.GetAllChanges filter
            |> Async.AwaitTask
            |> Async.map toTransferCalls
            |> Async.bind (fun e -> async { return! e })
            |> Async.map (Seq.map toExecutionFailure)
            |> Async.map (Seq.map check)
            |> Async.map (Async.Parallel)
            |> Async.bind (fun e -> async { return! e })
            |> Async.map (Seq.choose id)

        let nextRun (next: HexBigInteger) (max: HexBigInteger) =
            let transactionFailedFilter =
                transactionFailed.CreateFilterInput(BlockParameter(next), BlockParameter(max))

            let r =
                exec transactionFailed transactionFailedFilter

            [ r ]

        poll web3 nextRun from confirmations


    let watchForWrappingEvents
        (web3: Web3)
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
            |> Async.map
                (fun events ->
                    events
                    |> Seq.map
                        (fun event ->
                            { Log = event.Log
                              Event = ctor event.Event }))

        let nextRun (next: HexBigInteger) (max: HexBigInteger) =
            let erc20Filter =
                erc20Wrapped.CreateFilterInput(BlockParameter(next), BlockParameter(max))

            let erc721Filter =
                erc721Wrapped.CreateFilterInput(BlockParameter(next), BlockParameter(max))

            let r =
                exec erc20Wrapped erc20Filter Erc20Wrapped

            let r' =
                exec erc721Wrapped erc721Filter Erc721Wrapped

            [ r; r' ]

        poll web3 nextRun from confirmations
