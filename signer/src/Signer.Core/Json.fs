[<RequireQualifiedAccess>]
module Signer.Json

open Newtonsoft.Json

let serialize obj = JsonConvert.SerializeObject obj


