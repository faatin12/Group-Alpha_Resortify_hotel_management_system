using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class LoginForm : Form
    {
        private TextBox txtEmail;
        private TextBox txtPassword;
        private ComboBox cboRole;
        private CheckBox chkShowPassword;
        private Label lblError;
        private Button btnLogin;
        private Button btnClear;
        private LinkLabel lnkSignUp;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Login";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;

            var header = UIHelper.BuildHeader("Login", UIHelper.NavyHeader, null, null);
            Controls.Add(header);

            var lblEmail = new Label { Text = "Email", Location = new Point(40, 110), AutoSize = true, Font = UIHelper.BaseFont };
            txtEmail = new TextBox { Location = new Point(40, 132), Width = 340, Font = UIHelper.BaseFont };

            var lblPassword = new Label { Text = "Password", Location = new Point(40, 168), AutoSize = true, Font = UIHelper.BaseFont };
            txtPassword = new TextBox { Location = new Point(40, 190), Width = 340, Font = UIHelper.BaseFont, UseSystemPasswordChar = true };

            chkShowPassword = new CheckBox { Text = "Show Password", Location = new Point(40, 220), AutoSize = true, Font = UIHelper.BaseFont };
            chkShowPassword.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            var lblRole = new Label { Text = "Login as", Location = new Point(40, 254), AutoSize = true, Font = UIHelper.BaseFont };
            cboRole = new ComboBox { Location = new Point(40, 276), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboRole.Items.AddRange(new object[] { "Customer", "Admin (Hotel Owner)", "Super Admin" });
            cboRole.SelectedIndex = 0;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(40, 310);
            lblError.MaximumSize = new Size(340, 0);

            btnLogin = UIHelper.MakeButton("Login", UIHelper.NavyHeader

, 200, 38);
            btnLogin.Location = new Point(40, 340);
            btnLogin.Click += BtnLogin_Click;

            btnClear = UIHelper.MakeOutlineButton("Clear", UIHelper.NavyHeader

, 140, 38);
            btnClear.Location = new Point(250, 340);
            btnClear.Click += (s, e) => ClearForm();

            lnkSignUp = new LinkLabel
            {
                Text = "Don't Have an Account?  Create Account",
                AutoSize = true,
                Location = new Point(40, 392),
                Font = UIHelper.BaseFont,
                LinkColor = UIHelper.NavyHeader


            };
            lnkSignUp.LinkClicked += (s, e) =>
            {
                new SignUpForm().Show();
                Hide();
            };

            var lblHint = new Label
            {
                Text = "Quick login: super@gmail.com / admin@gmail.com  (password: 123456)",
                Location = new Point(40, 424),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic)
            };

            Controls.AddRange(new Control[]
            {
                lblEmail, txtEmail, lblPassword, txtPassword, chkShowPassword,
                lblRole, cboRole, lblError, btnLogin, btnClear, lnkSignUp, lblHint
            });

            AcceptButton = btnLogin;
        }

        private void ClearForm()
        {
            txtEmail.Clear();
            txtPassword.Clear();
            chkShowPassword.Checked = false;
            cboRole.SelectedIndex = 0;
            lblError.Visible = false;
            txtEmail.Focus();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Email and password are required.");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters.");
                return;
            }

            string wantedType = cboRole.SelectedIndex == 0 ? "Customer"
                               : cboRole.SelectedIndex == 1 ? "Admin"
                               : "SuperAdmin";

            var table = DbHelper.GetDataTable(
                "SELECT UserId, FullName, Email, Password, UserType, Status FROM Users WHERE Email = @Email",
                new SqlParameter("@Email", email));

            if (table.Rows.Count == 0)
            {
                ShowError("No account found with that email.");
                return;
            }

            var row = table.Rows[0];
            string storedHash = row["Password"].ToString();
            string userType = row["UserType"].ToString();
            string status = row["Status"].ToString();

            if (!PasswordHelper.Verify(password, storedHash))
            {
                ShowError("Incorrect password.");
                return;
            }
            if (userType != wantedType)
            {
                ShowError($"This account is registered as {userType}, not {wantedType}.");
                return;
            }
            if (userType != "SuperAdmin" && status != "Approved")
            {
                ShowError(status == "Pending"
                    ? "Your account is awaiting Super Admin approval."
                    : $"Your account status is '{status}'. Contact Resortify support.");
                return;
            }

            Session.SignIn(Convert.ToInt32(row["UserId"]), row["FullName"].ToString(), row["Email"].ToString(), userType);

            Form next = userType switch
            {
                "SuperAdmin" => new SuperAdminDashboardForm(),
                "Admin" => LoadAdminHotelThenDashboard(),
                _ => new CustomerHomeForm()
            };

            if (next == null) return; // Admin with no hotel yet was already routed to HotelProfileForm

            next.Show();
            Hide();
        }

        /// <summary>
        /// An Admin owns exactly one Hotel. Load it into the Session; if they
        /// haven't registered one yet, send them to Hotel Profile first instead
        /// of the Dashboard.
        /// </summary>
        private Form LoadAdminHotelThenDashboard()
        {
            var table = DbHelper.GetDataTable(
                "SELECT HotelId, HotelName FROM Hotels WHERE OwnerId = @OwnerId",
                new SqlParameter("@OwnerId", Session.UserId));

            if (table.Rows.Count == 0)
            {
                var profileForm = new HotelProfileForm(isFirstTimeSetup: true);
                profileForm.Show();
                Hide();
                return null;
            }

            Session.HotelId = Convert.ToInt32(table.Rows[0]["HotelId"]);
            Session.HotelName = table.Rows[0]["HotelName"].ToString();
            return new AdminDashboardForm();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}