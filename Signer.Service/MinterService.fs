module Signer.Worker.Minting

open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Netezos.Keys
open Nethereum.Web3
open Nichelson
open Signer
open Signer.Ethereum
open Signer.EventStore
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

type MinterService(logger: ILogger<MinterService>, web3: Web3, configuration: EthereumConfiguration, state: StateRocksDb) =

    member this.Check =
        asyncResult {
            let! block =
                web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync

            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)
            return block
        }
        |> AsyncResult.catch (fun err -> err.Message)



    member this.Work(store: EventStoreIpfs) =
        asyncResult {
            let startingBlock = defaultArg (state.GetEthereumLevel()) 7813092I
            logger.LogInformation("Resume ethereum watch at level {}", startingBlock)
            let signer = Signer.memorySigner key
            let apply = Minting.workflow signer target

            Watcher.watchFor
                web3
                { Contract = configuration.Contract
                  Wait = configuration.Node.Wait
                  From = startingBlock }
            |> AsyncSeq.bufferByCountAndTime 100 1000
            |> AsyncSeq.iterAsync (fun e ->
                async {
                    let! r = e |> Seq.map apply |> Async.Sequential

                    for result in r do
                        match result with
                        | Ok v ->
                            do! store.Append v |> Async.Ignore
                            let (MintingSigned { Level = level; Proof = { Signature = signature } }) = v
                            logger.LogInformation("Signature {s}", signature)
                            state.PutEthereumLevel(level)
                        | Error err -> logger.LogError("Error {}", err)


                    let! name = store.Publish()
                    logger.LogInformation("Head published at {addr}", name)

                })
            |> Async.RunSynchronously

            return ()

        }


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
