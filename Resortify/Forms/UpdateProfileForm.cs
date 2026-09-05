using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class UpdateProfileForm : Form
    {
        private TextBox txtName, txtPhone, txtAddress;
        private TextBox txtCurrentPwd, txtNewPwd, txtConfirmPwd;
        private Label lblError, lblPwdError;

        public UpdateProfileForm()
        {
            InitializeComponent();
            LoadExisting();
        }

        private Color RoleColor => Session.UserType == "Admin" ? UIHelper.AdminColor : UIHelper.CustomerColor;

        private void InitializeComponent()
        {
            Text = "Resortify - Update Profile";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Update Profile", RoleColor,
                onBack: (s, e) => Close(), onLogout: null));

            int y = 100;
            (Label, TextBox) Field(string label)
            {
                var l = new Label { Text = label, Location = new Point(32, y), AutoSize = true, Font = UIHelper.BaseFont };
                var t = new TextBox { Location = new Point(32, y + 22), Width = 400, Font = UIHelper.BaseFont };
                y += 56;
                return (l, t);
            }

            var (l1, t1) = Field("Full Name"); txtName = t1;
            var (l2, t2) = Field("Phone"); txtPhone = t2;
            var (l3, t3) = Field("Address"); txtAddress = t3;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(32, y);
            y += 20;

            var btnSaveProfile = UIHelper.MakeButton("Save Profile", RoleColor, 400, 34);
            btnSaveProfile.Location = new Point(32, y);
            btnSaveProfile.Click += BtnSaveProfile_Click;
            y += 54;

            var divider = new Label { Text = "Change Password", Location = new Point(32, y), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            y += 30;

            var (l4, t4) = Field("Current Password"); txtCurrentPwd = t4; txtCurrentPwd.UseSystemPasswordChar = true;
            var (l5, t5) = Field("New Password"); txtNewPwd = t5; txtNewPwd.UseSystemPasswordChar = true;
            var (l6, t6) = Field("Confirm New Password"); txtConfirmPwd = t6; txtConfirmPwd.UseSystemPasswordChar = true;

            lblPwdError = UIHelper.MakeErrorLabel();
            lblPwdError.Location = new Point(32, y);
            y += 20;

            var btnChangePwd = UIHelper.MakeButton("Change Password", RoleColor, 400, 34);
            btnChangePwd.Location = new Point(32, y);
            btnChangePwd.Click += BtnChangePassword_Click;

            Controls.AddRange(new Control[]
            {
                l1, t1, l2, t2, l3, t3, lblError, btnSaveProfile, divider,
                l4, t4, l5, t5, l6, t6, lblPwdError, btnChangePwd
            });
        }

        private void LoadExisting()
        {
            var table = DbHelper.GetDataTable(
                "SELECT FullName, Phone, Address FROM Users WHERE UserId = @Id",
                new SqlParameter("@Id", Session.UserId));
            if (table.Rows.Count == 0) return;
            txtName.Text = table.Rows[0]["FullName"].ToString();
            txtPhone.Text = table.Rows[0]["Phone"]?.ToString();
            txtAddress.Text = table.Rows[0]["Address"]?.ToString();
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                lblError.Text = "Full name cannot be empty.";
                lblError.Visible = true;
                return;
            }

            DbHelper.ExecuteNonQuery(
                "UPDATE Users SET FullName = @Name, Phone = @Phone, Address = @Address WHERE UserId = @Id",
                new SqlParameter("@Name", txtName.Text.Trim()),
                new SqlParameter("@Phone", (object)txtPhone.Text.Trim() ?? DBNull.Value),
                new SqlParameter("@Address", (object)txtAddress.Text.Trim() ?? DBNull.Value),
                new SqlParameter("@Id", Session.UserId));

            MessageBox.Show("Profile updated.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            lblPwdError.Visible = false;

            string current = txtCurrentPwd.Text;
            string newPwd = txtNewPwd.Text;
            string confirm = txtConfirmPwd.Text;

            var storedHash = DbHelper.ExecuteScalar(
                "SELECT Password FROM Users WHERE UserId = @Id", new SqlParameter("@Id", Session.UserId))?.ToString();

            if (!PasswordHelper.Verify(current, storedHash))
            {
                ShowPwdError("Current password is incorrect.");
                return;
            }
            if (newPwd.Length < 6)
            {
                ShowPwdError("New password must be at least 6 characters.");
                return;
            }
            if (newPwd != confirm)
            {
                ShowPwdError("New password and confirmation do not match.");
                return;
            }

            DbHelper.ExecuteNonQuery(
                "UPDATE Users SET Password = @Pwd WHERE UserId = @Id",
                new SqlParameter("@Pwd", PasswordHelper.HashPassword(newPwd)),
                new SqlParameter("@Id", Session.UserId));

            txtCurrentPwd.Clear(); txtNewPwd.Clear(); txtConfirmPwd.Clear();
            MessageBox.Show("Password changed.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPwdError(string message)
        {
            lblPwdError.Text = message;
            lblPwdError.Visible = true;
        }
    }
}
