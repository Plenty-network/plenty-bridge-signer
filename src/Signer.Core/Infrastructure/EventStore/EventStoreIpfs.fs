namespace Signer.EventStore

open Newtonsoft.Json.Linq
open Signer
open Signer.IPFS

type private Message =
    | Append of DomainEvent * AsyncReplyChannel<Result<EventId * DomainEvent, string>>
    | GetHead of AsyncReplyChannel<Cid option>
    | GetKey of AsyncReplyChannel<IpfsKey>

type EventStoreState =
    abstract PutHead: Cid -> unit
    abstract GetHead: unit -> Cid option

[<CLIMutable>]
type Erc20ParametersDto =
    { amount: string
      owner: string
      erc20: string
      blockHash: string
      logIndex: bigint }

[<CLIMutable>]
type Erc721ParametersDto =
    { tokenId: string
      owner: string
      erc721: string
      blockHash: string
      logIndex: bigint }

[<CLIMutable>]
type QuorumDto =
    { quorumContract: string
      minterContract: string
      chainId: string }

[<CLIMutable>]
type ErcMintDto<'T> =
    { level: string
      transactionHash: string
      parameters: 'T
      signerAddress: string
      signature: string
      quorum: QuorumDto }

[<CLIMutable>]
type ErcUnwrapDto<'T> =
    { level: string
      parameters: 'T
      signerAddress: string
      signature: string
      lockingContract: string }

[<CLIMutable>]
type Erc20UnwrapParametersDto =
    { amount: string
      owner: string
      erc20: string
      operationId: string }

[<CLIMutable>]
type Erc721UnwrapParametersDto =
    { tokenId: string
      owner: string
      erc721: string
      operationId: string }

type EventStoreIpfs(client: IpfsClient, state: EventStoreState, key: IpfsKey) =
    let serialize =
        function
        | Erc20MintingSigned { Level = level
                               TransactionHash = tx
                               Call = { Quorum = quorum
                                        Signature = signature
                                        SignerAddress = address
                                        Parameters = p } } ->
            let payload =
                { level = level.ToString()
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
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("Erc20MintingSigned")
            result.["payload"] <- payload
            result
        | Erc721MintingSigned { Level = level
                                TransactionHash = tx
                                Call = { Quorum = quorum
                                         Signature = signature
                                         SignerAddress = address
                                         Parameters = p } } ->
            let payload =
                { level = level.ToString()
                  signature = signature
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
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("Erc721MintingSigned")
            result.["payload"] <- payload
            result
        | Erc20UnwrapSigned { Level = level
                              Call = { Signature = signature
                                       SignerAddress = address
                                       LockingContract = lockingContract
                                       Parameters = p } } ->
            let payload =
                { level = level.ToString()
                  signature = signature
                  signerAddress = address 
                  lockingContract = lockingContract
                  parameters =
                      { erc20 = p.ERC20
                        amount = p.Amount.ToString()
                        owner = p.Owner
                        operationId = p.OperationId } }
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("Erc20UnwrapSigned")
            result.["payload"] <- payload
            result
        | Erc721UnwrapSigned { Level = level
                               Call = { Signature = signature
                                        SignerAddress = address
                                        LockingContract = lockingContract
                                        Parameters = p } } ->
            let payload =
                { level = level.ToString()
                  signature = signature
                  signerAddress = address
                  lockingContract = lockingContract
                  parameters =
                      { erc721 = p.ERC721
                        tokenId = p.TokenId.ToString()
                        owner = p.Owner
                        operationId = p.OperationId } }
                |> JObject.FromObject

            let result = JObject()
            result.["type"] <- JValue("Erc721UnwrapSigned")
            result.["payload"] <- payload
            result

    let link (Cid value) =
        let link = JObject()
        link.Add("/", JValue(value))
        link

    let append event (head: Cid option) =
        asyncResult {
            let payload = serialize event

            if head.IsSome then
                payload.["parent"] <- link head.Value

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
        MailboxProcessor.Start
            (fun inbox ->
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
                        | GetKey rc ->
                            rc.Reply key
                            do! messageLoop head

                    }

                (messageLoop (state.GetHead()))
                |> Async.map (fun _ -> ()))

    static member Create(client: IpfsClient, keyName: string, state: EventStoreState) =
        asyncResult {
            let! key = client.Key.Find(keyName)
            return EventStoreIpfs(client, state, key)
        }

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
