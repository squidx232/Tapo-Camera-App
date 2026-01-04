using System;
using System.Drawing;
using System.Windows.Forms;

namespace TapoControllerGUI
{
    public class CameraCredentialsDialog : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        public CameraCredentialsDialog(string defaultUsername = "admin", string defaultPassword = "")
        {
            Username = defaultUsername;
            Password = defaultPassword;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Camera Credentials";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(20)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Username
            var lblUsername = new Label
            {
                Text = "Username:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtUsername = new TextBox
            {
                Text = Username,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };

            // Password
            var lblPassword = new Label
            {
                Text = "Password:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtPassword = new TextBox
            {
                Text = Password,
                UseSystemPasswordChar = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };

            btnOK = new Button
            {
                Text = "OK",
                Width = 80,
                Height = 30,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnOK.Click += BtnOK_Click;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOK);

            mainPanel.Controls.Add(lblUsername, 0, 0);
            mainPanel.Controls.Add(txtUsername, 1, 0);
            mainPanel.Controls.Add(lblPassword, 0, 1);
            mainPanel.Controls.Add(txtPassword, 1, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            this.Controls.Add(mainPanel);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            Username = txtUsername.Text.Trim();
            Password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Please enter a username.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
