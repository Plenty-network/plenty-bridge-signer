namespace Signer.Tezos

open System.IO
open Amazon.KeyManagementService
open Amazon.KeyManagementService.Model
open Org.BouncyCastle.Crypto.Digests
open Signer
open Netezos.Keys

[<RequireQualifiedAccess>]
module Signer =

    let memorySigner (k: Key) =
        { new TezosSigner with
            member this.PublicAddress() = k.PubKey |> AsyncResult.retn

            member this.Sign bytes = bytes |> k.Sign |> AsyncResult.retn }

    let awsSigner (client: IAmazonKeyManagementService) (keyId: string) =
        { new TezosSigner with
            member this.PublicAddress() =
                let request = GetPublicKeyRequest()
                request.KeyId <- keyId

                client.GetPublicKeyAsync(request)
                |> Async.AwaitTask
                |> Async.map (fun r -> Crypto.Secp256k1.keyFromSpki (r.PublicKey.ToArray()))
                |> AsyncResult.catchAsync

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
                |> Async.map (fun r -> Crypto.Secp256k1.signatureFromDer (r.Signature.ToArray()))
                |> AsyncResult.catchAsync }
