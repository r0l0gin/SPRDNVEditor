# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of SPRDNVEditor
- Device profile management system for multi-device support
- Support for embedded FDL files within profiles
- Flash file reading with configurable addresses and sizes
- NV file comparison tool with visual difference highlighting
- Automatic device detection and mode switching
- Real-time logging and status updates
- Hex editor for binary data visualization
- Type system supporting various data formats (IMEI, MAC, IPv4, etc.)
- Organized output folders with timestamp naming
- Profile migration system for backward compatibility
- Self-contained profiles with embedded bootloader files

### Features
- **Core Functionality**
  - USB communication with SPRD devices
  - NV parameter reading, editing, and writing
  - Automatic CRC validation and data integrity checks
  - Support for boot mode and diagnostic mode operations

- **Device Profile Management**
  - JSON-based profile configuration
  - Multi-device support
  - Custom NV item mappings and type definitions
  - Embedded FDL1/FDL2 bootloader files
  - Automatic profile migration

- **Flash Memory Operations**
  - Multiple flash region reading
  - User-defined flash file configurations
  - Batch flash file operations
  - Organized output with profile and timestamp folders

- **File Comparison**
  - Side-by-side NV file comparison
  - Color-coded difference visualization
  - Statistical summary of changes
  - Support for identical, different, and unique items

- **User Interface**
  - Modern Windows Forms interface
  - Data grid with inline editing
  - Custom hex editor control
  - Real-time operation logging
  - Progress indicators and status updates

### Technical Details
- Built with .NET 6.0 for Windows
- SPRD boot mode protocol implementation
- HDLC communication protocol
- CRC32 data validation
- JSON serialization for configuration
- Modular architecture for extensibility

### Security & Safety
- Data backup recommendations
- Parameter validation and bounds checking
- Confirmation dialogs for write operations
- CRC validation to prevent data corruption
- Chunked operations to reduce device stress

## [0.1.0] - Initial Development

### Framework
- Project structure and basic architecture
- Windows Forms UI framework
- .NET 6.0 target framework
- Basic SPRD device communication

This represents the initial development milestone with core functionality implemented.

---

## Release Notes

### Version Numbering
This project follows Semantic Versioning:
- **MAJOR** version for incompatible API changes
- **MINOR** version for new functionality in a backwards compatible manner
- **PATCH** version for backwards compatible bug fixes

### Compatibility
- **Windows 10/11**: Primary target platform
- **.NET 6.0+**: Required runtime
- **SPRD Devices**: Supports SPRD/UNISOC devices with boot mode capability