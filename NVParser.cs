using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using SPRDNVEditor;

public enum NVItemType
{
    Binary,
    String,
    UInt32,
    UInt16,
    UInt8,
    IMEI,
    MAC,
    IPv4,
    Custom
}

public class NVTypeDefinition
{
    public ushort Id { get; set; }
    public NVItemType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CustomFormat { get; set; }
}

public class NVItem
{
    public ushort Id { get; set; }
    public byte[] Data { get; set; }
    public int Offset { get; set; }
    public NVItemType Type { get; set; } = NVItemType.Binary;
    public string Name { get; set; }
    public string Description { get; set; }

    public NVItem(ushort id, byte[] data, int offset)
    {
        Id = id;
        Data = data;
        Offset = offset;
    }

    public int Length => Data.Length;

    public string Text
    {
        get => ConvertDataToText();
        set => ConvertTextToData(value);
    }

    public string Hex
    {
        get => BitConverter.ToString(Data).Replace("-", " ").ToLower();
        set => ConvertHexToData(value);
    }

    private string ConvertDataToText()
    {
        if (Data == null || Data.Length == 0)
            return "";

        switch (Type)
        {
            case NVItemType.String:
                return Encoding.UTF8.GetString(Data).TrimEnd('\0');
            
            case NVItemType.UInt32:
                return Data.Length >= 4 ? BitConverter.ToUInt32(Data, 0).ToString() : "";
            
            case NVItemType.UInt16:
                return Data.Length >= 2 ? BitConverter.ToUInt16(Data, 0).ToString() : "";
            
            case NVItemType.UInt8:
                return Data.Length >= 1 ? Data[0].ToString() : "";
            
            case NVItemType.IMEI:
                return ConvertIMEI(Data);
            
            case NVItemType.MAC:
                return ConvertMAC(Data);
            
            case NVItemType.IPv4:
                return Data.Length >= 4 ? $"{Data[0]}.{Data[1]}.{Data[2]}.{Data[3]}" : "";
            
            case NVItemType.Binary:
            default:
                return BitConverter.ToString(Data).Replace("-", "");
        }
    }

    private void ConvertTextToData(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Data = new byte[0];
            return;
        }

        switch (Type)
        {
            case NVItemType.String:
                Data = Encoding.UTF8.GetBytes(value);
                break;
            
            case NVItemType.UInt32:
                if (uint.TryParse(value, out uint uintVal))
                    Data = BitConverter.GetBytes(uintVal);
                break;
            
            case NVItemType.UInt16:
                if (ushort.TryParse(value, out ushort ushortVal))
                    Data = BitConverter.GetBytes(ushortVal);
                break;
            
            case NVItemType.UInt8:
                if (byte.TryParse(value, out byte byteVal))
                    Data = new byte[] { byteVal };
                break;
            
            case NVItemType.IMEI:
                Data = ConvertIMEIToData(value);
                break;
            
            case NVItemType.MAC:
                Data = ConvertMACToData(value);
                break;
            
            case NVItemType.IPv4:
                Data = ConvertIPv4ToData(value);
                break;
            
            case NVItemType.Binary:
            default:
                Data = ConvertHexStringToData(value);
                break;
        }
    }

    private void ConvertHexToData(string hexValue)
    {
        if (string.IsNullOrEmpty(hexValue))
        {
            Data = new byte[0];
            return;
        }

        try
        {
            string cleanHex = hexValue.Replace(" ", "").Replace("-", "");
            if (cleanHex.Length % 2 != 0)
                cleanHex = "0" + cleanHex;

            Data = Enumerable.Range(0, cleanHex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(cleanHex.Substring(x, 2), 16))
                            .ToArray();
        }
        catch
        {
            Data = new byte[0];
        }
    }

    private string ConvertIMEI(byte[] bytes)
    {
        // IMEI is always 8 bytes (16 nibbles) in this encoding
        if (bytes == null || bytes.Length != 8)
            return string.Empty;

        var sb = new StringBuilder(16);

        // Build a 16‑character string: low nibble first, then high nibble
        foreach (byte b in bytes)
        {
            sb.Append((b & 0x0F).ToString("X"));        // low  nibble
            sb.Append(((b >> 4) & 0x0F).ToString("X")); // high nibble
        }

        // Throw away the first (padding) nibble → keep 15 digits
        string imei = sb.ToString().Substring(1, 15);

        Console.WriteLine("Hex To Imei : " + imei);
        return imei;

    }

    private byte[] ConvertIMEIToData(string imei)
    {
        // basic validation ------------------------------------------------------
        if (string.IsNullOrEmpty(imei)) return Array.Empty<byte>();
        imei = imei.Trim();
        if (imei.Length != 15 || !ulong.TryParse(imei, out _))
            return Array.Empty<byte>();

        byte[] result = new byte[8];

        // helper to convert one decimal char to a 4‑bit value
        static int Nibble(char c) => c - '0';    //  '0'..'9'  → 0..9

        // ---------- 1st byte: high‑nibble = first digit, low‑nibble = 0xA ------
        result[0] = (byte)((Nibble(imei[0]) << 4) | 0x0A);

        // ---------- remaining 7 bytes: swapped‑nibble pairs --------------------
        int byteIndex = 1;
        for (int i = 1; i < 15; i += 2)
        {
            int low = Nibble(imei[i]);      // first digit of the pair
            int high = Nibble(imei[i + 1]);  // second digit of the pair

            result[byteIndex++] = (byte)((high << 4) | low);
        }

        // (optional) debug printout
        Console.WriteLine("IMEI To Hex : " + BitConverter.ToString(result).Replace("-", " "));
        return result;
    }

    private string ConvertMAC(byte[] data)
    {
        if (data.Length < 6) return "";
        return string.Join(":", data.Take(6).Select(b => b.ToString("X2")));
    }

    private byte[] ConvertMACToData(string mac)
    {
        if (string.IsNullOrEmpty(mac))
            return new byte[6];
        
        try
        {
            var parts = mac.Split(':');
            if (parts.Length != 6)
                return new byte[6];
            
            var data = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                data[i] = Convert.ToByte(parts[i], 16);
            }
            return data;
        }
        catch
        {
            return new byte[6];
        }
    }

    private byte[] ConvertIPv4ToData(string ip)
    {
        if (string.IsNullOrEmpty(ip))
            return new byte[4];
        
        try
        {
            var parts = ip.Split('.');
            if (parts.Length != 4)
                return new byte[4];
            
            var data = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                data[i] = Convert.ToByte(parts[i]);
            }
            return data;
        }
        catch
        {
            return new byte[4];
        }
    }

    private byte[] ConvertHexStringToData(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return new byte[0];
        
        try
        {
            string cleanHex = hex.Replace(" ", "").Replace("-", "");
            if (cleanHex.Length % 2 != 0)
                cleanHex = "0" + cleanHex;

            return Enumerable.Range(0, cleanHex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(cleanHex.Substring(x, 2), 16))
                            .ToArray();
        }
        catch
        {
            return new byte[0];
        }
    }
}

