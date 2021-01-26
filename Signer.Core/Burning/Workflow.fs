module Signer.Burning

type EthereumAddress = EthereumAddress of string


[<RequireQualifiedAccess>]
module EthereumAddress =
    let value (EthereumAddress v) = v

type BurningParameters = {
    Amount: bigint
    Owner: EthereumAddress
}
