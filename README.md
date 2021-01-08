## Local setup

### Setup paket
`dotnet tool restore`

Generate a github developer token

Then:

`dotnet paket config add-token https://nuget.pkg.github.com/bender-labs/index.json  <your token>`
 
`dotnet paket restore`

### Requirements

For RocksDB to work on your machine, this libs must installed :
* libsnappy 
* liblz4
* libzstd

On MacOs : `brew install snappy lz4 zstd`

### Configure signing keys

For in memory signer, 
initialize local secrets repository : `cd Signer.Service && dotnet user-secrets init`
set your secret key in base58: `dotnet user-secrets set "Tezos:Signer:Key" "<your base58 key>"`

For AWS Signer:
Change the SignerType to `AWS` in app settings, and add the proper AWS configuration:
```
"AWS": {
    "Profile": "<profile you configured>",
    "Region": "<region to use>",
    "KeyId": "<KMS key id to use>"
  }
```