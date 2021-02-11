module Signer.``Ethereum crypto utils test``

open System.IO
open System.Text
open Amazon
open Amazon.KeyManagementService
open Xunit
open FsUnit.Xunit
open Signer.Ethereum

let getKey =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)
        return Keys.fromSpki bytes
    }

[<Fact>]
let ``Should import from der x509`` () =
    async {
        use pub = File.OpenRead("./sample/pub.der")
        let! bytes = pub.AsyncRead(int pub.Length)

        let key = Keys.fromSpki bytes

        key.GetPublicAddress()
        |> should equal "0x8F0FD9fbcc57ABE302C2b2b90459c498c5fc7bDF"

    }
    
[<Fact(Skip="Needs aws credentials")>]
let ``Should extract ethereum address from aws``() = async {
    let client = new AmazonKeyManagementServiceClient(RegionEndpoint.EUCentral1)
    let signer = Crypto.awsSigner client "aaab9b9c-606f-4c5a-a284-ca7cceff5133"
    
    let! key = signer.PublicAddress()
    
    match key with
    | Ok v -> v |> should equal "0x8F0FD9fbcc57ABE302C2b2b90459c498c5fc7bDF"
    | Error err -> failwith err
}

[<Fact(Skip="Cannot be stable. Nonce, etc.")>]
let ``Should sign using aws``() = async {
    let client = new AmazonKeyManagementServiceClient(RegionEndpoint.EUCentral1)
    let signer = Crypto.awsSigner client "aaab9b9c-606f-4c5a-a284-ca7cceff5133"
    
    let! signature = signer.Sign(Encoding.UTF8.GetBytes ("Coucou"))
    
    match signature with
    | Ok v -> v |> should equal "0x9fa1a5f02cd8652ad6dd6a8c11105eb958f7d29baf324276945087ccfa31fece16528416f9363546949a74780c2a3c080a9e16090663e324ec2a4bb162beba781b"
    | Error err -> failwith err
}