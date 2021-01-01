namespace Signer.IPFS

open System
open System.Globalization
open System.IO
open System.Net.Http
open System.Text
open System.Threading.Tasks
open System.Web
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization
open FSharp.Data

type private CidResponse = JsonProvider<""" { "Cid": {"/": "cid"}} """>

type private KeysResponse =
    JsonProvider<"""{"Keys":[{"Name":"self","Id":"k51qzi5uqu5dhuc1pto6x98woksrqgwhq6d1lff2hfymxmlk4qd7vqgtf980yl"}]}""">

type private NamePublishResponse =
    JsonProvider<"""{"Name":"k51qzi5uqu5dhuc1pto6x98woksrqgwhq6d1lff2hfymxmlk4qd7vqgtf980yl","Value":"/ipfs/natinert"}""">

type private NameResolveResponse = JsonProvider<"""{"Path":"/ipfs/v"}""">

module private Http =
    let serializer =
        JsonSerializer.Create
            (JsonSerializerSettings
                (ContractResolver = DefaultContractResolver(NamingStrategy = SnakeCaseNamingStrategy())))

    let jsonToStream obj (stream: MemoryStream) =
        use sw =
            new StreamWriter(stream, UTF8Encoding(false), 1024, true)

        use jtw = new JsonTextWriter(sw)
        serializer.Serialize(jtw, obj)
        jtw.Flush()

    let streamContent obj =
        let stream = new MemoryStream()
        jsonToStream obj stream
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        new StreamContent(stream)

    let multiPart obj =
        let content = streamContent obj

        let form =
            new MultipartFormDataContent("Upload----"
                                         + DateTime.Now.ToString(CultureInfo.InvariantCulture))

        form.Add content
        form

    let uriBuilder (baseUrl: string) path =
        let result = UriBuilder(baseUrl)
        result.Path <- sprintf "api/v0%s" path
        result

    let mapResponse (mapper: string -> 't) (r: Task<HttpResponseMessage>) =
        asyncResult {
            let! response =
                r
                |> Async.AwaitTask
                |> Async.Catch
                |> Async.map (fun v ->
                    match v with
                    | Choice1Of2 v -> if not v.IsSuccessStatusCode then Error v.ReasonPhrase else Ok v
                    | Choice2Of2 err -> Error err.Message)

            let! body =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> AsyncResult.ofAsync

            return! mapper body |> AsyncResult.ofSuccess
        }

type Key(client: HttpClient) =

    member this.List() =
        asyncResult {
            let! response =
                client.PostAsync("key/list?l=false", new StringContent(""))
                |> Http.mapResponse KeysResponse.Parse

            let v =
                response.Keys
                |> Seq.map (fun v -> { Name = v.Name; Id = v.Id })
                |> Seq.toList

            return v
        }


type Name(client: HttpClient) =
    member this.Publish((Cid cid), ?key: string, ?lifetime: string, ?ttl: string) =
        asyncResult {
            let query = HttpUtility.ParseQueryString ""
            query.["arg"] <- cid
            query.["key"] <- defaultArg key "self"
            query.["lifetime"] <- defaultArg lifetime "24h"
            query.["ttl"] <- (defaultArg ttl "0m")

            let! response =
                client.PostAsync("name/publish?" + query.ToString(), new StringContent(""))
                |> Http.mapResponse NamePublishResponse.Parse

            return
                { Name = response.Name
                  Value = response.Value }
        }

    member this.Resolve(path) =
        asyncResult {
            let query = HttpUtility.ParseQueryString ""
            query.["arg"] <- path

            let! response =
                client.PostAsync("name/resolve?" + query.ToString(), new StringContent(""))
                |> Http.mapResponse NameResolveResponse.Parse

            return!
                IpfsAddress.fromString response.Path
                |> AsyncResult.ofResult
        }

type Dag(client: HttpClient) =
    member this.PutDag<'T>(data: JObject, ?format: string, ?encoding: string, ?pin: bool) =
        asyncResult {
            let query = HttpUtility.ParseQueryString ""
            query.["format"] <- defaultArg format "dag-cbor"
            query.["input-enc"] <- defaultArg encoding "json"
            query.["pin"] <- (defaultArg pin true)
                .ToString()
                .ToLowerInvariant()

            let! cid =
                client.PostAsync("dag/put?" + query.ToString(), Http.multiPart data)
                |> Http.mapResponse CidResponse.Parse

            let v = cid.Cid.``/``
            return! Cid v |> AsyncResult.ofSuccess

        }

type IpfsClient(baseUrl: string) =

    let client =
        let c = new HttpClient()

        if not (baseUrl.EndsWith("/"))
        then c.BaseAddress <- Uri(baseUrl + "/api/v0/")
        else c.BaseAddress <- Uri(baseUrl + "api/v0/")

        c

    let dag = Dag(client)
    let key = Key(client)

    let name = Name(client)
    member this.Dag = dag
    member this.Key = key
    member this.Name = name
