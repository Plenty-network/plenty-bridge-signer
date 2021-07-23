module Signer.Endpoints

open Giraffe
open Giraffe.EndpointRouting
open Nichelson
open Signer
open Signer.Configuration
open Signer.EventStore
open Signer.IPFS
open Signer.PaymentAddress
open Signer.State.LiteDB
open FSharp.Control.Tasks
open Signer.AddToken

let statusHandler : HttpHandler =
    handleContext
        (fun ctx ->
            let s = ctx.GetService<StateLiteDb>()
            let eventStore = ctx.GetService<EventStoreState>()

            let head =
                Cid.value (defaultArg (eventStore.GetHead()) (Cid "None"))

            let eth =
                (defaultArg (s.GetEthereumLevel()) 0I).ToString()

            let tezos =
                (defaultArg (s.GetTezosLevel()) 0I).ToString()

            let transactionFailure =
                (defaultArg (s.GetEthereumErrorLevel()) 0I)
                    .ToString()

            ctx.WriteJsonAsync
                {| head = head
                   tezosLevel = tezos
                   transactionFailureLevel = transactionFailure
                   ethereumLevel = eth |}

            )

let keysHandler : HttpHandler =
    handleContext
        (fun ctx ->
            task {
                let tezSigner = ctx.GetService<TezosSigner>()
                let ethSigner = ctx.GetService<EthereumSigner>()
                let ipfsStore = ctx.GetService<IpfsClient>()
                let conf = ctx.GetService<IpfsConfiguration>()

                let! tezosKey =
                    tezSigner.PublicAddress()
                    |> AsyncResult.map (fun e -> e.GetBase58())

                let! ethKey = ethSigner.PublicAddress()


                let! ipfs = ipfsStore.Key.Find(conf.KeyName)


                let p =
                    match tezosKey, ethKey, ipfs with
                    | Ok t, Ok e, Ok i ->
                        {| tezosKey = t
                           ethereumKey = e
                           ipnsKey = i.Id |}
                    | _, _, _ -> failwith "Error retrieving keys"

                return! ctx.WriteJsonAsync p
            })

[<CLIMutable>]
type PaymentAddressPayload = { Address: string; Counter: uint64 }

let paymentAddressHandler : HttpHandler =
    handleContext
        (fun ctx ->
            task {
                let commandBus = ctx.GetService<ICommandBus>()

                let! payload = ctx.BindJsonAsync<PaymentAddressPayload>()

                let parameters : ChangePaymentAddressParameters =
                    { Address = TezosAddress.FromStringUnsafe payload.Address
                      Counter = payload.Counter }

                let! result = commandBus.PostAndReply(fun rc -> PaymentAddress(parameters, rc))

                match result with
                | Ok ({ Signature = signature
                        Quorum = quorum
                        Parameters = parameters },
                      _) ->
                    return!
                        ctx.WriteJsonAsync
                            {| Signature = signature
                               Quorum =
                                   {| QuorumContract = quorum.QuorumContract |> TezosAddress.Value
                                      ChainId = quorum.ChainId
                                      MinterContract = quorum.MinterContract |> TezosAddress.Value |}
                               Parameters =
                                   {| Counter = parameters.Counter
                                      Address = parameters.Address |> TezosAddress.Value |} |}
                | Error e -> return! ctx.WriteTextAsync e
            })

[<CLIMutable>]
type AddNftPayload =
    { EthContract: string
      TezosContract: string }

let addNftHandler : HttpHandler =
    handleContext
        (fun ctx ->
            task {
                let commandBus = ctx.GetService<ICommandBus>()
                let ipfsStore = ctx.GetService<IpfsClient>()
                let conf = ctx.GetService<IpfsConfiguration>()
                let! payload = ctx.BindJsonAsync<AddNftPayload>()

                let param =
                    NonFungible
                        { EthContract = payload.EthContract
                          TezosContract = payload.TezosContract }

                let! signature = commandBus.PostAndReply(fun rc -> AddToken(param, rc))
                let! ipnsKey = ipfsStore.Key.Find conf.KeyName

                match signature, ipnsKey with
                | Ok (s, _), Ok k -> return! ctx.WriteJsonAsync {| Signature = s; IpnsKey = k.Id |}
                | Error e, _ -> return! ctx.WriteTextAsync e
                | _, Error e -> return! ctx.WriteTextAsync e
            })


[<CLIMutable>]
type AddFtPayload =
    { EthContract: string
      TezosContract: string
      TokenId: uint }

let addFtHandler : HttpHandler =
    handleContext
        (fun ctx ->
            task {
                let commandBus = ctx.GetService<ICommandBus>()
                let ipfsStore = ctx.GetService<IpfsClient>()
                let conf = ctx.GetService<IpfsConfiguration>()
                let! payload = ctx.BindJsonAsync<AddFtPayload>()

                let param =
                    Fungible
                        { EthContract = payload.EthContract
                          TezosContract = payload.TezosContract
                          TokenId = payload.TokenId }

                let! signature = commandBus.PostAndReply(fun rc -> AddToken(param, rc))
                let! ipnsKey = ipfsStore.Key.Find conf.KeyName

                match signature, ipnsKey with
                | Ok (s, _), Ok k -> return! ctx.WriteJsonAsync {| Signature = s; IpnsKey = k.Id |}
                | Error e, _ -> return! ctx.WriteTextAsync e
                | _, Error e -> return! ctx.WriteTextAsync e
            })

let endpoints =
    [ GET [ route "status" statusHandler
            route "keys" keysHandler ]
      POST [ route "signatures/payment_address" paymentAddressHandler
             route "signatures/add_nft" addNftHandler
             route "signatures/add_token" addFtHandler ] ]
