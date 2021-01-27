namespace Signer

open Microsoft.Extensions.Hosting
open Signer.Configuration
open Signer.Worker.Minting
open Signer.Worker.Publish
open Signer.Worker.Unwrap
open Signer.Service

module Program =

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services
                    .AddCommonServices(hostContext.Configuration)
                    .AddPublisher()
                    .AddMinter(hostContext.Configuration)
                    .AddUnwrap(hostContext.Configuration)
                    .AddSigner(hostContext.Configuration)
                |> ignore)


    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
