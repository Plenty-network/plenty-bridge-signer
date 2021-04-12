namespace Signer

open Signer
open Signer.Ethereum
open Signer.Minting
open Signer.PaymentAddress
open Signer.Unwrap


type Reply<'v>(replyf: 'v -> unit) =
    member this.Reply value = replyf value


type SignerCommand =
    | Unwrap of (bigint * UnwrapCommand)
    | Minting of EthEventLog
    | PaymentAddress of (ChangePaymentAddressParameters * Reply<ChangePaymentAddressCall>)


type ICommandBus =
    abstract Post: SignerCommand -> (EventId * DomainEvent) list DomainResult

    abstract PostAndReply: (Reply<'r> -> SignerCommand) -> ('r * (EventId * DomainEvent) list) DomainResult


[<RequireQualifiedAccess>]
module CommandBus =

    let build (minter: MinterWorkflow)
              (unwrap: UnwrapWorkflow)
              (paymentAddress: ChangePaymentAddressWorkflow)
              (append: _ Append)
              =

        let dispatch (c: SignerCommand) =
            match c with
            | Unwrap (level, c) -> unwrap level c
            | Minting (l) -> minter l
            | PaymentAddress (p, rc) ->
                paymentAddress p
                |> AsyncResult.map (fun r ->
                    rc.Reply r
                    r)
                |> AsyncResult.map (fun _ -> Noop)

        let appendMiddleware (e: DomainEvent) =
            match e with
            | Erc20MintingFailed payload ->
                dispatch (Unwrap(payload.Level, (UnwrapErc20FromWrappingError payload)))
                |> AsyncResult.map (fun e' -> [ e; e' ])

            | Erc721MintingFailed payload ->
                dispatch (Unwrap(payload.Level, (UnwrapErc721FromWrappingError payload)))
                |> AsyncResult.map (fun e' -> [ e; e' ])

            | _ -> [ e ] |> AsyncResult.retn
            |> AsyncResult.bind (fun el ->
                let rec loop acc remaining =
                    asyncResult {
                        match remaining with
                        | [] -> return acc
                        | head :: tail ->
                            let! a = append head
                            return! loop (a :: acc) tail
                    }

                loop [] el)
        { new ICommandBus with

            member this.Post(c: SignerCommand) =
                c |> dispatch |> AsyncResult.bind appendMiddleware

            member this.PostAndReply(b: Reply<'r> -> SignerCommand) =
                async {
                    let r = ref (Unchecked.defaultof<'r>)
                    let c = b (Reply(fun x -> r := x))

                    return!
                        c
                        |> dispatch
                        |> AsyncResult.bind appendMiddleware
                        |> AsyncResult.map (fun v -> (!r, v))
                } }
