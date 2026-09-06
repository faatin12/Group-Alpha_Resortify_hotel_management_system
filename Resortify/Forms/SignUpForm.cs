using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class SignUpForm : Form
    {
        private TextBox txtName, txtPhone, txtEmail, txtAddress, txtPassword, txtConfirm;
        private CheckBox chkShowPassword;
        private Label lblError;
        private Button btnRegister;
        private Button btnClear;
        private LinkLabel lnkBackToLogin;

        public SignUpForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Sign Up";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Create Account", UIHelper.NavyHeader,
                onBack: (s, e) => { new LoginForm().Show(); Close(); }, onLogout: null));

            int y = 100;
            (Label, TextBox) Field(string label)
            {
                var l = new Label { Text = label, Location = new Point(40, y), AutoSize = true, Font = UIHelper.BaseFont };
                var t = new TextBox { Location = new Point(40, y + 22), Width = 380, Font = UIHelper.BaseFont };
                y += 56;
                return (l, t);
            }

            var (l1, t1) = Field("Full Name"); txtName = t1;
            var (l2, t2) = Field("Phone"); txtPhone = t2;
            var (l3, t3) = Field("Email"); txtEmail = t3;
            var (l4, t4) = Field("Address"); txtAddress = t4;
            var (l5, t5) = Field("Password"); txtPassword = t5; txtPassword.UseSystemPasswordChar = true;
            var (l6, t6) = Field("Confirm Password"); txtConfirm = t6; txtConfirm.UseSystemPasswordChar = true;

            chkShowPassword = new CheckBox { Text = "Show Password", Location = new Point(40, y), AutoSize = true, Font = UIHelper.BaseFont };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                bool reveal = chkShowPassword.Checked;
                txtPassword.UseSystemPasswordChar = !reveal;
                txtConfirm.UseSystemPasswordChar = !reveal;
            };
            y += 32;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(40, y);
            lblError.MaximumSize = new Size(380, 0);
            y += 26;

            btnRegister = UIHelper.MakeButton("Create Account", UIHelper.NavyHeader, 240, 38);
            btnRegister.Location = new Point(40, y);
            btnRegister.Click += BtnRegister_Click;

            btnClear = UIHelper.MakeOutlineButton("Clear", UIHelper.NavyHeader, 130, 38);
            btnClear.Location = new Point(290, y);
            btnClear.Click += (s, e) => ClearForm();
            y += 50;

            lnkBackToLogin = new LinkLabel
            {
                Text = "Already Have an Account?  Back to LOGIN",
                AutoSize = true,
                Location = new Point(40, y),
                Font = UIHelper.BaseFont,
                LinkColor = UIHelper.AccentPurple
            };
            lnkBackToLogin.LinkClicked += (s, e) => { new LoginForm().Show(); Close(); };

            Controls.AddRange(new Control[]
            {
                l1, t1, l2, t2, l3, t3, l4, t4, l5, t5, l6, t6,
                chkShowPassword, lblError, btnRegister, btnClear, lnkBackToLogin
            });

            AcceptButton = btnRegister;
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            txtPassword.Clear();
            txtConfirm.Clear();
            chkShowPassword.Checked = false;
            lblError.Visible = false;
            txtName.Focus();
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            string name = txtName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();
            string password = txtPassword.Text;
            string confirm = txtConfirm.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(address))
            {
                ShowError("Full name, email and address are required.");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters.");
                return;
            }
            if (password != confirm)
            {
                ShowError("Password and Confirm Password do not match.");
                return;
            }

            var existing = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email", new SqlParameter("@Email", email));
            if (Convert.ToInt32(existing) > 0)
            {
                ShowError("An account with this email already exists.");
                return;
            }

            // This public form only ever creates Customer accounts now.
            // Admin / Hotel Owner accounts are created by a Super Admin from
            // the "Add New Staff" screen instead (see AddStaffForm).
            string hash = PasswordHelper.HashPassword(password);

            DbHelper.ExecuteNonQuery(
                @"INSERT INTO Users (FullName, Email, Password, Phone, Address, UserType, Status)
                  VALUES (@Name, @Email, @Pwd, @Phone, @Address, 'Customer', 'Approved')",
                new SqlParameter("@Name", name),
                new SqlParameter("@Email", email),
                new SqlParameter("@Pwd", hash),
                new SqlParameter("@Phone", (object)phone ?? DBNull.Value),
                new SqlParameter("@Address", address));

            MessageBox.Show("Account created! You can now log in.", "Welcome to Resortify",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            new LoginForm().Show();
            Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}