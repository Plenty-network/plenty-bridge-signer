namespace Signer

open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Signer.Configuration
open Signer.Worker.Minting
open Signer.Worker.Publish
open Signer.Worker.Unwrap
open Signer.Service
open Microsoft.Extensions.Configuration

module Program =

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(fun (hostingContext: HostBuilderContext) (config: IConfigurationBuilder) ->
                let configFile = System.Environment.GetEnvironmentVariable("CONFIG_FILE")
                let configBaseDir = System.Environment.GetEnvironmentVariable("CONFIG_BASE_DIR")
                if not (System.String.IsNullOrEmpty(configFile)) then
                    if not(System.String.IsNullOrEmpty(configBaseDir)) then
                        config.AddJsonFile( new PhysicalFileProvider(configBaseDir), configFile, false, false) |> ignore
                    else
                        config.AddJsonFile(configFile, optional = false) |> ignore
                )
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
