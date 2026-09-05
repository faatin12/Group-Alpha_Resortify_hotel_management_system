using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class BookingCartForm : Form
    {
        private DataGridView grid;
        private DateTimePicker dtCheckIn, dtCheckOut;
        private NumericUpDown numQty;
        private Label lblError, lblTotal;
        private int? selectedCartId;

        public BookingCartForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Booking Cart";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Booking Cart", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(832, 260) };
            UIHelper.StyleGrid(grid);
            grid.SelectionChanged += (s, e) => PopulateFromSelection();
            Controls.Add(grid);

            int y = 380;
            var lblIn = new Label { Text = "Check-in", Location = new Point(24, y), AutoSize = true };
            dtCheckIn = new DateTimePicker { Location = new Point(24, y + 20), Width = 140, Format = DateTimePickerFormat.Short };

            var lblOut = new Label { Text = "Check-out", Location = new Point(180, y), AutoSize = true };
            dtCheckOut = new DateTimePicker { Location = new Point(180, y + 20), Width = 140, Format = DateTimePickerFormat.Short };

            var lblQty = new Label { Text = "Rooms", Location = new Point(336, y), AutoSize = true };
            numQty = new NumericUpDown { Location = new Point(336, y + 20), Width = 60, Minimum = 1, Maximum = 20 };

            var btnUpdate = UIHelper.MakeButton("Update Item", UIHelper.CustomerColor, 130);
            btnUpdate.Location = new Point(420, y + 18);
            btnUpdate.Click += (s, e) => UpdateSelected();

            var btnRemove = UIHelper.MakeButton("Remove Item", Color.FromArgb(120, 20, 20), 130);
            btnRemove.Location = new Point(560, y + 18);
            btnRemove.Click += (s, e) => RemoveSelected();

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(24, y + 56);

            lblTotal = new Label { Location = new Point(24, y + 88), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };

            var btnCheckout = UIHelper.MakeButton("Proceed to Checkout", UIHelper.CustomerColor, 220, 40);
            btnCheckout.Location = new Point(24, y + 122);
            btnCheckout.Click += (s, e) =>
            {
                if (grid.Rows.Count == 0) { ShowError("Your cart is empty."); return; }
                new CheckoutForm().Show();
                Close();
            };

            Controls.AddRange(new Control[] { lblIn, dtCheckIn, lblOut, dtCheckOut, lblQty, numQty, btnUpdate, btnRemove, lblError, lblTotal, btnCheckout });
        }

        private void LoadData()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT c.CartId, h.HotelName, r.RoomType, c.CheckInDate, c.CheckOutDate, c.Quantity,
                         DATEDIFF(DAY, c.CheckInDate, c.CheckOutDate) AS Nights,
                         ISNULL(o.DiscountPercent, 0) AS DiscountPercent,
                         r.PricePerNight
                  FROM Cart c
                  JOIN Rooms r ON r.RoomId = c.RoomId
                  JOIN Hotels h ON h.HotelId = r.HotelId
                  OUTER APPLY (
                      SELECT TOP 1 DiscountPercent FROM Offers o
                      WHERE o.RoomId = r.RoomId AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate
                      ORDER BY DiscountPercent DESC
                  ) o
                  WHERE c.CustomerId = @Id
                  ORDER BY c.AddedDate",
                new SqlParameter("@Id", Session.UserId));

            table.Columns.Add("UnitPrice", typeof(decimal));
            table.Columns.Add("Subtotal", typeof(decimal));
            decimal total = 0;
            foreach (System.Data.DataRow row in table.Rows)
            {
                decimal price = Convert.ToDecimal(row["PricePerNight"]);
                decimal pct = Convert.ToDecimal(row["DiscountPercent"]);
                decimal unit = Math.Round(price * (1 - pct / 100m), 2);
                int nights = Convert.ToInt32(row["Nights"]);
                int qty = Convert.ToInt32(row["Quantity"]);
                decimal subtotal = unit * nights * qty;
                row["UnitPrice"] = unit;
                row["Subtotal"] = subtotal;
                total += subtotal;
            }

            grid.DataSource = table;
            foreach (string hidden in new[] { "CartId", "PricePerNight", "DiscountPercent" })
                if (grid.Columns[hidden] != null) grid.Columns[hidden].Visible = false;

            lblTotal.Text = $"Cart Total: ${total:N2}";
        }

        private void PopulateFromSelection()
        {
            if (grid.CurrentRow == null) { selectedCartId = null; return; }
            selectedCartId = Convert.ToInt32(grid.CurrentRow.Cells["CartId"].Value);
            dtCheckIn.Value = Convert.ToDateTime(grid.CurrentRow.Cells["CheckInDate"].Value);
            dtCheckOut.Value = Convert.ToDateTime(grid.CurrentRow.Cells["CheckOutDate"].Value);
            numQty.Value = Convert.ToInt32(grid.CurrentRow.Cells["Quantity"].Value);
        }

        private void UpdateSelected()
        {
            lblError.Visible = false;
            if (selectedCartId == null) { ShowError("Select a cart item first."); return; }
            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date) { ShowError("Check-out date must be after check-in date."); return; }

            DbHelper.ExecuteNonQuery(
                "UPDATE Cart SET CheckInDate = @In, CheckOutDate = @Out, Quantity = @Qty WHERE CartId = @Id",
                new SqlParameter("@In", dtCheckIn.Value.Date),
                new SqlParameter("@Out", dtCheckOut.Value.Date),
                new SqlParameter("@Qty", numQty.Value),
                new SqlParameter("@Id", selectedCartId.Value));
            LoadData();
        }

        private void RemoveSelected()
        {
            if (selectedCartId == null) { ShowError("Select a cart item first."); return; }
            DbHelper.ExecuteNonQuery("DELETE FROM Cart WHERE CartId = @Id", new SqlParameter("@Id", selectedCartId.Value));
            selectedCartId = null;
            LoadData();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
