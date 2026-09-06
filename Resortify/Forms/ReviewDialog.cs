using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class ReviewDialog : Form
    {
        private readonly int hotelId;
        private NumericUpDown numRating;
        private TextBox txtComment;
        private Label lblError;

        public ReviewDialog(int hotelId, string hotelName)
        {
            this.hotelId = hotelId;
            InitializeComponent(hotelName);
        }

        private void InitializeComponent(string hotelName)
        {
            Text = "Resortify - Leave a Review";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var lblTitle = new Label { Text = $"Rate your stay at\n{hotelName}", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };

            var lblRating = new Label { Text = "Rating (1-5)", Location = new Point(20, 76), AutoSize = true, Font = UIHelper.BaseFont };
            numRating = new NumericUpDown { Location = new Point(20, 98), Width = 60, Minimum = 1, Maximum = 5, Value = 5 };

            var lblComment = new Label { Text = "Comment", Location = new Point(20, 134), AutoSize = true, Font = UIHelper.BaseFont };
            txtComment = new TextBox { Location = new Point(20, 156), Width = 330, Height = 70, Multiline = true, Font = UIHelper.BaseFont };

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(20, 232);

            var btnSubmit = UIHelper.MakeButton("Submit Review", UIHelper.CustomerColor, 330, 34);
            btnSubmit.Location = new Point(20, 254);
            btnSubmit.Click += BtnSubmit_Click;

            Controls.AddRange(new Control[] { lblTitle, lblRating, numRating, lblComment, txtComment, lblError, btnSubmit });
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            if (txtComment.Text.Trim().Length == 0)
            {
                lblError.Text = "Please add a short comment.";
                lblError.Visible = true;
                return;
            }

            // A customer can review a hotel only after completing a booking there.
            DataTable completedBooking = DbHelper.GetDataTable(
                @"SELECT TOP 1 b.BookingId
                  FROM Bookings b
                  JOIN BookingItems bi ON bi.BookingId = b.BookingId
                  JOIN Rooms r ON r.RoomId = bi.RoomId
                  WHERE b.CustomerId = @Cust
                    AND r.HotelId = @Hotel
                    AND b.Status = 'Completed'",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Hotel", hotelId));

            if (completedBooking.Rows.Count == 0)
            {
                lblError.Text = "You can review this hotel after completing a stay.";
                lblError.Visible = true;
                return;
            }

            // Keep the current Reviews table simple: one review per customer per hotel.
            DataTable existing = DbHelper.GetDataTable(
                @"SELECT TOP 1 ReviewId
                  FROM Reviews
                  WHERE CustomerId = @Cust AND HotelId = @Hotel",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Hotel", hotelId));

            if (existing.Rows.Count > 0)
            {
                lblError.Text = "You have already reviewed this hotel.";
                lblError.Visible = true;
                return;
            }

            DbHelper.ExecuteNonQuery(
                "INSERT INTO Reviews (CustomerId, HotelId, Rating, Comment) VALUES (@Cust, @Hotel, @Rating, @Comment)",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Hotel", hotelId),
                new SqlParameter("@Rating", numRating.Value),
                new SqlParameter("@Comment", txtComment.Text.Trim()));

            MessageBox.Show("Thanks for your review!", "Resortify",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}
