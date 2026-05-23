# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
## [ToDo]
- Uninstall old version(s) of MBAM before attempting to reinstall/update
- Get/display the top MAC address returned by ipconfig
- [optional] Create de-branded fork

## [Unreleased]
## [3.0.5] - 05-22-2026
### Added
- Install uBlock Origin in Firefox
### Fixed
- Fixed a typo for getting the Windows display version in ComputerInfo.cs - s/DisplayVersionz/DisplayVersions/

## [3.0.4] - 01-22-2026
### Added
- "New" Calling Card channel IDs to check for when making Calling Card shortcuts in NOC folder

## [3.0.3] - 01-11-2026
### Added
- Vertical scrollbar to "Misc. Installers" tab (only visible when needed)
- Added "DisplayVersion" (22h2, 25h2, etc.) to OS info. Labeled "Version," while the info previously carrying this label has been changed to "Build"
### Changed
- Updated LICENSE with current year

## [Released]
## [3.0.2] - 12-30-2025
### Added
- "MacriumService" stopped/disabled by bloatkiller ("Disable unnecessary services")
### Changed
- Changed "Programs" to "Tuneup"
- Changed "Ninite" to "Misc. Installers"
- "Misc. Installers" tab now checks for a local `installers` folder, in addition to a `ninite` folder
- "Misc. Installers" also now checks for `.msi` files, in addition to `.exe` files
- Moved Macrium Reflect 7 checkbox to "Tools" tab

## [3.0.1] - 12-26-2025
### Changed
- Version number (2.4.0 -> 3.0) - New anticipated release version number

## [3.0.0] - 12-26-2025
### Added
- Macrium Reflect 7 (Programs tab)
- New "Ninite" tab - Populated by executables in a local `ninite` folder
- "Export" button - Writes computer/asset info into a local file, named `asset.txt`
- Memory speed now collected/reported
- "Disable unnecessary services" checkbox - Downloads/runs a batch script, which disabled CCleaner, Glary Utilities, Macrium, DiagTrack, and SysMain services (see github.com/0xdeadgirl/batch-scripts/blob/main/bloatkiller.bat)
### Changed
- AdwCleaner executable is copied to `C:\AdwCleaner\`, and the NOC folder shortcut points to this
- Replaced Cortana and spaceman with Nerd
- Calling Card shortcut created in NOC folder
- Data sizes are calculated, instead of being explicitly defined (MB, GB, etc.)
- Lots of UI tweaks
### Removed
- Removed CCleaner, and replaced many references to it with Macrium Reflect
### Fixed
- Partitions not being reported when CD-ROMs and removable drives present
### Security
- Updated vulnerable dependencies