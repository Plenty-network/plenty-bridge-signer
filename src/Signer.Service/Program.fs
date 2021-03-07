namespace Signer

open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging
open Signer.Configuration
open Signer.Worker.Minting
open Signer.Worker.Publish
open Signer.Worker.Unwrap
open Signer.Service
open Signer.Endpoints
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.EndpointRouting

module Program =
    open Microsoft.AspNetCore

    let configureApp (config: IConfigurationBuilder) =
        let configFile =
            System.Environment.GetEnvironmentVariable("CONFIG_FILE")

        let configBaseDir =
            System.Environment.GetEnvironmentVariable("CONFIG_BASE_DIR")

        if not (System.String.IsNullOrEmpty(configFile)) then
            if not (System.String.IsNullOrEmpty(configBaseDir)) then
                config.AddJsonFile(new PhysicalFileProvider(configBaseDir), configFile, false, false)
                |> ignore
            else
                config.AddJsonFile(configFile, optional = false)
                |> ignore

    let configureServices (hostContext: WebHostBuilderContext) (services: IServiceCollection) =
        services
            .AddCommonServices(hostContext.Configuration)
            .AddPublisher()
            .AddMinter()
            .AddUnwrap()
            .AddSigner(hostContext.Configuration)
            .AddPaymentAddressWorkflow()
            .AddRouting()
            .AddGiraffe()
        |> ignore

    let createHostBuilder args =
        WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(configureApp)
            .ConfigureServices(configureServices)
            .SuppressStatusMessages(true)
            .Configure(fun app -> app.UseRouting().UseGiraffe(endpoints) |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code
