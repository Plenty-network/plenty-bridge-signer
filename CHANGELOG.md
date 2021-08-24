# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.1] - 2021-08-24
### Fixed
* Adds base58 check before trying to sign an unwrap.

## [0.6.0] - 2021-07-23
### Added
* New admin endpoints to sign payload to manage new tokens in the protocol

## [0.5.1] - 2021-06-23
### Changed
* Enhanced ipns publication : no longer waits for five minutes before broadcasting head.

## [0.5.0] - 2021-06-22
### Added
* Detects execution failure during an unwrap, and produces a new signature if needed

## [0.4.5] - 2021-04-25
### Fixed
* Detects when a bad locking contract is used

## [0.4.4] - 2021-04-20
### Changed
* revert operation id uses colon instead of slash for ERC20

## [0.4.3] - 2021-04-20
### Changed
* revert operation id uses colon instead of slash

## [0.4.2] - 2021-04-15

### Changed
* publishing error log frequency

## [0.4.1] - 2021-04-14

### Fixed
* big block parsing

## [0.4.0] - 2021-04-13
### Added
* Sign an unwrap, to release funds in case of a bad minting

### Fixed
* Signer node doesn't fail forever in case of a bad tezos minting address

## [0.3.1] - 2021-03-19
### Changed
* New minter contract parameters layout in events

## [0.3.0] - 2021-03-09
### Added
* Adds signer address in events payload

### Changed
* better keys in json log

## [0.2.1] - 2021-03-07

### Changed
* Default logging in json

## [0.2.0] - 2021-03-03

### Added
* Sign set payment address requests

### Changed
* Tezos polling enhancement. After a restart, signer will catchup at its last polled position, instead at the last observed event. 

###

## [0.1.2] - 2021-02-25

### Security
* bumps dependencies

## [0.1.1] - 2021-02-18

### Changed
- No longer include the targeted entrypoint name in the signature

## [0.1.0] - 2021-02-11

### Added
- Eth transaction id in events. Usefull for to match events with wath the indexer observes.

## [0.0.4] - 2021-02-10

### Fixed
- GitHub release issue

## [0.0.3] - 2021-02-10

### Added
- GitHub release issue

## [0.0.2] - 2021-02-10

### Added
- Just try to get the github release process right

## [0.0.1] - 2021-02-10

### Added
- Initial release
[Unreleased]: https://github.com/bender-labs/wrap-signer/compare/v0.1.2...HEAD
[0.1.2]: https://github.com/bender-labs/wrap-signer/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/bender-labs/wrap-signer/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/bender-labs/wrap-signer/compare/v0.0.4...v0.1.0
[0.0.4]: https://github.com/bender-labs/wrap-signer/compare/v0.0.3...v0.0.4
[0.0.3]: https://github.com/bender-labs/wrap-signer/compare/v0.0.2...v0.0.3
[0.0.2]: https://github.com/bender-labs/wrap-signer/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/bender-labs/wrap-signer/releases/tag/v0.0.1
