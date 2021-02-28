[<RequireQualifiedAccess>]
module Signer.Ethereum.Crypto

open System
open System.IO
open Amazon.KeyManagementService
open Amazon.KeyManagementService.Model
open Azure.Core
open Azure.Security.KeyVault.Keys
open Azure.Security.KeyVault.Keys.Cryptography
open FSharpx
open Nethereum.Signer
open Nethereum.Signer.Crypto
open Signer
open FSharpx.Control

let private messageSigner = EthereumMessageSigner()

let private findV signature hash pubKey =
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

let azureSigner (credential: TokenCredential) (vault: Uri) (keyId: string) =
    let keyClient = KeyClient(vault, credential)

    let keyQuery =
        keyClient.GetKeyAsync(keyId)
        |> Async.AwaitTask
        |> Async.map (fun v -> v.Value)
        |> Async.Cache

    let ethKey =
        keyQuery
        |> Async.map (fun v -> Keys.fromJsonWebKey v.Key)

    { new EthereumSigner with
        member this.Sign(p) =
            async {
                let bytes = messageSigner.HashPrefixedMessage(p)
                let! keyBundle = keyQuery
                let! ethKey = ethKey

                let! signResult =
                    CryptographyClient(keyBundle.Id, credential)
                        .SignAsync(SignatureAlgorithm.ES256K, bytes)
                    |> Async.AwaitTask

                let ecdsa =
                    ECDSASignatureFactory
                        .FromComponents(signResult.Signature)
                        .MakeCanonical()

                let signature = ECDSASignature.FromDER(ecdsa.ToDER())

                let ethSig =
                    EthECDSASignature.FromDER(signature.ToDER())

                let v =
                    findV signature bytes (ethKey.GetPubKey())

                ethSig.V <- v
                return (EthECDSASignature.CreateStringSignature(ethSig))
            }
            |> AsyncResult.catchAsync

        member this.PublicAddress() =
            ethKey
            |> Async.map (fun v -> v.GetPublicAddress())
            |> AsyncResult.catchAsync }

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
        |> Async.Cache

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
