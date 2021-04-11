namespace Signer.Tezos

open System
open System.IO
open Amazon.KeyManagementService
open Amazon.KeyManagementService.Model
open Azure.Core
open Azure.Security.KeyVault.Keys
open Azure.Security.KeyVault.Keys.Cryptography
open Org.BouncyCastle.Crypto.Digests
open Signer
open Netezos.Keys
open FSharpx.Control

[<RequireQualifiedAccess>]
module Crypto =

    let memorySigner (k: Key) =
        { new TezosSigner with
            member this.PublicAddress() = k.PubKey |> AsyncResult.retn

            member this.Sign bytes = bytes |> k.Sign |> AsyncResult.retn }

    let awsSigner (client: IAmazonKeyManagementService) (keyId: string) =

        let keyQuery =
            let request = GetPublicKeyRequest()
            request.KeyId <- keyId

            client.GetPublicKeyAsync(request)
            |> Async.AwaitTask
            |> Async.map (fun r -> Keys.Secp256k1.keyFromSpki (r.PublicKey.ToArray()))
            |> Async.Cache
            |> AsyncResult.catchAsync
        { new TezosSigner with
            member this.PublicAddress() = keyQuery


            member this.Sign bytes =
                let request = SignRequest()
                request.KeyId <- keyId
                request.SigningAlgorithm <- SigningAlgorithmSpec.ECDSA_SHA_256
                request.MessageType <- MessageType.DIGEST
                let b2 = Blake2bDigest(256)

                b2.BlockUpdate(bytes, 0, bytes.Length)
                let buffer = Array.zeroCreate (b2.GetDigestSize())
                b2.DoFinal(buffer, 0) |> ignore
                request.Message <- new MemoryStream(buffer)

                client.SignAsync(request)
                |> Async.AwaitTask
                |> Async.map (fun r -> Keys.Secp256k1.signatureFromDer (r.Signature.ToArray()))
                |> AsyncResult.catchAsync }

    let azureSigner (credential: TokenCredential) (vault: Uri) (keyId: string) =
        let keyClient = KeyClient(vault, credential)

        let keyQuery =
            keyClient.GetKeyAsync(keyId)
            |> Async.AwaitTask
            |> Async.map (fun v -> v.Value)
            |> Async.Cache
        { new TezosSigner with
            member this.PublicAddress() =
                keyQuery
                |> Async.map (fun v -> Keys.Secp256k1.keyFromJsonWebKey v.Key)
                |> AsyncResult.catchAsync

            member this.Sign bytes =
                async {
                    let b2 = Blake2bDigest(256)

                    b2.BlockUpdate(bytes, 0, bytes.Length)
                    let buffer = Array.zeroCreate (b2.GetDigestSize())
                    b2.DoFinal(buffer, 0) |> ignore
                    let! keyBundle = keyQuery

                    let! signResult =
                        CryptographyClient(keyBundle.Id, credential)
                            .SignAsync(SignatureAlgorithm.ES256K, buffer)
                        |> Async.AwaitTask

                    return Keys.Secp256k1.signatureFromComponents signResult.Signature
                }
                |> AsyncResult.catchAsync }
