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
