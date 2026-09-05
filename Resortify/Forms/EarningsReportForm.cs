using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class EarningsReportForm : Form
    {
        private DateTimePicker dtFrom, dtTo;
        private DataGridView grid;
        private Label lblTotal;

        public EarningsReportForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Earnings & Booking Report";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Earnings & Booking Report", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            var lblFrom = new Label { Text = "From", Location = new Point(24, 100), AutoSize = true, Font = UIHelper.BaseFont };
            dtFrom = new DateTimePicker { Location = new Point(24, 122), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-12) };
            dtFrom.ValueChanged += (s, e) => LoadData();

            var lblTo = new Label { Text = "To", Location = new Point(190, 100), AutoSize = true, Font = UIHelper.BaseFont };
            dtTo = new DateTimePicker { Location = new Point(190, 122), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(1) };
            dtTo.ValueChanged += (s, e) => LoadData();

            grid = new DataGridView { Location = new Point(24, 164), Size = new Size(872, 300) };
            UIHelper.StyleGrid(grid);

            lblTotal = new Label { Location = new Point(24, 476), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };

            Controls.AddRange(new Control[] { lblFrom, dtFrom, lblTo, dtTo, grid, lblTotal });
        }

        private void LoadData()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT u.FullName AS Guest, r.RoomType, bi.CheckInDate, bi.CheckOutDate, bi.Nights, bi.Subtotal, b.BookingDate, b.Status
                  FROM BookingItems bi
                  JOIN Bookings b ON b.BookingId = bi.BookingId
                  JOIN Rooms r ON r.RoomId = bi.RoomId
                  JOIN Users u ON u.UserId = b.CustomerId
                  WHERE r.HotelId = @Id AND b.BookingDate BETWEEN @From AND @To
                  ORDER BY b.BookingDate DESC",
                new SqlParameter("@Id", Session.HotelId),
                new SqlParameter("@From", dtFrom.Value.Date),
                new SqlParameter("@To", dtTo.Value.Date.AddDays(1).AddSeconds(-1)));

            grid.DataSource = table;

            decimal total = 0;
            foreach (System.Data.DataRow row in table.Rows) total += Convert.ToDecimal(row["Subtotal"]);
            lblTotal.Text = $"Running Total: ${total:N2}   (before Resortify's 10% platform commission)";
        }
    }
}