public static class NVParser
{
    private const int HEADER_SIZE = 4;
    private const int ALIGN = 4;
    private const int CHUNK_SIZE = 256; // Process in 256-byte chunks for better performance
    
    private static Dictionary<ushort, NVTypeDefinition> _typeDefinitions = new Dictionary<ushort, NVTypeDefinition>();
    private static Dictionary<string, NVItemMapping> _profileMappings = null;

    static NVParser()
    {
        InitializeDefaultTypes();
    }

    public static List<NVItem> ParseNV(byte[] buffer)
    {
        if (buffer.Length < HEADER_SIZE)
            throw new Exception("File too short.");

        return ParseNVChunked(buffer);
    }

    private static List<NVItem> ParseNVChunked(byte[] buffer)
    {
        var items = new List<NVItem>();
        int offset = HEADER_SIZE;
        
        // Process in chunks for better performance
        while (offset < buffer.Length)
        {
            int chunkEnd = Math.Min(offset + CHUNK_SIZE * 1024, buffer.Length); // 256KB chunks
            var chunkItems = ParseChunk(buffer, offset, chunkEnd);
            items.AddRange(chunkItems);
            
            if (chunkItems.Count == 0)
                break;
                
            offset = chunkItems.Last().Offset + chunkItems.Last().Length;
            offset = (offset + (ALIGN - 1)) & ~(ALIGN - 1);
        }
        
        // Apply type definitions
        ApplyTypeDefinitions(items);
        
        return items;
    }

    private static List<NVItem> ParseChunk(byte[] buffer, int startOffset, int endOffset)
    {
        var items = new List<NVItem>();
        int offset = startOffset;

        while (offset + 4 <= endOffset && offset + 4 <= buffer.Length)
        {
            ushort id = BitConverter.ToUInt16(buffer, offset);
            ushort len = BitConverter.ToUInt16(buffer, offset + 2);
            offset += 4;

            if (offset + len > buffer.Length)
                break;

            byte[] slice = new byte[len];
            Array.Copy(buffer, offset, slice, 0, len);
            items.Add(new NVItem(id, slice, offset));

            offset += len;
            offset = (offset + (ALIGN - 1)) & ~(ALIGN - 1);
        }
        
        return items;
    }

