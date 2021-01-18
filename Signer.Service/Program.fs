namespace Signer.Service

open LiteDB
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Signer.State.LiteDB
open Signer.Worker.Minting
open Signer.Worker.Publish

module Program =

    type IServiceCollection with
        member this.AddState(configuration: IConfiguration) =
            let liteDbPath = configuration.["LiteDB:Path"]

            let db = new LiteDatabase(sprintf "Filename=%s;Connection=direct" liteDbPath)

            this.AddSingleton(new StateLiteDb(db))

        member this.AddSigner(conf: IConfiguration) =
            this
                .AddSingleton(conf.GetSection("IPFS").Get<IpfsConfiguration>())
                .AddHostedService<SignerWorker>()

        member this.AddPublisher() = this.AddSingleton<PublishService>()

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddState(hostContext.Configuration)
                    .AddPublisher()
                    .AddMinter(hostContext.Configuration)
                    .AddSigner(hostContext.Configuration)
                |> ignore)


    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
