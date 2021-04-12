namespace Signer.EventStore

open Newtonsoft.Json.Linq
open Signer
open Signer.IPFS
open FSharpx.Control

type private Message =
    | Append of DomainEvent * AsyncReplyChannel<Result<EventId * DomainEvent, string>>
    | GetHead of AsyncReplyChannel<Cid option>
    | GetKey of AsyncReplyChannel<Result<IpfsKey, string>>

type EventStoreState =
    abstract PutHead: Cid -> unit
    abstract GetHead: unit -> Cid option


type EventStoreIpfs(client: IpfsClient, state: EventStoreState, keyName: string) =

    let toFact =
        function
        | Burn -> "Burn"
        | MintingError -> "MintingError"

    let key = client.Key.Find(keyName)

    let toJson (eventType: string) payload =
        let result = JObject()
        result.["type"] <- JValue(eventType)
        result.["payload"] <- JObject.FromObject(payload)
        result

    let toErc20MintingDto ({ Level = level
                             TransactionHash = tx
                             Call = { Quorum = quorum
                                      Signature = signature
                                      SignerAddress = address
                                      Parameters = p } }: ErcMint<Erc20MintingParameters>) =
        { level = level.ToString()
          observedFact = "Lock"
          signature = signature
          signerAddress = address
          transactionHash = tx
          parameters =
              { amount = p.Amount.ToString()
                owner = p.Owner.Value
                erc20 = p.Erc20
                blockHash = p.EventId.BlockHash
                logIndex = p.EventId.LogIndex }
          quorum =
              { quorumContract = quorum.QuorumContract.Value
                minterContract = quorum.MinterContract.Value
                chainId = quorum.ChainId } }

    let toErc721MintingDto ({ Level = level
                              TransactionHash = tx
                              Call = { Quorum = quorum
                                       Signature = signature
                                       SignerAddress = address
                                       Parameters = p } }: ErcMint<Erc721MintingParameters>) =
        { level = level.ToString()
          signature = signature
          observedFact = "Lock"
          signerAddress = address
          transactionHash = tx
          parameters =
              { tokenId = p.TokenId.ToString()
                owner = p.Owner.Value
                erc721 = p.Erc721
                blockHash = p.EventId.BlockHash
                logIndex = p.EventId.LogIndex }
          quorum =
              { quorumContract = quorum.QuorumContract.Value
                minterContract = quorum.MinterContract.Value
                chainId = quorum.ChainId } }

    let toErc20UnwrapDto ({ Level = level
                            ObservedFact = fact
                            Call = { Signature = signature
                                     SignerAddress = address
                                     LockingContract = lockingContract
                                     Parameters = p } }: ErcUnwrap<Erc20UnwrapParameters>) =
        { level = level.ToString()
          observedFact = toFact fact
          signature = signature
          signerAddress = address
          lockingContract = lockingContract
          parameters =
              { erc20 = p.ERC20
                amount = p.Amount.ToString()
                owner = p.Owner
                operationId = p.OperationId } }

    let toErc721UnwrapDto ({ Level = level
                             ObservedFact = fact
                             Call = { Signature = signature
                                      SignerAddress = address
                                      LockingContract = lockingContract
                                      Parameters = p } }: ErcUnwrap<Erc721UnwrapParameters>) =
        { level = level.ToString()
          signature = signature
          observedFact = toFact fact
          signerAddress = address
          lockingContract = lockingContract
          parameters =
              { erc721 = p.ERC721
                tokenId = p.TokenId.ToString()
                owner = p.Owner
                operationId = p.OperationId } }

    let toErc20MintingErrorDto ({ Level = level
                                  SignerAddress = address
                                  TransactionHash = txHash
                                  Reason = reason
                                  EventId = eventId
                                  Payload = p }: ErcMintError<Erc20MintingError>) =
        { level = level.ToString()
          transactionHash = txHash
          signerAddress = address
          reason = reason
          payload =
              { amount = p.Amount.ToString()
                owner = p.Owner
                erc20 = p.ERC20
                blockHash = eventId.BlockHash
                logIndex = eventId.LogIndex } }

    let toErc721MintingErrorDto ({ Level = level
                                   SignerAddress = address
                                   TransactionHash = txHash
                                   Reason = reason
                                   EventId = eventId
                                   Payload = p }: ErcMintError<Erc721MintingError>) =
        { level = level.ToString()
          transactionHash = txHash
          signerAddress = address
          reason = reason

          payload =
              { tokenId = p.TokenId.ToString()
                owner = p.Owner
                erc721 = p.ERC721
                blockHash = eventId.BlockHash
                logIndex = eventId.LogIndex } }

    let serialize =
        function

        | Erc20MintingSigned (e) ->
            e
            |> toErc20MintingDto
            |> toJson "Erc20MintingSigned"

        | Erc721MintingSigned (e) ->
            e
            |> toErc721MintingDto
            |> toJson "Erc721MintingSigned"

        | Erc20UnwrapSigned (e) ->
            e
            |> toErc20UnwrapDto
            |> toJson "Erc20UnwrapSigned"


        | Erc721UnwrapSigned (e) ->
            e
            |> toErc721UnwrapDto
            |> toJson "Erc721UnwrapSigned"

        | Erc20MintingFailed e ->
            e
            |> toErc20MintingErrorDto
            |> toJson "Erc20MintingFailed"

        | Erc721MintingFailed e ->
            e
            |> toErc721MintingErrorDto
            |> toJson "Erc721MintingFailed"

    let link (Cid value) =
        let link = JObject()
        link.Add("/", JValue(value))
        link

    let append event (head: Cid option) =
        asyncResult {
            let payload = serialize event

            if head.IsSome then payload.["parent"] <- link head.Value

            let! cid = client.Dag.PutDag(payload)
            state.PutHead cid
            return cid
        }

    let publish (cid) =
        asyncResult {
            let! key = key

            let r =
                match cid with
                | Some v ->
                    client.Name.Publish(v, key = key.Name)
                    |> AsyncResult.map (fun v -> v.Name)
                | None -> AsyncResult.ofSuccess key.Name

            return! r
        }

    let mailbox =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop (head: Cid option) =
                async {
                    let! message = inbox.Receive()

                    match message with
                    | Append (e, rc) ->
                        let! cid = append e head

                        match cid with
                        | Ok v ->
                            rc.Reply(Ok(EventId(Cid.value v), e))
                            do! messageLoop (Some v)
                        | Result.Error err -> rc.Reply(Result.Error err)
                    | GetHead rc ->
                        rc.Reply head
                        do! messageLoop head
                    | GetKey rc ->
                        let! key = key

                        match key with
                        | Ok v -> rc.Reply(Ok v)
                        | Result.Error _ as err -> rc.Reply(err)

                        do! messageLoop head

                }

            (messageLoop (state.GetHead()))
            |> Async.map (fun _ -> ()))

    static member Create(client: IpfsClient, keyName: string, state: EventStoreState) =
        EventStoreIpfs(client, state, keyName)

    member this.Append(e: DomainEvent) =
        mailbox.PostAndAsyncReply(fun rc -> Append(e, rc))

    member this.Publish() =
        async {
            let! head = mailbox.PostAndAsyncReply(GetHead)
            return! publish head
        }

    member this.GetKey() =
        async {
            let! key = mailbox.PostAndAsyncReply(GetKey)
            return key
        }
