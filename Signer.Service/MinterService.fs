module Signer.Worker.Minting

open System
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Netezos.Keys
open Nethereum.Contracts
open Nethereum.Hex.HexTypes
open Nethereum.Web3
open Nichelson
open Signer
open Signer.Ethereum
open Signer.Ethereum.Contract
open Signer.EventStore
open Signer.Minting
open Signer.State.RocksDb
open Signer.Tezos

let key =
    Key.FromBase58("edsk2y68dasGjkyZM2swAonhRzyNpn6mPCWtupdh8MYgkKZ6hvfRGL")


type EthereumConfiguration =
    { Node: EthNodeConfiguration
      Contract: string }

and EthNodeConfiguration = { Endpoint: string; Wait: int }

type TezosConfiguration =
    { QuorumContract: string
      MinterContract: string
      Node: TezosNodeConfiguration }

and TezosNodeConfiguration =
    { ChainId: string
      Endpoint: string
      Timeout: int }

type MinterService(logger: ILogger<MinterService>,
                   web3: Web3,
                   ethConfiguration: EthereumConfiguration,
                   tezosConfiguration: TezosConfiguration,
                   state: StateRocksDb) =

    let apply (workflow: MinterWorkflow) (blockLevel: HexBigInteger, events: EventLog<TransferEventDto> seq) =
        logger.LogInformation("Processing Block {level} containing {nb} event(s)", blockLevel.Value, events |> Seq.length)

        let applyOne (event: EventLog<TransferEventDto>) =
            logger.LogDebug("Processing {i}:{h}", event.Log.TransactionIndex, event.Log.TransactionHash)
            workflow event

        let rec f elements =
            asyncResult {
                match elements with
                | [] ->
                    return blockLevel.Value
                | [ last ] ->
                    let! _ = applyOne last
                    return blockLevel.Value
                | head :: tail ->
                    let! _ = applyOne head
                    return! f tail

            }

        f (events |> Seq.toList)


    member this.Check =
        asyncResult {
            let! block =
                web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync

            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)
            return block
        }
        |> AsyncResult.catch (fun err -> sprintf "Couldn't connect to ethereum node %s" err.Message)



    member this.Work(store: EventStoreIpfs) =
        let startingBlock =
            defaultArg (state.GetEthereumLevel()) 7832153I

        let target =
            { QuorumContract = TezosAddress.FromString tezosConfiguration.QuorumContract
              MinterContract = TezosAddress.FromString(tezosConfiguration.MinterContract + "%signer")
              ChainId = tezosConfiguration.Node.ChainId }

        logger.LogInformation("Resume ethereum watch at level {}", startingBlock)
        let signer = Signer.memorySigner key

        let workflow =
            Minting.workflow signer.Sign store.Append target

        let apply = apply workflow

        Watcher.watchFor
            web3
            { Contract = ethConfiguration.Contract
              Wait = ethConfiguration.Node.Wait
              From = startingBlock }
        |> AsyncSeq.iterAsync (fun event ->

            async {
                let! result = apply event
                match result with
                | Ok level -> state.PutEthereumLevel(level)
                | Error err -> return raise (ApplicationException(err))

            })



type IServiceCollection with
    member this.AddMinter(configuration: IConfiguration) =
        let ethereumConfiguration =
            { Contract = configuration.["Ethereum:Contract"]
              Node =
                  { Endpoint = configuration.["Ethereum:Node:Endpoint"]
                    Wait = configuration.GetValue<int>("Ethereum:Node:Wait") } }

        let tezosConfiguration =
            { QuorumContract = configuration.["Tezos:QuorumContract"]
              MinterContract = configuration.["Tezos:MinterContract"]
              Node =
                  { Endpoint = configuration.["Tezos:Node:Endpoint"]
                    Timeout = configuration.GetValue<int>("Tezos:Node:Timeout")
                    ChainId = configuration.["Tezos:Node:ChainId"] } }

        this
            .AddSingleton<TezosConfiguration>(tezosConfiguration)
            .AddSingleton<EthereumConfiguration>(ethereumConfiguration)
            .AddSingleton(Web3(ethereumConfiguration.Node.Endpoint))
            .AddSingleton<MinterService>()
