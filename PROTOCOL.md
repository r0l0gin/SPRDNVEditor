# SPRD Device Communication Protocol - Technical Deep Dive

This document provides detailed technical information about the communication protocol used by SPRDNVEditor to interact with SPRD (UNISOC) devices.

## Table of Contents

1. [Protocol Overview](#protocol-overview)
2. [Communication Layers](#communication-layers)
3. [Boot Mode Protocol](#boot-mode-protocol)
4. [HDLC Framing](#hdlc-framing)
5. [Command Structure](#command-structure)
6. [Memory Operations](#memory-operations)
7. [Error Handling](#error-handling)
8. [Implementation Details](#implementation-details)

## Protocol Overview

The SPRD device communication protocol is a proprietary protocol used by Spreadtrum (now UNISOC) devices for firmware download, debugging, and parameter management. SPRDNVEditor implements this protocol in C# to provide direct communication with devices without requiring vendor tools.

### Key Characteristics

- **Serial-based**: Communication occurs over USB-CDC (Communications Device Class) presenting as COM ports
- **HDLC Framing**: Uses HDLC-like framing for packet delimitation and error detection
- **Multi-stage Boot**: Requires loading FDL1 and FDL2 bootloaders sequentially
- **CRC Protection**: All data transfers include CRC32 checksums for integrity
- **Synchronous**: Commands follow a request-response pattern

## Communication Layers

### Layer Stack

```
┌─────────────────────────┐
│   Application Layer     │  (NV Operations, Flash Read/Write)
├─────────────────────────┤
│   Protocol Layer        │  (Command Formatting, Response Parsing)
├─────────────────────────┤
│   HDLC Framing Layer    │  (Packet Framing, CRC)
├─────────────────────────┤
│   Serial Transport      │  (COM Port, 115200 baud)
├─────────────────────────┤
│   USB CDC Layer         │  (USB-Serial Conversion)
└─────────────────────────┘
```

### Transport Configuration

```csharp
// Serial port settings
BaudRate: 115200
DataBits: 8
StopBits: One
Parity: None
Handshake: None
ReadTimeout: 5000ms
WriteTimeout: 5000ms
```

## Boot Mode Protocol

### Boot Sequence

1. **Device Detection**: Device enters boot mode on power-on with specific key combination
2. **Synchronization**: Send BSL_CMD_CONNECT (0x00) to establish communication
3. **FDL1 Loading**: Load first-stage bootloader
4. **FDL2 Loading**: Load second-stage bootloader with full functionality
5. **Mode Switch**: Device ready for operations (memory read/write, etc.)

### BSL Commands

```csharp
// Boot Support Library Commands
BSL_CMD_CONNECT         = 0x00  // Initial connection
BSL_CMD_START_DATA      = 0x01  // Begin data transfer
BSL_CMD_MIDST_DATA      = 0x02  // Continue data transfer
BSL_CMD_END_DATA        = 0x03  // End data transfer
BSL_CMD_EXEC_DATA       = 0x04  // Execute loaded code
BSL_REP_ACK             = 0x80  // Positive acknowledgment
BSL_REP_VER             = 0x81  // Version response
```

### Connection Handshake

```
Host → Device: 0x7E 0x00 0x00 [CRC16] 0x7E    // BSL_CMD_CONNECT
Device → Host: 0x7E 0x80 [Version] [CRC16] 0x7E  // BSL_REP_ACK
```

### FDL1 to FDL2 Transition - Critical Protocol Change

The transition from FDL1 to FDL2 involves a **critical change in checksum algorithms** that must be detected and handled correctly. The actual implementation uses automatic detection based on response validation:

#### Automatic Checksum Detection

The protocol implementation uses a flag `_useCrc16L` that automatically switches between checksum algorithms based on response validation:

```csharp
// Protocol setting - switches between checksum algorithms
private bool _useCrc16L = true;  // Starts with CRC16L, falls back to FrmChk
```

#### Checksum Algorithm Implementation (Actual Code)

**1. FrmChk (Frame Check) - Internet one's-complement 16-bit sum:**
```csharp
/// <summary>Internet one's-complement 16-bit sum ("frmChk").</summary>
public static ushort FrmChk(byte[] bytes, int offset = 0, int length = -1,
                           bool bigEndian = true,
                           ushort init = 0,
                           bool finalComplement = true)
{
    if (length == -1)
        length = bytes.Length - offset;

    ushort sum = init;
    int i;

    // Sum pairs of bytes as 16-bit words
    for (i = offset; i < offset + length - 1; i += 2)
    {
        ushort word = bigEndian 
            ? (ushort)((bytes[i] << 8) | bytes[i + 1])
            : (ushort)((bytes[i + 1] << 8) | bytes[i]);
        sum += word;
    }

    // Add remaining odd byte if present
    if (i == offset + length - 1)
    {
        ushort word = bigEndian 
            ? (ushort)(bytes[i] << 8)
            : (ushort)bytes[i];
        sum += word;
    }

    return finalComplement ? (ushort)~sum : sum;
}
```

**2. CRC16L (CRC16 with polynomial 0x1021 - CCITT/X.25):**
```csharp
/// <summary>
/// Calculate CRC-16-L with polynomial 0x1021 (CCITT/X.25)
/// </summary>
private ushort CalculateCrc16L(byte[] data, int offset = 0, int length = -1)
{
    if (length == -1)
        length = data.Length - offset;

    ushort crc = 0xFFFF;

    for (int i = offset; i < offset + length; i++)
    {
        crc ^= data[i];
        for (int j = 0; j < 8; j++)
        {
            if ((crc & 0x0001) != 0)
                crc = (ushort)((crc >> 1) ^ 0x8408);
            else
                crc >>= 1;
        }
    }

    return (ushort)~crc;
}
```

#### Smart Checksum Detection in AnalyzeResponse

The critical detection logic happens in the `AnalyzeResponse` method:

```csharp
private AnalyzedResponse AnalyzeResponse(byte[] response, string context)
{
    // ... extract response data ...
    
    // Extract and validate CRC
    result.ReceivedCrc = FromBigEndian16(unescapedContent, 4 + result.Length);

    // Try CRC16L first
    result.CalculatedCrc = CalculateCrc16L(unescapedContent, 0, 4 + result.Length);
    if (!(result.ReceivedCrc == result.CalculatedCrc))
    {
        // CRC16L failed, try FrmChk
        result.CalculatedCrc = FrmChk(unescapedContent, 0, 4 + result.Length);
        if (result.ReceivedCrc == result.CalculatedCrc)
            _useCrc16L = false;  // Switch to FrmChk for future packets
    } 
    else 
    {
        _useCrc16L = true;  // Confirm CRC16L is correct
    }

    result.CrcValid = result.ReceivedCrc == result.CalculatedCrc;
    // ... rest of analysis ...
}
```

#### ResetCRC - Protocol Initialization

The `ResetCRC()` method is called at critical transition points:

```csharp
public bool ResetCRC()
{
    // Check baud rate and establish connection
    if (!CheckBaud())
        return false;

    if (!Connect())
        return false;

    LogMessage?.Invoke("Communication established successfully");
    return true;
}
```

#### Loading Sequence with ResetCRC Calls

```csharp
private async Task<bool> LoadFDLAsync()
{
    // Initial connection and baud check
    if (!ResetCRC())
        return false;

    // Load FDL1
    if (!WriteData(0x210, fdl1Data, FDL1_ADDRESS))
        return false;
        
    // Execute FDL1
    if (!await ExecuteData())
        return false;
        
    // CRITICAL: Reset CRC detection after FDL1 execution
    if (!ResetCRC())  // This resets the protocol state
        return false;

    await Task.Delay(1000);  // Wait for FDL1 to stabilize
    
    // Load FDL2 (checksum algorithm will be auto-detected)
    if (!WriteData(0x2200, fdl2Data, FDL2_ADDRESS))
        return false;
        
    // Execute FDL2
    if (!await ExecuteData())
        return false;
        
    await Task.Delay(1000);  // Wait for FDL2 to stabilize
    return true;
}
```

### BSL_CMD_EXEC_DATA - Execute Loaded Code

The `BSL_CMD_EXEC_DATA` command is crucial for transitioning between bootloader stages:

#### Command Structure

```csharp
// Execute data command format
struct ExecuteDataCommand {
    byte Command = BSL_CMD_EXEC_DATA;  // 0x04
    // No additional parameters - executes code at previously loaded address
}
```

#### Execution Details

1. **Memory Preparation**: Code must be loaded to executable memory region
2. **Entry Point**: Execution starts at the base address specified in START_DATA
3. **Context Switch**: Device switches from current code to newly loaded code
4. **Response Timing**: No immediate response - device is rebooting

#### Actual ExecuteData Implementation

```csharp
private async Task<bool> ExecuteData()
{
    try
    {
        BMPackage package = new BMPackage
        {
            Type = BSL_CMD_EXEC_DATA,  // 0x04
            Length = 0,                // No data payload
            Data = new byte[0]         // Empty data
        };
        
        if (!SendPackage(package))
            return false;
        
        // Wait for device to execute and reboot
        await Task.Delay(1000);
        return true;
    }
    catch (Exception ex)
    {
        LogMessage?.Invoke($"ExecuteData error: {ex.Message}");
        return false;
    }
}
```

#### Packet Serialization (Actual Implementation)

The actual packet serialization shows how checksum detection works:

```csharp
private byte[] SerializePackage(BMPackage package)
{
    List<byte> packet = new List<byte>();
    
    // Add type and length in big-endian
    packet.AddRange(ToBigEndian(package.Type));
    packet.AddRange(ToBigEndian(package.Length));
    
    // Add data payload
    if (package.Data != null && package.Data.Length > 0)
    {
        packet.AddRange(package.Data);
    }
    
    // Calculate and add CRC - this is where algorithm selection happens
    ushort crc = _useCrc16L ? CalculateCrc16L(packet.ToArray()) : FrmChk(packet.ToArray());
    packet.AddRange(ToBigEndian(crc));
    
    // Apply HDLC escaping
    byte[] escapedPacket = ApplyHdlcEscaping(packet.ToArray());
    
    // Build final frame with HDLC flags
    List<byte> frame = new List<byte>();
    frame.Add(HDLC_FLAG);        // 0x7E
    frame.AddRange(escapedPacket);
    frame.Add(HDLC_FLAG);        // 0x7E
    
    return frame.ToArray();
}
```

#### Critical Timing Considerations

```csharp
// Timing configuration for different phases
public static class BootloaderTiming
{
    // Boot ROM → FDL1
    public const int FDL1_BOOT_TIME_MS = 200;
    public const int FDL1_CONNECT_TIMEOUT_MS = 1000;
    
    // FDL1 → FDL2  
    public const int FDL2_BOOT_TIME_MS = 500;
    public const int FDL2_CONNECT_TIMEOUT_MS = 2000;
    
    // Command timeouts
    public const int EXEC_CMD_TIMEOUT_MS = 100;  // No response expected
    public const int DATA_TRANSFER_TIMEOUT_MS = 5000;
}
```

#### State Transition Validation

```csharp
private bool ValidateStateTransition(BootloaderPhase from, BootloaderPhase to)
{
    // Validate expected transitions
    switch (from)
    {
        case BootloaderPhase.BootROM:
            return to == BootloaderPhase.FDL1;
            
        case BootloaderPhase.FDL1:
            return to == BootloaderPhase.FDL2;
            
        case BootloaderPhase.FDL2:
            return false;  // No further transitions
            
        default:
            return false;
    }
}
```

## HDLC Framing

### Frame Structure

```
┌──────┬───────────┬──────────┬─────────┬──────┐
│ FLAG │  HEADER   │ PAYLOAD  │  CRC    │ FLAG │
│ 0x7E │ (2 bytes) │ (n bytes)│(2 bytes)│ 0x7E │
└──────┴───────────┴──────────┴─────────┴──────┘
```

### Escape Sequences

Special bytes are escaped to prevent framing errors:

```csharp
// Escape mappings
0x7E → 0x7D 0x5E  // Frame delimiter
0x7D → 0x7D 0x5D  // Escape character
0x11 → 0x7D 0x31  // XON
0x13 → 0x7D 0x33  // XOFF
0x91 → 0x7D 0xB1  // Extended XON
0x93 → 0x7D 0xB3  // Extended XOFF
```

### CRC Calculation

```csharp
// CRC16 implementation (polynomial: 0x8408)
private static ushort CalculateCRC16(byte[] data)
{
    ushort crc = 0xFFFF;
    
    foreach (byte b in data)
    {
        crc ^= b;
        for (int i = 0; i < 8; i++)
        {
            if ((crc & 0x0001) != 0)
                crc = (ushort)((crc >> 1) ^ 0x8408);
            else
                crc >>= 1;
        }
    }
    
    return (ushort)~crc;
}
```

## Command Structure

### After FDL2 Load - Extended Commands

```csharp
// Memory operation commands
CMD_READ_FLASH      = 0x10  // Read memory
CMD_READ_NAND       = 0x11  // Read NAND flash
CMD_READ_MEMORY     = 0x12  // Generic memory read
CMD_PROGRAM_FLASH   = 0x13  // Write flash
CMD_ERASE_FLASH     = 0x14  // Erase flash sectors
CMD_REPARTITION     = 0x15  // Modify partitions
CMD_READ_CHIP_TYPE  = 0x16  // Get chip information
CMD_CHECK_NAND      = 0x17  // NAND status check
CMD_RESET           = 0x18  // Device reset
CMD_WRITE_DATA      = 0x94  // Write NV data

// Response codes
RESPONSE_ACK        = 0x80  // Success
RESPONSE_NACK       = 0x81  // Failure
```

### BMPackage Structure (Actual Implementation)

The protocol uses a BMPackage structure for all communications:

```csharp
private class BMPackage
{
    public ushort Type { get; set; }    // Command type (big-endian)
    public ushort Length { get; set; }  // Data length (big-endian)
    public byte[] Data { get; set; }    // Payload data
}
```

### Command Packet Format

```
┌────────────┬────────────┬──────────────┬─────────┐
│    Type    │   Length   │   Payload    │   CRC   │
│ (2 bytes)  │ (2 bytes)  │  (n bytes)   │(2 bytes)│
└────────────┴────────────┴──────────────┴─────────┘
```

## Memory Operations

### Read Memory Command

```csharp
// Read memory structure
struct ReadMemoryCommand {
    byte Command = CMD_READ_MEMORY;  // 0x12
    uint Address;                    // Target address (big-endian)
    uint Length;                     // Bytes to read (big-endian)
}

// Example: Read NV parameters
Address: 0x90000001  // NV base address
Length:  0x000B0BA8  // 723,880 bytes
```

### Write Memory Command

```csharp
// Write memory structure
struct WriteMemoryCommand {
    byte Command = CMD_WRITE_DATA;   // 0x94
    uint Address;                    // Target address
    uint Length;                     // Data length
    byte[] Data;                     // Payload
    uint CRC32;                      // Data checksum
}
```

## Error Handling

### Retry Mechanism

```csharp
// Retry configuration
const int MAX_RETRIES = 3;
const int RETRY_DELAY_MS = 100;
const int READ_TIMEOUT_MS = 5000;

// Retry loop implementation
for (int retry = 0; retry < MAX_RETRIES; retry++)
{
    try 
    {
        // Send command
        SendCommand(cmd);
        
        // Wait for response
        var response = ReadResponse(timeout: READ_TIMEOUT_MS);
        
        if (response.IsValid)
            return response;
    }
    catch (TimeoutException)
    {
        if (retry == MAX_RETRIES - 1)
            throw;
            
        Thread.Sleep(RETRY_DELAY_MS);
    }
}
```

### Error Recovery

1. **Timeout Recovery**: Flush buffers and resynchronize
2. **CRC Errors**: Request retransmission
3. **Protocol Errors**: Reset to known state
4. **Device Errors**: Attempt device reset

## Implementation Details

### Chunked Reading

For large memory reads, data is transferred in chunks to maintain stability:

```csharp
// Chunk configuration
const int READ_CHUNK_SIZE = 12288;  // 12KB chunks

// Chunked read implementation
public byte[] ReadMemory(uint address, uint totalSize)
{
    var result = new List<byte>();
    uint offset = 0;
    
    while (offset < totalSize)
    {
        uint chunkSize = Math.Min(READ_CHUNK_SIZE, totalSize - offset);
        byte[] chunk = ReadMemoryChunk(address + offset, chunkSize);
        result.AddRange(chunk);
        offset += chunkSize;
        
        // Progress callback
        OnProgress?.Invoke(offset, totalSize);
    }
    
    return result.ToArray();
}
```

### State Machine

```
┌─────────────┐
│ Disconnected│
└──────┬──────┘
       │ Connect
┌──────▼──────┐
│  Boot Mode  │
└──────┬──────┘
       │ Load FDL1
┌──────▼──────┐
│ FDL1 Active │
└──────┬──────┘
       │ Load FDL2
┌──────▼──────┐
│ FDL2 Active │
└──────┬──────┘
       │ Operations
┌──────▼──────┐
│ Operational │ ←─┐
└──────┬──────┘   │
       │          │
       └──────────┘
         Commands
```

### Performance Optimizations

1. **Burst Reading**: Minimize command overhead by reading large chunks
2. **Pipelining**: Prepare next command while waiting for response
3. **Buffer Management**: Pre-allocate buffers to reduce GC pressure
4. **Async I/O**: Use async serial port operations for better responsiveness

### CRC32 Implementation

```csharp
// CRC32 for data integrity (polynomial: 0x04C11DB7)
public static uint CalculateCRC32(byte[] data)
{
    uint[] table = GenerateCRC32Table();
    uint crc = 0xFFFFFFFF;
    
    foreach (byte b in data)
    {
        crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
    }
    
    return ~crc;
}

private static uint[] GenerateCRC32Table()
{
    uint[] table = new uint[256];
    uint polynomial = 0x04C11DB7;
    
    for (uint i = 0; i < 256; i++)
    {
        uint crc = i << 24;
        for (int j = 0; j < 8; j++)
        {
            if ((crc & 0x80000000) != 0)
                crc = (crc << 1) ^ polynomial;
            else
                crc <<= 1;
        }
        table[i] = crc;
    }
    
    return table;
}
```

## Protocol Analysis Tools

### Debugging Support

```csharp
// Protocol logging
public enum LogLevel
{
    Raw,      // Raw bytes
    Frame,    // HDLC frames
    Command,  // Parsed commands
    Data      // Application data
}

// Example log output
[Raw]     TX: 7E 00 12 01 00 00 90 00 00 30 AF 7E
[Frame]   TX: CMD_READ_MEMORY addr=0x90000001 len=48
[Command] Reading NV header from 0x90000001
[Data]    NV Magic: 0x5A5A, Version: 0x01, Size: 0x0B0BA8
```

### Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| No response | Wrong baud rate | Verify 115200 baud |
| CRC errors | Noise/interference | Shorter USB cable |
| Timeout | Device busy | Increase timeout |
| NACK response | Invalid address | Check memory map |
| Partial data | Buffer overflow | Reduce chunk size |
| FDL2 fails to load | Wrong checksum type | Ensure using FrmChk for FDL1→FDL2 |
| Can't connect after EXEC | Phase detection error | Try both checksum types |
| Device stuck in boot loop | Bad FDL image | Verify FDL binary integrity |

### FDL Loading Troubleshooting

#### Checksum Mismatch Errors

The actual implementation uses automatic checksum detection rather than manual phase detection. The critical insight is that the `_useCrc16L` flag automatically switches based on response validation in `AnalyzeResponse`:

```csharp
// Automatic protocol detection in AnalyzeResponse method
private AnalyzedResponse AnalyzeResponse(byte[] response, string context)
{
    // Extract CRC from response
    result.ReceivedCrc = FromBigEndian16(unescapedContent, 4 + result.Length);

    // Try CRC16L first (default)
    result.CalculatedCrc = CalculateCrc16L(unescapedContent, 0, 4 + result.Length);
    
    if (!(result.ReceivedCrc == result.CalculatedCrc))
    {
        // CRC16L validation failed, try FrmChk
        result.CalculatedCrc = FrmChk(unescapedContent, 0, 4 + result.Length);
        if (result.ReceivedCrc == result.CalculatedCrc)
        {
            _useCrc16L = false;  // Switch to FrmChk for future packets
            LogMessage?.Invoke("Switched to FrmChk checksum algorithm");
        }
    } 
    else 
    {
        _useCrc16L = true;  // Confirm CRC16L is working
        LogMessage?.Invoke("Using CRC16L checksum algorithm");
    }

    result.CrcValid = result.ReceivedCrc == result.CalculatedCrc;
    return result;
}
```

#### Key Protocol Constants (Actual Implementation)

```csharp
// FDL load addresses
private const uint FDL1_ADDRESS = 0x6200;        // FDL1 execution address
private const uint FDL2_ADDRESS = 0x80100000;    // FDL2 execution address

// Data transfer type constants
private const ushort TYPE_FDL1_DATA = 0x210;     // FDL1 data transfer type
private const ushort TYPE_FDL2_DATA = 0x2200;    // FDL2 data transfer type
```

#### Complete Loading Sequence (Actual Implementation)

```csharp
private async Task<bool> LoadFDLAsync()
{
    try
    {
        // Initial connection with automatic protocol detection
        if (!ResetCRC())
            return false;

        // Load FDL1
        byte[] fdl1Data = GetFdl1Data();
        if (fdl1Data == null || fdl1Data.Length == 0)
        {
            LogMessage?.Invoke("FDL1 data not available");
            return false;
        }
        
        // WriteData handles protocol automatically based on _useCrc16L flag
        if (!WriteData(0x210, fdl1Data, FDL1_ADDRESS))
        {
            LogMessage?.Invoke("Failed to download FDL1");
            return false;
        }
        
        // Execute FDL1
        if (!await ExecuteData())
        {
            LogMessage?.Invoke("Failed to execute FDL1");
            return false;
        }
        
        // CRITICAL: Reset protocol state after FDL1 execution
        // This allows automatic detection of the new bootloader's protocol
        if (!ResetCRC())
            return false;

        await Task.Delay(1000);  // Wait for FDL1 to fully initialize
        
        // Load FDL2
        byte[] fdl2Data = GetFdl2Data();
        if (fdl2Data == null || fdl2Data.Length == 0)
        {
            LogMessage?.Invoke("FDL2 data not available");
            return false;
        }
        
        // Load FDL2 - protocol will be automatically detected
        if (!WriteData(0x2200, fdl2Data, FDL2_ADDRESS))
        {
            LogMessage?.Invoke("Failed to download FDL2");
            return false;
        }
        
        // Execute FDL2 - final bootloader stage
        if (!await ExecuteData())
        {
            LogMessage?.Invoke("Failed to execute FDL2");
            return false;
        }
        
        await Task.Delay(1000);  // Wait for FDL2 to fully initialize
        
        LogMessage?.Invoke("Successfully loaded FDL1 and FDL2");
        return true;
    }
    catch (Exception ex)
    {
        LogMessage?.Invoke($"LoadFDL error: {ex.Message}");
        return false;
    }
}
```

#### Critical Implementation Details

1. **Automatic Protocol Detection**: The `_useCrc16L` flag switches automatically based on response validation
2. **ResetCRC() Calls**: Called at critical transition points to re-establish communication and detect protocol changes
3. **Timing**: 1000ms delays allow bootloaders to fully initialize before next operation
4. **Error Handling**: Each step is validated before proceeding to the next phase
5. **Self-Correcting**: If wrong checksum is used, the next response will trigger automatic algorithm switch

## Security Considerations

1. **No Authentication**: Protocol has no built-in authentication
2. **No Encryption**: All data transmitted in plaintext
3. **Full Access**: FDL2 provides unrestricted memory access
4. **Permanent Changes**: NV writes can brick device if incorrect

## References

- SPRD Boot ROM Protocol Specification
- HDLC (High-Level Data Link Control) - ISO/IEC 13239
- USB CDC Class Specification v1.2
- UNISOC Developer Documentation

---

*This document represents the reverse-engineered protocol implementation. Use at your own risk. Always backup device data before making modifications.*