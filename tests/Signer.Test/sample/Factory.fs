[<RequireQualifiedAccess>]
module Signer.Test.Factory

open Nichelson
open Signer

let multisig = "KT1MsooZb43dWi5GpHLeoYw5gyXj9viUuMcE"

let benderContract =
    "KT1VUNmGa1JYJuNxNS4XDzwpsc9N1gpcCBN2"

let target: Quorum =
    { QuorumContract = TezosAddress.FromStringUnsafe multisig
      MinterContract = TezosAddress.FromStringUnsafe benderContract
      ChainId = "NetXm8tYqnMWky1" }

