using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    /// <summary>
    /// Lets a Hotel Owner (Admin) manage the cleaning / housekeeping /
    /// maintenance staff for their own hotel: add, edit, activate/deactivate,
    /// remove, and assign a day/night shift. Scoped to Session.HotelId.
    /// </summary>
    public partial class StaffManagementForm : Form
    {
        private DataGridView grid;

        public StaffManagementForm()
        {
            InitializeComponent();
            LoadStaff();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Staff Management";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Staff Management", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(832, 350) };
            UIHelper.StyleGrid(grid);

            var btnAdd = UIHelper.MakeButton("Add Staff", UIHelper.AdminColor, 150, 34);
            btnAdd.Location = new Point(24, 462);
            btnAdd.Click += (s, e) => AddStaff();

            var btnEdit = UIHelper.MakeButton("Edit Selected", UIHelper.AdminColor, 150, 34);
            btnEdit.Location = new Point(184, 462);
            btnEdit.Click += (s, e) => EditSelected();

            var btnToggle = UIHelper.MakeButton("Activate / Deactivate", Color.FromArgb(120, 90, 20), 170, 34);
            btnToggle.Location = new Point(344, 462);
            btnToggle.Click += (s, e) => ToggleStatus();

            var btnRemove = UIHelper.MakeButton("Remove Staff", Color.FromArgb(120, 20, 20), 150, 34);
            btnRemove.Location = new Point(524, 462);
            btnRemove.Click += (s, e) => RemoveSelected();

            Controls.AddRange(new Control[] { grid, btnAdd, btnEdit, btnToggle, btnRemove });
        }

        private void LoadStaff()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT StaffId, FullName, Role, Phone, Shift, Status, HiredDate
                  FROM Staff
                  WHERE HotelId = @Id
                  ORDER BY Role, FullName",
                new SqlParameter("@Id", Session.HotelId));

            if (grid.Columns["StaffId"] != null) grid.Columns["StaffId"].Visible = false;
        }

        private void AddStaff()
        {
            using var dlg = new StaffDialog(Session.HotelId ?? 0);
            if (dlg.ShowDialog() == DialogResult.OK) LoadStaff();
        }

        private void EditSelected()
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Select a staff member first.", "Resortify"); return; }

            int staffId = Convert.ToInt32(grid.CurrentRow.Cells["StaffId"].Value);
            string name = grid.CurrentRow.Cells["FullName"].Value.ToString();
            string role = grid.CurrentRow.Cells["Role"].Value.ToString();
            string phone = grid.CurrentRow.Cells["Phone"].Value?.ToString() ?? "";
            string shift = grid.CurrentRow.Cells["Shift"].Value.ToString();

            using var dlg = new StaffDialog(Session.HotelId ?? 0, staffId, name, role, phone, shift);
            if (dlg.ShowDialog() == DialogResult.OK) LoadStaff();
        }

        private void ToggleStatus()
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Select a staff member first.", "Resortify"); return; }

            int staffId = Convert.ToInt32(grid.CurrentRow.Cells["StaffId"].Value);
            string currentStatus = grid.CurrentRow.Cells["Status"].Value.ToString();
            string newStatus = currentStatus == "Active" ? "Inactive" : "Active";

            DbHelper.ExecuteNonQuery(
                "UPDATE Staff SET Status = @Status WHERE StaffId = @Id AND HotelId = @Hotel",
                new SqlParameter("@Status", newStatus),
                new SqlParameter("@Id", staffId),
                new SqlParameter("@Hotel", Session.HotelId));

            LoadStaff();
        }

        private void RemoveSelected()
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Select a staff member first.", "Resortify"); return; }

            string name = grid.CurrentRow.Cells["FullName"].Value.ToString();
            if (MessageBox.Show($"Remove {name} from staff?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int staffId = Convert.ToInt32(grid.CurrentRow.Cells["StaffId"].Value);
            DbHelper.ExecuteNonQuery(
                "DELETE FROM Staff WHERE StaffId = @Id AND HotelId = @Hotel",
                new SqlParameter("@Id", staffId),
                new SqlParameter("@Hotel", Session.HotelId));

            LoadStaff();
        }
    }
}