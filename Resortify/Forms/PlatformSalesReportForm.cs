using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class PlatformSalesReportForm : Form
    {
        private DateTimePicker dtFrom, dtTo;
        private DataGridView grid;
        private Label lblTotals;
        private const decimal CommissionRate = 0.10m;

        public PlatformSalesReportForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Platform Sales & Commission Report";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Platform Sales & Commission Report", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            var lblFrom = new Label { Text = "From", Location = new Point(24, 100), AutoSize = true, Font = UIHelper.BaseFont };
            dtFrom = new DateTimePicker { Location = new Point(24, 122), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-12) };
            dtFrom.ValueChanged += (s, e) => LoadData();

            var lblTo = new Label { Text = "To", Location = new Point(190, 100), AutoSize = true, Font = UIHelper.BaseFont };
            dtTo = new DateTimePicker { Location = new Point(190, 122), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(1) };
            dtTo.ValueChanged += (s, e) => LoadData();

            grid = new DataGridView { Location = new Point(24, 164), Size = new Size(852, 300) };
            UIHelper.StyleGrid(grid);

            lblTotals = new Label { Location = new Point(24, 476), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };

            Controls.AddRange(new Control[] { lblFrom, dtFrom, lblTo, dtTo, grid, lblTotals });
        }

        private void LoadData()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT h.HotelId, h.HotelName,
                         COUNT(bi.BookingItemId) AS Bookings,
                         SUM(bi.Subtotal) AS Revenue,
                         SUM(bi.Subtotal) * @Rate AS Commission
                  FROM Hotels h
                  JOIN Rooms r ON r.HotelId = h.HotelId
                  JOIN BookingItems bi ON bi.RoomId = r.RoomId
                  JOIN Bookings b ON b.BookingId = bi.BookingId
                  WHERE b.BookingDate BETWEEN @From AND @To
                  GROUP BY h.HotelId, h.HotelName
                  ORDER BY Revenue DESC",
                new SqlParameter("@Rate", CommissionRate),
                new SqlParameter("@From", dtFrom.Value.Date),
                new SqlParameter("@To", dtTo.Value.Date.AddDays(1).AddSeconds(-1)));

            grid.DataSource = table;
            if (grid.Columns["HotelId"] != null) grid.Columns["HotelId"].Visible = false;

            decimal totalRevenue = 0, totalCommission = 0; int totalBookings = 0;
            foreach (System.Data.DataRow row in table.Rows)
            {
                totalBookings += Convert.ToInt32(row["Bookings"]);
                totalRevenue += Convert.ToDecimal(row["Revenue"]);
                totalCommission += Convert.ToDecimal(row["Commission"]);
            }
            lblTotals.Text = $"Total: {totalBookings} bookings   |   Revenue ${totalRevenue:N2}   |   Commission (10%) ${totalCommission:N2}";
        }
    }
}
