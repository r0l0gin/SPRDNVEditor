using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPRDNVEditor
{
    public class DeviceProfileManager
    {
        private const string ProfilesDirectory = "Profiles";
        private const string ProfileExtension = ".sprdprofile";
        private DeviceProfile? _currentProfile;
        private List<DeviceProfile> _profiles = new List<DeviceProfile>();

        public DeviceProfile? CurrentProfile => _currentProfile;
        public IReadOnlyList<DeviceProfile> Profiles => _profiles.AsReadOnly();

        public event EventHandler<DeviceProfile>? ProfileChanged;

        public DeviceProfileManager()
        {
            EnsureDirectoriesExist();
            LoadAllProfiles();
        }

        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(ProfilesDirectory))
                Directory.CreateDirectory(ProfilesDirectory);
        }

        public void LoadAllProfiles()
        {
            _profiles.Clear();

            // Load user profiles
            LoadProfilesFromDirectory(ProfilesDirectory, isDefault: false);

            // If no current profile and we have profiles, select the first one
            if (_currentProfile == null && _profiles.Count > 0)
            {
                SetCurrentProfile(_profiles[0]);
            }
        }

        private void LoadProfilesFromDirectory(string directory, bool isDefault)
        {
            var profileFiles = Directory.GetFiles(directory, "*" + ProfileExtension);

            foreach (var file in profileFiles)
            {
                try
                {
                    var profile = LoadProfileFromFile(file);
                    if (profile != null)
                    {
                        profile.FilePath = file;
                        _profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load profile {file}: {ex.Message}");
                }
            }
        }

        public DeviceProfile? LoadProfileFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new JsonStringEnumConverter());

                var profile = JsonSerializer.Deserialize<DeviceProfile>(json, options);
                if (profile != null)
                {
                    profile.FilePath = filePath;

                    // Migration: Embed FDL files if they exist and aren't already embedded
                    bool needsSave = false;

                    if (!profile.HasEmbeddedFdl1 && !string.IsNullOrEmpty(profile.Fdl1Path))
                    {
                        var fdl1Path = profile.Fdl1Path;
                        if (!Path.IsPathRooted(fdl1Path))
                        {
                            fdl1Path = Path.Combine(Path.GetDirectoryName(filePath) ?? "", fdl1Path);
                        }

                        if (File.Exists(fdl1Path))
                        {
                            try
                            {
                                profile.Fdl1Data = File.ReadAllBytes(fdl1Path);
                                needsSave = true;
                                System.Diagnostics.Debug.WriteLine($"Migrated FDL1 for profile {profile.Name}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to migrate FDL1 for {profile.Name}: {ex.Message}");
                            }
                        }
                    }

                    if (!profile.HasEmbeddedFdl2 && !string.IsNullOrEmpty(profile.Fdl2Path))
                    {
                        var fdl2Path = profile.Fdl2Path;
                        if (!Path.IsPathRooted(fdl2Path))
                        {
                            fdl2Path = Path.Combine(Path.GetDirectoryName(filePath) ?? "", fdl2Path);
                        }

                        if (File.Exists(fdl2Path))
                        {
                            try
                            {
                                profile.Fdl2Data = File.ReadAllBytes(fdl2Path);
                                needsSave = true;
                                System.Diagnostics.Debug.WriteLine($"Migrated FDL2 for profile {profile.Name}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to migrate FDL2 for {profile.Name}: {ex.Message}");
                            }
                        }
                    }

                    // Auto-save the migrated profile
                    if (needsSave)
                    {
                        SaveProfile(profile, filePath);
                    }
                }
                return profile;
            }
            catch
            {
                return null;
            }
        }

        public bool SaveProfile(DeviceProfile profile, string? filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = profile.FilePath;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(ProfilesDirectory, $"{profile.Name}{ProfileExtension}");
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new JsonStringEnumConverter());

                var json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(filePath, json);

                profile.FilePath = filePath;
                profile.IsModified = false;

                // Update the profile in our list
                var existingIndex = _profiles.FindIndex(p => p.FilePath == filePath);
                if (existingIndex >= 0)
                {
                    _profiles[existingIndex] = profile;
                }
                else
                {
                    _profiles.Add(profile);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetCurrentProfile(DeviceProfile profile)
        {
            _currentProfile = profile;
            ProfileChanged?.Invoke(this, profile);
        }

        public void CreateNewProfile(string name)
        {
            var profile = new DeviceProfile
            {
                Name = name,
                Description = $"Profile for {name}"
            };

            // Initialize with some common NV item mappings
            profile.NVItemMappings["IMEI1"] = new NVItemMapping
            {
                Id = 1,
                Name = "IMEI1",
                Description = "First IMEI",
                Type = NVItemType.IMEI,
                Size = 15
            };

            profile.NVItemMappings["IMEI2"] = new NVItemMapping
            {
                Id = 160, // As mentioned, this varies by device
                Name = "IMEI2",
                Description = "Second IMEI",
                Type = NVItemType.IMEI,
                Size = 15
            };

            // Add some example flash file definitions
            profile.FlashFiles.Add(new FlashFileDefinition
            {
                Name = "Bootloader",
                Description = "Device bootloader region",
                BaseAddress = 0x80000000,
                Size = 0x10000,
                FileName = "bootloader.bin",
                Enabled = false
            });

            profile.FlashFiles.Add(new FlashFileDefinition
            {
                Name = "Calibration Data",
                Description = "RF calibration data",
                BaseAddress = 0x90100000,
                Size = 0x1000,
                FileName = "calibration.bin",
                Enabled = true
            });

            SaveProfile(profile);
            SetCurrentProfile(profile);
        }

        public bool DeleteProfile(DeviceProfile profile)
        {
            try
            {
                if (!string.IsNullOrEmpty(profile.FilePath) && File.Exists(profile.FilePath))
                {
                    File.Delete(profile.FilePath);
                }

                _profiles.Remove(profile);

                if (_currentProfile == profile)
                {
                    _currentProfile = _profiles.FirstOrDefault();
                    if (_currentProfile != null)
                    {
                        ProfileChanged?.Invoke(this, _currentProfile);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateNVItemMapping(ushort itemId, NVItemType type, string name, string description)
        {
            if (_currentProfile == null) return;

            var key = $"NV_{itemId:X4}";

            // Check if mapping exists
            if (!_currentProfile.NVItemMappings.ContainsKey(key))
            {
                _currentProfile.NVItemMappings[key] = new NVItemMapping();
            }

            // Update mapping
            var mapping = _currentProfile.NVItemMappings[key];
            mapping.Id = itemId;
            mapping.Type = type;
            mapping.Name = name ?? $"NV_{itemId:X4}";
            mapping.Description = description ?? "";

            // Mark profile as modified
            _currentProfile.IsModified = true;

            // Auto-save the profile
            SaveProfile(_currentProfile);
        }
    }
}