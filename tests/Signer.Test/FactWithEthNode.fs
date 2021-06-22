namespace Signer.Test

open Xunit

type FactWithEthNodeAttribute() as this =
    inherit FactAttribute()

    let ENV_VAR = "ETH_NODE"

    do
        if System.String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(ENV_VAR)) then
            this.Skip <- "No eth node"

    static member NodeUrl =
        System.Environment.GetEnvironmentVariable("ETH_NODE")
