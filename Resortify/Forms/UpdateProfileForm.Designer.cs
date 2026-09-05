using System;
using Resortify.Data;
using Resortify.Helpers;
using System.Drawing;
using System.Windows.Forms;
using Font = System.Drawing.Font;
using static System.Net.Mime.MediaTypeNames;

namespace Resortify.Forms
{
    partial class UpdateProfileForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtName, txtPhone, txtAddress;
        private TextBox txtCurrentPwd, txtNewPwd, txtConfirmPwd;
        private Label lblError, lblPwdError;

        private Color RoleColor => Session.UserType == "Admin" ? UIHelper.AdminColor : UIHelper.CustomerColor;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

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
    }
}