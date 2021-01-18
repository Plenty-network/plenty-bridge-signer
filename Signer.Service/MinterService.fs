module Signer.Worker.Minting

open System
open Amazon.KeyManagementService
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


[<CLIMutable>]
type EthNodeConfiguration = { Endpoint: string; Wait: int }


[<CLIMutable>]
type EthereumConfiguration =
    { Node: EthNodeConfiguration
      Contract: string }


[<CLIMutable>]
type TezosNodeConfiguration =
    { ChainId: string
      Endpoint: string
      Timeout: int }

type SignerType =
    | AWS = 0
    | Memory = 1

[<CLIMutable>]
type TezosConfiguration =
    { QuorumContract: string
      MinterContract: string
      Node: TezosNodeConfiguration }

type MinterService(logger: ILogger<MinterService>,
                   web3: Web3,
                   ethConfiguration: EthereumConfiguration,
                   tezosConfiguration: TezosConfiguration,
                   signer: TezosSigner,
                   state: StateRocksDb) =

    let mutable startingBlock: bigint = 0I
    
    let apply (workflow: MinterWorkflow) (blockLevel: HexBigInteger, events: EventLog<WrapAskedEventDto> seq) =
        logger.LogInformation
            ("Processing Block {level} containing {nb} event(s)", blockLevel.Value, events |> Seq.length)

        let applyOne (event: EventLog<WrapAskedEventDto>) =
            logger.LogDebug("Processing {i}:{h}", event.Log.TransactionIndex, event.Log.TransactionHash)
            workflow event

        let rec f elements =
            asyncResult {
                match elements with
                | [] -> return blockLevel.Value
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
                |> AsyncResult.catch(fun err -> sprintf "Couldn't connect to ethereum node %s" err.Message)
            startingBlock <- defaultArg (state.GetEthereumLevel()) block.Value // 4103600I // block.Value 
            state.PutEthereumLevel startingBlock
            logger.LogInformation("Connected to ethereum node at level {level}", block.Value)
            let! addr = signer.PublicAddress()
                        |> AsyncResult.catch(fun err -> sprintf "Couldn't get public key %s" err.Message)
            logger.LogInformation("Using signing tezos address {hash} {key}", addr.Address, addr.GetBase58())
            return ()
        }
        |> AsyncResult.catch (fun err -> sprintf "Unexpected check error %s" err.Message)



    member this.Work(store: EventStoreIpfs) =
        let target =
            { QuorumContract = TezosAddress.FromString tezosConfiguration.QuorumContract
              MinterContract = TezosAddress.FromString(tezosConfiguration.MinterContract + "%signer")
              ChainId = tezosConfiguration.Node.ChainId }

        logger.LogInformation("Resume ethereum watch at level {}", startingBlock)

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


let configureSigner (services:IServiceCollection) (configuration: IConfiguration) =
    let signerType = configuration.GetSection("Tezos:Signer:Type").Get<SignerType>()

    let createAwsSigner(s: IServiceProvider) =
        let kms = s.GetService<IAmazonKeyManagementService>()
        let keyId = configuration.["AWS:KeyId"]
        Signer.awsSigner kms keyId :> obj

    let service =
        match signerType with
        | SignerType.AWS ->
            services.AddAWSService<IAmazonKeyManagementService>() |> ignore
            ServiceDescriptor(typeof<TezosSigner>, createAwsSigner, ServiceLifetime.Singleton)
        | SignerType.Memory ->
            let key = configuration.["Tezos:Signer:Key"]
            ServiceDescriptor(typeof<TezosSigner>, Signer.memorySigner(Key.FromBase58 key))
        | _ as v -> failwith (sprintf "Unknown signer type: %A" v)
    services.Add(service)
    

type IServiceCollection with
    member this.AddMinter(configuration: IConfiguration) =
        let web3Factory (s: IServiceProvider) =
            let conf = s.GetService<EthereumConfiguration>()
            Web3(conf.Node.Endpoint) :> obj
        this.Add(ServiceDescriptor(typeof<Web3>, web3Factory, ServiceLifetime.Singleton))
        configureSigner this configuration
        this
            .AddSingleton(configuration
                .GetSection("Tezos")
                .Get<TezosConfiguration>())
            .AddSingleton(configuration
                .GetSection("Ethereum")
                .Get<EthereumConfiguration>())
            .AddSingleton<MinterService>()
