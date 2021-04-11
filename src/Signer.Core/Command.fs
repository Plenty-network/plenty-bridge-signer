namespace Signer

open Signer
open Signer.Ethereum
open Signer.Minting
open Signer.PaymentAddress
open Signer.Unwrap

type Command<'p, 'r> = { Parameter: 'p }


type Reply<'v>(replyf: 'v -> unit) =
    member this.Reply value = replyf value


type SignerCommand =
    | Unwrap of (bigint * UnwrapCommand)
    | Minting of EthEventLog
    | PaymentAddress of (ChangePaymentAddressParameters * Reply<ChangePaymentAddressCall>)


type ICommandBus =
    abstract Post: SignerCommand -> unit DomainResult

    abstract PostAndReply: (Reply<'r> -> SignerCommand) -> 'r DomainResult

type CommandBus(minter: MinterWorkflow,
                unwrap: UnwrapWorkflow,
                paymentAddress: ChangePaymentAddressWorkflow,
                append: _ Append) =

    let dispatch (c: SignerCommand) =
        let append = AsyncResult.bind append

        match c with
        | Unwrap (level, c) ->
            unwrap level c
            |> append
            |> AsyncResult.map (fun _ -> ())
        | Minting (l) ->
            minter l
            |> append
            |> AsyncResult.map (fun _ -> ())
        | PaymentAddress (p, rc) ->
            paymentAddress p
            |> AsyncResult.map (fun r ->
                rc.Reply r
                r)
            |> AsyncResult.map (fun _ -> ())

    interface ICommandBus with

        member this.Post(c: SignerCommand) =
            c |> dispatch |> AsyncResult.map (fun _ -> ())

        member this.PostAndReply(b: Reply<'r> -> SignerCommand) =
            async {
                let r = ref (Unchecked.defaultof<'r>)
                let c = b (Reply(fun x -> r := x))
                return! c |> dispatch |> AsyncResult.map (fun _ -> !r)
            }



[<RequireQualifiedAccess>]
module CommandBus =

    let build (minter: MinterWorkflow)
              (unwrap: UnwrapWorkflow)
              (paymentAddress: ChangePaymentAddressWorkflow)
              (append: _ Append)
              =
        CommandBus(minter, unwrap, paymentAddress, append)
