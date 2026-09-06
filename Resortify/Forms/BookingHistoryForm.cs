using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    // Shows all bookings made by the logged-in customer.
    public partial class BookingHistoryForm : Form
    {
        private DataGridView bookingGrid;
        private TextBox detailsBox;

        private int selectedBookingId = 0;
        private int selectedHotelId = 0;

        public BookingHistoryForm()
        {
            InitializeComponent();
            LoadBookings();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - My Bookings";
            ClientSize = new Size(1120, 700);
            MinimumSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;

            Controls.Add(UIHelper.BuildHeader(
                "My Bookings",
                UIHelper.CustomerColor,
                Back_Click,
                null));

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(24, 18, 24, 20);
            root.RowCount = 2;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            bookingGrid = new DataGridView();
            bookingGrid.Dock = DockStyle.Fill;
            UIHelper.StyleGrid(bookingGrid);
            bookingGrid.SelectionChanged += BookingGrid_SelectionChanged;
            root.Controls.Add(bookingGrid, 0, 0);

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Fill;
            bottom.BackColor = Color.White;
            bottom.Padding = new Padding(12);
            bottom.BorderStyle = BorderStyle.FixedSingle;

            detailsBox = new TextBox();
            detailsBox.Dock = DockStyle.Fill;
            detailsBox.Multiline = true;
            detailsBox.ReadOnly = true;
            detailsBox.ScrollBars = ScrollBars.Vertical;
            detailsBox.BorderStyle = BorderStyle.None;
            detailsBox.Font = new Font("Segoe UI", 9.5F);

            bottom.Controls.Add(detailsBox);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 50;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.Padding = new Padding(0, 6, 0, 0);

            Button refreshButton = UIHelper.MakeButton(
                "Refresh Bookings", UIHelper.CustomerColor, 150, 34);
            refreshButton.Click += Refresh_Click;

            Button cancelButton = UIHelper.MakeButton(
                "Cancel Booking", Color.FromArgb(120, 20, 20), 150, 34);
            cancelButton.Click += Cancel_Click;

            Button reviewButton = UIHelper.MakeButton(
                "Leave a Review", UIHelper.CustomerColor, 150, 34);
            reviewButton.Click += Review_Click;

            actions.Controls.Add(refreshButton);
            actions.Controls.Add(cancelButton);
            actions.Controls.Add(reviewButton);
            bottom.Controls.Add(actions);

            root.Controls.Add(bottom, 0, 1);
            Controls.Add(root);
        }

        private void LoadBookings()
        {
            string sql = @"
                SELECT
                    b.BookingId,
                    h.HotelId,
                    h.HotelName,
                    h.City,
                    r.RoomType,
                    MIN(bi.CheckInDate) AS CheckIn,
                    MAX(bi.CheckOutDate) AS CheckOut,
                    SUM(bi.Nights) AS Nights,
                    SUM(bi.Quantity) AS Rooms,
                    MAX(bi.Guests) AS Guests,
                    b.RoomAmount,
                    b.ServiceAmount,
                    b.DiscountAmount,
                    b.CouponCode,
                    b.CouponDiscountAmount,
                    b.TotalAmount AS GrandTotal,
                    b.PaymentMethod,
                    b.Status,
                    b.BookingDate
                FROM Bookings b
                JOIN BookingItems bi ON bi.BookingId=b.BookingId
                JOIN Rooms r ON r.RoomId=bi.RoomId
                JOIN Hotels h ON h.HotelId=r.HotelId
                WHERE b.CustomerId=@Id
                GROUP BY
                    b.BookingId, h.HotelId, h.HotelName, h.City,
                    r.RoomType, b.RoomAmount, b.ServiceAmount,
                    b.DiscountAmount, b.CouponCode,
                    b.CouponDiscountAmount, b.TotalAmount,
                    b.PaymentMethod, b.Status, b.BookingDate
                ORDER BY b.BookingDate DESC";

            bookingGrid.DataSource = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", Session.UserId));

            if (bookingGrid.Columns["HotelId"] != null)
            {
                bookingGrid.Columns["HotelId"].Visible = false;
            }

            if (bookingGrid.Columns["GrandTotal"] != null)
            {
                bookingGrid.Columns["GrandTotal"].HeaderText = "Grand Total";
            }

            LoadSelectedDetails();
        }

        private void LoadSelectedDetails()
        {
            if (bookingGrid.CurrentRow == null)
            {
                selectedBookingId = 0;
                selectedHotelId = 0;
                detailsBox.Text =
                    "Select a booking to see its complete room and service details.";
                return;
            }

            selectedBookingId = Convert.ToInt32(
                bookingGrid.CurrentRow.Cells["BookingId"].Value);
            selectedHotelId = Convert.ToInt32(
                bookingGrid.CurrentRow.Cells["HotelId"].Value);

            string sql = @"
                SELECT
                    h.HotelName,
                    h.City,
                    r.RoomType,
                    bi.CheckInDate,
                    bi.CheckOutDate,
                    bi.Nights,
                    bi.Quantity,
                    bi.Guests,
                    bi.UnitPrice,
                    ISNULL(
                        (SELECT SUM(bis.UnitPrice * bis.Quantity)
                         FROM BookingItemServices bis
                         WHERE bis.BookingItemId=bi.BookingItemId), 0) AS ServiceAmount,
                    ISNULL(
                        (SELECT STRING_AGG(
                            CONCAT(sc.ServiceName,
                                CASE WHEN sc.IsFree=1 THEN ' (Free)'
                                ELSE CONCAT(' (+$', FORMAT(bis.UnitPrice,'N2'), ')') END),
                            ', ')
                         FROM BookingItemServices bis
                         JOIN ServiceCatalog sc ON sc.ServiceId=bis.ServiceId
                         WHERE bis.BookingItemId=bi.BookingItemId), 'None') AS Services
                FROM BookingItems bi
                JOIN Rooms r ON r.RoomId=bi.RoomId
                JOIN Hotels h ON h.HotelId=r.HotelId
                WHERE bi.BookingId=@Id
                ORDER BY bi.BookingItemId";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", selectedBookingId));

            string text = "";

            foreach (DataRow row in table.Rows)
            {
                text += row["HotelName"] + " • " + row["City"] + "\r\n";
                text += row["RoomType"] + " • " +
                    Convert.ToDateTime(row["CheckInDate"]).ToString("dd MMM yyyy") +
                    " → " +
                    Convert.ToDateTime(row["CheckOutDate"]).ToString("dd MMM yyyy") +
                    " • " + row["Nights"] + " night(s) • " +
                    row["Quantity"] + " room(s) • " +
                    row["Guests"] + " guest(s)\r\n";
                text += "Services: " + row["Services"] + "\r\n\r\n";
            }

            DataGridViewRow selectedRow = bookingGrid.CurrentRow;
            text += "Room amount: $" +
                Convert.ToDecimal(selectedRow.Cells["RoomAmount"].Value).ToString("N2") + "\r\n";
            text += "Service amount: $" +
                Convert.ToDecimal(selectedRow.Cells["ServiceAmount"].Value).ToString("N2") + "\r\n";
            text += "Discount: $" +
                Convert.ToDecimal(selectedRow.Cells["DiscountAmount"].Value).ToString("N2") + "\r\n";
            text += "Grand total: $" +
                Convert.ToDecimal(selectedRow.Cells["GrandTotal"].Value).ToString("N2") + "\r\n";
            text += "Payment: " + selectedRow.Cells["PaymentMethod"].Value + "\r\n";
            text += "Status: " + selectedRow.Cells["Status"].Value;

            detailsBox.Text = text;
        }

        private void CancelSelected()
        {
            if (selectedBookingId == 0)
            {
                MessageBox.Show("Select a booking first.", "Resortify");
                return;
            }

            string status = bookingGrid.CurrentRow.Cells["Status"].Value.ToString();

            if (status != "Confirmed" && status != "Approved")
            {
                MessageBox.Show(
                    "Only approved or confirmed bookings can be cancelled.",
                    "Resortify");
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Cancel this booking?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            DbHelper.ExecuteNonQuery(
                "UPDATE Bookings SET Status='Cancelled' " +
                "WHERE BookingId=@Id AND CustomerId=@CustomerId",
                new SqlParameter("@Id", selectedBookingId),
                new SqlParameter("@CustomerId", Session.UserId));

            LoadBookings();
        }

        private void LeaveReview()
        {
            if (selectedBookingId == 0)
            {
                MessageBox.Show("Select a booking first.", "Resortify");
                return;
            }

            string status = bookingGrid.CurrentRow.Cells["Status"].Value.ToString();

            if (status != "Completed")
            {
                MessageBox.Show(
                    "You can leave a review after your booking is completed.",
                    "Resortify",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DataTable existing = DbHelper.GetDataTable(
                @"SELECT TOP 1 ReviewId
                  FROM Reviews
                  WHERE CustomerId = @Cust AND HotelId = @Hotel",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Hotel", selectedHotelId));

            if (existing.Rows.Count > 0)
            {
                MessageBox.Show(
                    "You have already reviewed this hotel.",
                    "Resortify",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string hotelName = bookingGrid.CurrentRow.Cells["HotelName"].Value.ToString();
            ReviewDialog dialog = new ReviewDialog(selectedHotelId, hotelName);
            dialog.ShowDialog(this);
            dialog.Dispose();
        }

        private void BookingGrid_SelectionChanged(object sender, EventArgs e)
        {
            LoadSelectedDetails();
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            LoadBookings();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            CancelSelected();
        }

        private void Review_Click(object sender, EventArgs e)
        {
            LeaveReview();
        }

        private void Back_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
