using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class AvailabilityDashboardForm : Form
    {
        private DataGridView grid;
        private Label lblBanner;

        public AvailabilityDashboardForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Availability Dashboard";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Availability Dashboard", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            lblBanner = new Label
            {
                Location = new Point(24, 100),
                Size = new Size(712, 30),
                BackColor = UIHelper.LowStockRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            grid = new DataGridView { Location = new Point(24, 140), Size = new Size(712, 300) };
            UIHelper.StyleGrid(grid);
            grid.CellFormatting += (s, e) =>
            {
                if (grid.Columns[e.ColumnIndex].Name == "Status" && e.Value?.ToString() == "LOW STOCK")
                {
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.BackColor = UIHelper.LowStockRed;
                    e.CellStyle.Font = new Font(UIHelper.BaseFont, FontStyle.Bold);
                }
            };

            Controls.AddRange(new Control[] { lblBanner, grid });
        }

        private void LoadData()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT r.RoomId, r.RoomType, r.TotalRooms, r.MinAvailability,
                         ISNULL(bk.BookedCount, 0) AS Booked,
                         r.TotalRooms - ISNULL(bk.BookedCount, 0) AS Available
                  FROM Rooms r
                  OUTER APPLY (
                      SELECT COUNT(*) AS BookedCount
                      FROM BookingItems bi
                      JOIN Bookings b ON b.BookingId = bi.BookingId
                      WHERE bi.RoomId = r.RoomId AND b.Status = 'Confirmed'
                        AND bi.CheckOutDate >= CAST(GETDATE() AS DATE)
                  ) bk
                  WHERE r.HotelId = @Id
                  ORDER BY r.RoomType",
                new SqlParameter("@Id", Session.HotelId));

            table.Columns.Add("Status", typeof(string));
            bool anyLow = false;
            foreach (System.Data.DataRow row in table.Rows)
            {
                int available = Convert.ToInt32(row["Available"]);
                int minAvail = Convert.ToInt32(row["MinAvailability"]);
                bool low = available <= minAvail;
                row["Status"] = low ? "LOW STOCK" : "OK";
                anyLow |= low;
            }

            grid.DataSource = table;
            if (grid.Columns["RoomId"] != null) grid.Columns["RoomId"].Visible = false;

            lblBanner.Visible = anyLow;
            lblBanner.Text = "⚠ One or more room types are at or below the minimum-availability threshold.";
        }
    }
}
