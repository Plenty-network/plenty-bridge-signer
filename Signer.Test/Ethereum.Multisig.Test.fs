module Signer.Ethereum.``Multisig test``

open FsUnit.Xunit
open Nethereum.Signer
open Nethereum.Web3
open Org.BouncyCastle.Utilities.Encoders
open Signer
open Signer.Ethereum
open Signer.Ethereum.Multisig
open Xunit

let web3 = Web3("https://localhost:8545")

[<Fact>]
let ``Should create erc20 transfer call`` () =
    let data =
        erc20TransferCall
            { Erc20 = "0x42775d50b7Db4768f32d0267b399DE8ED7e56700"
              Owner = "0x95ADDFfF52B727E0d2317a2f1f255350f743813E"
              Amount = 10I
              OperationId = "ooLfc6nEYiHH7jUfGHLahCuPS7YkQRyNNt3Thamoy24664EFtDK" }

    data
    |> Hex.ToHexString
    |> should
        equal
           "a9059cbb00000000000000000000000095addfff52b727e0d2317a2f1f255350f743813e000000000000000000000000000000000000000000000000000000000000000a"

[<Fact>]
let ``Should create transaction hash`` () =
    asyncResult {

        let p: Erc20UnwrapParameters =
            { Erc20 = "0x42775d50b7Db4768f32d0267b399DE8ED7e56700"
              Owner = "0x95ADDFfF52B727E0d2317a2f1f255350f743813E"
              Amount = 10I
              OperationId = "ooLfc6nEYiHH7jUfGHLahCuPS7YkQRyNNt3Thamoy24664EFtDK" }


        let! hash =
            transactionHash
                web3
                "0x6e3d2fF2C4727B9E7F50D9604D7D661de2Ac2c46"
                p.Erc20
                p.OperationId
                (erc20TransferCall p)

        hash
        |> Hex.ToHexString
        |> should equal "a74ceb8c9008aaa095c1169df3e0619ac619c564e1406426617f7c52cbe435fc"
    }
    |> Async.Ignore


[<Fact>]
let ``Should sign`` () =
    asyncResult {
        let key =
            EthECKey("92e8c95392686c2b21be66f4fbd54bac1906d75a1ba0d83a4f012095c051671c")

        let p: Erc20UnwrapParameters =
            { Erc20 = "0x42775d50b7Db4768f32d0267b399DE8ED7e56700"
              Owner = "0x95ADDFfF52B727E0d2317a2f1f255350f743813E"
              Amount = 10I
              OperationId = "ooLfc6nEYiHH7jUfGHLahCuPS7YkQRyNNt3Thamoy24664EFtDK" }

        let! hash =
            transactionHash
                web3
                "0x6e3d2fF2C4727B9E7F50D9604D7D661de2Ac2c46"
                p.Erc20
                p.OperationId
                (erc20TransferCall p)

        let! r = Crypto.memorySigner(key).Sign(hash)

        r
        |> should
            equal
               "0x8841ed8de5efebb9f52cb5b3eaa1c5b70e9fbd381f177e4ed4ce6bcd309ac5cb1f5eea6881b14b4fc44e5a5e5198068173576148a6b123b8c9c73f7fb68d66ff1b"
    }
    |> Async.Ignore
