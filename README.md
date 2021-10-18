# Wrap Signer

Signer node and indexer for wrap protocol

---

## Builds


GitHub Actions |
:---: |
[![.NET Core](https://github.com/bender-labs/wrap-signer/actions/workflows/build.yml/badge.svg)](https://github.com/bender-labs/wrap-signer/actions/workflows/build.yml)|
[![Build History](https://buildstats.info/github/chart/bender-labs/wrap-signer)](https://github.com/bender-labs/wrap-signer/actions?query=branch%3Amaster) |


---

### Building


```sh
$ dotnet build
```

---

### Environment Variables

- `CONFIGURATION` will set the [configuration](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x#options) of the dotnet commands.  If not set, it will default to Release.
  - `CONFIGURATION=Debug ./build.sh` will result in `-c` additions to commands such as in `dotnet build -c Debug`
- `GITHUB_TOKEN` will be used to upload release notes and NuGet packages to GitHub.
  - Be sure to set this before releasing
- `DISABLE_COVERAGE` Will disable running code coverage metrics.  AltCover can have [severe performance degradation](https://github.com/SteveGilham/altcover/issues/57) so it's worth disabling when looking to do a quicker feedback loop.
  - `DISABLE_COVERAGE=1 ./build.sh`


---

## Local setup

- [dotnet SDK](https://www.microsoft.com/net/download/core) 5.0 or higher

### Setup dotnet
```sh
dotnet tool restore
dotnet restore
```



### Configure signing keys

For in memory signer, 
- initialize local secrets repository : `cd Signer.Service && dotnet user-secrets init`
- set your tezos private key in base58: `dotnet user-secrets set "Tezos:Signer:Key" "<your base58 key>"`
- set your ethereum private key in hexa without 0x prefix: `dotnet user-secrets set "Ethereum:Signer:Key" "<your ex key>"`

For AWS Signer:
Change the SignerType to `AWS` in app settings, and add the proper AWS configuration:
```
"AWS": {
    "Profile": "<profile you configured>",
    "Region": "<region to use>",
  }
```
KeyId, in signer configuration, should be specified :
```
"Tezos": {
    "Signer":{
        "Type":"AWS",
        "KeyID":"<your key id>"
    }
}
```

### Profiles

Signer can run locally on local blockchains (flexteza + ganache),or using testnet
By default, `dotnet run --project Signer.Service` uses the local mode.
To use the testnet profile `dotnet run --project Signer.Service --launch-profile testnet`

In any case, you will need a local ipfs node. 

---


### Releasing

- Update the `CHANGELOG.md` with an "Unreleased" section containing release notes for this version, in [KeepAChangelog](https://keepachangelog.com/en/1.1.0/) format.
- Create a tag with the desired version, and push it. Github action will take care of the rest




Here's an example of adding an "Unreleased" section to a `CHANGELOG.md` with a `0.1.0` section already released.

```markdown
## [Unreleased]

### Added
- Does cool stuff!

### Fixed
- Fixes that silly oversight

## [0.1.0] - 2017-03-17
First release

### Added
- This release already has lots of features

[Unreleased]: https://github.com/user/MyCoolNewApp.git/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/user/MyCoolNewApp.git/releases/tag/v0.1.0
```

- You can then use the `Release` target, specifying the version number either in the `RELEASE_VERSION` environment
  variable, or else as a parameter after the target name.  This will:
  - update `CHANGELOG.md`, moving changes from the `Unreleased` section into a new `0.2.0` section
    - if there were any prerelease versions of 0.2.0 in the changelog, it will also collect their changes into the final 0.2.0 entry
  - make a commit bumping the version:  `Bump version to 0.2.0` and adds the new changelog section to the commit's body
  - push a git tag
  - create a GitHub release for that git tag
  
