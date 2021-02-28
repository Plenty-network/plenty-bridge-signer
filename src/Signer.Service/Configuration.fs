module Signer.Configuration

open System
open LiteDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Netezos.Rpc
open Nethereum.Web3
open Signer.EventStore
open Signer.IPFS
open Signer.State.LiteDB

[<CLIMutable>]
type EthNodeConfiguration =
    { Endpoint: string
      Confirmations: int }


[<CLIMutable>]
type EthereumConfiguration =
    { InitialLevel: int
      Node: EthNodeConfiguration
      LockingContract: string }

[<CLIMutable>]
type TezosNodeConfiguration =
    { ChainId: string
      Endpoint: string
      Confirmations: int }

type SignerType =
    | AWS = 0
    | Memory = 1
    | Azure = 2

[<CLIMutable>]
type TezosConfiguration =
    { QuorumContract: string
      MinterContract: string
      InitialLevel: int
      Node: TezosNodeConfiguration }

[<CLIMutable>]
type IpfsConfiguration = { Endpoint: string; KeyName: string }

type IServiceCollection with
    member this.AddState(configuration: IConfiguration) =
        let stateFactory (_: IServiceProvider) =
            let liteDbPath = configuration.["LiteDB:Path"]

            let db =
                new LiteDatabase(sprintf "Filename=%s;Connection=direct" liteDbPath)

            new StateLiteDb(db) :> obj


        this.Add(ServiceDescriptor(typeof<StateLiteDb>, stateFactory, ServiceLifetime.Singleton))

        this
            .AddSingleton<EventStoreState>(fun x -> x.GetRequiredService<StateLiteDb>() :> EventStoreState)
            .AddSingleton<IpfsClient>(fun x ->
                let conf =
                    x.GetRequiredService<IpfsConfiguration>()

                IpfsClient(conf.Endpoint))

    member this.AddConfiguration(configuration: IConfiguration) =
        this
            .AddSingleton(configuration
                .GetSection("Tezos")
                .Get<TezosConfiguration>())
            .AddSingleton(configuration
                .GetSection("Ethereum")
                .Get<EthereumConfiguration>())
            .AddSingleton(configuration
                .GetSection("IPFS")
                .Get<IpfsConfiguration>())

    member this.AddWeb3() =
        let web3Factory (s: IServiceProvider) =
            let conf = s.GetService<EthereumConfiguration>()
            Web3(conf.Node.Endpoint) :> obj

        this.Add(ServiceDescriptor(typeof<Web3>, web3Factory, ServiceLifetime.Singleton))
        this

    member this.AddTezosRpc() =
        let tezosRpcFactory (s: IServiceProvider) =
            let conf = s.GetService<TezosConfiguration>()
            new TezosRpc(conf.Node.Endpoint) :> obj

        this.Add(ServiceDescriptor(typeof<TezosRpc>, tezosRpcFactory, ServiceLifetime.Singleton))
        this

    member this.AddCommonServices(configuration: IConfiguration) =
        this
            .AddConfiguration(configuration)
            .AddState(configuration)
            .AddWeb3()
            .AddTezosRpc()
