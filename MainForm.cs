using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO.Ports;

namespace SPRDNVEditor
{
    public partial class MainForm : Form
    {
        private USBFastConnect _usbConnect;
        private SPRDNVToolInternal _nvTool;
        private List<NVItem> _nvItems = new List<NVItem>();
        private bool _isDeviceConnected = false;
        private HexEditor _hexEditor;
        private DeviceProfileManager _profileManager;
        
        // UI Controls
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private GroupBox deviceGroupBox;
        private Label deviceStatusLabel;
        private Button refreshPortsButton;
        private ComboBox portComboBox;
        private Label portLabel;
        private GroupBox nvGroupBox;
        private Button readNVButton;
        private Button writeNVButton;
        private Button loadFileButton;
        private Button saveFileButton;
        private Button readFlashButton;
        private Button compareFilesButton;
        private DataGridView nvDataGridView;
        private TextBox logTextBox;
        private DataGridViewTextBoxColumn Id;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn Hex;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private GroupBox logGroupBox;
        private GroupBox hexEditorGroupBox;
        private ContextMenuStrip nvContextMenu;
        private System.ComponentModel.IContainer components;
        private CheckBox chkErsUsr;
        private CheckBox chkErsRun;
        private CheckBox chkAuto;
        private ToolStripMenuItem changeTypeMenuItem;
        private ComboBox deviceProfileComboBox;
        private Label profileLabel;
        private Button manageProfilesButton;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeProfileManager();
            InitializeNVTool();
            InitializeUSBConnect();
            UpdateUI();
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            deviceGroupBox = new GroupBox();
            deviceStatusLabel = new Label();
            portLabel = new Label();
            portComboBox = new ComboBox();
            refreshPortsButton = new Button();
            profileLabel = new Label();
            deviceProfileComboBox = new ComboBox();
            manageProfilesButton = new Button();
            readFlashButton = new Button();
            nvGroupBox = new GroupBox();
            chkAuto = new CheckBox();
            chkErsUsr = new CheckBox();
            chkErsRun = new CheckBox();
            readNVButton = new Button();
            writeNVButton = new Button();
            loadFileButton = new Button();
            saveFileButton = new Button();
            compareFilesButton = new Button();
            nvDataGridView = new DataGridView();
            Id = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            Hex = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new DataGridViewTextBoxColumn();
            nvContextMenu = new ContextMenuStrip(components);
            changeTypeMenuItem = new ToolStripMenuItem();
            hexEditorGroupBox = new GroupBox();
            _hexEditor = new HexEditor();
            logGroupBox = new GroupBox();
            logTextBox = new TextBox();
            statusStrip.SuspendLayout();
            deviceGroupBox.SuspendLayout();
            nvGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nvDataGridView).BeginInit();
            nvContextMenu.SuspendLayout();
            hexEditorGroupBox.SuspendLayout();
            logGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
            statusStrip.Location = new Point(0, 658);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(890, 22);
            statusStrip.TabIndex = 0;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(112, 17);
            statusLabel.Text = "Waiting for device...";
            // 
            // deviceGroupBox
            // 
            deviceGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            deviceGroupBox.Controls.Add(deviceStatusLabel);
            deviceGroupBox.Controls.Add(portLabel);
            deviceGroupBox.Controls.Add(portComboBox);
            deviceGroupBox.Controls.Add(refreshPortsButton);
            deviceGroupBox.Controls.Add(profileLabel);
            deviceGroupBox.Controls.Add(deviceProfileComboBox);
            deviceGroupBox.Controls.Add(manageProfilesButton);
            deviceGroupBox.Controls.Add(readFlashButton);
            deviceGroupBox.Location = new Point(12, 12);
            deviceGroupBox.Name = "deviceGroupBox";
            deviceGroupBox.Size = new Size(866, 80);
            deviceGroupBox.TabIndex = 1;
            deviceGroupBox.TabStop = false;
            deviceGroupBox.Text = "Device Connection";
            // 
            // deviceStatusLabel
            // 
            deviceStatusLabel.ForeColor = Color.Red;
            deviceStatusLabel.Location = new Point(15, 25);
            deviceStatusLabel.Name = "deviceStatusLabel";
            deviceStatusLabel.Size = new Size(200, 20);
            deviceStatusLabel.TabIndex = 0;
            deviceStatusLabel.Text = "No device connected";
            // 
            // portLabel
            // 
            portLabel.Location = new Point(15, 50);
            portLabel.Name = "portLabel";
            portLabel.Size = new Size(40, 20);
            portLabel.TabIndex = 1;
            portLabel.Text = "Port:";
            // 
            // portComboBox
            // 
            portComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            portComboBox.Location = new Point(60, 48);
            portComboBox.Name = "portComboBox";
            portComboBox.Size = new Size(100, 23);
            portComboBox.TabIndex = 2;
            // 
            // refreshPortsButton
            // 
            refreshPortsButton.Location = new Point(170, 47);
            refreshPortsButton.Name = "refreshPortsButton";
            refreshPortsButton.Size = new Size(100, 25);
            refreshPortsButton.TabIndex = 3;
            refreshPortsButton.Text = "Refresh Ports";
            refreshPortsButton.Click += RefreshPortsButton_Click;
            // 
            // profileLabel
            // 
            profileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            profileLabel.AutoSize = true;
            profileLabel.Location = new Point(331, 52);
            profileLabel.Name = "profileLabel";
            profileLabel.Size = new Size(82, 15);
            profileLabel.TabIndex = 4;
            profileLabel.Text = "Device Profile:";
            // 
            // deviceProfileComboBox
            // 
            deviceProfileComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deviceProfileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            deviceProfileComboBox.Location = new Point(419, 47);
            deviceProfileComboBox.Name = "deviceProfileComboBox";
            deviceProfileComboBox.Size = new Size(200, 23);
            deviceProfileComboBox.TabIndex = 5;
            deviceProfileComboBox.SelectedIndexChanged += DeviceProfileComboBox_SelectedIndexChanged;
            // 
            // manageProfilesButton
            // 
            manageProfilesButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            manageProfilesButton.Location = new Point(625, 46);
            manageProfilesButton.Name = "manageProfilesButton";
            manageProfilesButton.Size = new Size(110, 25);
            manageProfilesButton.TabIndex = 6;
            manageProfilesButton.Text = "Manage Profiles";
            manageProfilesButton.Click += ManageProfilesButton_Click;
            // 
            // readFlashButton
            // 
            readFlashButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            readFlashButton.Location = new Point(741, 47);
            readFlashButton.Name = "readFlashButton";
            readFlashButton.Size = new Size(110, 25);
            readFlashButton.TabIndex = 8;
            readFlashButton.Text = "Read Flash Files";
            readFlashButton.Click += ReadFlashButton_Click;
            // 
            // nvGroupBox
            // 
            nvGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            nvGroupBox.Controls.Add(chkAuto);
            nvGroupBox.Controls.Add(chkErsUsr);
            nvGroupBox.Controls.Add(chkErsRun);
            nvGroupBox.Controls.Add(readNVButton);
            nvGroupBox.Controls.Add(writeNVButton);
            nvGroupBox.Controls.Add(loadFileButton);
            nvGroupBox.Controls.Add(saveFileButton);
            nvGroupBox.Controls.Add(compareFilesButton);
            nvGroupBox.Controls.Add(nvDataGridView);
            nvGroupBox.Location = new Point(12, 100);
            nvGroupBox.Name = "nvGroupBox";
            nvGroupBox.Size = new Size(866, 307);
            nvGroupBox.TabIndex = 2;
            nvGroupBox.TabStop = false;
            nvGroupBox.Text = "NV Parameters";
            // 
            // chkAuto
            // 
            chkAuto.AutoSize = true;
            chkAuto.Location = new Point(768, 32);
            chkAuto.Name = "chkAuto";
            chkAuto.Size = new Size(83, 19);
            chkAuto.TabIndex = 7;
            chkAuto.Text = "Auto Write";
            chkAuto.UseVisualStyleBackColor = true;
            // 
            // chkErsUsr
            // 
            chkErsUsr.AutoSize = true;
            chkErsUsr.Location = new Point(667, 32);
            chkErsUsr.Name = "chkErsUsr";
            chkErsUsr.Size = new Size(95, 19);
            chkErsUsr.TabIndex = 6;
            chkErsUsr.Text = "Erase UserNV";
            chkErsUsr.UseVisualStyleBackColor = true;
            // 
            // chkErsRun
            // 
            chkErsRun.AutoSize = true;
            chkErsRun.Location = new Point(568, 32);
            chkErsRun.Name = "chkErsRun";
            chkErsRun.Size = new Size(93, 19);
            chkErsRun.TabIndex = 5;
            chkErsRun.Text = "Erase RunNV";
            chkErsRun.UseVisualStyleBackColor = true;
            // 
            // readNVButton
            // 
            readNVButton.Location = new Point(15, 25);
            readNVButton.Name = "readNVButton";
            readNVButton.Size = new Size(100, 30);
            readNVButton.TabIndex = 0;
            readNVButton.Text = "Read NV File";
            readNVButton.Click += ReadNVButton_Click;
            // 
            // writeNVButton
            // 
            writeNVButton.Location = new Point(125, 25);
            writeNVButton.Name = "writeNVButton";
            writeNVButton.Size = new Size(100, 30);
            writeNVButton.TabIndex = 1;
            writeNVButton.Text = "Write NV File";
            writeNVButton.Click += WriteNVButton_Click;
            // 
            // loadFileButton
            // 
            loadFileButton.Location = new Point(235, 25);
            loadFileButton.Name = "loadFileButton";
            loadFileButton.Size = new Size(100, 30);
            loadFileButton.TabIndex = 2;
            loadFileButton.Text = "Load File";
            loadFileButton.Click += LoadFileButton_Click;
            // 
            // saveFileButton
            // 
            saveFileButton.Location = new Point(345, 25);
            saveFileButton.Name = "saveFileButton";
            saveFileButton.Size = new Size(100, 30);
            saveFileButton.TabIndex = 3;
            saveFileButton.Text = "Save File";
            saveFileButton.Click += SaveFileButton_Click;
            // 
            // compareFilesButton
            // 
            compareFilesButton.Location = new Point(451, 25);
            compareFilesButton.Name = "compareFilesButton";
            compareFilesButton.Size = new Size(110, 30);
            compareFilesButton.TabIndex = 9;
            compareFilesButton.Text = "Compare Files";
            compareFilesButton.Click += CompareFilesButton_Click;
            // 
            // nvDataGridView
            // 
            nvDataGridView.AllowUserToAddRows = false;
            nvDataGridView.AllowUserToDeleteRows = false;
            nvDataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            nvDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            nvDataGridView.Columns.AddRange(new DataGridViewColumn[] { Id, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3, Hex, dataGridViewTextBoxColumn5 });
            nvDataGridView.ContextMenuStrip = nvContextMenu;
            nvDataGridView.Location = new Point(15, 65);
            nvDataGridView.MultiSelect = false;
            nvDataGridView.Name = "nvDataGridView";
            nvDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            nvDataGridView.Size = new Size(836, 227);
            nvDataGridView.TabIndex = 4;
            nvDataGridView.CellEndEdit += NvDataGridView_CellEndEdit;
            nvDataGridView.SelectionChanged += NvDataGridView_SelectionChanged;
            // 
            // Id
            // 
            Id.HeaderText = "Id";
            Id.Name = "Id";
            Id.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Offset";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "Length";
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // Hex
            // 
            Hex.HeaderText = "Hex";
            Hex.Name = "Hex";
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.HeaderText = "Text";
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // nvContextMenu
            // 
            nvContextMenu.Items.AddRange(new ToolStripItem[] { changeTypeMenuItem });
            nvContextMenu.Name = "nvContextMenu";
            nvContextMenu.Size = new Size(153, 26);
            // 
            // changeTypeMenuItem
            // 
            changeTypeMenuItem.Name = "changeTypeMenuItem";
            changeTypeMenuItem.Size = new Size(152, 22);
            changeTypeMenuItem.Text = "Change Type...";
            changeTypeMenuItem.Click += ChangeTypeMenuItem_Click;
            // 
            // hexEditorGroupBox
            // 
            hexEditorGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            hexEditorGroupBox.Controls.Add(_hexEditor);
            hexEditorGroupBox.Location = new Point(12, 413);
            hexEditorGroupBox.Name = "hexEditorGroupBox";
            hexEditorGroupBox.Size = new Size(866, 117);
            hexEditorGroupBox.TabIndex = 3;
            hexEditorGroupBox.TabStop = false;
            hexEditorGroupBox.Text = "Hex Editor";
            // 
            // _hexEditor
            // 
            _hexEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _hexEditor.BackColor = Color.White;
            _hexEditor.Data = null;
            _hexEditor.Location = new Point(15, 25);
            _hexEditor.Name = "_hexEditor";
            _hexEditor.SelectedByteIndex = -1;
            _hexEditor.Size = new Size(836, 77);
            _hexEditor.TabIndex = 0;
            _hexEditor.DataChanged += HexEditor_DataChanged;
            // 
            // logGroupBox
            // 
            logGroupBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logGroupBox.Controls.Add(logTextBox);
            logGroupBox.Location = new Point(12, 540);
            logGroupBox.Name = "logGroupBox";
            logGroupBox.Size = new Size(866, 100);
            logGroupBox.TabIndex = 4;
            logGroupBox.TabStop = false;
            logGroupBox.Text = "Log";
            // 
            // logTextBox
            // 
            logTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logTextBox.Location = new Point(15, 25);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(836, 65);
            logTextBox.TabIndex = 0;
            // 
            // MainForm
            // 
            ClientSize = new Size(890, 680);
            Controls.Add(statusStrip);
            Controls.Add(deviceGroupBox);
            Controls.Add(nvGroupBox);
            Controls.Add(hexEditorGroupBox);
            Controls.Add(logGroupBox);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SPRD NV Parameter Editor";
            FormClosing += MainForm_FormClosing;
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            deviceGroupBox.ResumeLayout(false);
            deviceGroupBox.PerformLayout();
            nvGroupBox.ResumeLayout(false);
            nvGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nvDataGridView).EndInit();
            nvContextMenu.ResumeLayout(false);
            hexEditorGroupBox.ResumeLayout(false);
            logGroupBox.ResumeLayout(false);
            logGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void InitializeUSBConnect()
        {
            _usbConnect = new USBFastConnect();
            _usbConnect.DeviceConnected += UsbConnect_DeviceConnected;
            _usbConnect.DeviceDisconnected += UsbConnect_DeviceDisconnected;
            _usbConnect.StartWatching();
        }
        
        private void InitializeProfileManager()
        {
            _profileManager = new DeviceProfileManager();
            _profileManager.ProfileChanged += OnProfileChanged;
            LoadProfilesToComboBox();
        }

        private void InitializeNVTool()
        {
            _nvTool = new SPRDNVToolInternal();
            _nvTool.LogMessage += LogMessage;
        }
        
        private void UpdateUI()
        {
            bool hasDevice = _isDeviceConnected;
            string selectedPort = portComboBox.SelectedItem?.ToString();
            bool hasSelectedPort = !string.IsNullOrEmpty(selectedPort);
            
            readNVButton.Enabled = hasDevice && hasSelectedPort;
            writeNVButton.Enabled = hasDevice && hasSelectedPort && _nvItems.Count > 0;
            readFlashButton.Enabled = hasDevice && hasSelectedPort;            
        }
        
        private void RefreshPorts()
        {
            string currentSelection = portComboBox.SelectedItem?.ToString();
            portComboBox.Items.Clear();
            
            string[] diagPorts = _usbConnect.GetDiagPorts();
            foreach (string port in diagPorts)
            {
                portComboBox.Items.Add(port);
            }
            
            if (!string.IsNullOrEmpty(currentSelection) && portComboBox.Items.Contains(currentSelection))
            {
                portComboBox.SelectedItem = currentSelection;
            }
            else if (portComboBox.Items.Count > 0)
            {
                portComboBox.SelectedIndex = 0;
            }

            UpdateUI();
        }

        private async void UsbConnect_DeviceConnected(object sender, DeviceEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UsbConnect_DeviceConnected(sender, e)));
                return;
            }

            if (e.IsBootMode)
            {
                LogMessage($"Boot mode device detected: {e.Caption}");
                LogMessage("Switching to DIAG mode...");

                string port = _usbConnect.SwitchToDiagMode();
                if (port != null && await _nvTool.ConnectToPortAsync(port))
                {
                    LogMessage("Device switched to DIAG mode successfully");
                    _isDeviceConnected = true;
                    deviceStatusLabel.Text = "Device connected (DIAG mode)";
                    deviceStatusLabel.ForeColor = Color.Green;
                    statusLabel.Text = "Device ready";

                    RefreshPorts();

                    if (chkAuto.Checked)
                    {
                        if (portComboBox.SelectedItem == null)
                        {
                            return;
                        }

                        if (_nvItems.Count == 0)
                        {
                            return;
                        }

                        string selectedPort = portComboBox.SelectedItem.ToString();

                        try
                        {
                            statusLabel.Text = "Writing NV parameters...";
                            LogMessage("Starting NV parameter write...");

                            // Convert NV structure back to binary
                            byte[] nvData = NVParser.BuildNV(_nvItems);

                            uint checksum = _nvTool.DoNVCheckSum(nvData);

                            // Write NV data using profile settings
                            var profile = _profileManager.CurrentProfile;
                            uint nvAddress = profile?.NVBaseAddress ?? 0x90000001;
                            
                            bool success = _nvTool.WriteData(0xB000, nvData, nvAddress, (int)checksum);

                            if (success)
                            {
                                if (chkErsRun.Checked)
                                    await _nvTool.EraseFlash(0x90000003, 0x4000);
                                if (chkErsUsr.Checked)
                                    await _nvTool.EraseFlash(0x9000001A, 0x4000);

                                success = await _nvTool.Resest();
                            }

                            if (success)
                            {
                                LogMessage("NV parameters written successfully");
                                statusLabel.Text = "NV parameters written successfully";
                            }
                            else
                            {
                                LogMessage("Failed to write NV parameters");
                                statusLabel.Text = "Failed to write NV parameters";
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error writing NV parameters: {ex.Message}");
                            statusLabel.Text = "Error writing NV parameters";
                        }
                        finally
                        {
                            writeNVButton.Enabled = true;
                            _nvTool.Disconnect();
                        }
                    }
                }
                else
                {
                    LogMessage("Failed to switch device to DIAG mode");
                }
            }
        }
        
        private void UsbConnect_DeviceDisconnected(object sender, DeviceEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UsbConnect_DeviceDisconnected(sender, e)));
                return;
            }
            
            _isDeviceConnected = false;
            deviceStatusLabel.Text = "No device connected";
            deviceStatusLabel.ForeColor = Color.Red;
            statusLabel.Text = "Waiting for device...";

            RefreshPorts();
        }

        private void RefreshPortsButton_Click(object sender, EventArgs e)
        {
            RefreshPorts();
        }
        
        private async void ReadNVButton_Click(object sender, EventArgs e)
        {
            if (portComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string selectedPort = portComboBox.SelectedItem.ToString();
            
            try
            {
                readNVButton.Enabled = false;
                statusLabel.Text = "Reading NV parameters...";
                LogMessage("Starting NV parameter read...");
                
                // Connect to device using selected port
                /*bool connected = await ConnectToDevice(selectedPort);
                if (!connected)
                {
                    LogMessage("Failed to connect to device");
                    return;
                }*/
                
                // Read NV data using profile settings
                var profile = _profileManager.CurrentProfile;
                uint nvAddress = profile?.NVBaseAddress ?? 0x90000001;
                int nvSize = profile?.NVDataSize ?? 0x0B0BA8;
                
                byte[] nvData = _nvTool.ReadFlash(nvAddress, (uint)nvSize);
                
                if (nvData != null && nvData.Length > 0)
                {
                    LogMessage($"NV data read successfully: {nvData.Length} bytes");

                    // Create output folder structure
                    string outputFolder = CreateOutputFolder();
                    
                    // Save NV data
                    string nvOutputFile = Path.Combine(outputFolder, "nv_data.bin");
                    File.WriteAllBytes(nvOutputFile, nvData);
                    LogMessage($"NV data saved to: {nvOutputFile}");

                    // Set profile mappings before parsing
                    if (_profileManager.CurrentProfile != null)
                    {
                        NVParser.SetProfileMappings(_profileManager.CurrentProfile.NVItemMappings);
                    }
                    
                    // Parse NV data
                    _nvItems = NVParser.ParseNV(nvData);
                    DisplayNVData();
                    
                    statusLabel.Text = "NV parameters read successfully";
                    LogMessage("NV parameter reading completed");
                }
                else
                {
                    LogMessage("Failed to read NV data");
                    statusLabel.Text = "Failed to read NV parameters";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error reading NV parameters: {ex.Message}");
                statusLabel.Text = "Error reading NV parameters";
                MessageBox.Show($"Error reading NV parameters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                readNVButton.Enabled = true;
                _nvTool.Disconnect();
            }
        }
        
        private async void WriteNVButton_Click(object sender, EventArgs e)
        {
            if (portComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (_nvItems.Count == 0)
            {
                MessageBox.Show("No NV data to write. Please read NV parameters first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string selectedPort = portComboBox.SelectedItem.ToString();
            
            try
            {
                writeNVButton.Enabled = false;
                statusLabel.Text = "Writing NV parameters...";
                LogMessage("Starting NV parameter write...");
                
                // Connect to device using selected port
                /*bool connected = await ConnectToDevice(selectedPort);
                if (!connected)
                {
                    LogMessage("Failed to connect to device");
                    return;
                }*/
                
                // Convert NV structure back to binary
                byte[] nvData = NVParser.BuildNV(_nvItems);
                
                uint checksum = _nvTool.DoNVCheckSum(nvData);

                // Write NV data using profile settings
                var profile = _profileManager.CurrentProfile;
                uint nvAddress = profile?.NVBaseAddress ?? 0x90000001;
                
                bool success = _nvTool.WriteData(0xB000, nvData, nvAddress, (int) checksum);

                if (success)
                {
                    if(chkErsRun.Checked)
                        await _nvTool.EraseFlash(0x90000003, 0x4000);
                    if (chkErsUsr.Checked)
                        await _nvTool.EraseFlash(0x9000001A, 0x4000);

                    success = await _nvTool.Resest();
                }

                if (success)
                {
                    LogMessage("NV parameters written successfully");
                    statusLabel.Text = "NV parameters written successfully";
                    MessageBox.Show("NV parameters written successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("Failed to write NV parameters");
                    statusLabel.Text = "Failed to write NV parameters";
                    MessageBox.Show("Failed to write NV parameters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error writing NV parameters: {ex.Message}");
                statusLabel.Text = "Error writing NV parameters";
                MessageBox.Show($"Error writing NV parameters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                writeNVButton.Enabled = true;
                _nvTool.Disconnect();
            }
        }
        
        private void LoadFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                openFileDialog.Title = "Select NV file to load";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] nvData = File.ReadAllBytes(openFileDialog.FileName);
                        
                        var profile = _profileManager.CurrentProfile;
                        int expectedSize = profile?.NVDataSize ?? 0x0B0BA8;
                        
                        if (nvData.Length != expectedSize)
                        {
                            if (MessageBox.Show($"File size ({nvData.Length} bytes) doesn't match expected NV size ({expectedSize} bytes). Continue anyway?", 
                                "Size Mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                            {
                                return;
                            }
                        }

                        // Set profile mappings before parsing
                        if (_profileManager.CurrentProfile != null)
                        {
                            NVParser.SetProfileMappings(_profileManager.CurrentProfile.NVItemMappings);
                        }
                        
                        _nvItems = NVParser.ParseNV(nvData);
                        DisplayNVData();
                        
                        LogMessage($"NV file loaded: {openFileDialog.FileName}");
                        statusLabel.Text = "NV file loaded successfully";
                        UpdateUI();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error loading NV file: {ex.Message}");
                        MessageBox.Show($"Error loading NV file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void SaveFileButton_Click(object sender, EventArgs e)
        {
            if (_nvItems.Count == 0)
            {
                MessageBox.Show("No NV data to save. Please read NV parameters first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                saveFileDialog.Title = "Save NV file";
                saveFileDialog.FileName = "nv_backup.bin";
                
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] nvData = NVParser.BuildNV(_nvItems);
                        File.WriteAllBytes(saveFileDialog.FileName, nvData);
                        
                        LogMessage($"NV file saved: {saveFileDialog.FileName}");
                        statusLabel.Text = "NV file saved successfully";
                        MessageBox.Show("NV file saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error saving NV file: {ex.Message}");
                        MessageBox.Show($"Error saving NV file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void NvDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string cellValue = nvDataGridView.Rows[e.RowIndex].Cells["Id"].Value.ToString();
                string idStr = cellValue.Split('(')[0].Trim();
                ushort itemId = Convert.ToUInt16(idStr);
                
                NVItem found = _nvItems.FirstOrDefault(p => p.Id == itemId);
                if (found == null) return;

                string newValue = nvDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                
                if (e.ColumnIndex == nvDataGridView.Columns["Hex"].Index)
                {
                    // Update hex value and sync text
                    found.Hex = newValue;
                    nvDataGridView.Rows[e.RowIndex].Cells["dataGridViewTextBoxColumn5"].Value = found.Text;
                    LogMessage($"Updated {itemId} hex to: {newValue}");
                }
                else if (e.ColumnIndex == nvDataGridView.Columns["dataGridViewTextBoxColumn5"].Index)
                {
                    // Update text value and sync hex
                    found.Text = newValue;
                    nvDataGridView.Rows[e.RowIndex].Cells["Hex"].Value = found.Hex;
                    LogMessage($"Updated {itemId} text to: {newValue}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating parameter: {ex.Message}");
                MessageBox.Show($"Error updating parameter: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Revert to original value
                DisplayNVData();
            }
        }
        
        private async Task<bool> ConnectToDevice(string portName)
        {
            try
            {
                // Connect to the device using the internal tool
                return await _nvTool.ConnectToPortAsync(portName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error connecting to device: {ex.Message}");
                return false;
            }
        }
        
        private void DisplayNVData()
        {
            nvDataGridView.SuspendLayout();
            nvDataGridView.Rows.Clear();
            
            foreach (var item in _nvItems)
            {
                // Get type definition for better display
                var typeDef = NVParser.GetTypeDefinition(item.Id);
                string itemName = typeDef?.Name ?? $"NV_{item.Id:04X}";
                
                // Show only first 16 bytes in the grid
                string truncatedHex = item.Data.Length > 16 
                    ? BitConverter.ToString(item.Data, 0, 16).Replace("-", " ") + "..."
                    : item.Hex;
                string truncatedText = item.Data.Length > 16
                    ? item.Text.Substring(0, Math.Min(16, item.Text.Length)) + "..."
                    : item.Text;
                    
                nvDataGridView.Rows.Add(
                    $"{item.Id} ({itemName})",
                    $"0x{item.Offset:X4}",
                    item.Length,
                    truncatedHex,
                    truncatedText
                );
            }

            nvDataGridView.ResumeLayout();
            UpdateUI();
        }
        
        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LogMessage(message)));
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.ScrollToCaret();
        }
        
        private void ChangeTypeMenuItem_Click(object sender, EventArgs e)
        {
            if (nvDataGridView.SelectedRows.Count == 0)
                return;

            try
            {
                int rowIndex = nvDataGridView.SelectedRows[0].Index;
                string cellValue = nvDataGridView.Rows[rowIndex].Cells["Id"].Value.ToString();
                string idStr = cellValue.Split('(')[0].Trim();
                ushort itemId = Convert.ToUInt16(idStr);

                NVItem item = _nvItems.FirstOrDefault(p => p.Id == itemId);
                if (item == null) return;

                // Show type selection dialog
                using (var typeDialog = new TypeSelectionDialog(item.Type))
                {
                    if (typeDialog.ShowDialog() == DialogResult.OK)
                    {
                        item.Type = typeDialog.SelectedType;
                        NVParser.SetTypeDefinition(itemId, typeDialog.SelectedType, typeDialog.TypeName, typeDialog.TypeDescription);
                        
                        // Save to profile if requested
                        if (typeDialog.SaveToProfile && _profileManager.CurrentProfile != null)
                        {
                            _profileManager.UpdateNVItemMapping(itemId, typeDialog.SelectedType, typeDialog.TypeName, typeDialog.TypeDescription);
                            LogMessage($"Saved type mapping for NV {itemId} to profile");
                        }
                        
                        // Refresh the display
                        DisplayNVData();
                        
                        LogMessage($"Changed type for NV {itemId} to {typeDialog.SelectedType}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error changing type: {ex.Message}");
                MessageBox.Show($"Error changing type: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NvDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (nvDataGridView.SelectedRows.Count == 0)
            {
                _hexEditor.Data = new byte[0];
                return;
            }

            try
            {
                int rowIndex = nvDataGridView.SelectedRows[0].Index;
                string cellValue = nvDataGridView.Rows[rowIndex].Cells["Id"].Value.ToString();
                string idStr = cellValue.Split('(')[0].Trim();
                ushort itemId = Convert.ToUInt16(idStr);

                NVItem item = _nvItems.FirstOrDefault(p => p.Id == itemId);
                if (item != null)
                {
                    _hexEditor.Data = item.Data;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating hex editor: {ex.Message}");
            }
        }

        private void HexEditor_DataChanged(object sender, byte[] newData)
        {
            if (nvDataGridView.SelectedRows.Count == 0)
                return;

            try
            {
                int rowIndex = nvDataGridView.SelectedRows[0].Index;
                string cellValue = nvDataGridView.Rows[rowIndex].Cells["Id"].Value.ToString();
                string idStr = cellValue.Split('(')[0].Trim();
                ushort itemId = Convert.ToUInt16(idStr);

                NVItem item = _nvItems.FirstOrDefault(p => p.Id == itemId);
                if (item != null)
                {
                    item.Data = newData;
                    
                    // Update the grid display with new truncated data
                    string truncatedHex = newData.Length > 16 
                        ? BitConverter.ToString(newData, 0, 16).Replace("-", " ") + "..."
                        : item.Hex;
                    string truncatedText = newData.Length > 16
                        ? item.Text.Substring(0, Math.Min(16, item.Text.Length)) + "..."
                        : item.Text;
                    
                    nvDataGridView.Rows[rowIndex].Cells["Hex"].Value = truncatedHex;
                    nvDataGridView.Rows[rowIndex].Cells["dataGridViewTextBoxColumn5"].Value = truncatedText;
                    nvDataGridView.Rows[rowIndex].Cells["dataGridViewTextBoxColumn3"].Value = newData.Length;
                    
                    LogMessage($"Updated NV item {itemId} with {newData.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating NV data: {ex.Message}");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _usbConnect?.Dispose();
                _nvTool?.Disconnect();
            }
            catch (Exception ex)
            {
                LogMessage($"Error during cleanup: {ex.Message}");
            }
        }

        private void LoadProfilesToComboBox()
        {
            deviceProfileComboBox.Items.Clear();
            
            foreach (var profile in _profileManager.Profiles)
            {
                deviceProfileComboBox.Items.Add(profile);
            }
            
            if (_profileManager.CurrentProfile != null)
            {
                deviceProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
            }
            else if (deviceProfileComboBox.Items.Count > 0)
            {
                deviceProfileComboBox.SelectedIndex = 0;
            }
        }

        private void DeviceProfileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedProfile = deviceProfileComboBox.SelectedItem as DeviceProfile;
            if (selectedProfile != null && selectedProfile != _profileManager.CurrentProfile)
            {
                _profileManager.SetCurrentProfile(selectedProfile);
            }
        }

        private void ManageProfilesButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new ProfileManagerDialog(_profileManager))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadProfilesToComboBox();
                }
            }
        }

        private void OnProfileChanged(object sender, DeviceProfile profile)
        {
            LogMessage($"Device profile changed to: {profile.Name}");
            
            // Update NV Tool with new profile settings if needed
            UpdateNVToolWithProfile(profile);
            
            // Update NVParser with profile mappings
            if (profile != null)
            {
                NVParser.SetProfileMappings(profile.NVItemMappings);
            }
            
            // Re-display NV data if we have any to apply new type mappings
            if (_nvItems.Count > 0)
            {
                DisplayNVData();
            }
        }

        private void UpdateNVToolWithProfile(DeviceProfile profile)
        {
            if (_nvTool != null && profile != null)
            {
                _nvTool.SetDeviceProfile(profile);
            }
        }

        private string CreateOutputFolder()
        {
            var profile = _profileManager.CurrentProfile;
            string profileName = profile?.Name ?? "Unknown";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            
            // Create safe folder name
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string folderName = $"{safeName}_{timestamp}";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FlashReads", folderName);
            
            Directory.CreateDirectory(fullPath);
            LogMessage($"Created output folder: {fullPath}");
            
            return fullPath;
        }

        private async void ReadFlashButton_Click(object sender, EventArgs e)
        {
            if (portComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var profile = _profileManager.CurrentProfile;
            if (profile?.FlashFiles == null || profile.FlashFiles.Count == 0)
            {
                MessageBox.Show("No flash files are defined in the current profile. Please manage profiles and add flash file definitions first.", 
                    "No Flash Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var enabledFiles = profile.FlashFiles.Where(f => f.Enabled).ToList();
            if (enabledFiles.Count == 0)
            {
                MessageBox.Show("No flash files are enabled in the current profile. Please enable at least one flash file definition.", 
                    "No Enabled Flash Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                readFlashButton.Enabled = false;
                statusLabel.Text = "Reading flash files...";
                LogMessage("Starting flash files read...");

                // Create output folder structure
                string outputFolder = CreateOutputFolder();

                // Read flash files
                await ReadAdditionalFlashFiles(outputFolder);

                statusLabel.Text = "Flash files read successfully";
                LogMessage("Flash files reading completed");
                MessageBox.Show($"Flash files have been read successfully and saved to:\n{outputFolder}", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error reading flash files: {ex.Message}");
                statusLabel.Text = "Error reading flash files";
                MessageBox.Show($"Error reading flash files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                readFlashButton.Enabled = true;
                _nvTool.Disconnect();
            }
        }

        private async Task ReadAdditionalFlashFiles(string outputFolder)
        {
            var profile = _profileManager.CurrentProfile;
            if (profile?.FlashFiles == null || profile.FlashFiles.Count == 0)
            {
                LogMessage("No additional flash files defined in profile");
                return;
            }

            var enabledFiles = profile.FlashFiles.Where(f => f.Enabled).ToList();
            if (enabledFiles.Count == 0)
            {
                LogMessage("No enabled flash files to read");
                return;
            }

            LogMessage($"Reading {enabledFiles.Count} additional flash files...");

            foreach (var flashFile in enabledFiles)
            {
                try
                {
                    statusLabel.Text = $"Reading {flashFile.Name}...";
                    LogMessage($"Reading flash file: {flashFile.Name} (0x{flashFile.BaseAddress:X8}, 0x{flashFile.Size:X} bytes)");

                    byte[] data = _nvTool.ReadFlash(flashFile.BaseAddress, flashFile.Size);
                    if (data != null && data.Length > 0)
                    {
                        string fileName = string.IsNullOrWhiteSpace(flashFile.FileName) ? 
                            $"{flashFile.Name}.bin" : flashFile.FileName;
                        
                        // Ensure safe filename
                        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
                        string filePath = Path.Combine(outputFolder, fileName);
                        
                        File.WriteAllBytes(filePath, data);
                        LogMessage($"Flash file saved: {filePath} ({data.Length} bytes)");
                    }
                    else
                    {
                        LogMessage($"Failed to read flash file: {flashFile.Name}");
                    }

                    // Small delay between reads to avoid overwhelming the device
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error reading flash file '{flashFile.Name}': {ex.Message}");
                }
            }

            LogMessage("Additional flash files reading completed");
        }

        private void CompareFilesButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (var compareDialog = new NVComparisonDialog(_profileManager))
                {
                    compareDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error opening comparison dialog: {ex.Message}");
                MessageBox.Show($"Error opening comparison dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}