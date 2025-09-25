using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SPRDNVEditor
{
    /// <summary>
    /// Internal SPRD NV Tool implementation for WinForms
    /// </summary>
    public class SPRDNVToolInternal
    {
        private SerialPort _port;
        private bool _connected = false;
        private readonly object _lockObject = new object();
        private DeviceProfile _deviceProfile;
        
        // Protocol constants
        private const byte HDLC_FLAG = 0x7E;
        private const byte HDLC_ESCAPE = 0x7D;
        private const byte HDLC_ESCAPE_XOR = 0x20;
        
        // Boot mode commands
        private const byte BSL_CMD_CHECK_BAUD = 0x7E;
        private const byte BSL_CMD_CONNECT = 0x00;
        private const byte BSL_CMD_START_DATA = 0x01;
        private const byte BSL_CMD_MIDST_DATA = 0x02;
        private const byte BSL_CMD_END_DATA = 0x03;
        private const byte BSL_CMD_EXEC_DATA = 0x04;
        private const byte BSL_CMD_RESET = 0x05;
        private const byte BSL_CMD_READ_FLASH = 0x06;
        private const byte BSL_CMD_ERASE_FLASH = 0x0A;

        // Boot mode responses
        private const byte BSL_REP_ACK = 0x80;
        private const byte BSL_REP_VER = 0x81;
        private const byte BSL_REP_INVALID_CMD = 0x82;
        private const byte BSL_REP_READ_FLASH = 0x93;
        
        // FDL addresses
        private const uint FDL1_ADDRESS = 0x6200;
        private const uint FDL2_ADDRESS = 0x80100000;
        
        // CRC-16 lookup table (polynomial 0x8005)
        private static readonly ushort[] Crc16Table = new ushort[256];
        
        // CRC-16-L polynomial (0x1021 - CCITT/X.25)
        private const ushort CRC16L_POLY = 0x1021;
        
        // Protocol settings
        private bool _useCrc = true;
        private bool _bigEndian = true;
        private bool _useCrc16L = true;
        
        // Events for logging
        public event Action<string> LogMessage;
        
        static SPRDNVToolInternal()
        {
            // Initialize CRC-16 lookup table (polynomial 0x8005)
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (ushort)((crc >> 1) ^ 0x8005);
                    else
                        crc >>= 1;
                }
                Crc16Table[i] = crc;
            }
        }

        public void setSerialPort(SerialPort sp)
        {
            if (_port != null && _port.IsOpen)
                _port = sp;
        }

        /// <summary>
        /// Connect to device using specific port
        /// </summary>
        public async Task<bool> ConnectToPortAsync(string portName)
        {
            try
            {
                LogMessage?.Invoke($"Connecting to port {portName}...");

                // Open port
                if (_port == null || !_port.IsOpen)
                {
                    _port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                    _port.ReadTimeout = 5000;
                    _port.WriteTimeout = 5000;
                    _port.Open();
                }
                
                // Check baud rate and establish connection
                if (!ResetCRC())
                {
                    LogMessage?.Invoke("Failed to establish communication with device");
                    return false;
                }
                
                LogMessage?.Invoke("Device communication established");
                
                // Load FDL1 and FDL2
                if (!await LoadFDLAsync())
                {
                    LogMessage?.Invoke("Failed to load FDL");
                    return false;
                }
                
                LogMessage?.Invoke("FDL loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Connection error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Read flash memory in chunks
        /// </summary>
        public byte[] ReadFlash(uint address, uint size)
        {
            if (!_connected)
            {
                LogMessage?.Invoke("Device not connected");
                return null;
            }
            
            try
            {
                LogMessage?.Invoke($"Reading flash: Address=0x{address:X8}, Size=0x{size:X}");
                
                const uint chunkSize = 0x3000; // 12KB chunks
                List<byte> allFlashData = new List<byte>();
                uint currentAddress = 0x0;
                uint remainingSize = size;
                
                while (remainingSize > 0)
                {
                    uint currentChunkSize = Math.Min(chunkSize, remainingSize);

                    uint tryNumber = 0;
                    byte[] chunkData = ReadFlashChunk(address, currentAddress, currentChunkSize);
                    while (chunkData == null && tryNumber++ < 3)
                    {
                        LogMessage?.Invoke($"Failed to read chunk at address 0x{currentAddress:X8}, retrying");
                        chunkData = ReadFlashChunk(address, currentAddress, currentChunkSize);
                    }

                    if (chunkData == null)
                    {
                        LogMessage?.Invoke($"Failed to read chunk at address 0x{currentAddress:X8}");
                        return null;
                    }
                    
                    allFlashData.AddRange(chunkData);
                    currentAddress += currentChunkSize;
                    remainingSize -= currentChunkSize;
                    
                    LogMessage?.Invoke($"Progress: {allFlashData.Count}/{size} bytes ({(double)allFlashData.Count / size * 100:F1}%)");
                }
                
                LogMessage?.Invoke($"Flash read completed: {allFlashData.Count} bytes");
                return allFlashData.ToArray();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"ReadFlash error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Write data to device
        /// </summary>
        public bool WriteData(int maxPacketSize, byte[] data, uint address, int checksum = -1)
        {
            if (!_connected)
            {
                LogMessage?.Invoke("Device not connected");
                return false;
            }
            
            try
            {
                LogMessage?.Invoke($"Writing data: Address=0x{address:X8}, Size={data.Length} bytes");

                //const int maxPacketSize = 0x210; // 512 bytes
                int offset = 0;
                
                // Send start data header
                byte[] startHeader = BuildStartDataHeader(address, (uint)data.Length, checksum);
                BMPackage startPackage = new BMPackage
                {
                    Type = BSL_CMD_START_DATA,
                    Length = (ushort)startHeader.Length,
                    Data = startHeader
                };
                
                if (!SendPackage(startPackage))
                {
                    return false;
                }
                
                // Send data in chunks
                while (offset < data.Length)
                {
                    int packetSize = Math.Min(maxPacketSize, data.Length - offset);
                    byte[] packet = new byte[packetSize];
                    Array.Copy(data, offset, packet, 0, packetSize);
                    
                    BMPackage package = new BMPackage
                    {
                        Type = BSL_CMD_MIDST_DATA,
                        Length = (ushort)packet.Length,
                        Data = packet
                    };
                    
                    if (!SendPackage(package))
                    {
                        return false;
                    }
                    
                    offset += packetSize;
                }
                
                // Send end data
                BMPackage endPackage = new BMPackage
                {
                    Type = BSL_CMD_END_DATA,
                    Length = 0,
                    Data = new byte[0]
                };
                
                if (!SendPackage(endPackage))
                {
                    return false;
                }
                
                LogMessage?.Invoke("Data written successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"WriteData error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from device
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _connected = false;
                _port?.Close();
                _port?.Dispose();
                _port = null;
                LogMessage?.Invoke("Disconnected from device");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Disconnect error: {ex.Message}");
            }
        }

        public bool ResetCRC()
        {
            // Check baud rate and establish connection
            if (!CheckBaud())
            {
                LogMessage?.Invoke("Failed to establish communication with device");
                return false;
            }

            LogMessage?.Invoke("Device communication established");

            // Connect to device
            if (!Connect())
            {
                LogMessage?.Invoke("Failed to connect to device");
                return false;
            }

            LogMessage?.Invoke("Connected to device");

            return true;
        }

        // Private helper methods
        private bool CheckBaud()
        {
            try
            {
                LogMessage?.Invoke("=== Checking Baud Rate ===");
                byte[] command = { BSL_CMD_CHECK_BAUD };

                lock (_lockObject)
                {
                    LogMessage?.Invoke("Sending CheckBaud command...");
                    WritePort(command);

                    // Wait for response with multiple attempts
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            byte[] response = ReadPort(2000);

                            if (response.Length > 0)
                            {
                                LogMessage?.Invoke($"CheckBaud response received ({response.Length} bytes)");
                                AnalyzedResponse analyzed = AnalyzeResponse(response, "CHECK_BAUD");

                                return true;
                            }
                            else
                            {
                                LogMessage?.Invoke($"CheckBaud attempt {attempt + 1}: No response");
                            }
                        }
                        catch (TimeoutException)
                        {
                            LogMessage?.Invoke($"CheckBaud attempt {attempt + 1}: Timeout");
                        }

                        if (attempt < 2) Thread.Sleep(500);
                    }
                }

                LogMessage?.Invoke("CheckBaud failed after all attempts");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"CheckBaud error: {ex.Message}");
                return false;
            }
        }

        private bool Connect()
        {
            try
            {
                LogMessage?.Invoke("=== Connecting to Device ===");

                BMPackage package = new BMPackage
                {
                    Type = BSL_CMD_CONNECT,
                    Length = 0,
                    Data = new byte[0]
                };

                if(SendPackage(package))
                {
                    _connected = true;
                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Connect error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LoadFDLAsync()
        {
            try
            {
                // Load FDL1
                byte[] fdl1Data = GetFdl1Data();
                if (fdl1Data == null || fdl1Data.Length == 0)
                {
                    LogMessage?.Invoke("FDL1 data not available");
                    return false;
                }
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
                
                if (!ResetCRC())
                    return false;

                await Task.Delay(1000);
                
                // Load FDL2
                byte[] fdl2Data = GetFdl2Data();
                if (fdl2Data == null || fdl2Data.Length == 0)
                {
                    LogMessage?.Invoke("FDL2 data not available");
                    return false;
                }
                if (!WriteData(0x2200, fdl2Data, FDL2_ADDRESS))
                {
                    LogMessage?.Invoke("Failed to download FDL2");
                    return false;
                }
                
                // Execute FDL2
                if (!await ExecuteData())
                {
                    LogMessage?.Invoke("Failed to execute FDL2");
                    return false;
                }
                
                await Task.Delay(1000);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"LoadFDL error: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ExecuteData()
        {
            try
            {
                BMPackage package = new BMPackage
                {
                    Type = BSL_CMD_EXEC_DATA,
                    Length = 0,
                    Data = new byte[0]
                };
                
                if (!SendPackage(package))
                    return false;
                
                await Task.Delay(1000);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"ExecuteData error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EraseFlash(uint address, uint size)
        {
            try
            {
                LogMessage?.Invoke($"Erasing Flash : Address=0x{address:X8}, Size={size} bytes");

                byte[] header = new byte[8];
                WriteUInt32BE(address, header.AsSpan(0, 4));
                WriteUInt32BE(size, header.AsSpan(4, 4));
                
                BMPackage package = new BMPackage
                {
                    Type = BSL_CMD_ERASE_FLASH,
                    Length = (ushort) header.Length,
                    Data = header
                };

                if (!SendPackage(package))
                    return false;

                await Task.Delay(1000);
                LogMessage?.Invoke($"Flash Earsed : Address=0x{address:X8}, Size={size} bytes");

                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"ExecuteData error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Resest()
        {
            try
            {
                BMPackage package = new BMPackage
                {
                    Type = BSL_CMD_RESET,
                    Length = 0,
                    Data = new byte[0]
                };

                if (!SendPackage(package))
                    return false;

                await Task.Delay(1000);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"ExecuteData error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read a single chunk of flash memory
        /// </summary>
        private byte[] ReadFlashChunk(uint address, uint currentAddress, uint size)
        {
            try
            {
                byte[] header = new byte[12];          // 4 bytes address + 4 bytes size + 4 bytes currentAddress
                WriteUInt32BE(address, header.AsSpan(0, 4));
                WriteUInt32BE(size, header.AsSpan(4, 4));
                WriteUInt32BE(currentAddress, header.AsSpan(8, 4));

                BMPackage package = new BMPackage
                {
                    Type = BSL_CMD_READ_FLASH,
                    Length = (ushort)header.Length,
                    Data = header
                };

                lock (_lockObject)
                {
                    byte[] packageData = SerializePackage(package);
                    WritePort(packageData);

                    // Read response with timeout and multiple attempts
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            byte[] response = ReadPort(15000); // Longer timeout for flash reads

                            if (response.Length > 0)
                            {
                                AnalyzedResponse analyzed = AnalyzeResponse(response, "READ_FLASH_CHUNK");

                                if (analyzed.IsValid && analyzed.Type == BSL_REP_READ_FLASH)
                                {
                                    if (analyzed.Data != null && analyzed.Data.Length > 0)
                                    {
                                        Console.WriteLine($"Chunk read successful: {analyzed.Data.Length} bytes");
                                        return analyzed.Data;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Flash chunk response has no data");
                                    }
                                }
                                else if (analyzed.IsValid)
                                {
                                    Console.WriteLine($"Unexpected response type: 0x{analyzed.Type:X4}");
                                    if (analyzed.Type == BSL_REP_INVALID_CMD)
                                    {
                                        Console.WriteLine("Device reports invalid command - flash read may not be supported");
                                        return null;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Invalid response: {analyzed.ErrorMessage}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Flash chunk attempt {attempt + 1}: No response");
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine($"Flash chunk attempt {attempt + 1}: Timeout");
                        }

                        if (attempt < 2) Thread.Sleep(1000);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadFlashChunk error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Log data with timestamp and direction
        /// </summary>
        private void LogData(string direction, byte[] data, int length = -1)
        {
            return; 

            if (length == -1) length = data.Length;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string hexData = BitConverter.ToString(data, 0, length).Replace("-", " ");

            File.AppendAllText("Log.txt", $"[{timestamp}] {direction}: {hexData}\r\n");
            //LogMessage?.Invoke($"[{timestamp}] {direction}: {hexData}");

            // Also log ASCII representation if printable
            StringBuilder ascii = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                char c = (char)data[i];
                ascii.Append(char.IsControl(c) ? '.' : c);
            }

            if (length > 0)
            {
                File.AppendAllText("Log.txt", $"[{timestamp}] {direction} ASCII: {ascii}\r\n");
                //LogMessage?.Invoke($"[{timestamp}] {direction} ASCII: {ascii}");
            }
        }

        /// <summary>
        /// Enhanced serial port write with logging
        /// </summary>
        private void WritePort(byte[] data)
        {
            LogData("TX", data);
            _port.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Enhanced serial port read with logging
        /// </summary>
        private byte[] ReadPort(int timeout = -1)
        {
            int attempt = 0;

            if (timeout != -1)
            {
                _port.ReadTimeout = timeout;
            }

            while (_port.BytesToRead < 1 && attempt++ < 20)
            {
                Thread.Sleep(50); 
            }

            if(_port.BytesToRead < 1)
                return new byte[0];

            byte[] buffer = new byte[_port.BytesToRead];
            int bytesRead = _port.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                LogData("RX", buffer, bytesRead);
            }

            return buffer;
        }

        private bool SendPackage(BMPackage package)
        {
            try
            {
                lock (_lockObject)
                {
                    byte[] packageData = SerializePackage(package);
                    WritePort(packageData);

                    // Wait for ACK with multiple attempts
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            byte[] response = ReadPort(3000);

                            if (response.Length > 0)
                            {
                                BMPackage responsePackage = DeserializePackage(response, response.Length);
                                return responsePackage != null && responsePackage.Type == BSL_REP_ACK;
                            }
                            else
                            {
                                Console.WriteLine($"Package attempt {attempt + 1}: No response");
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine($"Package attempt {attempt + 1}: Timeout");
                        }

                        if (attempt < 2) Thread.Sleep(500);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"SendPackage error: {ex.Message}");
                return false;
            }
        }
        
        private byte[] SerializePackage(BMPackage package)
        {
            List<byte> packet = new List<byte>();
            
            // Add type and length in big-endian
            packet.AddRange(ToBigEndian(package.Type));
            packet.AddRange(ToBigEndian(package.Length));
            
            // Add data
            if (package.Data != null && package.Data.Length > 0)
            {
                packet.AddRange(package.Data);
            }
            
            // Calculate and add CRC
            ushort crc = _useCrc16L ? CalculateCrc16L(packet.ToArray()) : FrmChk(packet.ToArray());
            packet.AddRange(ToBigEndian(crc));
            
            // Apply HDLC escaping
            byte[] escapedPacket = ApplyHdlcEscaping(packet.ToArray());
            
            // Build final frame
            List<byte> frame = new List<byte>();
            frame.Add(HDLC_FLAG);
            frame.AddRange(escapedPacket);
            frame.Add(HDLC_FLAG);
            
            return frame.ToArray();
        }
        
        private BMPackage DeserializePackage(byte[] data, int length)
        {
            try
            {
                if (length < 3) return null;
                
                // Find frame boundaries
                int startIndex = -1, endIndex = -1;
                for (int i = 0; i < length; i++)
                {
                    if (data[i] == HDLC_FLAG)
                    {
                        if (startIndex == -1)
                            startIndex = i;
                        else
                        {
                            endIndex = i;
                            break;
                        }
                    }
                }
                
                if (startIndex == -1 || endIndex == -1) return null;
                
                // Extract and unescape packet data
                byte[] frameData = new byte[endIndex - startIndex - 1];
                Array.Copy(data, startIndex + 1, frameData, 0, frameData.Length);
                byte[] unescapedData = RemoveHdlcEscaping(frameData);
                
                if (unescapedData.Length < 6) return null;
                
                // Parse packet
                ushort type = FromBigEndian16(unescapedData, 0);
                ushort dataLength = FromBigEndian16(unescapedData, 2);
                
                BMPackage package = new BMPackage
                {
                    Type = type,
                    Length = dataLength
                };
                
                if (dataLength > 0)
                {
                    package.Data = new byte[dataLength];
                    Array.Copy(unescapedData, 4, package.Data, 0, dataLength);
                }
                
                return package;
            }
            catch
            {
                return null;
            }
        }
        
        // Utility methods
        private byte[] ToBigEndian(ushort value)
        {
            return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        }
        
        private ushort FromBigEndian16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
        
        private byte[] ApplyHdlcEscaping(byte[] data)
        {
            List<byte> escaped = new List<byte>();
            
            foreach (byte b in data)
            {
                if (b == HDLC_FLAG || b == HDLC_ESCAPE)
                {
                    escaped.Add(HDLC_ESCAPE);
                    escaped.Add((byte)(b ^ HDLC_ESCAPE_XOR));
                }
                else
                {
                    escaped.Add(b);
                }
            }
            
            return escaped.ToArray();
        }
        
        private byte[] RemoveHdlcEscaping(byte[] data)
        {
            List<byte> unescaped = new List<byte>();
            
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == HDLC_ESCAPE && i + 1 < data.Length)
                {
                    unescaped.Add((byte)(data[i + 1] ^ HDLC_ESCAPE_XOR));
                    i++; // Skip next byte
                }
                else
                {
                    unescaped.Add(data[i]);
                }
            }
            
            return unescaped.ToArray();
        }

        /// <summary>
        /// Analyze and display response data, returning parsed structure
        /// </summary>
        private AnalyzedResponse AnalyzeResponse(byte[] response, string context)
        {
            Console.WriteLine($"=== Response Analysis ({context}) ===");
            Console.WriteLine($"Response Length: {response.Length} bytes");

            AnalyzedResponse result = new AnalyzedResponse
            {
                RawData = response,
                IsValid = false
            };

            if (response.Length == 0)
            {
                Console.WriteLine("Empty response");
                result.ErrorMessage = "Empty response";
                Console.WriteLine("=== End Response Analysis ===");
                return result;
            }

            // Check for HDLC framing
            bool hasHdlcFraming = response.Length >= 2 && response[0] == 0x7E && response[response.Length - 1] == 0x7E;
            result.HasHdlcFraming = hasHdlcFraming;
            Console.WriteLine($"HDLC Framing: {hasHdlcFraming}");

            if (hasHdlcFraming && response.Length >= 7) // Minimum: 7E + TYPE(2) + LEN(2) + CRC(2) + 7E
            {
                try
                {
                    // Extract packet content (remove HDLC flags)
                    byte[] packetContent = new byte[response.Length - 2];
                    Array.Copy(response, 1, packetContent, 0, packetContent.Length);

                    // Remove HDLC escaping
                    byte[] unescapedContent = RemoveHdlcEscaping(packetContent);

                    if (unescapedContent.Length >= 4)
                    {
                        result.Type = FromBigEndian16(unescapedContent, 0);
                        result.Length = FromBigEndian16(unescapedContent, 2);

                        Console.WriteLine($"Packet Type: 0x{result.Type:X4}");
                        Console.WriteLine($"Packet Length: {result.Length}");

                        if (unescapedContent.Length >= 4 + result.Length + 2)
                        {
                            // Extract data
                            result.Data = new byte[result.Length];
                            if (result.Length > 0)
                            {
                                Array.Copy(unescapedContent, 4, result.Data, 0, result.Length);
                            }

                            // Extract and validate CRC
                            result.ReceivedCrc = FromBigEndian16(unescapedContent, 4 + result.Length);

                            result.CalculatedCrc = CalculateCrc16L(unescapedContent, 0, 4 + result.Length);
                            if (!(result.ReceivedCrc == result.CalculatedCrc))
                            {
                                result.CalculatedCrc = FrmChk(unescapedContent, 0, 4 + result.Length);
                                if (result.ReceivedCrc == result.CalculatedCrc)
                                    _useCrc16L = false;
                            } else
                            {
                                _useCrc16L = true;
                            }

                                result.CrcValid = result.ReceivedCrc == result.CalculatedCrc;

                            Console.WriteLine($"Data: {(result.Data.Length > 0 ? BitConverter.ToString(result.Data) : "None")}");
                            Console.WriteLine($"CRC: Received=0x{result.ReceivedCrc:X4}, Calculated=0x{result.CalculatedCrc:X4}, Valid={result.CrcValid}");

                            // Interpret common response types
                            InterpretResponseType(result.Type, result.Data);

                            result.IsValid = true;
                        }
                        else
                        {
                            result.ErrorMessage = "Insufficient data for complete packet";
                            Console.WriteLine(result.ErrorMessage);
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "Insufficient data for packet header";
                        Console.WriteLine(result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Error parsing response: {ex.Message}";
                    Console.WriteLine(result.ErrorMessage);
                }
            }
            else
            {
                Console.WriteLine("Response doesn't match expected HDLC packet format");

                // Check for common patterns (simple responses without HDLC framing)
                if (response.Length >= 1)
                {
                    result.Type = response[0];
                    Console.WriteLine($"First byte: 0x{response[0]:X2}");

                    if (response[0] == 0x81)
                    {
                        Console.WriteLine("Possible BSL_REP_VER response");
                        result.IsValid = true;
                        if (response.Length > 1)
                        {
                            result.Data = new byte[response.Length - 1];
                            Array.Copy(response, 1, result.Data, 0, result.Data.Length);
                            result.Length = (ushort)result.Data.Length;
                        }
                    }
                    else if (response[0] == 0x80)
                    {
                        Console.WriteLine("Possible BSL_REP_ACK response");
                        result.IsValid = true;
                    }
                    else if (response[0] == 0x82)
                    {
                        Console.WriteLine("Possible BSL_REP_INVALID_CMD response");
                        result.IsValid = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Unknown response format";
                    }
                }
                else
                {
                    result.ErrorMessage = "Response too short";
                }
            }

            Console.WriteLine("=== End Response Analysis ===");
            return result;
        }

        /// <summary>
        /// Interpret response type
        /// </summary>
        private void InterpretResponseType(ushort type, byte[] data)
        {
            switch (type)
            {
                case 0x80: // BSL_REP_ACK
                    Console.WriteLine("Response: ACK");
                    break;
                case 0x81: // BSL_REP_VER
                    Console.WriteLine("Response: Version Information");
                    break;
                case 0x82: // BSL_REP_INVALID_CMD
                    Console.WriteLine("Response: Invalid Command");
                    break;
                case 0x93: // BSL_REP_READ_FLASH
                    Console.WriteLine("Response: Flash Read Data");
                    if (data != null && data.Length > 0)
                    {
                        Console.WriteLine($"Flash data received: {data.Length} bytes");
                        // Show first few bytes for verification
                        int previewLen = Math.Min(16, data.Length);
                        string preview = BitConverter.ToString(data, 0, previewLen);
                        Console.WriteLine($"First {previewLen} bytes: {preview}");
                    }
                    break;
                case 0x95: // BSL_REP_READ_NVITEM
                    Console.WriteLine("Response: NV Item Read");
                    break;
                default:
                    Console.WriteLine($"Response: Unknown type 0x{type:X4}");
                    break;
            }
        }

        /// <summary>
        /// Calculate CRC-16 with polynomial 0x8005
        /// </summary>
        private ushort CalculateCrc16(byte[] data, int offset = 0, int length = -1)
        {
            if (length == -1)
                length = data.Length - offset;

            ushort crc = 0;
            for (int i = offset; i < offset + length; i++)
            {
                crc = (ushort)((crc >> 8) ^ Crc16Table[(crc ^ data[i]) & 0xFF]);
            }
            return crc;
        }

        /// <summary>Internet one�s-complement 16-bit sum ("frmChk").</summary>
        public static ushort FrmChk(byte[] bytes, int offset = 0, int length = -1,
                                   bool bigEndian = true,
                                   ushort init = 0,
                                   bool finalComplement = true)
        {
            uint sum = init;           // 32-bit to hold carries
            int i = offset;
            if (length == -1)
                length = bytes.Length;

            // ---- main 16-bit words ----
            for (; i + 1 < length; i += 2)
            {
                ushort word = bigEndian
                    ? (ushort)((bytes[i] << 8) | bytes[i + 1])
                    : (ushort)((bytes[i]) | (bytes[i + 1] << 8));

                sum += word;
                sum = (sum & 0xFFFF) + (sum >> 16);   // end-around carry
            }

            // ---- odd trailing byte ----
            if (i < length)
            {
                ushort word = bigEndian ? (ushort)(bytes[i] << 8)
                                         : (ushort)bytes[i];
                sum += word;
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            if (finalComplement) sum = ~sum;
            return (ushort)(sum & 0xFFFF);
        }

        /// <summary>
        /// Calculate CRC-16-L with polynomial 0x1021 (CCITT/X.25)
        /// </summary>
        private ushort CalculateCrc16L(byte[] data, int offset = 0, int length = -1)
        {
            if (length == -1)
                length = data.Length - offset;

            ushort crc = 0;
            for (int i = offset; i < offset + length; i++)
            {
                byte b = data[i];
                for (int bit = 0x80; bit != 0; bit >>= 1)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)(crc << 1);
                        crc = (ushort)(crc ^ CRC16L_POLY);
                    }
                    else
                    {
                        crc = (ushort)(crc << 1);
                    }

                    if ((b & bit) != 0)
                    {
                        crc = (ushort)(crc ^ CRC16L_POLY);
                    }
                }
            }

            //if (CRC16L_INIT == 0x0)
            return crc;
            //else
            //    return (ushort)~crc;
        }

        public uint DoNVCheckSum(byte[] data)
        {
            if (data == null || data.Length < 2)
                throw new ArgumentException("Buffer is too small.");

            // Calculate CRC-16 (starting from offset 2)
            ushort crc = CalculateCrc16L(data, 2, data.Length - 2);

            // Write CRC as big-endian
            data[0] = (byte)(crc >> 8);
            data[1] = (byte)(crc & 0xFF);

            // Sum all bytes
            uint sum = 0;
            foreach (byte b in data)
            {
                sum += b;
            }

            return sum;
        }

        private byte[] BuildStartDataHeader(uint address, uint length, int checksum = -1)
        {
            byte[] header = new byte[(checksum > -1 ? 12 : 8)];
            WriteUInt32BE(address, header.AsSpan(0, 4));
            WriteUInt32BE(length, header.AsSpan(4, 4));
            if(checksum > -1)
                WriteUInt32BE((uint) checksum, header.AsSpan(8, 4));

            return header;
        }
        
        private void WriteUInt32BE(uint value, Span<byte> dest)
        {
            dest[0] = (byte)(value >> 24);
            dest[1] = (byte)(value >> 16);
            dest[2] = (byte)(value >> 8);
            dest[3] = (byte)value;
        }
        
        private class BMPackage
        {
            public ushort Type { get; set; }
            public ushort Length { get; set; }
            public byte[] Data { get; set; }
        }

        /// <summary>
        /// Analyzed response structure
        /// </summary>
        private class AnalyzedResponse
        {
            public bool IsValid { get; set; }
            public bool HasHdlcFraming { get; set; }
            public ushort Type { get; set; }
            public ushort Length { get; set; }
            public byte[] Data { get; set; }
            public ushort ReceivedCrc { get; set; }
            public ushort CalculatedCrc { get; set; }
            public bool CrcValid { get; set; }
            public string ErrorMessage { get; set; }
            public byte[] RawData { get; set; }

            public BMPackage ToBMPackage()
            {
                if (!IsValid) return null;

                return new BMPackage
                {
                    Type = Type,
                    Length = Length,
                    Data = Data
                };
            }
        }

        /// <summary>
        /// Set the device profile to use for NV operations
        /// </summary>
        public void SetDeviceProfile(DeviceProfile profile)
        {
            _deviceProfile = profile;
            LogMessage?.Invoke($"Device profile set: {profile?.Name ?? "None"}");
        }

        /// <summary>
        /// Get the current NV base address from profile or default
        /// </summary>
        private uint GetNVBaseAddress()
        {
            return _deviceProfile?.NVBaseAddress ?? 0x90000001;
        }

        /// <summary>
        /// Get the current NV data size from profile or default
        /// </summary>
        private int GetNVDataSize()
        {
            return _deviceProfile?.NVDataSize ?? 0x0B0BA8;
        }

        /// <summary>
        /// Get FDL1 data from profile (embedded) or file
        /// </summary>
        private byte[] GetFdl1Data()
        {
            // First check if profile has embedded data
            if (_deviceProfile?.HasEmbeddedFdl1 == true)
            {
                LogMessage?.Invoke("Using embedded FDL1 data from profile");
                return _deviceProfile.Fdl1Data;
            }

            // Fall back to file
            string fdl1Path = _deviceProfile?.Fdl1Path ?? "fdl1.bin";
            if (!Path.IsPathRooted(fdl1Path))
                fdl1Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fdl1Path);

            if (File.Exists(fdl1Path))
            {
                LogMessage?.Invoke($"Loading FDL1 from file: {fdl1Path}");
                try
                {
                    return File.ReadAllBytes(fdl1Path);
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error reading FDL1 file: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Get FDL2 data from profile (embedded) or file
        /// </summary>
        private byte[] GetFdl2Data()
        {
            // First check if profile has embedded data
            if (_deviceProfile?.HasEmbeddedFdl2 == true)
            {
                LogMessage?.Invoke("Using embedded FDL2 data from profile");
                return _deviceProfile.Fdl2Data;
            }

            // Fall back to file
            string fdl2Path = _deviceProfile?.Fdl2Path ?? "fdl2.bin";
            if (!Path.IsPathRooted(fdl2Path))
                fdl2Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fdl2Path);

            if (File.Exists(fdl2Path))
            {
                LogMessage?.Invoke($"Loading FDL2 from file: {fdl2Path}");
                try
                {
                    return File.ReadAllBytes(fdl2Path);
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error reading FDL2 file: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Get FDL1 path from profile or default
        /// </summary>
        public string GetFdl1Path()
        {
            return _deviceProfile?.Fdl1Path ?? "fdl1.bin";
        }

        /// <summary>
        /// Get FDL2 path from profile or default
        /// </summary>
        public string GetFdl2Path()
        {
            return _deviceProfile?.Fdl2Path ?? "fdl2.bin";
        }

    }
}