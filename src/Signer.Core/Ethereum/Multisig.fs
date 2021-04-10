module Signer.Ethereum.Multisig

open Nethereum.Web3
open Org.BouncyCastle.Utilities.Encoders
open Signer

type UnwrapParameters =
    { TokenContract: string
      Destination: string
      Amount: bigint
      OperationId: string }

type PackParameters =
    { LockingContract: string
      ErcContract: string
      OperationId: string
      Data: byte [] }

type EthPack = PackParameters -> byte array DomainResult


let private disconnectedWeb3 = Web3()

let erc20TransferCall (p: Erc20UnwrapParameters) =
    let erc20 =
        disconnectedWeb3.Eth.GetContract(Contract.erc20Abi, p.ERC20)

    let transfer = erc20.GetFunction("transfer")

    let data =
        transfer.CreateCallInput(p.Owner, p.Amount).Data

    Hex.Decode(data.[2..])

let erc721SafeTransferCall (lockingContract: string) (p: Erc721UnwrapParameters) =

    let erc721 =
        disconnectedWeb3.Eth.GetContract(Contract.erc721Abi, p.ERC721)

    let transfer = erc721.GetFunction("safeTransferFrom")

    let data =
        transfer
            .CreateCallInput(lockingContract, p.Owner, p.TokenId)
            .Data

    Hex.Decode(data.[2..])

let transactionHash (web3: Web3): EthPack =
    fun { LockingContract = lockingContractAddress
          ErcContract = destination
          Data = data
          OperationId = operationId } ->
        let locking =
            web3.Eth.GetContract(Contract.lockingContractAbi, lockingContractAddress)

        asyncResult {
            let! hash =
                locking
                    .GetFunction("getTransactionHash")
                    .CallAsync<byte array>(destination, 0, data, operationId)
                |> Async.AwaitTask
                |> AsyncResult.ofAsync
                |> AsyncResult.catch (fun err -> err.Message)


            return hash
        }
