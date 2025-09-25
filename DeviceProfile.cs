using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPRDNVEditor
{
    /// <summary>
    /// Represents a device profile containing configuration settings for specific SPRD devices.
    /// Profiles include NV memory layout, FDL files, flash definitions, and NV item mappings.
    /// </summary>
    public class DeviceProfile
    {
        /// <summary>Gets or sets the display name of this device profile.</summary>
        public string Name { get; set; } = "";
        
        /// <summary>Gets or sets the description of this device profile.</summary>
        public string Description { get; set; } = "";
        
        /// <summary>Gets or sets the file path to the FDL1 bootloader file.</summary>
        public string Fdl1Path { get; set; } = "fdl1.bin";
        
        /// <summary>Gets or sets the file path to the FDL2 bootloader file.</summary>
        public string Fdl2Path { get; set; } = "fdl2.bin";
        
        /// <summary>Gets or sets the embedded FDL1 bootloader data.</summary>
        public byte[]? Fdl1Data { get; set; }
        
        /// <summary>Gets or sets the embedded FDL2 bootloader data.</summary>
        public byte[]? Fdl2Data { get; set; }
        
        /// <summary>Gets or sets the base address for NV parameters in device memory.</summary>
        public uint NVBaseAddress { get; set; } = 0x90000001;
        
        /// <summary>Gets or sets the total size of NV data in bytes.</summary>
        public int NVDataSize { get; set; } = 0x0B0BA8;
        
        /// <summary>Gets or sets the chunk size for reading data from device.</summary>
        public int ReadChunkSize { get; set; } = 12288;
        
        /// <summary>Gets or sets the NV item type mappings for this device.</summary>
        public Dictionary<string, NVItemMapping> NVItemMappings { get; set; } = new Dictionary<string, NVItemMapping>();
        
        /// <summary>Gets or sets the flash file definitions for additional memory regions.</summary>
        public List<FlashFileDefinition> FlashFiles { get; set; } = new List<FlashFileDefinition>();
        
        [JsonIgnore]
        public bool IsModified { get; set; }
        
        [JsonIgnore]
        public string FilePath { get; set; } = "";

        public DeviceProfile Clone()
        {
            var json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<DeviceProfile>(json) ?? new DeviceProfile();
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>Gets a value indicating whether this profile has embedded FDL1 data.</summary>
        [JsonIgnore]
        public bool HasEmbeddedFdl1 => Fdl1Data != null && Fdl1Data.Length > 0;

        /// <summary>Gets a value indicating whether this profile has embedded FDL2 data.</summary>
        [JsonIgnore]
        public bool HasEmbeddedFdl2 => Fdl2Data != null && Fdl2Data.Length > 0;
    }

    /// <summary>
    /// Represents a mapping configuration for an NV item, defining its type and display properties.
    /// </summary>
    public class NVItemMapping
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public NVItemType Type { get; set; } = NVItemType.Binary;
        public int Size { get; set; }
        public int Offset { get; set; }
    }

    /// <summary>
    /// Defines a flash memory region that can be read from the device.
    /// </summary>
    public class FlashFileDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public uint BaseAddress { get; set; }
        public uint Size { get; set; }
        public string FileName { get; set; } = "";
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Returns a string representation of this flash file definition.
        /// </summary>
        /// <returns>A formatted string showing name, base address, and size.</returns>
        public override string ToString()
        {
            return $"{Name} (0x{BaseAddress:X8}, 0x{Size:X})";
        }
    }
}