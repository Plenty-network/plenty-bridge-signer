# Wrap Signer

Signer node and indexer for wrap protocol

---

## Builds


GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/bender-labs/wrap-signer/workflows/Build%20master/badge.svg)](https://github.com/bender-labs/wrap-signer/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/bender-labs/wrap-signer)](https://github.com/bender-labs/wrap-signer/actions?query=branch%3Amaster) |


---

### Building


```sh
> build.cmd <optional buildtarget> // on windows
$ ./build.sh  <optional buildtarget>// on unix
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
running `build.sh` will restore everything the first time around


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

- [Create a GitHub OAuth Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
  - You can then set the `GITHUB_TOKEN` to upload release notes and artifacts to github
  - Otherwise it will fallback to username/password

- Then update the `CHANGELOG.md` with an "Unreleased" section containing release notes for this version, in [KeepAChangelog](https://keepachangelog.com/en/1.1.0/) format.


NOTE: Its highly recommend to add a link to the Pull Request next to the release note that it affects. The reason for this is when the `RELEASE` target is run, it will add these new notes into the body of git commit. GitHub will notice the links and will update the Pull Request with what commit referenced it saying ["added a commit that referenced this pull request"](https://github.com/bender-labs/wrap-signer/pull/179#ref-commit-837ad59). Since the build script automates the commit message, it will say "Bump Version to x.y.z". The benefit of this is when users goto a Pull Request, it will be clear when and which version those code changes released. Also when reading the `CHANGELOG`, if someone is curious about how or why those changes were made, they can easily discover the work and discussions.

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


macOS/Linux Parameter:

```sh
./build.sh Release 0.2.0
```

macOS/Linux Environment Variable:

```sh
RELEASE_VERSION=0.2.0 ./build.sh Release
```
