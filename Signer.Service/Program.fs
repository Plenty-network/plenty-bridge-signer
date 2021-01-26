namespace Signer.Service

open System
open LiteDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Nethereum.Web3
open Signer.State.LiteDB
open Signer.Worker.Minting
open Signer.Worker.Publish
open Signer.Worker.Unwrap

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let liteDbPath = configuration.["LiteDB:Path"]

            let db =
                new LiteDatabase(sprintf "Filename=%s;Connection=direct" liteDbPath)

            this.AddSingleton(new StateLiteDb(db))

        member this.AddSigner(conf: IConfiguration) =
            this
                .AddSingleton(conf.GetSection("IPFS").Get<IpfsConfiguration>())
                .AddHostedService<SignerWorker>()

        member this.AddConfiguration(configuration: IConfiguration) =
            this
                .AddSingleton(configuration
                    .GetSection("Tezos")
                    .Get<TezosConfiguration>())
                .AddSingleton(configuration
                    .GetSection("Ethereum")
                    .Get<EthereumConfiguration>())

        member this.AddWeb3() =
            let web3Factory (s: IServiceProvider) =
                let conf = s.GetService<EthereumConfiguration>()
                Web3(conf.Node.Endpoint) :> obj
            this.Add(ServiceDescriptor(typeof<Web3>, web3Factory, ServiceLifetime.Singleton))
            this
        
        member this.AddPublisher() = this.AddSingleton<PublishService>()

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddConfiguration(hostContext.Configuration)
                    .AddWeb3()
                    .AddPublisher()
                    .AddMinter(hostContext.Configuration)
                    .AddUnwrap(hostContext.Configuration)
                    .AddSigner(hostContext.Configuration)
                |> ignore)


    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
