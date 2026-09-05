using System;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class UpdateProfileForm : Form
    {
        public UpdateProfileForm()
        {
            InitializeComponent();
            LoadExisting();
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
                new SqlParameter("@Phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? DBNull.Value : txtPhone.Text.Trim()),
                new SqlParameter("@Address", string.IsNullOrWhiteSpace(txtAddress.Text) ? DBNull.Value : txtAddress.Text.Trim()),
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
                "SELECT Password FROM Users WHERE UserId = @Id",
                new SqlParameter("@Id", Session.UserId))?.ToString();

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

            txtCurrentPwd.Clear();
            txtNewPwd.Clear();
            txtConfirmPwd.Clear();

            MessageBox.Show("Password changed.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPwdError(string message)
        {
            lblPwdError.Text = message;
            lblPwdError.Visible = true;
        }
    }
}