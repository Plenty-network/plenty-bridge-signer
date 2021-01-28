module Signer.Ethereum.Multisig

open System.Text
open Nethereum.Web3
open Org.BouncyCastle.Utilities.Encoders
open Signer

type UnwrapParameters = {
    TokenContract: string
    Destination: string
    Amount: bigint
    OperationId: string
}
type EthPack = UnwrapParameters -> byte array DomainResult

let transferCall (web3: Web3) erc20Address destination amount =
    let erc20 =
        web3.Eth.GetContract(Contract.erc20Abi, erc20Address)

    let transfer = erc20.GetFunction("transfer")

    let data =
        transfer.CreateCallInput(destination, amount).Data

    Hex.Decode(data.[2..])

let transactionHash (web3: Web3) lockingContractAddress (parameters: UnwrapParameters) =
    asyncResult {
        let locking =
            web3.Eth.GetContract(Contract.lockingContractAbi, lockingContractAddress)

        let data =
            transferCall web3 parameters.TokenContract parameters.Destination parameters.Amount

        let! hash =
            locking
                .GetFunction("getTransactionHash")
                .CallAsync<byte array>(parameters.Destination, 0, data, parameters.OperationId)
            |> Async.AwaitTask
            |> AsyncResult.ofAsync
            |> AsyncResult.catch (fun err -> err.Message)
        return hash
    }
