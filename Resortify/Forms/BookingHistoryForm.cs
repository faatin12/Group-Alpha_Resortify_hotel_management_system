using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class BookingHistoryForm : Form
    {
        private DataGridView grid;

        public BookingHistoryForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - My Bookings";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("My Bookings", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(852, 340) };
            UIHelper.StyleGrid(grid);

            var btnCancel = UIHelper.MakeButton("Cancel Booking", Color.FromArgb(120, 20, 20), 160);
            btnCancel.Location = new Point(24, 456);
            btnCancel.Click += (s, e) => CancelSelected();

            var btnReview = UIHelper.MakeButton("Leave a Review", UIHelper.CustomerColor, 160);
            btnReview.Location = new Point(196, 456);
            btnReview.Click += (s, e) => LeaveReview();

            Controls.AddRange(new Control[] { grid, btnCancel, btnReview });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT b.BookingId, h.HotelId, h.HotelName, r.RoomType, bi.CheckInDate, bi.CheckOutDate,
                         bi.Subtotal, b.PaymentMethod, b.Status, b.BookingDate
                  FROM BookingItems bi
                  JOIN Bookings b ON b.BookingId = bi.BookingId
                  JOIN Rooms r ON r.RoomId = bi.RoomId
                  JOIN Hotels h ON h.HotelId = r.HotelId
                  WHERE b.CustomerId = @Id
                  ORDER BY b.BookingDate DESC",
                new SqlParameter("@Id", Session.UserId));
            if (grid.Columns["HotelId"] != null) grid.Columns["HotelId"].Visible = false;
        }

        private void CancelSelected()
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Select a booking first.", "Resortify"); return; }
            string status = grid.CurrentRow.Cells["Status"].Value.ToString();
            if (status != "Confirmed")
            {
                MessageBox.Show("Only confirmed (not yet completed or already cancelled) bookings can be cancelled.", "Resortify");
                return;
            }
            if (MessageBox.Show("Cancel this booking?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            int bookingId = Convert.ToInt32(grid.CurrentRow.Cells["BookingId"].Value);
            DbHelper.ExecuteNonQuery("UPDATE Bookings SET Status = 'Cancelled' WHERE BookingId = @Id",
                new SqlParameter("@Id", bookingId));
            LoadData();
        }

        private void LeaveReview()
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Select a booking first.", "Resortify"); return; }
            int hotelId = Convert.ToInt32(grid.CurrentRow.Cells["HotelId"].Value);
            string hotelName = grid.CurrentRow.Cells["HotelName"].Value.ToString();
            using var dlg = new ReviewDialog(hotelId, hotelName);
            dlg.ShowDialog();
        }
    }
}
