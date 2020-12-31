module Signer.Pipeline.Ethereum

open System.Threading
open System.Threading.Tasks
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Netezos.Keys
open Nethereum.Web3
open Nichelson
open Signer
open Signer.Ethereum
open Signer.EventStore
open Signer.IPFS
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

type EthereumWorker(logger: ILogger<EthereumWorker>, configuration: EthereumConfiguration, state: StateRocksDb) =
    inherit BackgroundService()


    override _.ExecuteAsync(ct: CancellationToken) =
        EventStoreIpfs.Create(IpfsClient("http://localhost:5001"), "bender", state.GetHead())
        |> AsyncResult.bind (fun store ->
            let ep = configuration.Node.Endpoint
            let web3 = Web3(ep)
            let startingBlock = 7813092I
            let signer = Signer.memorySigner key

            let apply =
                Minting.workflow signer store.Append target

            Watcher.watchFor
                web3
                { Contract = configuration.Contract
                  Wait = configuration.Node.Wait
                  From = startingBlock }
            |> AsyncSeq.iterAsync (fun e ->
                async {
                    let! r = apply e

                    match r with
                    | Ok (_, MintingSigned ({ Proof = { Signature = s } })) ->
                        logger.LogInformation("Signature {s}", s)
                    | Error err -> logger.LogError err
                })
            |> Async.RunSynchronously

            AsyncResult.retn ())
        |> (fun a -> Async.StartAsTask(a, cancellationToken = ct)) :> Task

type IServiceCollection with
    member this.AddEthereumPipeline(configuration: IConfiguration) =
        let ethereumConfiguration =
            { Contract = configuration.["Ethereum:Contract"]
              Node =
                  { Endpoint = configuration.["Ethereum:Node:Endpoint"]
                    Wait = configuration.GetValue<int>("Ethereum:Node:Wait") } }

        this
            .AddSingleton<EthereumConfiguration>(ethereumConfiguration)
            .AddHostedService<EthereumWorker>()
