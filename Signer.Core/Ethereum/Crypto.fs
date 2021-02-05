[<RequireQualifiedAccess>]
module Signer.Ethereum.Crypto

open System.IO
open Amazon.KeyManagementService
open Amazon.KeyManagementService.Model
open FSharpx
open Nethereum.Signer
open Nethereum.Signer.Crypto
open Nichelson
open Signer

let private messageSigner = EthereumMessageSigner()

let awsSigner (client: IAmazonKeyManagementService) (keyId: string) =

    let publicKey =
        async {
            let request = GetPublicKeyRequest()
            request.KeyId <- keyId

            let! key =
                client.GetPublicKeyAsync(request)
                |> Async.AwaitTask

            return Keys.fromSpki (key.PublicKey.ToArray())
        }

    let findV signature hash pubKey =
        let ecKey =
            ECKey.RecoverFromSignature(0, signature, hash, false)

        let recovered = ecKey.GetPubKey(false)

        if recovered = pubKey then
            [| 27uy |]
        else
            let ecKey =
                ECKey.RecoverFromSignature(1, signature, hash, false)

            let recovered = ecKey.GetPubKey(false)
            if recovered = pubKey then [| 28uy |] else failwith "No value for V found"
    { new EthereumSigner with

        member this.Sign(p) =
            async {
                let bytes = messageSigner.HashPrefixedMessage(p)
                let request = SignRequest()
                request.KeyId <- keyId
                request.SigningAlgorithm <- SigningAlgorithmSpec.ECDSA_SHA_256
                request.MessageType <- MessageType.DIGEST
                request.Message <- new MemoryStream(bytes)

                let! myKey = publicKey
                let! r = client.SignAsync(request) |> Async.AwaitTask

                let signature =
                    ECDSASignature
                        .FromDER(r.Signature.ToArray())
                        .MakeCanonical()

                let ethSig =
                    EthECDSASignature.FromDER(signature.ToDER())

                let v =
                    findV signature bytes (myKey.GetPubKey())

                ethSig.V <- v
                return (EthECDSASignature.CreateStringSignature(ethSig))
            }
            |> AsyncResult.catchAsync


        member this.PublicAddress() =
            publicKey
            |> Async.map (fun k -> k.GetPublicAddress())
            |> AsyncResult.catchAsync }

let memorySigner (key: EthECKey) =
    { new EthereumSigner with

        member this.Sign(p) =
            messageSigner.Sign(p, key)
            |> AsyncResult.ofSuccess

        member this.PublicAddress() =
            key.GetPublicAddress() |> AsyncResult.ofSuccess }
