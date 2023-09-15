# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2023-07-26
### Changed
- Removed editor analytics.
- Removed Pro License requirement.

## [2.0.0] - 2022-03-15
### Added
- Editor analytics for sender type, receiver type and message counts

### Changed
- Updated the required minimum Unity Editor version to Unity 2022.2.

## [1.0.0] - 2022-10-03

## [1.0.0-beta.2] - 2022-07-13
### Added
- Added support for the TCP protocol.
- Logged messages may optionally persist through a domain reload.
- Added a search box to the message log.
- Added duplicate argument option to OscMessageHandler and OscMessageOutput components. 

### Changed
- Reordered update methods to reduce latency in some cases.
- OSC Monitor window message log automatically scrolls to show new messages.
- OSC Monitor window updates less often to improve performance when many messages are being sent.
- Added duplicate argument option to OscMessageHandler and OscMessageOutput components.
- Arguments added to an OscMessageHandler are created with an event targeting the component's GameObject.
- Arguments added to an OscMessageOutput target the component's GameObject by default.
  
### Fixed
- OscMessageOutput does not work in Il2cpp builds.
- Fix undo when adding arguments on OscMessageHandler and OscMessageOutput components.
- Prevent assiging properties that didn't have a public getter on OscMessageOutput components.
  
## [1.0.0-beta.1] - 2022-05-26

- Initial version of the OSC package.
