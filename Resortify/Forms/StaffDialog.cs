using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    /// <summary>
    /// Add or edit one Staff row (cleaning / housekeeping / maintenance) for
    /// the current Admin's hotel, including day/night shift assignment.
    /// Pass staffId = null to add a new staff member, or an existing id
    /// (plus their current values) to edit one.
    /// </summary>
    public partial class StaffDialog : Form
    {
        private readonly int hotelId;
        private readonly int? staffId;

        private TextBox txtName, txtPhone;
        private ComboBox cboRole, cboShift;
        private Label lblError;
        private Button btnSave;

        public StaffDialog(int hotelId, int? staffId = null,
            string existingName = null, string existingRole = null,
            string existingPhone = null, string existingShift = null)
        {
            this.hotelId = hotelId;
            this.staffId = staffId;
            InitializeComponent();

            if (staffId.HasValue)
            {
                Text = "Resortify - Edit Staff";
                txtName.Text = existingName;
                cboRole.SelectedItem = existingRole;
                txtPhone.Text = existingPhone;
                cboShift.SelectedItem = existingShift;
                btnSave.Text = "Save Changes";
            }
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Add Staff";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var lblTitle = new Label { Text = "Staff Details", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };

            var lblName = new Label { Text = "Full Name", Location = new Point(20, 56), AutoSize = true, Font = UIHelper.BaseFont };
            txtName = new TextBox { Location = new Point(20, 78), Width = 300, Font = UIHelper.BaseFont };

            var lblRole = new Label { Text = "Role", Location = new Point(20, 114), AutoSize = true, Font = UIHelper.BaseFont };
            cboRole = new ComboBox { Location = new Point(20, 136), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboRole.Items.AddRange(new object[] { "Cleaning", "Housekeeping", "Maintenance" });
            cboRole.SelectedIndex = 0;

            var lblPhone = new Label { Text = "Phone", Location = new Point(20, 172), AutoSize = true, Font = UIHelper.BaseFont };
            txtPhone = new TextBox { Location = new Point(20, 194), Width = 300, Font = UIHelper.BaseFont };

            var lblShift = new Label { Text = "Shift", Location = new Point(20, 230), AutoSize = true, Font = UIHelper.BaseFont };
            cboShift = new ComboBox { Location = new Point(20, 252), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboShift.Items.AddRange(new object[] { "Day", "Night" });
            cboShift.SelectedIndex = 0;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(20, 292);
            lblError.MaximumSize = new Size(300, 0);

            btnSave = UIHelper.MakeButton("Add Staff", UIHelper.AdminColor, 300, 34);
            btnSave.Location = new Point(20, 316);
            btnSave.Click += BtnSave_Click;

            Controls.AddRange(new Control[]
            {
                lblTitle, lblName, txtName, lblRole, cboRole, lblPhone, txtPhone,
                lblShift, cboShift, lblError, btnSave
            });

            AcceptButton = btnSave;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            string name = txtName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string role = cboRole.SelectedItem?.ToString();
            string shift = cboShift.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("Full name is required.");
                return;
            }
            if (role == null || shift == null)
            {
                ShowError("Select a role and a shift.");
                return;
            }

            if (staffId.HasValue)
            {
                DbHelper.ExecuteNonQuery(
                    @"UPDATE Staff SET FullName = @Name, Role = @Role, Phone = @Phone, Shift = @Shift
                      WHERE StaffId = @Id AND HotelId = @Hotel",
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Role", role),
                    new SqlParameter("@Phone", (object)phone ?? DBNull.Value),
                    new SqlParameter("@Shift", shift),
                    new SqlParameter("@Id", staffId.Value),
                    new SqlParameter("@Hotel", hotelId));
            }
            else
            {
                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO Staff (HotelId, FullName, Role, Phone, Shift)
                      VALUES (@Hotel, @Name, @Role, @Phone, @Shift)",
                    new SqlParameter("@Hotel", hotelId),
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Role", role),
                    new SqlParameter("@Phone", (object)phone ?? DBNull.Value),
                    new SqlParameter("@Shift", shift));
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}