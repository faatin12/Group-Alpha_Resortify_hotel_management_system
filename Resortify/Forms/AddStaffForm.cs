using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    /// <summary>
    /// Lets a Super Admin create a Hotel Owner (Admin) account directly --
    /// no public sign-up, no Pending approval step, since the Super Admin
    /// is vouching for the account by creating it themselves. This is the
    /// only place outside the (now Customer-only) SignUpForm that inserts
    /// a new row into Users.
    /// </summary>
    public partial class AddStaffForm : Form
    {
        private TextBox txtName, txtEmail, txtPhone, txtAddress, txtPassword, txtConfirm;
        private CheckBox chkShowPassword;
        private Label lblError;
        private Button btnCreate;
        private Button btnClear;

        public AddStaffForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Add New Staff";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Add New Staff (Hotel Owner)", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            int y = 100;
            (Label, TextBox) Field(string label)
            {
                var l = new Label { Text = label, Location = new Point(40, y), AutoSize = true, Font = UIHelper.BaseFont };
                var t = new TextBox { Location = new Point(40, y + 22), Width = 380, Font = UIHelper.BaseFont };
                y += 56;
                return (l, t);
            }

            var lblRoleNote = new Label
            {
                Text = "This form creates an Admin / Hotel Owner account only.\nCustomers sign up for themselves from the Login screen.",
                Location = new Point(40, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            y += 44;

            var (l1, t1) = Field("Full Name"); txtName = t1;
            var (l2, t2) = Field("Username / Email"); txtEmail = t2;
            var (l3, t3) = Field("Phone"); txtPhone = t3;
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
            y += 30;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(40, y);
            lblError.MaximumSize = new Size(380, 0);
            y += 16;

            btnCreate = UIHelper.MakeButton("Create Staff Account", UIHelper.SuperAdmin, 240, 38);
            btnCreate.Location = new Point(40, y);
            btnCreate.Click += BtnCreate_Click;

            btnClear = UIHelper.MakeOutlineButton("Clear", UIHelper.SuperAdmin, 130, 38);
            btnClear.Location = new Point(290, y);
            btnClear.Click += (s, e) => ClearForm();

            Controls.AddRange(new Control[]
            {
                lblRoleNote, l1, t1, l2, t2, l3, t3, l4, t4, l5, t5, l6, t6,
                chkShowPassword, lblError, btnCreate, btnClear
            });

            AcceptButton = btnCreate;
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtEmail.Clear();
            txtPhone.Clear();
            txtAddress.Clear();
            txtPassword.Clear();
            txtConfirm.Clear();
            chkShowPassword.Checked = false;
            lblError.Visible = false;
            txtName.Focus();
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string address = txtAddress.Text.Trim();
            string password = txtPassword.Text;
            string confirm = txtConfirm.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                ShowError("Full name and username/email are required.");
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
                ShowError("An account with this username/email already exists.");
                return;
            }

            string hash = PasswordHelper.HashPassword(password);

            // Created by a Super Admin, so it goes straight in as Approved --
            // no Pending review step, unlike the public sign-up flow.
            DbHelper.ExecuteNonQuery(
                @"INSERT INTO Users (FullName, Email, Password, Phone, Address, UserType, Status)
                  VALUES (@Name, @Email, @Pwd, @Phone, @Address, 'Admin', 'Approved')",
                new SqlParameter("@Name", name),
                new SqlParameter("@Email", email),
                new SqlParameter("@Pwd", hash),
                new SqlParameter("@Phone", (object)phone ?? DBNull.Value),
                new SqlParameter("@Address", (object)address ?? DBNull.Value));

            MessageBox.Show(
                $"Staff account created.\n\nUsername: {email}\nThey can log in as Admin (Hotel Owner) right away " +
                "and will be prompted to register their hotel on first login.",
                "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ClearForm();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}