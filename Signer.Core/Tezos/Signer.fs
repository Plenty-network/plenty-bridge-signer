namespace Signer.Tezos

open Signer
open Netezos.Keys
open Netezos.Ledger

module Signer =
    let memorySigner (k: Key): Signer =
        fun (p: byte []) ->
            k.Sign(p)
            |> AsyncResult.retn

    let ledgerSigner (ledger: TezosLedgerClient): Signer =
        fun (p: byte []) ->
            ledger.Sign(p)
            |> Async.AwaitTask
            |> Async.map Ok
            |> AsyncResult.catch (fun err -> err.Message)

    let ledgerKey =
        async {
            let! ledger = Ledger.Client.get () |> Async.AwaitTask

            return!
                ledger.GetPublicKeyAsync(ECKind.NistP256)
                |> Async.AwaitTask
        // return! ledger.GetPublicKeyAsync() |> Async.AwaitTask
    }