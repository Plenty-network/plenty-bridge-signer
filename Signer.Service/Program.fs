namespace Signer.Service

open Microsoft.Extensions.Hosting
open Signer.Pipeline.Ethereum

module Program =
    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.AddEthereumPipeline(hostContext.Configuration)
                |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