    private static void ApplyTypeDefinitions(List<NVItem> items)
    {
        foreach (var item in items)
        {
            // First check profile mappings
            var key = $"NV_{item.Id:X4}";
            if (_profileMappings != null && _profileMappings.TryGetValue(key, out var mapping))
            {
                item.Type = mapping.Type;
                item.Name = mapping.Name;
                item.Description = mapping.Description;
            }
            // Then check type definitions
            else if (_typeDefinitions.TryGetValue(item.Id, out var typeDef))
            {
                item.Type = typeDef.Type;
                item.Name = typeDef.Name;
                item.Description = typeDef.Description;
            }
            else
            {
                // Try to infer type from data
                item.Type = InferTypeFromData(item.Data);
            }
        }
    }

    private static NVItemType InferTypeFromData(byte[] data)
    {
        if (data.Length == 0)
            return NVItemType.Binary;

        // Check for IMEI pattern (15 digits in BCD format)
        if (data.Length == 8 && IsValidIMEI(data))
            return NVItemType.IMEI;

        // Check for MAC address pattern
        if (data.Length == 6 && IsValidMAC(data))
            return NVItemType.MAC;

        // Check for IPv4 pattern
        if (data.Length == 4)
            return NVItemType.IPv4;

        // Check for string pattern (printable ASCII)
        if (IsStringData(data))
            return NVItemType.String;

        // Check for integer patterns
        if (data.Length == 4)
            return NVItemType.UInt32;
        if (data.Length == 2)
            return NVItemType.UInt16;
        if (data.Length == 1)
            return NVItemType.UInt8;

        return NVItemType.Binary;
    }

    private static bool IsValidIMEI(byte[] data)
    {
        if (data.Length != 8) return false;
        
        try
        {
            for (int i = 0; i < 8; i++)
            {
                byte b = data[i];
                int digit1 = b & 0x0F;
                int digit2 = (b & 0xF0) >> 4;
                
                if (digit1 > 9 || digit2 > 9)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidMAC(byte[] data)
    {
        if (data.Length != 6) return false;
        
        // Check if all zeros (invalid MAC)
        if (data.All(b => b == 0))
            return false;
        
        // Check if all 0xFF (invalid MAC)
        if (data.All(b => b == 0xFF))
            return false;
        
        return true;
    }

    private static bool IsStringData(byte[] data)
    {
        if (data.Length == 0) return false;
        
        int printableCount = 0;
        int nullCount = 0;
        
        foreach (byte b in data)
        {
            if (b == 0)
                nullCount++;
            else if (b >= 32 && b <= 126) // Printable ASCII
                printableCount++;
        }
        
        // Consider it a string if most characters are printable
        return (printableCount + nullCount) >= data.Length * 0.8;
    }

    public static byte[] BuildNV(List<NVItem> items)
    {
        using (var ms = new MemoryStream())
        {
            ms.Write(new byte[HEADER_SIZE], 0, HEADER_SIZE);

            foreach (var item in items)
            {
                byte[] header = new byte[4];
                BitConverter.GetBytes(item.Id).CopyTo(header, 0);
                BitConverter.GetBytes((ushort)item.Length).CopyTo(header, 2);

                ms.Write(header, 0, 4);
                ms.Write(item.Data, 0, item.Data.Length);

                int pad = (ALIGN - (item.Length % ALIGN)) % ALIGN;
                if (pad > 0)
                    ms.Write(new byte[pad], 0, pad);
            }
            return ms.ToArray();
        }
    }

    public static void SetTypeDefinition(ushort id, NVItemType type, string name = null, string description = null)
    {
        var typeDef = new NVTypeDefinition
        {
            Id = id,
            Type = type,
            Name = name ?? $"NV_{id:04X}",
            Description = description ?? ""
        };
        
        _typeDefinitions[id] = typeDef;
    }

    public static NVTypeDefinition GetTypeDefinition(ushort id)
    {
        return _typeDefinitions.TryGetValue(id, out var typeDef) ? typeDef : null;
    }

    public static Dictionary<ushort, NVTypeDefinition> GetAllTypeDefinitions()
    {
        return new Dictionary<ushort, NVTypeDefinition>(_typeDefinitions);
    }

    public static void SetProfileMappings(Dictionary<string, NVItemMapping> mappings)
    {
        _profileMappings = mappings;
    }

    private static void InitializeDefaultTypes()
    {
        // Add common SPRD NV parameter types
        var defaultTypes = new Dictionary<ushort, (NVItemType type, string name, string description)>
        {
            { 0x0005, (NVItemType.IMEI, "IMEI1", "International Mobile Equipment Identity") },
            { 0x0179, (NVItemType.IMEI, "IMEI2", "International Mobile Equipment Identity") },
        };

        foreach (var kvp in defaultTypes)
        {
            if (!_typeDefinitions.ContainsKey(kvp.Key))
            {
                _typeDefinitions[kvp.Key] = new NVTypeDefinition
                {
                    Id = kvp.Key,
                    Type = kvp.Value.type,
                    Name = kvp.Value.name,
                    Description = kvp.Value.description
                };
            }
        }
    }
}
