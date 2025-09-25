using System;
using System.Drawing;
using System.Windows.Forms;

namespace SPRDNVEditor
{
    public partial class FlashFileEditDialog : Form
    {
        public FlashFileDefinition FlashFile { get; private set; }

        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private TextBox baseAddressTextBox;
        private TextBox sizeTextBox;
        private TextBox fileNameTextBox;
        private CheckBox enabledCheckBox;
        private Button okButton;
        private Button cancelButton;

        public FlashFileEditDialog(FlashFileDefinition? flashFile = null)
        {
            FlashFile = flashFile?.Clone() ?? new FlashFileDefinition();
            InitializeComponent();
            LoadFlashFileToUI();
        }

        private void InitializeComponent()
        {
            Text = "Flash File Definition";
            Size = new Size(400, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Name
            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(12, 15),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            nameTextBox = new TextBox
            {
                Location = new Point(100, 15),
                Size = new Size(260, 23)
            };

            // Description
            var descriptionLabel = new Label
            {
                Text = "Description:",
                Location = new Point(12, 45),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            descriptionTextBox = new TextBox
            {
                Location = new Point(100, 45),
                Size = new Size(260, 23)
            };

            // Base Address
            var baseAddressLabel = new Label
            {
                Text = "Base Address:",
                Location = new Point(12, 75),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            baseAddressTextBox = new TextBox
            {
                Location = new Point(100, 75),
                Size = new Size(120, 23),
                PlaceholderText = "0x12345678"
            };

            // Size
            var sizeLabel = new Label
            {
                Text = "Size:",
                Location = new Point(12, 105),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            sizeTextBox = new TextBox
            {
                Location = new Point(100, 105),
                Size = new Size(120, 23),
                PlaceholderText = "0x1000"
            };

            // File Name
            var fileNameLabel = new Label
            {
                Text = "File Name:",
                Location = new Point(12, 135),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            fileNameTextBox = new TextBox
            {
                Location = new Point(100, 135),
                Size = new Size(260, 23),
                PlaceholderText = "filename.bin"
            };

            // Enabled
            enabledCheckBox = new CheckBox
            {
                Text = "Enabled",
                Location = new Point(100, 165),
                Size = new Size(80, 23),
                Checked = true
            };

            // Buttons
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(200, 220),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 220),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.AddRange(new Control[] {
                nameLabel, nameTextBox,
                descriptionLabel, descriptionTextBox,
                baseAddressLabel, baseAddressTextBox,
                sizeLabel, sizeTextBox,
                fileNameLabel, fileNameTextBox,
                enabledCheckBox,
                okButton, cancelButton
            });
        }

        private void LoadFlashFileToUI()
        {
            nameTextBox.Text = FlashFile.Name;
            descriptionTextBox.Text = FlashFile.Description;
            baseAddressTextBox.Text = $"0x{FlashFile.BaseAddress:X8}";
            sizeTextBox.Text = $"0x{FlashFile.Size:X}";
            fileNameTextBox.Text = FlashFile.FileName;
            enabledCheckBox.Checked = FlashFile.Enabled;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameTextBox.Focus();
                return;
            }

            if (!TryParseHex(baseAddressTextBox.Text, out uint baseAddress))
            {
                MessageBox.Show("Invalid base address format. Use hex format like 0x12345678.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                baseAddressTextBox.Focus();
                return;
            }

            if (!TryParseHex(sizeTextBox.Text, out uint size) || size == 0)
            {
                MessageBox.Show("Invalid size format or zero size. Use hex format like 0x1000.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                sizeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(fileNameTextBox.Text))
            {
                MessageBox.Show("File name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                fileNameTextBox.Focus();
                return;
            }

            // Update flash file with values from UI
            FlashFile.Name = nameTextBox.Text.Trim();
            FlashFile.Description = descriptionTextBox.Text.Trim();
            FlashFile.BaseAddress = baseAddress;
            FlashFile.Size = size;
            FlashFile.FileName = fileNameTextBox.Text.Trim();
            FlashFile.Enabled = enabledCheckBox.Checked;
        }

        private static bool TryParseHex(string input, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string cleanInput = input.Trim();
            if (cleanInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleanInput = cleanInput.Substring(2);

            return uint.TryParse(cleanInput, System.Globalization.NumberStyles.HexNumber, null, out value);
        }
    }

    public static class FlashFileDefinitionExtensions
    {
        public static FlashFileDefinition Clone(this FlashFileDefinition original)
        {
            return new FlashFileDefinition
            {
                Name = original.Name,
                Description = original.Description,
                BaseAddress = original.BaseAddress,
                Size = original.Size,
                FileName = original.FileName,
                Enabled = original.Enabled
            };
        }
    }
}