using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class HotelDetailsForm : Form
    {
        private readonly int hotelId;
        private ComboBox cboRoom;
        private DateTimePicker dtCheckIn, dtCheckOut;
        private NumericUpDown numQty;
        private Label lblError, lblHotelInfo;
        private DataGridView gridReviews;

        public HotelDetailsForm(int hotelId)
        {
            this.hotelId = hotelId;
            InitializeComponent();
            LoadHotel();
            LoadRooms();
            LoadReviews();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Hotel Details";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Hotel Details", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            lblHotelInfo = new Label { Location = new Point(24, 100), Size = new Size(772, 60), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

            var lblRoom = new Label { Text = "Room Type", Location = new Point(24, 172), AutoSize = true, Font = UIHelper.BaseFont };
            cboRoom = new ComboBox { Location = new Point(24, 194), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };

            var lblIn = new Label { Text = "Check-in", Location = new Point(340, 172), AutoSize = true, Font = UIHelper.BaseFont };
            dtCheckIn = new DateTimePicker { Location = new Point(340, 194), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) };

            var lblOut = new Label { Text = "Check-out", Location = new Point(484, 172), AutoSize = true, Font = UIHelper.BaseFont };
            dtCheckOut = new DateTimePicker { Location = new Point(484, 194), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(2) };

            var lblQty = new Label { Text = "Rooms", Location = new Point(628, 172), AutoSize = true, Font = UIHelper.BaseFont };
            numQty = new NumericUpDown { Location = new Point(628, 194), Width = 60, Minimum = 1, Maximum = 20, Value = 1 };

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(24, 228);

            var btnAddToCart = UIHelper.MakeButton("Add to Cart", UIHelper.CustomerColor, 200);
            btnAddToCart.Location = new Point(24, 256);
            btnAddToCart.Click += BtnAddToCart_Click;

            var lblReviews = new Label { Text = "Guest Reviews", Location = new Point(24, 306), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            gridReviews = new DataGridView { Location = new Point(24, 336), Size = new Size(772, 250) };
            UIHelper.StyleGrid(gridReviews);

            Controls.AddRange(new Control[]
            {
                lblHotelInfo, lblRoom, cboRoom, lblIn, dtCheckIn, lblOut, dtCheckOut, lblQty, numQty,
                lblError, btnAddToCart, lblReviews, gridReviews
            });
        }

        private void LoadHotel()
        {
            var table = DbHelper.GetDataTable(
                "SELECT HotelName, Category, City, Address, StarRating FROM Hotels WHERE HotelId = @Id",
                new SqlParameter("@Id", hotelId));
            if (table.Rows.Count == 0) { lblHotelInfo.Text = "Hotel not found."; return; }
            var row = table.Rows[0];
            string stars = row["StarRating"] == DBNull.Value ? "Unrated" : $"{Convert.ToDecimal(row["StarRating"]):N1} ★";
            lblHotelInfo.Text = $"{row["HotelName"]}  ({row["Category"]})\n{row["Address"]}, {row["City"]}   •   {stars}";
        }

        private void LoadRooms()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT r.RoomId, r.RoomType, r.PricePerNight,
                         o.DiscountPercent
                  FROM Rooms r
                  OUTER APPLY (
                      SELECT TOP 1 DiscountPercent FROM Offers o
                      WHERE o.RoomId = r.RoomId AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate
                      ORDER BY DiscountPercent DESC
                  ) o
                  WHERE r.HotelId = @Id
                  ORDER BY r.RoomType",
                new SqlParameter("@Id", hotelId));

            table.Columns.Add("Display", typeof(string));
            table.Columns.Add("EffectivePrice", typeof(decimal));
            foreach (System.Data.DataRow row in table.Rows)
            {
                decimal price = Convert.ToDecimal(row["PricePerNight"]);
                if (row["DiscountPercent"] != DBNull.Value)
                {
                    decimal pct = Convert.ToDecimal(row["DiscountPercent"]);
                    decimal discounted = Math.Round(price * (1 - pct / 100m), 2);
                    row["Display"] = $"{row["RoomType"]} - ${price:N2} → ${discounted:N2} ({pct}% off)";
                    row["EffectivePrice"] = discounted;
                }
                else
                {
                    row["Display"] = $"{row["RoomType"]} - ${price:N2}/night";
                    row["EffectivePrice"] = price;
                }
            }

            cboRoom.DisplayMember = "Display";
            cboRoom.ValueMember = "RoomId";
            cboRoom.DataSource = table;
        }

        private void LoadReviews()
        {
            gridReviews.DataSource = DbHelper.GetDataTable(
                @"SELECT u.FullName AS Reviewer, rv.Rating, rv.Comment, rv.ReviewDate
                  FROM Reviews rv JOIN Users u ON u.UserId = rv.CustomerId
                  WHERE rv.HotelId = @Id ORDER BY rv.ReviewDate DESC",
                new SqlParameter("@Id", hotelId));
        }

        private void BtnAddToCart_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            if (cboRoom.SelectedValue == null) { ShowError("This hotel has no rooms yet."); return; }
            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date) { ShowError("Check-out date must be after check-in date."); return; }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);

            DbHelper.ExecuteNonQuery(
                @"INSERT INTO Cart (CustomerId, RoomId, CheckInDate, CheckOutDate, Quantity)
                  VALUES (@Cust, @Room, @In, @Out, @Qty)",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Room", roomId),
                new SqlParameter("@In", dtCheckIn.Value.Date),
                new SqlParameter("@Out", dtCheckOut.Value.Date),
                new SqlParameter("@Qty", numQty.Value));

            MessageBox.Show("Added to your booking cart.", "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
