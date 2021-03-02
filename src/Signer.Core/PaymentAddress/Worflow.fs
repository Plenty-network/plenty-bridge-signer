module Signer.PaymentAddress

open Signer.Tezos
open FSharpx.Control

type ChangePaymentAddressCall =
    { Signature: string
      Quorum: Quorum
      Parameters: ChangePaymentAddressParameters }

let private toChangePaymentAddressCall
    (target: Quorum)
    (p: ChangePaymentAddressParameters)
    (signature: TezosSignature)
    =
    { Signature = signature.ToBase58()
      Quorum = target
      Parameters = p }

type ChangePaymentAddressWorkflow = ChangePaymentAddressParameters -> DomainResult<ChangePaymentAddressCall>

let workflow (signer: TezosSigner) (target: Quorum) : ChangePaymentAddressWorkflow =
    let packAndSign =
        Multisig.packChangePaymentAddress target
        >> AsyncResult.ofResult
        >> AsyncResult.bind signer.Sign

    let toChangePaymentAddressCall = toChangePaymentAddressCall target

    fun (p: ChangePaymentAddressParameters) ->
        let toChangePaymentAddressCall = toChangePaymentAddressCall p

        packAndSign p
        |> AsyncResult.map toChangePaymentAddressCall
