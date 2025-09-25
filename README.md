# SPRDNVEditor

A Windows Forms application for editing SPRD (UNISOC) device NV parameters. This tool provides a graphical interface for reading, editing, and writing NV parameters from SPRD mobile devices through USB communication.

## Features

### Core Functionality
- **Protocol Re-implementation (C#)**: Complete re-implementation of the device communication protocol in C# (framing, checksums/CRC, timeouts, retries, error handling)
- **Device Communication**: USB communication with SPRD devices in boot and diagnostic modes
- **NV Parameter Management**: Read, edit, and write NV parameters with automatic parsing
- **Data Visualization**: Hex editor and structured data grid views
- **Type System**: Support for various data types (Binary, String, UInt32/16/8, IMEI, MAC, IPv4, Custom)

### Device Profile Management
- **Multi-Device Support**: Configure profiles for different device types
- **FDL Integration**: Embedded FDL1/FDL2 bootloader files within profiles
- **Custom Mappings**: Device-specific NV item mappings and type definitions
- **Self-Contained Profiles**: Profiles include all necessary configuration data

### Flash Memory Operations
- **Multiple Flash Reading**: Configure and read multiple flash regions
- **Organized Output**: Automatic folder structure with device profile and timestamp
- **Custom Definitions**: User-defined flash file addresses, sizes, and descriptions
- **Batch Operations**: Read multiple flash files in a single operation

### File Comparison
- **NV File Comparison**: Side-by-side comparison of two NV files
- **Visual Differences**: Color-coded display of identical, different, and unique items
- **Detailed Analysis**: Comprehensive comparison with statistics and summaries

## System Requirements

- **Operating System**: Windows 10/11
- **.NET Runtime**: .NET 6.0 or later
- **Hardware**: SPRD device with USB drivers installed
- **Files**: FDL1 and FDL2 binary files (fdl1.bin and fdl2.bin)

## Installation

### Pre-built Releases
1. Download the latest release from the [Releases](https://github.com/r0l0gin/SPRDNVEditor/releases) page
2. Extract the ZIP file to your preferred location
3. Ensure you have the required FDL files (fdl1.bin, fdl2.bin)
4. Run `SPRDNVEditor.exe`

### Building from Source
```bash
git clone https://github.com/r0l0gin/SPRDNVEditor.git
cd SPRDNVEditor
dotnet restore
dotnet build -c Release
```

### Publishing for Distribution
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Basic Operation
1. **Connect Device**: Connect your SPRD device in boot mode
2. **Select Profile**: Choose the appropriate device profile from the dropdown
3. **Select Port**: Choose the COM port from the device connection panel
4. **Read NV Data**: Click "Read NV File" to download NV parameters from device
5. **Edit Parameters**: Modify values in the data grid or hex editor
6. **Write Changes**: Click "Write NV File" to upload modified parameters

### Device Profile Management
1. Click "Manage Profiles" to open the profile manager
2. Create new profiles or edit existing ones
3. Configure NV base addresses, data sizes, and FDL files
4. Add flash file definitions for additional memory regions
5. Save profiles for reuse across sessions

### File Comparison
1. Click "Compare Files" to open the comparison dialog
2. Select two NV files using the Browse buttons
3. Click "Compare Files" to analyze differences
4. Review color-coded results:
   - 🟢 **Green**: Identical items
   - 🔴 **Red**: Different values
   - 🟡 **Yellow**: Only in first file
   - 🔵 **Blue**: Only in second file

### Flash File Reading
1. Configure flash file definitions in device profiles
2. Use "Read Flash Files" for standalone flash reading
3. Files are automatically organized in timestamped folders

## Configuration

### Device Profiles
Device profiles are stored as JSON files in the `Profiles` directory. Each profile contains:

```json
{
  "Name": "device",
  "Description": "Profile for device",
  "NVBaseAddress": 2415919105,
  "NVDataSize": 723880,
  "Fdl1Data": "base64-encoded-fdl1-data",
  "Fdl2Data": "base64-encoded-fdl2-data",
  "FlashFiles": [
    {
      "Name": "Bootloader",
      "Description": "Device bootloader region",
      "BaseAddress": 2147483648,
      "Size": 65536,
      "FileName": "bootloader.bin",
      "Enabled": true
    }
  ],
  "NVItemMappings": {
    "NV_0001": {
      "Id": 1,
      "Name": "IMEI1",
      "Description": "Primary IMEI",
      "Type": "IMEI",
      "Size": 15
    }
  }
}
```

### Memory Layout
- **NV Base Address**: 0x90000001 (virtual address for NV parameters)
- **NV Memory Size**: 0x0B0BA8 bytes (723,880 bytes total)
- **Read Chunk Size**: 12KB for efficient transfer
- **Protocol**: SPRD boot mode with CRC32 validation

## Development

### Architecture
The application follows a modular architecture:

- **MainForm.cs**: Main UI orchestration and user interactions
- **SPRDNVToolInternal.cs**: Low-level device communication and protocol handling
- **NVParser.cs**: NV parameter parsing and data type management
- **DeviceProfileManager.cs**: Profile management and persistence
- **USBFastConnect.cs**: USB device detection and connection management

### Key Components
- **Device Communication**: HDLC protocol implementation for SPRD devices
- **Data Parsing**: Flexible type system with custom formatting support
- **Profile System**: JSON-based configuration with automatic migration
- **UI Framework**: Windows Forms with custom hex editor control

### Testing
```bash
# Run unit tests
dotnet test

# Build and test
dotnet build && dotnet test
```

## Protocol Details

For comprehensive technical information about the SPRD device communication protocol, including HDLC framing, command structures, and implementation details, see [PROTOCOL.md](PROTOCOL.md).

### SPRD Boot Mode Communication
1. **Device Detection**: Automatic detection of devices in boot mode
2. **FDL Loading**: Sequential loading of FDL1 and FDL2 bootloaders
3. **Memory Access**: Direct memory read/write operations
4. **Mode Switching**: Automatic switching between boot and diagnostic modes

### NV Parameter Structure
- **Header**: Magic numbers and size information
- **Items**: Variable-length records with ID, length, and data
- **Checksum**: CRC32 validation for data integrity

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding conventions
- Add unit tests for new functionality
- Update documentation for API changes
- Ensure compatibility with existing profiles

## Safety and Security

### Data Safety
- **Backup Recommended**: Always backup original NV parameters before modification
- **Validation**: Parameter bounds checking and type validation
- **Confirmation Dialogs**: Write operations require user confirmation

### Device Protection
- **CRC Validation**: Prevents data corruption during transfers
- **Chunked Operations**: Reduces memory stress on device
- **State Management**: Automatic recovery from protocol errors

## Troubleshooting

### Common Issues

**Device Not Detected**
- Ensure SPRD device drivers are installed
- Check USB cable and connection
- Verify device is in boot mode

**Communication Errors**
- Try different COM port
- Restart application and reconnect device
- Check FDL files are present and valid

**Profile Loading Errors**
- Verify JSON syntax in profile files
- Ensure FDL files exist at specified paths
- Check file permissions

### Debug Mode
Enable debug logging by setting environment variable:
```
SET SPRDNV_DEBUG=1
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


## Disclaimer

This tool is for educational and development purposes. Use at your own risk. The authors are not responsible for any damage to devices or data loss that may occur from using this software. Always backup your device data before making modifications.