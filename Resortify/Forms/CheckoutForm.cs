using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class CheckoutForm : Form
    {
        private class CartLine
        {
            public int CartId; public int RoomId; public string HotelName; public string RoomType;
            public DateTime CheckIn; public DateTime CheckOut; public int Nights; public int Quantity;
            public decimal UnitPrice; public decimal Subtotal;
        }

        private List<CartLine> lines = new();
        private ListBox lstSummary;
        private Label lblTotal;
        private ComboBox cboPayment;
        private TextBox txtCardNumber;
        private Button btnPay;
        private Panel invoicePanel;

        public CheckoutForm()
        {
            InitializeComponent();
            LoadCart();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Checkout";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Checkout", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            var lblSummary = new Label { Text = "Order Summary", Location = new Point(24, 100), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            lstSummary = new ListBox { Location = new Point(24, 128), Size = new Size(420, 180), Font = UIHelper.BaseFont };

            lblTotal = new Label { Location = new Point(24, 318), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };

            var lblPayment = new Label { Text = "Payment Method", Location = new Point(24, 356), AutoSize = true, Font = UIHelper.BaseFont };
            cboPayment = new ComboBox { Location = new Point(24, 378), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboPayment.Items.AddRange(new object[] { "Credit Card", "Debit Card", "Mobile Banking", "bKash", "SSLCommerz" });
            cboPayment.SelectedIndex = 0;

            var lblCard = new Label { Text = "Card / Account Number", Location = new Point(24, 414), AutoSize = true, Font = UIHelper.BaseFont };
            txtCardNumber = new TextBox { Location = new Point(24, 436), Width = 300, Font = UIHelper.BaseFont, PlaceholderText = "e.g. 4111 1111 1111 1111" };

            btnPay = UIHelper.MakeButton("Confirm & Pay", UIHelper.CustomerColor, 300, 42);
            btnPay.Location = new Point(24, 476);
            btnPay.Click += BtnPay_Click;

            invoicePanel = new Panel
            {
                Location = new Point(480, 100),
                Size = new Size(430, 420),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250),
                Visible = false
            };

            Controls.AddRange(new Control[] { lblSummary, lstSummary, lblTotal, lblPayment, cboPayment, lblCard, txtCardNumber, btnPay, invoicePanel });
        }

        private void LoadCart()
        {
            var table = DbHelper.GetDataTable(
                @"SELECT c.CartId, c.RoomId, h.HotelName, r.RoomType, c.CheckInDate, c.CheckOutDate, c.Quantity,
                         DATEDIFF(DAY, c.CheckInDate, c.CheckOutDate) AS Nights,
                         r.PricePerNight, ISNULL(o.DiscountPercent, 0) AS DiscountPercent
                  FROM Cart c
                  JOIN Rooms r ON r.RoomId = c.RoomId
                  JOIN Hotels h ON h.HotelId = r.HotelId
                  OUTER APPLY (
                      SELECT TOP 1 DiscountPercent FROM Offers o
                      WHERE o.RoomId = r.RoomId AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate
                      ORDER BY DiscountPercent DESC
                  ) o
                  WHERE c.CustomerId = @Id",
                new SqlParameter("@Id", Session.UserId));

            lines.Clear();
            lstSummary.Items.Clear();
            decimal total = 0;
            foreach (System.Data.DataRow row in table.Rows)
            {
                decimal price = Convert.ToDecimal(row["PricePerNight"]);
                decimal pct = Convert.ToDecimal(row["DiscountPercent"]);
                decimal unit = Math.Round(price * (1 - pct / 100m), 2);
                int nights = Convert.ToInt32(row["Nights"]);
                int qty = Convert.ToInt32(row["Quantity"]);
                decimal subtotal = unit * nights * qty;

                var line = new CartLine
                {
                    CartId = Convert.ToInt32(row["CartId"]),
                    RoomId = Convert.ToInt32(row["RoomId"]),
                    HotelName = row["HotelName"].ToString(),
                    RoomType = row["RoomType"].ToString(),
                    CheckIn = Convert.ToDateTime(row["CheckInDate"]),
                    CheckOut = Convert.ToDateTime(row["CheckOutDate"]),
                    Nights = nights,
                    Quantity = qty,
                    UnitPrice = unit,
                    Subtotal = subtotal
                };
                lines.Add(line);
                total += subtotal;
                lstSummary.Items.Add($"{line.HotelName} - {line.RoomType} x{qty}  ({nights}n)   ${subtotal:N2}");
            }

            lblTotal.Text = $"Total Due: ${total:N2}";
            btnPay.Enabled = lines.Count > 0;
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (lines.Count == 0) return;
            if (string.IsNullOrWhiteSpace(txtCardNumber.Text))
            {
                MessageBox.Show("Enter a card / account number to continue.", "Resortify");
                return;
            }

            decimal total = 0;
            foreach (var l in lines) total += l.Subtotal;
            string paymentMethod = cboPayment.SelectedItem.ToString();
            int newBookingId = 0;
            DateTime bookingDate = DateTime.Now;

            // Single transaction: one Bookings row + one BookingItems row per room booked,
            // then clear the cart -- all or nothing, so a mid-way failure never leaves a
            // half-confirmed booking (Chapter 8's "careful transaction handling" concern).
            DbHelper.RunTransaction((conn, tx) =>
            {
                using (var cmd = new SqlCommand(
                    @"INSERT INTO Bookings (CustomerId, TotalAmount, PaymentMethod, Status)
                      OUTPUT INSERTED.BookingId
                      VALUES (@Cust, @Total, @Method, 'Confirmed')", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Cust", Session.UserId);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Method", paymentMethod);
                    newBookingId = (int)cmd.ExecuteScalar();
                }

                foreach (var line in lines)
                {
                    // one BookingItem row per room unit booked, matching the schema
                    // (BookingItems has no Quantity column -- each row is one room instance)
                    for (int i = 0; i < line.Quantity; i++)
                    {
                        using var cmd = new SqlCommand(
                            @"INSERT INTO BookingItems (BookingId, RoomId, CheckInDate, CheckOutDate, Nights, UnitPrice, Subtotal)
                              VALUES (@Booking, @Room, @In, @Out, @Nights, @Unit, @Sub)", conn, tx);
                        cmd.Parameters.AddWithValue("@Booking", newBookingId);
                        cmd.Parameters.AddWithValue("@Room", line.RoomId);
                        cmd.Parameters.AddWithValue("@In", line.CheckIn);
                        cmd.Parameters.AddWithValue("@Out", line.CheckOut);
                        cmd.Parameters.AddWithValue("@Nights", line.Nights);
                        cmd.Parameters.AddWithValue("@Unit", line.UnitPrice);
                        cmd.Parameters.AddWithValue("@Sub", line.UnitPrice * line.Nights);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var cmd = new SqlCommand("DELETE FROM Cart WHERE CustomerId = @Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", Session.UserId);
                    cmd.ExecuteNonQuery();
                }
            });

            ShowInvoice(newBookingId, bookingDate, total, paymentMethod);
            btnPay.Enabled = false;
        }

        private void ShowInvoice(int bookingId, DateTime date, decimal total, string method)
        {
            invoicePanel.Controls.Clear();
            invoicePanel.Visible = true;

            var title = new Label { Text = "INVOICE", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };
            var status = new Label { Text = "CONFIRMED", ForeColor = Color.White, BackColor = Color.FromArgb(46, 125, 50), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(320, 18), AutoSize = true, Padding = new Padding(6, 3, 6, 3) };
            var meta = new Label { Text = $"Booking #{bookingId}\n{date:dd MMM yyyy, hh:mm tt}\nGuest: {Session.FullName}", Location = new Point(16, 50), AutoSize = true, Font = UIHelper.BaseFont };

            invoicePanel.Controls.AddRange(new Control[] { title, status, meta });

            int y = 120;
            foreach (var line in lines)
            {
                var l = new Label
                {
                    Text = $"{line.HotelName} — {line.RoomType}\n{line.CheckIn:dd MMM} to {line.CheckOut:dd MMM}, {line.Nights} night(s) x{line.Quantity}",
                    Location = new Point(16, y),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F)
                };
                var p = new Label { Text = $"${line.Subtotal:N2}", Location = new Point(340, y), AutoSize = true, Font = UIHelper.BaseFont };
                invoicePanel.Controls.Add(l);
                invoicePanel.Controls.Add(p);
                y += 44;
            }

            var totalLabel = new Label { Text = $"Total Paid: ${total:N2}", Location = new Point(16, y + 10), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            var methodLabel = new Label { Text = $"Payment Method: {method}", Location = new Point(16, y + 40), AutoSize = true, Font = UIHelper.BaseFont };
            invoicePanel.Controls.Add(totalLabel);
            invoicePanel.Controls.Add(methodLabel);

            var btnDone = UIHelper.MakeButton("View My Bookings", UIHelper.CustomerColor, 200);
            btnDone.Location = new Point(16, y + 76);
            btnDone.Click += (s, e) => { new BookingHistoryForm().Show(); Close(); };
            invoicePanel.Controls.Add(btnDone);
        }
    }
}
