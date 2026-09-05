using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class HotelProfileForm : Form
    {
        private readonly bool isFirstTimeSetup;
        private TextBox txtName, txtCity, txtAddress, txtPhone, txtImagePath;
        private ComboBox cboCategory;
        private Button btnSave;
        private Label lblStatus;

        public HotelProfileForm(bool isFirstTimeSetup)
        {
            this.isFirstTimeSetup = isFirstTimeSetup;
            InitializeComponent();
            if (!isFirstTimeSetup) LoadExisting();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Hotel Profile";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Hotel Profile", UIHelper.AdminColor,
                onBack: isFirstTimeSetup ? null : (EventHandler)((s, e) => Close()),
                onLogout: (s, e) => { Session.SignOut(); new LoginForm().Show(); Close(); }));

            int y = 100;
            (Label, TextBox) Field(string label)
            {
                var l = new Label { Text = label, Location = new Point(32, y), AutoSize = true, Font = UIHelper.BaseFont };
                var t = new TextBox { Location = new Point(32, y + 22), Width = 400, Font = UIHelper.BaseFont };
                t.TextChanged += (s, e) => ValidateForm();
                y += 56;
                return (l, t);
            }

            var (l1, t1) = Field("Hotel Name *"); txtName = t1;

            var lblCat = new Label { Text = "Category", Location = new Point(32, y), AutoSize = true, Font = UIHelper.BaseFont };
            cboCategory = new ComboBox { Location = new Point(32, y + 22), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboCategory.Items.AddRange(new object[] { "Hotel", "Resort", "Guest House", "Villa Resort", "Cottage Retreat" });
            cboCategory.SelectedIndex = 0;
            y += 56;

            var (l2, t2) = Field("City *"); txtCity = t2;
            var (l3, t3) = Field("Full Address *"); txtAddress = t3;
            var (l4, t4) = Field("Contact Number"); txtPhone = t4;

            var lblImg = new Label { Text = "Logo / Photo", Location = new Point(32, y), AutoSize = true, Font = UIHelper.BaseFont };
            txtImagePath = new TextBox { Location = new Point(32, y + 22), Width = 300, Font = UIHelper.BaseFont, ReadOnly = true };
            var btnBrowse = UIHelper.MakeButton("Browse...", Color.Gray, 90, 26);
            btnBrowse.Location = new Point(340, y + 21);
            btnBrowse.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp" };
                if (dlg.ShowDialog() == DialogResult.OK) txtImagePath.Text = dlg.FileName;
            };
            y += 56;

            lblStatus = new Label { Location = new Point(32, y), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.Gray };
            y += 26;

            btnSave = UIHelper.MakeButton("Save", UIHelper.AdminColor, 400, 38);
            btnSave.Location = new Point(32, y);
            btnSave.Enabled = false;
            btnSave.Click += BtnSave_Click;

            Controls.AddRange(new Control[] { l1, t1, lblCat, cboCategory, l2, t2, l3, t3, l4, t4, lblImg, txtImagePath, btnBrowse, lblStatus, btnSave });

            if (isFirstTimeSetup)
                lblStatus.Text = "Register your hotel to get started. A Super Admin will review it before it goes live.";
        }

        private void ValidateForm()
        {
            btnSave.Enabled = !string.IsNullOrWhiteSpace(txtName.Text)
                            && !string.IsNullOrWhiteSpace(txtCity.Text)
                            && !string.IsNullOrWhiteSpace(txtAddress.Text);
        }

        private void LoadExisting()
        {
            var table = DbHelper.GetDataTable(
                "SELECT * FROM Hotels WHERE HotelId = @Id", new SqlParameter("@Id", Session.HotelId));
            if (table.Rows.Count == 0) return;
            var row = table.Rows[0];
            txtName.Text = row["HotelName"].ToString();
            txtCity.Text = row["City"].ToString();
            txtAddress.Text = row["Address"].ToString();
            txtPhone.Text = row["Phone"]?.ToString();
            txtImagePath.Text = row["ImagePath"]?.ToString();
            if (cboCategory.Items.Contains(row["Category"].ToString())) cboCategory.SelectedItem = row["Category"].ToString();
            lblStatus.Text = $"Approval status: {row["Status"]}";
            ValidateForm();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (isFirstTimeSetup)
            {
                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO Hotels (OwnerId, HotelName, Category, City, Address, Phone, Status)
                      VALUES (@Owner, @Name, @Cat, @City, @Addr, @Phone, 'Pending')",
                    new SqlParameter("@Owner", Session.UserId),
                    new SqlParameter("@Name", txtName.Text.Trim()),
                    new SqlParameter("@Cat", cboCategory.SelectedItem.ToString()),
                    new SqlParameter("@City", txtCity.Text.Trim()),
                    new SqlParameter("@Addr", txtAddress.Text.Trim()),
                    new SqlParameter("@Phone", (object)txtPhone.Text.Trim() ?? DBNull.Value));

                var id = DbHelper.ExecuteScalar("SELECT HotelId FROM Hotels WHERE OwnerId = @Owner",
                    new SqlParameter("@Owner", Session.UserId));
                Session.HotelId = Convert.ToInt32(id);
                Session.HotelName = txtName.Text.Trim();

                MessageBox.Show("Hotel submitted for approval. You can explore your dashboard while you wait.",
                    "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                new AdminDashboardForm().Show();
                Close();
            }
            else
            {
                DbHelper.ExecuteNonQuery(
                    @"UPDATE Hotels SET HotelName=@Name, Category=@Cat, City=@City, Address=@Addr,
                      Phone=@Phone, ImagePath=@Img WHERE HotelId = @Id",
                    new SqlParameter("@Name", txtName.Text.Trim()),
                    new SqlParameter("@Cat", cboCategory.SelectedItem.ToString()),
                    new SqlParameter("@City", txtCity.Text.Trim()),
                    new SqlParameter("@Addr", txtAddress.Text.Trim()),
                    new SqlParameter("@Phone", (object)txtPhone.Text.Trim() ?? DBNull.Value),
                    new SqlParameter("@Img", (object)txtImagePath.Text ?? DBNull.Value),
                    new SqlParameter("@Id", Session.HotelId));
                Session.HotelName = txtName.Text.Trim();
                MessageBox.Show("Hotel profile updated.", "Resortify");
                Close();
            }
        }
    }
}
