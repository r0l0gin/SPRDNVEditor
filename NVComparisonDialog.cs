using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SPRDNVEditor
{
    public partial class NVComparisonDialog : Form
    {
        private TextBox file1PathTextBox;
        private TextBox file2PathTextBox;
        private Button browseFile1Button;
        private Button browseFile2Button;
        private Button compareButton;
        private Button closeButton;
        private DataGridView comparisonDataGridView;
        private Label file1Label;
        private Label file2Label;
        private Label resultsLabel;
        private DeviceProfileManager _profileManager;
        
        // Data grid columns
        private DataGridViewTextBoxColumn idColumn;
        private DataGridViewTextBoxColumn nameColumn;
        private DataGridViewTextBoxColumn offsetColumn;
        private DataGridViewTextBoxColumn lengthColumn;
        private DataGridViewTextBoxColumn file1ValueColumn;
        private DataGridViewTextBoxColumn file2ValueColumn;
        private DataGridViewTextBoxColumn statusColumn;

        public NVComparisonDialog(DeviceProfileManager profileManager)
        {
            _profileManager = profileManager;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "NV Files Comparison";
            Size = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(800, 500);

            // File 1 selection
            file1Label = new Label
            {
                Text = "First NV File:",
                Location = new Point(12, 15),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            file1PathTextBox = new TextBox
            {
                Location = new Point(100, 15),
                Size = new Size(350, 23),
                ReadOnly = true
            };

            browseFile1Button = new Button
            {
                Text = "Browse...",
                Location = new Point(460, 14),
                Size = new Size(75, 25)
            };
            browseFile1Button.Click += BrowseFile1Button_Click;

            // File 2 selection
            file2Label = new Label
            {
                Text = "Second NV File:",
                Location = new Point(12, 45),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            file2PathTextBox = new TextBox
            {
                Location = new Point(100, 45),
                Size = new Size(350, 23),
                ReadOnly = true
            };

            browseFile2Button = new Button
            {
                Text = "Browse...",
                Location = new Point(460, 44),
                Size = new Size(75, 25)
            };
            browseFile2Button.Click += BrowseFile2Button_Click;

            // Compare button
            compareButton = new Button
            {
                Text = "Compare Files",
                Location = new Point(550, 25),
                Size = new Size(100, 30),
                Enabled = false
            };
            compareButton.Click += CompareButton_Click;

            // Results label
            resultsLabel = new Label
            {
                Text = "Select two NV files to compare",
                Location = new Point(12, 80),
                Size = new Size(400, 20),
                ForeColor = Color.Gray
            };

            // Comparison results grid
            comparisonDataGridView = new DataGridView
            {
                Location = new Point(12, 105),
                Size = new Size(960, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Initialize columns
            idColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "NV ID",
                Name = "IdColumn",
                FillWeight = 80
            };

            nameColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Name = "NameColumn",
                FillWeight = 120
            };

            offsetColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Offset",
                Name = "OffsetColumn",
                FillWeight = 80
            };

            lengthColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Length",
                Name = "LengthColumn",
                FillWeight = 80
            };

            file1ValueColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "File 1 Value",
                Name = "File1ValueColumn",
                FillWeight = 150
            };

            file2ValueColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "File 2 Value",
                Name = "File2ValueColumn",
                FillWeight = 150
            };

            statusColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Status",
                Name = "StatusColumn",
                FillWeight = 100
            };

            comparisonDataGridView.Columns.AddRange(new DataGridViewColumn[] {
                idColumn, nameColumn, offsetColumn, lengthColumn, 
                file1ValueColumn, file2ValueColumn, statusColumn
            });

            // Close button
            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(897, 520),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[] {
                file1Label, file1PathTextBox, browseFile1Button,
                file2Label, file2PathTextBox, browseFile2Button,
                compareButton, resultsLabel, comparisonDataGridView, closeButton
            });

            AcceptButton = closeButton;
            CancelButton = closeButton;
        }

        private void BrowseFile1Button_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                openDialog.Title = "Select First NV File";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    file1PathTextBox.Text = openDialog.FileName;
                    UpdateCompareButtonState();
                }
            }
        }

        private void BrowseFile2Button_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                openDialog.Title = "Select Second NV File";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    file2PathTextBox.Text = openDialog.FileName;
                    UpdateCompareButtonState();
                }
            }
        }

        private void UpdateCompareButtonState()
        {
            compareButton.Enabled = !string.IsNullOrEmpty(file1PathTextBox.Text) && 
                                   !string.IsNullOrEmpty(file2PathTextBox.Text);
        }

        private void CompareButton_Click(object sender, EventArgs e)
        {
            try
            {
                compareButton.Enabled = false;
                resultsLabel.Text = "Comparing files...";
                resultsLabel.ForeColor = Color.Blue;

                CompareNVFiles(file1PathTextBox.Text, file2PathTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error comparing files: {ex.Message}", "Comparison Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                resultsLabel.Text = $"Error: {ex.Message}";
                resultsLabel.ForeColor = Color.Red;
            }
            finally
            {
                compareButton.Enabled = true;
            }
        }

        private void CompareNVFiles(string file1Path, string file2Path)
        {
            // Load both files
            byte[] file1Data = System.IO.File.ReadAllBytes(file1Path);
            byte[] file2Data = System.IO.File.ReadAllBytes(file2Path);

            // Set profile mappings if available
            if (_profileManager.CurrentProfile != null)
            {
                NVParser.SetProfileMappings(_profileManager.CurrentProfile.NVItemMappings);
            }

            // Parse both files
            List<NVItem> nvItems1 = NVParser.ParseNV(file1Data);
            List<NVItem> nvItems2 = NVParser.ParseNV(file2Data);

            // Clear previous results
            comparisonDataGridView.Rows.Clear();

            // Get all unique NV IDs from both files
            var allIds = nvItems1.Select(item => item.Id)
                               .Union(nvItems2.Select(item => item.Id))
                               .OrderBy(id => id)
                               .ToList();

            int identicalCount = 0;
            int differentCount = 0;
            int onlyInFile1Count = 0;
            int onlyInFile2Count = 0;

            foreach (ushort id in allIds)
            {
                var item1 = nvItems1.FirstOrDefault(item => item.Id == id);
                var item2 = nvItems2.FirstOrDefault(item => item.Id == id);

                string status;
                Color rowColor = Color.White;
                string file1Value = "";
                string file2Value = "";

                // Get type definition for better display
                var typeDef = NVParser.GetTypeDefinition(id);
                string itemName = typeDef?.Name ?? $"NV_{id:04X}";

                if (item1 != null && item2 != null)
                {
                    // Both files have this NV item
                    file1Value = GetDisplayValue(item1);
                    file2Value = GetDisplayValue(item2);

                    if (item1.Data.SequenceEqual(item2.Data))
                    {
                        status = "Identical";
                        rowColor = Color.LightGreen;
                        identicalCount++;
                    }
                    else
                    {
                        status = "Different";
                        rowColor = Color.LightCoral;
                        differentCount++;
                    }
                }
                else if (item1 != null)
                {
                    // Only in file 1
                    file1Value = GetDisplayValue(item1);
                    file2Value = "(not present)";
                    status = "Only in File 1";
                    rowColor = Color.LightYellow;
                    onlyInFile1Count++;
                }
                else
                {
                    // Only in file 2
                    file1Value = "(not present)";
                    file2Value = GetDisplayValue(item2!);
                    status = "Only in File 2";
                    rowColor = Color.LightBlue;
                    onlyInFile2Count++;
                }

                int rowIndex = comparisonDataGridView.Rows.Add(
                    $"{id} (0x{id:X4})",
                    itemName,
                    item1?.Offset.ToString("X4") ?? item2?.Offset.ToString("X4") ?? "",
                    item1?.Length.ToString() ?? item2?.Length.ToString() ?? "",
                    file1Value,
                    file2Value,
                    status
                );

                comparisonDataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = rowColor;
            }

            // Update results summary
            int totalItems = allIds.Count;
            resultsLabel.Text = $"Comparison complete: {totalItems} total items - " +
                              $"{identicalCount} identical, {differentCount} different, " +
                              $"{onlyInFile1Count} only in file 1, {onlyInFile2Count} only in file 2";
            resultsLabel.ForeColor = Color.Black;
        }

        private string GetDisplayValue(NVItem item)
        {
            // Limit display length for readability
            string displayText = item.Text;
            if (displayText.Length > 50)
            {
                displayText = displayText.Substring(0, 50) + "...";
            }
            return displayText;
        }
    }
}