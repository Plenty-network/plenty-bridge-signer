module Signer.AddToken

open Signer.Tezos

type AddToken =
    | Fungible of AddFungibleTokenParameters
    | NonFungible of AddNftParameters

type AddTokenWorkflow = AddToken -> DomainResult<string>


let workflow (signer: TezosSigner) (target: Quorum) : AddTokenWorkflow =

    let packAndSign p =
        match p with
        | Fungible v -> Multisig.packAddFungibleToken target v
        | NonFungible v -> Multisig.packAddNft target v
        |> AsyncResult.ofResult
        |> AsyncResult.bind signer.Sign

    fun (p: AddToken) ->
        packAndSign p
        |> AsyncResult.map (fun s -> s.ToBase58())
