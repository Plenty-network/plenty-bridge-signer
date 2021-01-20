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

[<Fact>]
let ``Should give quorum contract owners`` () =
    async {
        let client =
            BcdClient("https://api.better-call.dev", "delphinet")

        let! owners = client.GetQuorumOwners("KT1GuAQRrWVdwaTpURLutxGFEXHfFmBB8r5m", 375151UL)

        owners.Length |> should equal 1
        owners.[0] |> should equal {
            IPNSPeerId = "0024080112203b7e495ee69372f4705df0c93a9b7731e6caa7208a7352b4d34566274ea15d69"
            PublicKey = "sppk7a8xPov96ZwVh7mKi6nkkQS8r8ycYHDp7YahhnF3q1Xb3AQmBpL"
        }
    }
