namespace Indexer.Infrastructure.BCD

open System
open System.Net.Http

type NetworkState = {
    Level: uint64
    Hash: string
    Timestamp: DateTime
}

type QuorumOwner = {
    IPNSPeerId: string
    PublicKey: string
}

type BcdClient(baseUrl:string, network:string) =
    
    let client =
        let r = new HttpClient()
        r.BaseAddress <- Uri(baseUrl)
        r
    
    
    member this.GetCurrentState() = async {
        let! response = client.GetAsync("/v1/stats") |> Async.AwaitTask
        if not response.IsSuccessStatusCode then failwith "Bad"
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let value = StatsResponse.Parse(content)
        let stat =
                   value
                   |> Array.find (fun e -> e.Network = network)
        return {
            Level = uint64 stat.Level
            Hash = stat.Hash
            Timestamp = stat.Timestamp.Date
        }
    }
    
    member this.GetQuorumOwners(address: string, level: uint64) = async {
        let! response = client.GetAsync(sprintf "/v1/contract/%s/%s/storage?level=%i" network address level ) |> Async.AwaitTask
        if not response.IsSuccessStatusCode then failwith "Bad"
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let value = StorageResponse.Parse(content)
        let signers =
                      value.Children
                      |> Array.find(fun e -> e.Name = "signers")
        return signers.Children
                |> Array.map(fun e -> {
                    IPNSPeerId = e.Name
                    PublicKey = e.Value
                })              
    }
