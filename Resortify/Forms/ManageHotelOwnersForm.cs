using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class ManageHotelOwnersForm : Form
    {
        private DataGridView grid;

        public ManageHotelOwnersForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Manage Hotel Owners";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Manage Hotel Owners", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            // Adjusted grid size to fit nicely within the 880 width window with padding
            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(832, 380) };
            UIHelper.StyleGrid(grid);
            Controls.Add(grid);

            // Action Buttons neatly spaced out at the bottom
            var btnApproveUser = UIHelper.MakeButton("Approve User", Color.FromArgb(46, 125, 50), 130);
            btnApproveUser.Location = new Point(24, 494);
            btnApproveUser.Click += (s, e) => UpdateUserStatus("Approved");

            var btnRejectUser = UIHelper.MakeButton("Reject User", Color.FromArgb(198, 40, 40), 130);
            btnRejectUser.Location = new Point(160, 494);
            btnRejectUser.Click += (s, e) => UpdateUserStatus("Rejected");

            var btnApproveHotel = UIHelper.MakeButton("Approve Hotel", Color.FromArgb(0, 102, 204), 130);
            btnApproveHotel.Location = new Point(296, 494);
            btnApproveHotel.Click += (s, e) => ApproveHotel();

            var btnSuspend = UIHelper.MakeButton("Suspend Hotel", Color.FromArgb(214, 118, 27), 120);
            btnSuspend.Location = new Point(432, 494);
            btnSuspend.Click += (s, e) => SuspendHotel();

            var btnDelete = UIHelper.MakeButton("Delete Hotel", Color.FromArgb(120, 20, 20), 120);
            btnDelete.Location = new Point(558, 494);
            btnDelete.Click += (s, e) => DeleteHotel();

            var btnRefresh = UIHelper.MakeButton("Refresh", Color.Gray, 90);
            btnRefresh.Location = new Point(684, 494);
            btnRefresh.Click += (s, e) => LoadData();

            Controls.AddRange(new Control[] { btnApproveUser, btnRejectUser, btnApproveHotel, btnSuspend, btnDelete, btnRefresh });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT u.UserId, u.FullName AS Owner, u.Email, u.Status AS AccountStatus,
                         h.HotelId, ISNULL(h.HotelName, '(not registered yet)') AS Hotel,
                         ISNULL(h.Status, '-') AS HotelStatus
                  FROM Users u
                  LEFT JOIN Hotels h ON h.OwnerId = u.UserId
                  WHERE u.UserType = 'Admin'
                  ORDER BY u.Status, u.FullName");

            if (grid.Columns["UserId"] != null) grid.Columns["UserId"].Visible = false;
            if (grid.Columns["HotelId"] != null) grid.Columns["HotelId"].Visible = false;
        }

        private bool TryGetSelectedUserId(out int userId, out int hotelId, out bool hasHotel)
        {
            userId = 0; hotelId = 0; hasHotel = false;
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Select a row first.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            userId = Convert.ToInt32(grid.CurrentRow.Cells["UserId"].Value);
            var hotelCell = grid.CurrentRow.Cells["HotelId"].Value;
            if (hotelCell != DBNull.Value)
            {
                hotelId = Convert.ToInt32(hotelCell);
                hasHotel = true;
            }
            return true;
        }

        private void UpdateUserStatus(string status)
        {
            if (!TryGetSelectedUserId(out int userId, out _, out _)) return;
            DbHelper.ExecuteNonQuery("UPDATE Users SET Status = @Status WHERE UserId = @Id",
                new SqlParameter("@Status", status), new SqlParameter("@Id", userId));
            LoadData();
        }

        private void ApproveHotel()
        {
            if (!TryGetSelectedUserId(out _, out int hotelId, out bool hasHotel)) return;
            if (!hasHotel)
            {
                MessageBox.Show("This owner has not registered a hotel yet.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DbHelper.ExecuteNonQuery("UPDATE Hotels SET Status = 'Approved' WHERE HotelId = @Id",
                new SqlParameter("@Id", hotelId));

            MessageBox.Show("Hotel approved successfully! Customers can now view it.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        private void SuspendHotel()
        {
            if (!TryGetSelectedUserId(out _, out int hotelId, out bool hasHotel)) return;
            if (!hasHotel)
            {
                MessageBox.Show("This owner has not registered a hotel yet.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DbHelper.ExecuteNonQuery("UPDATE Hotels SET Status = 'Suspended' WHERE HotelId = @Id",
                new SqlParameter("@Id", hotelId));
            LoadData();
        }

        private void DeleteHotel()
        {
            if (!TryGetSelectedUserId(out _, out int hotelId, out bool hasHotel)) return;
            if (!hasHotel)
            {
                MessageBox.Show("This owner has not registered a hotel yet.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int activeBookings = Convert.ToInt32(DbHelper.ExecuteScalar(
                @"SELECT COUNT(*) FROM BookingItems bi
                  JOIN Rooms r ON r.RoomId = bi.RoomId
                  JOIN Bookings b ON b.BookingId = bi.BookingId
                  WHERE r.HotelId = @Id AND b.Status = 'Confirmed'",
                new SqlParameter("@Id", hotelId)));

            if (activeBookings > 0)
            {
                MessageBox.Show("Cannot delete: this hotel has active (confirmed) bookings.",
                    "Delete blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Permanently delete this hotel and its rooms?", "Confirm delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            DbHelper.RunTransaction((conn, tx) =>
            {
                void Exec(string sql)
                {
                    using var cmd = new SqlCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("@Id", hotelId);
                    cmd.ExecuteNonQuery();
                }
                Exec("DELETE FROM Offers WHERE RoomId IN (SELECT RoomId FROM Rooms WHERE HotelId = @Id)");
                Exec("DELETE FROM Cart WHERE RoomId IN (SELECT RoomId FROM Rooms WHERE HotelId = @Id)");
                Exec("DELETE FROM Reviews WHERE HotelId = @Id");
                Exec("DELETE FROM Rooms WHERE HotelId = @Id");
                Exec("DELETE FROM Hotels WHERE HotelId = @Id");
            });
            LoadData();
        }
    }
}