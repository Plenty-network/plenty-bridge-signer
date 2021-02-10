module Indexer.Endpoints

open Giraffe
open Giraffe.EndpointRouting

let statusHandler: HttpHandler =
    handleContext (fun ctx ->
        ctx.WriteJsonAsync
            ({| message = "hello" |})
        )

let endpoints =
    [ GET [ route "status" statusHandler ] ]
