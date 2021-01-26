namespace Signer.EventStore

open Newtonsoft.Json.Linq
open Signer
open Signer.IPFS

type private Message =
    | Append of DomainEvent * AsyncReplyChannel<Result<EventId * DomainEvent, string>>
    | GetHead of AsyncReplyChannel<Cid option>

type EventStoreState =
    abstract PutHead: Cid -> unit
    abstract GetHead: unit -> Cid option

type ProofDto =
    { amount: string
      owner: string
      tokenId: string
      txId: string
      signature: string }

type MintingSignedDto =
    { level: string
      proof: ProofDto
      quorum: QuorumDto }

and QuorumDto =
    { quorumContract: string
      minterContract: string
      chainId: string }

type UnwrapSignedDto =
    { level: string
      proof: ProofDto
      lockingContract: string }

type EventStoreIpfs(client: IpfsClient, state: EventStoreState, key: IpfsKey) =
    let serialize =
        function
        | MintingSigned { Level = level
                          Proof = proof
                          Quorum = quorum } ->
            let payload =
                { level = level.ToString()
                  proof =
                      { amount = proof.Amount.ToString()
                        owner = proof.Owner
                        tokenId = proof.TokenId
                        txId = proof.OperationId
                        signature = proof.Signature }
                  quorum =
                      { quorumContract = quorum.QuorumContract
                        minterContract = quorum.MinterContract
                        chainId = quorum.ChainId } }
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("MintingSigned")
            result.["payload"] <- payload
            result
        | UnwrapSigned { Level = level
                         Proof = proof
                         Quorum = quorum } ->
            let payload =
                { level = level.ToString()
                  proof =
                      { amount = proof.Amount.ToString()
                        owner = proof.Owner
                        tokenId = proof.TokenId
                        txId = proof.OperationId
                        signature = proof.Signature }
                  lockingContract = quorum.LockingContract }
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("UnwrapSigned")
            result.["payload"] <- payload
            result

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
                        | Error err -> rc.Reply(Error err)
                    | GetHead rc ->
                        rc.Reply head
                        do! messageLoop head

                }

            (messageLoop (state.GetHead()))
            |> Async.map (fun _ -> ()))

    static member Create(client: IpfsClient, keyName: string, state: EventStoreState) =
        asyncResult {
            let! keys = client.Key.List()

            let! key =
                keys
                |> Seq.tryFind (fun k -> k.Name = keyName)
                |> AsyncResult.ofOption "Key not found"

            return EventStoreIpfs(client, state, key)
        }

    member this.Append(e: DomainEvent) =
        mailbox.PostAndAsyncReply(fun rc -> Append(e, rc))

    member this.Publish() =
        async {
            let! head = mailbox.PostAndAsyncReply(GetHead)
            return! publish head
        }
