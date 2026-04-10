# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive XML documentation for all public APIs
- Detailed error handling with specific error codes and messages
- Input validation for DPI, zoom levels, and file parameters
- Better TIFF codec error detection and user-friendly messages
- README with complete documentation and examples
- CHANGELOG for tracking project history

### Changed
- Improved parameter naming consistency (Dpi -> dpi in method signatures)
- Enhanced error messages with actionable information
- Refactored providers with better null checking and validation
- Removed unused using statements across all files
- Made private fields readonly where appropriate
- Improved disposal patterns in TiffToBitmapListProvider

### Fixed
- TIFF decoder now explicitly specifies codec ID (fixes 0x88982F41 error on certain machines)
- Better handling of missing TIFF codec with clear error messages
- Proper disposal of bitmaps on error in TiffToBitmapListProvider
- Improved error context in LoadFileAsync

### Security
- Added bounds checking for DPI values (1-2400)
- Enhanced file path validation
- Better exception handling to prevent information leakage

## [Previous Versions]

### Changed
- Initial implementation with basic PDF and TIFF viewing capabilities
- Zoom, pan, and navigation features
- Lazy loading and virtual scrolling
- Temp file support
