namespace Signer.IPFS

open System.Text.RegularExpressions


type Cid = Cid of string

[<RequireQualifiedAccess>]
module Cid =
    let value (Cid v) = v

type IpfsKey = { Name: string; Id: string }

type NamePublish = { Name: string; Value: string }

type IpfsAddress = { Protocol: string; Path: string }

module IpfsAddress =

    let fromString (v: string) =
        let m = Regex.Match(v, "\/([^\/]*)\/(.*)")
        if m.Success then
            Ok { Protocol = m.Groups.[1].Value
                 Path = m.Groups.[2].Value }
        else Error $"Invalid ipfs address : %s{v}"
        

        
