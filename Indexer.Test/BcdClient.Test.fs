module Indexer.Test.``BcdClient Tests``


open FsUnit.Xunit
open Indexer.Infrastructure.BCD
open Xunit

[<Fact>]
let ``Should give network state`` () =
    async {
        let client =
            BcdClient("https://api.better-call.dev", "delphinet")

        let! state = client.GetCurrentState()

        state |> should not' (equal null)
    }
