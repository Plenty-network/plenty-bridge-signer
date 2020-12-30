namespace Signer.EventStore

open Newtonsoft.Json.Linq
open Signer
open Signer.IPFS

type private Message = Append of DomainEvent * AsyncReplyChannel<EventId>

type Append<'e> = 'e -> Async<EventId>

type private MintingSignedDto =
    { level: string
      proof: ProofDto
      quorum: QuorumDto }

and ProofDto =
    { amount: string
      owner: string
      tokenId: string
      txId: string
      signature: string }

and QuorumDto =
    { quorumContract: string
      minterContract: string
      chainId: string }


type EventStoreIpfs(client: IpfsClient, head: Cid) =
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
                        txId = proof.TxId
                        signature = proof.Signature }
                  quorum =
                      { quorumContract = quorum.QuorumContract
                        minterContract = quorum.MinterContract
                        chainId = quorum.ChainId } }
                |> JObject.FromObject
            let result =  JObject()
            result.["type"] <- JValue("MintingSigned")
            result.["payload"] <- payload
            result
            
    let link (Cid value) =
            let link = JObject()
            link.Add("/", JValue(value))
            link

    let mailbox =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop (head: Cid) =
                async {
                    let! message = inbox.Receive()

                    match message with
                    | Append (e, rc) ->
                        let payload = serialize e
                        payload.["parent"] <- link head
                        let! cid = client.Dag.PutDag(payload)
                        // todo : ipns
                        match cid with
                        | Ok value -> 
                            rc.Reply (EventId 10UL)
                            return! messageLoop(value)
                        | Error err -> failwith err     
                }

            messageLoop (head))

    static member Create(client: IpfsClient) = async {
        // todo : fetch head
        ()
    }
    
    member this.Append(e: DomainEvent) =
        mailbox.PostAndAsyncReply(fun rc -> Append(e, rc))

