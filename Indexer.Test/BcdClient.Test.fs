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

        let! owners = client.GetQuorumOwners("KT1VYSbzNFM5UhDmdRgypFhVPtKYGjLNspBL", 378279UL)

        owners.Length |> should equal 1
        owners.[0] |> should equal {
            IPNSPeerId = "k51qzi5uqu5dhnyxm2b8k5kypnu9jo13b89l6iddek76k7u5lig48ti5v82c15"
            PublicKey = "sppk7a8xPov96ZwVh7mKi6nkkQS8r8ycYHDp7YahhnF3q1Xb3AQmBpL"
        }
    }
