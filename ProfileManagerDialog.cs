using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SPRDNVEditor
{
    public partial class ProfileManagerDialog : Form
    {
        private DeviceProfileManager _profileManager;
        private ListBox profileListBox;
        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private TextBox fdl1TextBox;
        private TextBox fdl2TextBox;
        private NumericUpDown baseAddressNumeric;
        private NumericUpDown dataSizeNumeric;
        private Button newButton;
        private Button saveButton;
        private Button deleteButton;
        private Button loadButton;
        private Button saveAsButton;
        private Button closeButton;
        private GroupBox profileDetailsGroup;
        private Label nameLabel;
        private Label descriptionLabel;
        private Label fdl1Label;
        private Label fdl2Label;
        private Label baseAddressLabel;
        private Label dataSizeLabel;
        private Button browseFdl1Button;
        private Button browseFdl2Button;
        private GroupBox flashFilesGroup;
        private ListBox flashFilesListBox;
        private Button addFlashFileButton;
        private Button editFlashFileButton;
        private Button removeFlashFileButton;

        public ProfileManagerDialog(DeviceProfileManager profileManager)
        {
            _profileManager = profileManager;
            InitializeComponent();
            LoadProfiles();
        }

        private void InitializeComponent()
        {
            Text = "Device Profile Manager";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            
            // Profile list
            profileListBox = new ListBox
            {
                Location = new Point(12, 12),
                Size = new Size(200, 500),
                Name = "profileListBox"
            };
            profileListBox.SelectedIndexChanged += ProfileListBox_SelectedIndexChanged;
            
            // Buttons panel
            var buttonsPanel = new Panel
            {
                Location = new Point(12, 520),
                Size = new Size(200, 35)
            };
            
            newButton = new Button
            {
                Location = new Point(0, 0),
                Size = new Size(60, 25),
                Text = "New",
                Name = "newButton"
            };
            newButton.Click += NewButton_Click;
            
            loadButton = new Button
            {
                Location = new Point(65, 0),
                Size = new Size(60, 25),
                Text = "Load",
                Name = "loadButton"
            };
            loadButton.Click += LoadButton_Click;
            
            deleteButton = new Button
            {
                Location = new Point(130, 0),
                Size = new Size(60, 25),
                Text = "Delete",
                Name = "deleteButton"
            };
            deleteButton.Click += DeleteButton_Click;
            
            buttonsPanel.Controls.AddRange(new Control[] { newButton, loadButton, deleteButton });
            
            // Profile details group
            profileDetailsGroup = new GroupBox
            {
                Location = new Point(220, 12),
                Size = new Size(360, 250),
                Text = "Profile Details",
                Name = "profileDetailsGroup"
            };
            
            nameLabel = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(80, 23),
                Text = "Name:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            nameTextBox = new TextBox
            {
                Location = new Point(95, 25),
                Size = new Size(250, 23),
                Name = "nameTextBox"
            };
            
            descriptionLabel = new Label
            {
                Location = new Point(10, 55),
                Size = new Size(80, 23),
                Text = "Description:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            descriptionTextBox = new TextBox
            {
                Location = new Point(95, 55),
                Size = new Size(250, 60),
                Multiline = true,
                Name = "descriptionTextBox"
            };
            
            fdl1Label = new Label
            {
                Location = new Point(10, 125),
                Size = new Size(80, 23),
                Text = "FDL1 File:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            fdl1TextBox = new TextBox
            {
                Location = new Point(95, 125),
                Size = new Size(180, 23),
                Name = "fdl1TextBox"
            };
            
            browseFdl1Button = new Button
            {
                Location = new Point(280, 124),
                Size = new Size(65, 25),
                Text = "Browse",
                Name = "browseFdl1Button"
            };
            browseFdl1Button.Click += BrowseFdl1Button_Click;
            
            fdl2Label = new Label
            {
                Location = new Point(10, 155),
                Size = new Size(80, 23),
                Text = "FDL2 File:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            fdl2TextBox = new TextBox
            {
                Location = new Point(95, 155),
                Size = new Size(180, 23),
                Name = "fdl2TextBox"
            };
            
            browseFdl2Button = new Button
            {
                Location = new Point(280, 154),
                Size = new Size(65, 25),
                Text = "Browse",
                Name = "browseFdl2Button"
            };
            browseFdl2Button.Click += BrowseFdl2Button_Click;
            
            baseAddressLabel = new Label
            {
                Location = new Point(10, 185),
                Size = new Size(80, 23),
                Text = "Base Address:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            baseAddressNumeric = new NumericUpDown
            {
                Location = new Point(95, 185),
                Size = new Size(120, 23),
                Minimum = 0,
                Maximum = uint.MaxValue,
                Hexadecimal = true,
                Name = "baseAddressNumeric"
            };
            
            dataSizeLabel = new Label
            {
                Location = new Point(10, 215),
                Size = new Size(80, 23),
                Text = "Data Size:",
                TextAlign = ContentAlignment.MiddleRight
            };
            
            dataSizeNumeric = new NumericUpDown
            {
                Location = new Point(95, 215),
                Size = new Size(120, 23),
                Minimum = 0,
                Maximum = int.MaxValue,
                Hexadecimal = true,
                Name = "dataSizeNumeric"
            };
            
            profileDetailsGroup.Controls.AddRange(new Control[] {
                nameLabel, nameTextBox,
                descriptionLabel, descriptionTextBox,
                fdl1Label, fdl1TextBox, browseFdl1Button,
                fdl2Label, fdl2TextBox, browseFdl2Button,
                baseAddressLabel, baseAddressNumeric,
                dataSizeLabel, dataSizeNumeric
            });
            
            // Flash files group
            flashFilesGroup = new GroupBox
            {
                Location = new Point(220, 270),
                Size = new Size(360, 240),
                Text = "Flash Files",
                Name = "flashFilesGroup"
            };
            
            flashFilesListBox = new ListBox
            {
                Location = new Point(10, 25),
                Size = new Size(250, 170),
                Name = "flashFilesListBox"
            };
            
            addFlashFileButton = new Button
            {
                Location = new Point(270, 25),
                Size = new Size(75, 25),
                Text = "Add",
                Name = "addFlashFileButton"
            };
            addFlashFileButton.Click += AddFlashFileButton_Click;
            
            editFlashFileButton = new Button
            {
                Location = new Point(270, 55),
                Size = new Size(75, 25),
                Text = "Edit",
                Name = "editFlashFileButton"
            };
            editFlashFileButton.Click += EditFlashFileButton_Click;
            
            removeFlashFileButton = new Button
            {
                Location = new Point(270, 85),
                Size = new Size(75, 25),
                Text = "Remove",
                Name = "removeFlashFileButton"
            };
            removeFlashFileButton.Click += RemoveFlashFileButton_Click;
            
            flashFilesGroup.Controls.AddRange(new Control[] {
                flashFilesListBox, addFlashFileButton, editFlashFileButton, removeFlashFileButton
            });
            
            // Bottom buttons
            saveButton = new Button
            {
                Location = new Point(340, 520),
                Size = new Size(75, 25),
                Text = "Save",
                Name = "saveButton"
            };
            saveButton.Click += SaveButton_Click;
            
            saveAsButton = new Button
            {
                Location = new Point(420, 520),
                Size = new Size(75, 25),
                Text = "Save As",
                Name = "saveAsButton"
            };
            saveAsButton.Click += SaveAsButton_Click;
            
            closeButton = new Button
            {
                Location = new Point(505, 520),
                Size = new Size(75, 25),
                Text = "Close",
                DialogResult = DialogResult.OK,
                Name = "closeButton"
            };
            
            Controls.AddRange(new Control[] {
                profileListBox, buttonsPanel, profileDetailsGroup, flashFilesGroup,
                saveButton, saveAsButton, closeButton
            });
        }

        private void LoadProfiles()
        {
            profileListBox.Items.Clear();
            foreach (var profile in _profileManager.Profiles)
            {
                profileListBox.Items.Add(profile);
            }
            
            if (_profileManager.CurrentProfile != null)
            {
                profileListBox.SelectedItem = _profileManager.CurrentProfile;
            }
        }

        private void ProfileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            if (selectedProfile != null)
            {
                LoadProfileToUI(selectedProfile);
            }
        }

        private void LoadProfileToUI(DeviceProfile profile)
        {
            nameTextBox.Text = profile.Name;
            descriptionTextBox.Text = profile.Description;
            fdl1TextBox.Text = profile.Fdl1Path;
            fdl2TextBox.Text = profile.Fdl2Path;
            baseAddressNumeric.Value = profile.NVBaseAddress;
            dataSizeNumeric.Value = profile.NVDataSize;

            // Update FDL labels to show embedded status
            if (profile.HasEmbeddedFdl1)
            {
                fdl1Label.Text = "FDL1 File: ✓";
                fdl1Label.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                fdl1Label.Text = "FDL1 File:";
                fdl1Label.ForeColor = System.Drawing.SystemColors.ControlText;
            }

            if (profile.HasEmbeddedFdl2)
            {
                fdl2Label.Text = "FDL2 File: ✓";
                fdl2Label.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                fdl2Label.Text = "FDL2 File:";
                fdl2Label.ForeColor = System.Drawing.SystemColors.ControlText;
            }

            // Load flash files
            flashFilesListBox.Items.Clear();
            foreach (var flashFile in profile.FlashFiles)
            {
                flashFilesListBox.Items.Add(flashFile);
            }
        }

        private DeviceProfile GetProfileFromUI()
        {
            var profile = new DeviceProfile
            {
                Name = nameTextBox.Text,
                Description = descriptionTextBox.Text,
                Fdl1Path = fdl1TextBox.Text,
                Fdl2Path = fdl2TextBox.Text,
                NVBaseAddress = (uint)baseAddressNumeric.Value,
                NVDataSize = (int)dataSizeNumeric.Value
            };

            // Load FDL file contents if files exist
            if (!string.IsNullOrEmpty(profile.Fdl1Path) && File.Exists(profile.Fdl1Path))
            {
                try
                {
                    profile.Fdl1Data = File.ReadAllBytes(profile.Fdl1Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Warning: Could not read FDL1 file: {ex.Message}", "Warning", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (!string.IsNullOrEmpty(profile.Fdl2Path) && File.Exists(profile.Fdl2Path))
            {
                try
                {
                    profile.Fdl2Data = File.ReadAllBytes(profile.Fdl2Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Warning: Could not read FDL2 file: {ex.Message}", "Warning", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // Update flash files from UI (if any changes were made, they should already be in the selected profile)
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            if (selectedProfile != null)
            {
                profile.FlashFiles = new List<FlashFileDefinition>(selectedProfile.FlashFiles);
            }

            return profile;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            var inputDialog = new Form
            {
                Text = "New Profile",
                Size = new Size(300, 120),
                StartPosition = FormStartPosition.CenterParent
            };
            
            var label = new Label
            {
                Text = "Profile Name:",
                Location = new Point(10, 10),
                Size = new Size(80, 23)
            };
            
            var textBox = new TextBox
            {
                Location = new Point(95, 10),
                Size = new Size(180, 23)
            };
            
            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(120, 45),
                DialogResult = DialogResult.OK
            };
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(200, 45),
                DialogResult = DialogResult.Cancel
            };
            
            inputDialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            
            if (inputDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                _profileManager.CreateNewProfile(textBox.Text);
                LoadProfiles();
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            if (selectedProfile != null)
            {
                var updatedProfile = GetProfileFromUI();
                updatedProfile.FilePath = selectedProfile.FilePath;
                updatedProfile.NVItemMappings = selectedProfile.NVItemMappings;
                
                // Preserve existing embedded data if new files weren't loaded
                if (updatedProfile.Fdl1Data == null && selectedProfile.HasEmbeddedFdl1)
                {
                    updatedProfile.Fdl1Data = selectedProfile.Fdl1Data;
                }
                if (updatedProfile.Fdl2Data == null && selectedProfile.HasEmbeddedFdl2)
                {
                    updatedProfile.Fdl2Data = selectedProfile.Fdl2Data;
                }
                
                _profileManager.SaveProfile(updatedProfile);
                LoadProfiles();
                
                MessageBox.Show("Profile saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "SPRD Profile (*.sprdprofile)|*.sprdprofile";
                saveDialog.Title = "Save Profile As";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var profile = GetProfileFromUI();
                    _profileManager.SaveProfile(profile, saveDialog.FileName);
                    LoadProfiles();
                    
                    MessageBox.Show("Profile saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "SPRD Profile (*.sprdprofile)|*.sprdprofile";
                openDialog.Title = "Load Profile";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    var profile = _profileManager.LoadProfileFromFile(openDialog.FileName);
                    if (profile != null)
                    {
                        _profileManager.SaveProfile(profile);
                        LoadProfiles();
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            if (selectedProfile != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the profile '{selectedProfile.Name}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _profileManager.DeleteProfile(selectedProfile);
                    LoadProfiles();
                }
            }
        }

        private void BrowseFdl1Button_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                openDialog.Title = "Select FDL1 File";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    fdl1TextBox.Text = openDialog.FileName;
                }
            }
        }

        private void BrowseFdl2Button_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                openDialog.Title = "Select FDL2 File";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    fdl2TextBox.Text = openDialog.FileName;
                }
            }
        }

        private void AddFlashFileButton_Click(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            if (selectedProfile == null) return;

            using (var dialog = new FlashFileEditDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedProfile.FlashFiles.Add(dialog.FlashFile);
                    flashFilesListBox.Items.Add(dialog.FlashFile);
                }
            }
        }

        private void EditFlashFileButton_Click(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            var selectedFlashFile = flashFilesListBox.SelectedItem as FlashFileDefinition;
            if (selectedProfile == null || selectedFlashFile == null) return;

            using (var dialog = new FlashFileEditDialog(selectedFlashFile))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Update the item in the list
                    int index = flashFilesListBox.SelectedIndex;
                    selectedProfile.FlashFiles[selectedProfile.FlashFiles.IndexOf(selectedFlashFile)] = dialog.FlashFile;
                    flashFilesListBox.Items[index] = dialog.FlashFile;
                }
            }
        }

        private void RemoveFlashFileButton_Click(object sender, EventArgs e)
        {
            var selectedProfile = profileListBox.SelectedItem as DeviceProfile;
            var selectedFlashFile = flashFilesListBox.SelectedItem as FlashFileDefinition;
            if (selectedProfile == null || selectedFlashFile == null) return;

            var result = MessageBox.Show($"Are you sure you want to remove '{selectedFlashFile.Name}'?", 
                "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                selectedProfile.FlashFiles.Remove(selectedFlashFile);
                flashFilesListBox.Items.Remove(selectedFlashFile);
            }
        }
    }
}