using System;
using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SPRDNVEditor
{
    /// <summary>
    /// USB Fast Connect utility for detecting and switching devices to DIAG mode
    /// </summary>
    public class USBFastConnect
    {
        private ManagementEventWatcher _deviceWatcher;
        private bool _isWatching = false;
        
        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        
        /// <summary>
        /// Start watching for USB device connections
        /// </summary>
        public void StartWatching()
        {
            if (_isWatching) return;
            
            try
            {
                WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBControllerDevice'");
                _deviceWatcher = new ManagementEventWatcher(query);
                _deviceWatcher.EventArrived += OnDeviceEvent;
                _deviceWatcher.Start();
                _isWatching = true;
                
                // Also do an initial scan for already connected devices
                ScanForDevices();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start device watching: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stop watching for USB device connections
        /// </summary>
        public void StopWatching()
        {
            if (!_isWatching) return;
            
            try
            {
                _deviceWatcher?.Stop();
                _deviceWatcher?.Dispose();
                _deviceWatcher = null;
                _isWatching = false;
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Error stopping device watcher: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Scan for currently connected devices
        /// </summary>
        public void ScanForDevices()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT * FROM Win32_USBControllerDevice");
                
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string deviceId = queryObj["Dependent"]?.ToString();
                    //string caption = queryObj["Caption"]?.ToString();
                    
                    if (deviceId != null && deviceId.Contains("VID_1782&PID_4D00"))
                    {
                        DeviceConnected?.Invoke(this, new DeviceEventArgs
                        {
                            DeviceId = deviceId,
                            Caption = "",
                            IsBootMode = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for devices: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send payload to switch device to DIAG mode
        /// </summary>
        public string SwitchToDiagMode()
        {
            try
            {
                // Find the COM port for this device
                string comPort = FindComPortForDevice();
                if (string.IsNullOrEmpty(comPort))
                {
                    Console.WriteLine("Could not find COM port for device");
                    return null;
                }
                
                // Send the DIAG mode payload
                return comPort;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding COM: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Find COM port for a specific device ID
        /// </summary>
        private string FindComPortForDevice()
        {
            try
            {
                string selectedPort = "";
                String pattern = String.Format("VID_{0}.PID_{1}", "1782", "4D00");
                Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);

                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string foundName = obj["Name"]?.ToString(); // e.g., "SPRD DIAG (COM3)"
                    string foundDeviceID = obj["PNPDeviceID"]?.ToString(); // e.g., "USB\VID_2E04&PID_0024&MI_02\..."

                    if (foundName != null && _rx.Match(foundDeviceID).Success)
                    {
                        // Extract COM port number
                        int startIndex = foundName.LastIndexOf("(COM") + 4;
                        int endIndex = foundName.LastIndexOf(")");
                        selectedPort = foundName.Substring(startIndex, endIndex - startIndex);

                        return "COM" + selectedPort;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding COM port: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Get available DIAG ports
        /// </summary>
        public string[] GetDiagPorts()
        {
            var diagPorts = new List<string>();
            
            string[] targetDevices = {
                "SPRD U2S Diag",
                "SCI USB2Serial",
                "SCI Android USB2Serial",
                "USB Serial Port",
                "Prolific USB-to-Serial Comm Port",
                "UNISOC DIAG PORT"
            };
            
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");
                
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string caption = queryObj["Caption"]?.ToString();
                    if (caption != null)
                    {
                        foreach (string target in targetDevices)
                        {
                            if (caption.Contains(target))
                            {
                                int startIndex = caption.LastIndexOf("(COM") + 4;
                                int endIndex = caption.LastIndexOf(")");
                                if (startIndex > 3 && endIndex > startIndex)
                                {
                                    string portNumber = caption.Substring(startIndex, endIndex - startIndex);
                                    string portName = "COM" + portNumber;
                                    if (!diagPorts.Contains(portName))
                                    {
                                        diagPorts.Add(portName);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting DIAG ports: {ex.Message}");
            }
            
            return diagPorts.ToArray();
        }
        
        private void OnDeviceEvent(object sender, EventArrivedEventArgs e)
        {
            // Device change detected, rescan for devices
            Task.Run(() =>
            {
                Thread.Sleep(1000); // Wait a bit for device to settle
                ScanForDevices();
            });
        }
        
        public void Dispose()
        {
            StopWatching();
        }
    }
    
    /// <summary>
    /// Device event arguments
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public string Caption { get; set; }
        public bool IsBootMode { get; set; }
    }
}