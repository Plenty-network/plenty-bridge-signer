namespace Indexer.Infrastructure.BCD

open System
open System.Net.Http

type NetworkState = {
    Level: uint64
    Hash: string
    Timestamp: DateTime
}

type BcdClient(baseUrl:string, network:string) =
    
    let client =
        let r = HttpClient()
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
