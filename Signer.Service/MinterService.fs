module Signer.Worker.Minting

open System
open System.Threading
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

let words =
    [ "negative"
      "shoe"
      "enlist"
      "emotion"
      "monkey"
      "sell"
      "increase"
      "toddler"
      "grace"
      "noise"
      "tree"
      "perfect"
      "regular"
      "nothing"
      "stadium" ]

let key =
    Key.FromMnemonic(Mnemonic.Parse(words), "vespwozi.vztxobwc@tezos.example.org", "")

let multisig = "KT1MsooZb43dWi5GpHLeoYw5gyXj9viUuMcE"

let benderContract =
    "KT1VUNmGa1JYJuNxNS4XDzwpsc9N1gpcCBN2%signer"

let target =
    { MultisigContract = TezosAddress.FromString multisig
      BenderContract = TezosAddress.FromString(benderContract)
      ChainId = "NetXm8tYqnMWky1" }

type NodeConfiguration = { Endpoint: string; Wait: int }

type EthereumConfiguration =
    { Node: NodeConfiguration
      Contract: string }

type MinterService(logger: ILogger<MinterService>,
                   web3: Web3,
                   configuration: EthereumConfiguration,
                   state: StateRocksDb) =

    let apply (workflow: MinterWorkflow) (blockLevel: HexBigInteger, events: EventLog<TransferEventDto> seq) =
        logger.LogInformation("Processing Block {level} containing {nb} events", blockLevel.Value, events |> Seq.length)

        let rec f elements =
            asyncResult {
                match elements with
                | [] ->
                    return blockLevel.Value 
                | [ last ] ->
                    let! _ = workflow last
                    return blockLevel.Value
                | head :: tail ->
                    let! _ = workflow head
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
            defaultArg (state.GetEthereumLevel()) 7813092I

        logger.LogInformation("Resume ethereum watch at level {}", startingBlock)
        let signer = Signer.memorySigner key

        let workflow =
            Minting.workflow signer store.Append target

        let apply = apply workflow

        Watcher.watchFor
            web3
            { Contract = configuration.Contract
              Wait = configuration.Node.Wait
              From = startingBlock }
        |> AsyncSeq.bufferByCountAndTime 1000 5000
        |> AsyncSeq.iterAsync (fun e ->
            async {
                for event in e do
                    let! result =  apply event
                    match result with
                    | Ok level ->  state.PutEthereumLevel(level)
                    | Error err -> return raise(ApplicationException(err))
                let! name = store.Publish()
                logger.LogInformation("Head published at {addr}", name)
            })



type IServiceCollection with
    member this.AddMinter(configuration: IConfiguration) =
        let ethereumConfiguration =
            { Contract = configuration.["Ethereum:Contract"]
              Node =
                  { Endpoint = configuration.["Ethereum:Node:Endpoint"]
                    Wait = configuration.GetValue<int>("Ethereum:Node:Wait") } }

        this
            .AddSingleton<EthereumConfiguration>(ethereumConfiguration)
            .AddSingleton(Web3(ethereumConfiguration.Node.Endpoint))
            .AddSingleton<MinterService>()
