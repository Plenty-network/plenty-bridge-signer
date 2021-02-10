module Signer.Ethereum.``Multisig test``

open FsUnit.Xunit
open Nethereum.Signer
open Nethereum.Web3
open Org.BouncyCastle.Utilities.Encoders
open Signer
open Signer.Ethereum
open Signer.Ethereum.Multisig
open Xunit

let web3 = Web3("http://localhost:8545")

let key =
    EthECKey("b92d594b7433a2e7eaeae7ac66709cbdf0bb569f088028171936540f2ca62193")

let erc20Params: Erc20UnwrapParameters =
    { ERC20 = "0xD368146a3BFF47E0fA8cadA1FfE00F6738374721"
      Owner = "0x8178C9C1BE2A48DCf9ea8AD7A99577DA7a283de5"
      Amount = 10I
      OperationId = "ooh6Bz4sLxKZ9dWZ5BVpJu8Snn1qZ7zse416gAUhiR5w2m1wSEd/24" }

let erc721Params: Erc721UnwrapParameters =
    { ERC721 = "0xA71007e73288789D4F430D5B685182Fe84189094"
      Owner = "0x8178C9C1BE2A48DCf9ea8AD7A99577DA7a283de5"
      TokenId = 1337I
      OperationId = "ooh6Bz4sLxKZ9dWZ5BVpJu8Snn1qZ7zse416gAUhiR5w2m1wSEd/24" }

let lockingContract =
    "0x9b51c20109eA7adF2807849C616F202D58991c30"

[<Fact>]
let ``Should create erc20 transfer call`` () =
    let data = erc20TransferCall erc20Params

    data
    |> Hex.ToHexString
    |> should
        equal
           "a9059cbb0000000000000000000000008178c9c1be2a48dcf9ea8ad7a99577da7a283de5000000000000000000000000000000000000000000000000000000000000000a"

[<Fact>]
let ``Should create erc721 transfer call`` () =
    let data =
        erc721SafeTransferCall lockingContract erc721Params

    data
    |> Hex.ToHexString
    |> should
        equal
           "42842e0e0000000000000000000000009b51c20109ea7adf2807849c616f202d58991c300000000000000000000000008178c9c1be2a48dcf9ea8ad7a99577da7a283de50000000000000000000000000000000000000000000000000000000000000539"

[<Fact(Skip="Needs an actual ethereum node")>]
let ``Should create erc20 transaction hash`` () =
    async {

        let! hash =
            transactionHash
                web3
                lockingContract
                erc20Params.ERC20
                erc20Params.OperationId
                (erc20TransferCall erc20Params)

        match hash with
        | Ok hash ->
            hash
            |> Hex.ToHexString
            |> should equal "741814a83e14126c15e2a07fc42e52ca10e70c1355a13b468f5e75cec1ba99e3"
        | Error err -> failwith err
    }

[<Fact(Skip="Needs an actual ethereum node")>]
let ``Should create erc721 transaction hash`` () =
    async {

        let! hash =
            transactionHash
                web3
                lockingContract
                erc721Params.ERC721
                erc721Params.OperationId
                (erc721SafeTransferCall lockingContract erc721Params)

        match hash with
        | Ok hash ->
            hash
            |> Hex.ToHexString
            |> should equal "aa0df064ceb6fcceddbc3ea154b6cb1ec6c717ca3d7cd320a04b376ec5ef1ba4"
        | Error err -> failwith err
    }

[<Fact(Skip="Needs an actual ethereum node")>]
let ``Should sign erc20 unwrap`` () =
    async {
        let sign =
            Crypto.memorySigner(key).Sign |> AsyncResult.bind

        let! r =
            transactionHash
                web3
                lockingContract
                erc20Params.Owner
                erc20Params.OperationId
                (erc20TransferCall erc20Params)
            |> sign

        match r with
        | Ok r ->
            r
            |> should
                equal
                   "0x37fcbd4932b8a314a505e6625ab502b803aeba7a345b44cf193e260d20c2158a06cebf5321801584d319e58455f2e66b74f4f711e7de1872ef8902d150abe6781c"
        | Error err -> failwith err
    }


[<Fact(Skip="Needs an actual ethereum node")>]
let ``Should sign erc721 unwrap`` () =
    async {
        let sign =
            Crypto.memorySigner(key).Sign |> AsyncResult.bind

        let! r =
            transactionHash
                web3
                lockingContract
                erc721Params.Owner
                erc721Params.OperationId
                (erc721SafeTransferCall lockingContract erc721Params)
            |> sign

        match r with
        | Ok r ->
            r
            |> should
                equal
                   "0x652b55206a7624d0652f8472400be68543b9c84fc1ceec0cf3435106cc1220cc1ab8134e1b87123cdcec069094e33b2c438d330be16254d151a0b16da14f95fe1c"
        | Error err -> failwith err
    }