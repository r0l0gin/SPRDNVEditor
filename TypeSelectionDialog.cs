using System;
using System.Drawing;
using System.Windows.Forms;

namespace SPRDNVEditor
{
    public partial class TypeSelectionDialog : Form
    {
        public NVItemType SelectedType { get; private set; }
        public string TypeName { get; private set; }
        public string TypeDescription { get; private set; }
        public bool SaveToProfile { get; private set; }

        private ComboBox typeComboBox;
        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private CheckBox saveToProfileCheckBox;
        private Button okButton;
        private Button cancelButton;

        public TypeSelectionDialog(NVItemType currentType)
        {
            InitializeComponent();
            SelectedType = currentType;
            
            // Populate type combo box
            typeComboBox.DataSource = Enum.GetValues(typeof(NVItemType));
            typeComboBox.SelectedItem = currentType;
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form properties
            Text = "Select NV Parameter Type";
            Size = new Size(400, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Type label and combo box
            var typeLabel = new Label
            {
                Text = "Type:",
                Location = new Point(12, 15),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };

            typeComboBox = new ComboBox
            {
                Location = new Point(100, 12),
                Size = new Size(260, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Name label and text box
            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(12, 45),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };

            nameTextBox = new TextBox
            {
                Location = new Point(100, 42),
                Size = new Size(260, 23)
            };

            // Description label and text box
            var descriptionLabel = new Label
            {
                Text = "Description:",
                Location = new Point(12, 75),
                Size = new Size(80, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };

            descriptionTextBox = new TextBox
            {
                Location = new Point(100, 72),
                Size = new Size(260, 23)
            };

            // Save to profile checkbox
            saveToProfileCheckBox = new CheckBox
            {
                Text = "Save this type mapping to device profile",
                Location = new Point(12, 105),
                Size = new Size(250, 23),
                AutoSize = true,
                Checked = true
            };

            // OK button
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(200, 135),
                Size = new Size(75, 23),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            // Cancel button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 135),
                Size = new Size(75, 23),
                DialogResult = DialogResult.Cancel
            };

            // Add controls to form
            Controls.AddRange(new Control[] 
            {
                typeLabel, typeComboBox,
                nameLabel, nameTextBox,
                descriptionLabel, descriptionTextBox,
                saveToProfileCheckBox,
                okButton, cancelButton
            });

            // Set default button
            AcceptButton = okButton;
            CancelButton = cancelButton;

            ResumeLayout();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SelectedType = (NVItemType)typeComboBox.SelectedItem;
            TypeName = nameTextBox.Text;
            TypeDescription = descriptionTextBox.Text;
            SaveToProfile = saveToProfileCheckBox.Checked;
        }
    }
}