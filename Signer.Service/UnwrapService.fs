module Signer.Worker.Unwrap

open System
open FSharp.Control
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Netezos.Rpc
open Nethereum.Signer
open Nethereum.Web3
open Signer
open Signer.EventStore
open Signer.State.LiteDB
open Signer.Worker.Minting
open TzWatch.Domain
open TzWatch.Sync

type UnwrapService(logger: ILogger<UnwrapService>,
                   web3: Web3,
                   ethConfiguration: EthereumConfiguration,
                   tezosConfiguration: TezosConfiguration,
                   signer: EthereumSigner,
                   state: StateLiteDb) =
    member this.Work(store: EventStoreIpfs) =
        logger.LogInformation("Resume tezos watch at level {}", 0)

        let pack =
            Ethereum.Multisig.transactionHash web3 ethConfiguration.LockingContract

        let workflow =
            Unwrap.workflow signer pack ethConfiguration.LockingContract store.Append

        let parameters =
            { Contract = (ContractAddress.createUnsafe tezosConfiguration.MinterContract)
              Interests = [ EntryPoint "unwrap" ]
              Confirmations = 0u }

        let poller =
            SyncNode(new TezosRpc(tezosConfiguration.Node.Endpoint), tezosConfiguration.Node.ChainId)

        Subscription.run poller (Height 3) parameters
        |> AsyncSeq.iterAsync (fun event ->
            async {
                logger.LogDebug("Event from tezos level:{e} hash:{val}", event.Level, event.Hash)
                let! result = workflow event

                match result with
                | Ok level -> logger.LogInformation "Wrapped event" // state.PutEthereumLevel(level)
                | Error err -> return raise (ApplicationException(err))
            })




let configureSigner (services: IServiceCollection) (configuration: IConfiguration) =
    let signerType =
        configuration
            .GetSection("Ethereum:Signer:Type")
            .Get<SignerType>()

    (*let createAwsSigner(s: IServiceProvider) =
        let kms = s.GetService<IAmazonKeyManagementService>()
        let keyId = configuration.["AWS:EthKeyId"]
        Signer.awsSigner kms keyId :> obj*)

    let service =
        match signerType with
        | SignerType.AWS -> failwith "Not implemented"
        (*services.AddAWSService<IAmazonKeyManagementService>() |> ignore
            ServiceDescriptor(typeof<TezosSigner>, createAwsSigner, ServiceLifetime.Singleton)*)
        | SignerType.Memory ->
            let key =
                EthECKey(configuration.["Ethereum:Signer:Key"])

            let signer = Signer.Ethereum.Signer.memorySigner key

            ServiceDescriptor(typeof<EthereumSigner>, signer)
        | v -> failwith (sprintf "Unknown signer type: %A" v)

    services.Add(service)


type IServiceCollection with
    member this.AddUnwrap(configuration: IConfiguration) =

        configureSigner this configuration
        this.AddSingleton<UnwrapService>()
