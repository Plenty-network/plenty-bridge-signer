# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2021-03-09
### Adds
* Adds signer address in events payload

### Changes
* better keys in json log

## [0.2.1] - 2021-03-07

### Changes
* Default logging in json

## [0.2.0] - 2021-03-03

### Adds
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
