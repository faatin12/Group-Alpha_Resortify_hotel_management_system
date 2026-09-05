using System;
using System.Collections.Generic;
using System.Data;
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
            public int CartId, RoomId, Nights, Quantity, Guests;
            public string HotelName = "", City = "", RoomType = "", Services = "";
            public DateTime CheckIn, CheckOut;
            public decimal UnitPrice, RoomSubtotal, ServiceAmount, DiscountAmount, GrandTotal;
        }
        private readonly List<CartLine> lines = new();
        private ListBox lstSummary;
        private Label lblRoomTotal, lblServiceTotal, lblDiscountTotal, lblGrandTotal, lblCouponStatus, lblError;
        private ComboBox cboPayment;
        private Label lblTransaction;
        private TextBox txtTransaction, txtCoupon;
        private Button btnConfirm, btnApplyCoupon, btnRemoveCoupon;
        private Panel bottomPanel;
        private string appliedCouponCode = "";
        private decimal couponDiscount = 0m;
        public CheckoutForm()
        {
            InitializeComponent();
            LoadCart();
        }
        private void InitializeComponent()
        {
            Text = "Resortify - Checkout";
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 102, 24, 20),
                ColumnCount = 2,
                RowCount = 1
            }
            ;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            var summary = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            var title = new Label
            {
                Text = "Booking Summary",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader
            }
            ;
            lstSummary = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                IntegralHeight = false,
                HorizontalScrollbar = true,
                BorderStyle = BorderStyle.None
            }
            ;
            summary.Controls.Add(lstSummary);
            summary.Controls.Add(title);
            root.Controls.Add(summary, 0, 0);
            var payment = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 9,
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            payment.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            payment.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            payment.Controls.Add(new Label
            {
                Text = "Payment & Discount",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader,
                TextAlign = ContentAlignment.MiddleLeft
            }
            , 0, 0);
            cboPayment = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 0, 4)
            }
            ;
            cboPayment.Items.AddRange(new object[] {
                "Pay at Hotel", "Credit Card", "Debit Card", "bKash", "Nagad", "Rocket", "Mobile Banking"
            }
            );
            cboPayment.SelectedIndex = 0;
            cboPayment.SelectedIndexChanged += (s, e) => UpdatePaymentFields();
            payment.Controls.Add(cboPayment, 0, 1);
            lblTransaction = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }
            ;
            payment.Controls.Add(lblTransaction, 0, 2);
            txtTransaction = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3),
                PlaceholderText = "Enter transaction ID"
            }
            ;
            payment.Controls.Add(txtTransaction, 0, 3);
            var paymentInfo = new Label
            {
                Text = "Pay at Hotel confirms immediately. Online payments require a transaction ID and stay pending until the hotel validates it.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 5, 0, 4),
                AutoSize = false,
                AutoEllipsis = true
            }
            ;
            payment.Controls.Add(paymentInfo, 0, 4);
            var couponPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 2, 0, 2)
            }
            ;
            couponPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            couponPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            couponPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            couponPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            couponPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            txtCoupon = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 6, 4),
                PlaceholderText = "Coupon code"
            }
            ;
            btnApplyCoupon = UIHelper.MakeButton("Apply", UIHelper.CustomerColor, 80, 30);
            btnApplyCoupon.Dock = DockStyle.Fill;
            btnApplyCoupon.Margin = new Padding(0, 4, 6, 4);
            btnApplyCoupon.Click += (s, e) => ApplyCoupon();
            btnRemoveCoupon = UIHelper.MakeButton("Remove", Color.FromArgb(95, 95, 95), 70, 30);
            btnRemoveCoupon.Dock = DockStyle.Fill;
            btnRemoveCoupon.Margin = new Padding(0, 4, 0, 4);
            btnRemoveCoupon.Click += (s, e) => RemoveCoupon();
            lblCouponStatus = new Label
            {
                Text = "Have a coupon? Try WELCOME10, RESORT15 or GETAWAY20.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }
            ;
            couponPanel.Controls.Add(txtCoupon, 0, 0);
            couponPanel.Controls.Add(btnApplyCoupon, 1, 0);
            couponPanel.Controls.Add(btnRemoveCoupon, 2, 0);
            couponPanel.Controls.Add(lblCouponStatus, 0, 1);
            couponPanel.SetColumnSpan(lblCouponStatus, 3);
            payment.Controls.Add(couponPanel, 0, 5);
            var totals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Margin = new Padding(0, 5, 0, 3),
                Padding = new Padding(0, 4, 0, 4)
            }
            ;
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            totals.Controls.Add(new Label
            {
                Text = "Room subtotal",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }
            , 0, 0);
            lblRoomTotal = Amount();
            totals.Controls.Add(lblRoomTotal, 1, 0);
            totals.Controls.Add(new Label
            {
                Text = "Services / extras",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }
            , 0, 1);
            lblServiceTotal = Amount();
            totals.Controls.Add(lblServiceTotal, 1, 1);
            totals.Controls.Add(new Label
            {
                Text = "Total discount",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }
            , 0, 2);
            lblDiscountTotal = Amount();
            totals.Controls.Add(lblDiscountTotal, 1, 2);
            totals.Controls.Add(new Label
            {
                Text = "Grand total",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            }
            , 0, 3);
            lblGrandTotal = Amount(true);
            totals.Controls.Add(lblGrandTotal, 1, 3);
            payment.Controls.Add(totals, 0, 6);
            lblError = UIHelper.MakeErrorLabel();
            lblError.Dock = DockStyle.Fill;
            lblError.AutoSize = false;
            lblError.TextAlign = ContentAlignment.MiddleLeft;
            payment.Controls.Add(lblError, 0, 7);
            btnConfirm = UIHelper.MakeButton("Confirm Booking", UIHelper.CustomerColor, 230, 40);
            btnConfirm.Anchor = AnchorStyles.Right;
            btnConfirm.Click += BtnPay_Click;
            payment.Controls.Add(btnConfirm, 0, 8);
            root.Controls.Add(payment, 1, 0);
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 0,
                Visible = false
            }
            ;
            Controls.Add(bottomPanel);
            Controls.Add(root);
            Controls.Add(UIHelper.BuildHeader("Checkout", UIHelper.CustomerColor, (s, e) => Close(), null));
            UpdatePaymentFields();
        }
        private Label Amount(bool strong = false) => new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", strong ? 14F : 10F, FontStyle.Bold),
            ForeColor = UIHelper.CustomerColor
        }
        ;
        private void LoadCart()
        {
            var table = DbHelper.GetDataTable(@"SELECT c.CartId,c.RoomId,h.HotelName,h.City,r.RoomType,c.CheckInDate,c.CheckOutDate,c.Quantity,c.Guests,c.DiscountAmount,DATEDIFF(DAY,c.CheckInDate,c.CheckOutDate) Nights,r.PricePerNight,
                ISNULL((SELECT SUM(cs.UnitPrice*cs.Quantity) FROM CartServices cs WHERE cs.CartId=c.CartId),0) ServiceAmount,
                ISNULL((SELECT STRING_AGG(CONCAT(sc.ServiceName,CASE WHEN sc.IsFree=1 THEN ' (Free)' ELSE CONCAT(' (+$',FORMAT(cs.UnitPrice,'N2'),')') END),', ') FROM CartServices cs JOIN ServiceCatalog sc ON sc.ServiceId=cs.ServiceId WHERE cs.CartId=c.CartId),'None') Services
                FROM Cart c JOIN Rooms r ON r.RoomId=c.RoomId JOIN Hotels h ON h.HotelId=r.HotelId WHERE c.CustomerId=@Id ORDER BY c.AddedDate", new SqlParameter("@Id", Session.UserId));
            lines.Clear();
            lstSummary.Items.Clear();
            foreach (DataRow r in table.Rows)
            {
                int nights = Convert.ToInt32(r["Nights"]), qty = Convert.ToInt32(r["Quantity"]);
                decimal unit = Convert.ToDecimal(r["PricePerNight"]), room = unit * nights * qty, service = Convert.ToDecimal(r["ServiceAmount"]), discount = Convert.ToDecimal(r["DiscountAmount"]);
                var line = new CartLine
                {
                    CartId = Convert.ToInt32(r["CartId"]),
                    RoomId = Convert.ToInt32(r["RoomId"]),
                    HotelName = r["HotelName"].ToString(),
                    City = r["City"].ToString(),
                    RoomType = r["RoomType"].ToString(),
                    CheckIn = Convert.ToDateTime(r["CheckInDate"]),
                    CheckOut = Convert.ToDateTime(r["CheckOutDate"]),
                    Nights = nights,
                    Quantity = qty,
                    Guests = Convert.ToInt32(r["Guests"]),
                    Services = r["Services"].ToString(),
                    UnitPrice = unit,
                    RoomSubtotal = room,
                    ServiceAmount = service,
                    DiscountAmount = discount
                }
                ;
                lines.Add(line);
                lstSummary.Items.Add($"{line.HotelName} • {line.City}\n{line.RoomType} • {line.CheckIn:dd MMM yyyy} → {line.CheckOut:dd MMM yyyy} • {nights} night(s) • {qty} room(s) • {line.Guests} guest(s)\nRoom: ${room:N2} • Services: ${service:N2} • Offer: -${discount:N2}\nServices: {line.Services}");
            }
            LoadAppliedCoupon();
            RecalculateTotals();
            btnConfirm.Enabled = lines.Count > 0;
            if (lines.Count == 0) ShowError("Your cart is empty. Return to the dashboard and select a room.");
        }
        private void LoadAppliedCoupon()
        {
            appliedCouponCode = "";
            couponDiscount = 0m;
            if (lines.Count == 0)
            {
                lblCouponStatus.Text = "Enter a coupon code if you have one.";
                return;
            }
            var t = DbHelper.GetDataTable("SELECT CouponCode,DiscountAmount FROM CartCoupons WHERE CustomerId=@Id", new SqlParameter("@Id", Session.UserId));
            if (t.Rows.Count > 0)
            {
                appliedCouponCode = Convert.ToString(t.Rows[0]["CouponCode"]) ?? "";
                couponDiscount = Convert.ToDecimal(t.Rows[0]["DiscountAmount"]);
                txtCoupon.Text = appliedCouponCode;
                lblCouponStatus.Text = $"Coupon {appliedCouponCode} applied: -${couponDiscount:N2}";
                lblCouponStatus.ForeColor = UIHelper.CustomerColor;
            }
            else lblCouponStatus.Text = "Have a coupon? Try WELCOME10, RESORT15 or GETAWAY20.";
        }
        private decimal BaseAfterOffers()
        {
            decimal room = 0, services = 0, offers = 0;
            foreach (var l in lines)
            {
                room += l.RoomSubtotal;
                services += l.ServiceAmount;
                offers += l.DiscountAmount;
            }
            return Math.Max(0m, room + services - offers);
        }
        private void RecalculateTotals()
        {
            decimal roomTotal = 0, serviceTotal = 0, offerDiscount = 0;
            foreach (var l in lines)
            {
                roomTotal += l.RoomSubtotal;
                serviceTotal += l.ServiceAmount;
                offerDiscount += l.DiscountAmount;
            }
            decimal baseTotal = Math.Max(0m, roomTotal + serviceTotal - offerDiscount);
            if (couponDiscount > baseTotal) couponDiscount = baseTotal;
            decimal totalDiscount = offerDiscount + couponDiscount;
            decimal grand = Math.Max(0m, baseTotal - couponDiscount);
            lblRoomTotal.Text = $"${roomTotal:N2}";
            lblServiceTotal.Text = $"${serviceTotal:N2}";
            lblDiscountTotal.Text = totalDiscount > 0 ? $"-${totalDiscount:N2}" : "$0.00";
            lblGrandTotal.Text = $"${grand:N2}";
        }
        private void ApplyCoupon()
        {
            lblError.Visible = false;
            if (lines.Count == 0)
            {
                ShowError("Your cart is empty.");
                return;
            }
            string code = txtCoupon.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Enter a coupon code.");
                return;
            }
            decimal baseTotal = BaseAfterOffers();
            var t = DbHelper.GetDataTable(@"SELECT TOP 1 CouponId,Code,Description,DiscountPercent,MaxDiscountAmount,MinimumBookingAmount,ValidFrom,ValidTo,MaxUses,UsedCount,Active
                FROM Coupons WHERE UPPER(Code)=@Code", new SqlParameter("@Code", code));
            if (t.Rows.Count == 0)
            {
                RemoveCoupon(false);
                ShowError("Invalid coupon code.");
                return;
            }
            DataRow r = t.Rows[0];
            DateTime today = DateTime.Today;
            if (!Convert.ToBoolean(r["Active"]) || today < Convert.ToDateTime(r["ValidFrom"]).Date || today > Convert.ToDateTime(r["ValidTo"]).Date)
            {
                ShowError("This coupon is not currently valid.");
                return;
            }
            if (r["MaxUses"] != DBNull.Value && Convert.ToInt32(r["UsedCount"]) >= Convert.ToInt32(r["MaxUses"]))
            {
                ShowError("This coupon has reached its usage limit.");
                return;
            }
            decimal minimum = Convert.ToDecimal(r["MinimumBookingAmount"]);
            if (baseTotal < minimum)
            {
                ShowError($"This coupon requires a minimum booking of ${minimum:N2}.");
                return;
            }
            decimal discount = Math.Round(baseTotal * Convert.ToDecimal(r["DiscountPercent"]) / 100m, 2);
            if (r["MaxDiscountAmount"] != DBNull.Value) discount = Math.Min(discount, Convert.ToDecimal(r["MaxDiscountAmount"]));
            discount = Math.Min(discount, baseTotal);
            DbHelper.ExecuteNonQuery(@"MERGE CartCoupons AS target USING (SELECT @CustomerId CustomerId) AS source ON target.CustomerId=source.CustomerId
                WHEN MATCHED THEN UPDATE SET CouponId=@CouponId,CouponCode=@Code,DiscountAmount=@Discount,AppliedAt=GETDATE()
                WHEN NOT MATCHED THEN INSERT(CustomerId,CouponId,CouponCode,DiscountAmount) VALUES(@CustomerId,@CouponId,@Code,@Discount);",
            new SqlParameter("@CustomerId", Session.UserId), new SqlParameter("@CouponId", Convert.ToInt32(r["CouponId"])), new SqlParameter("@Code", r["Code"]), new SqlParameter("@Discount", discount));
            appliedCouponCode = Convert.ToString(r["Code"]) ?? code;
            couponDiscount = discount;
            lblCouponStatus.Text = $"Coupon {appliedCouponCode} applied: -${discount:N2}";
            lblCouponStatus.ForeColor = UIHelper.CustomerColor;
            RecalculateTotals();
        }
        private void RemoveCoupon(bool showMessage = true)
        {
            DbHelper.ExecuteNonQuery("DELETE FROM CartCoupons WHERE CustomerId=@Id", new SqlParameter("@Id", Session.UserId));
            appliedCouponCode = "";
            couponDiscount = 0m;
            txtCoupon.Clear();
            lblCouponStatus.Text = "No coupon applied.";
            lblCouponStatus.ForeColor = Color.DimGray;
            RecalculateTotals();
            if (showMessage) MessageBox.Show("Coupon removed.", "Coupon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void UpdatePaymentFields()
        {
            bool online = cboPayment.SelectedItem != null && cboPayment.SelectedItem.ToString() != "Pay at Hotel";
            lblTransaction.Text = online ? "Transaction ID *  (required for online payment)" : "Transaction ID  (not required for Pay at Hotel)";
            lblTransaction.ForeColor = online ? Color.Firebrick : Color.FromArgb(70, 70, 70);
            txtTransaction.Enabled = online;
            txtTransaction.Visible = online;
            if (!online) txtTransaction.Clear();
        }
        private void BtnPay_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            if (lines.Count == 0) return;
            if (cboPayment.SelectedItem == null)
            {
                ShowError("Choose a payment option to continue.");
                return;
            }
            string method = cboPayment.SelectedItem.ToString();
            bool online = method != "Pay at Hotel";
            string transactionId = txtTransaction.Text.Trim();
            if (online && string.IsNullOrWhiteSpace(transactionId))
            {
                ShowError("Please enter the transaction ID for your online payment.");
                txtTransaction.Focus();
                return;
            }
            decimal roomTotal = 0, serviceTotal = 0, offerDiscount = 0;
            foreach (var l in lines)
            {
                roomTotal += l.RoomSubtotal;
                serviceTotal += l.ServiceAmount;
                offerDiscount += l.DiscountAmount;
            }
            decimal baseTotal = Math.Max(0m, roomTotal + serviceTotal - offerDiscount);
            couponDiscount = Math.Min(couponDiscount, baseTotal);
            decimal totalDiscount = offerDiscount + couponDiscount;
            decimal grandTotal = Math.Max(0m, baseTotal - couponDiscount);
            string confirmationText = online
            ? $"Please review your booking.\n\nPayment method: {method}\nTransaction ID: {transactionId}\nCoupon: {(string.IsNullOrWhiteSpace(appliedCouponCode) ? "None" : appliedCouponCode)}\nDiscount: -${totalDiscount:N2}\nGrand total: ${grandTotal:N2}\n\nYour booking will stay pending until the hotel validates the transaction ID.\n\nSubmit this booking request?"
            : $"Please review your booking.\n\nPayment method: Pay at Hotel\nCoupon: {(string.IsNullOrWhiteSpace(appliedCouponCode) ? "None" : appliedCouponCode)}\nDiscount: -${totalDiscount:N2}\nGrand total: ${grandTotal:N2}\n\nPay at Hotel bookings are confirmed immediately.\n\nConfirm this booking?";
            if (MessageBox.Show(confirmationText, online ? "Submit Booking Request" : "Confirm Booking", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            btnConfirm.Enabled = false;
            try
            {
                int bookingId = 0;
                DbHelper.RunTransaction((conn, tx) =>
                {
                    foreach (var line in lines)
                    {
                        using var check = new SqlCommand(@"SELECT r.TotalRooms-ISNULL((SELECT SUM(bi.Quantity) FROM BookingItems bi JOIN Bookings b ON b.BookingId=bi.BookingId WHERE bi.RoomId=r.RoomId AND b.Status IN('Pending','Approved','Confirmed','Completed') AND bi.CheckInDate<c.CheckOutDate AND bi.CheckOutDate>c.CheckInDate),0) FROM Cart c JOIN Rooms r WITH (UPDLOCK,HOLDLOCK) ON r.RoomId=c.RoomId WHERE c.CartId=@Cart AND c.CustomerId=@Cust", conn, tx);
                        check.Parameters.AddWithValue("@Cart", line.CartId); check.Parameters.AddWithValue("@Cust", Session.UserId);
                        int available = Convert.ToInt32(check.ExecuteScalar() ?? 0);
                        if (available < line.Quantity) throw new InvalidOperationException($"{line.RoomType} is no longer available for the selected dates. Only {available} room(s) remain.");
                    }
                    if (!string.IsNullOrWhiteSpace(appliedCouponCode))
                    {
                        using var couponCheck = new SqlCommand("SELECT CouponId,DiscountPercent,MaxDiscountAmount,MinimumBookingAmount,MaxUses,UsedCount,Active,ValidFrom,ValidTo FROM Coupons WITH (UPDLOCK,HOLDLOCK) WHERE Code=@Code", conn, tx);
                        couponCheck.Parameters.AddWithValue("@Code", appliedCouponCode);
                        using var cr = couponCheck.ExecuteReader();
                        if (!cr.Read()) throw new InvalidOperationException("The coupon is no longer available.");
                        if (!cr.GetBoolean(cr.GetOrdinal("Active")) || DateTime.Today < cr.GetDateTime(cr.GetOrdinal("ValidFrom")).Date || DateTime.Today > cr.GetDateTime(cr.GetOrdinal("ValidTo")).Date) throw new InvalidOperationException("The coupon is no longer valid.");
                        int maxUses = cr.IsDBNull(cr.GetOrdinal("MaxUses")) ? 0 : cr.GetInt32(cr.GetOrdinal("MaxUses"));
                        int used = cr.GetInt32(cr.GetOrdinal("UsedCount"));
                        if (maxUses > 0 && used >= maxUses) throw new InvalidOperationException("The coupon usage limit has been reached.");
                        decimal minimum = cr.GetDecimal(cr.GetOrdinal("MinimumBookingAmount"));
                        if (baseTotal < minimum) throw new InvalidOperationException($"The coupon requires a minimum booking of ${minimum:N2}.");
                        cr.Close();
                        using var inc = new SqlCommand("UPDATE Coupons SET UsedCount=UsedCount+1 WHERE Code=@Code", conn, tx); inc.Parameters.AddWithValue("@Code", appliedCouponCode); inc.ExecuteNonQuery();
                    }
                    string status = online ? "Pending" : "Confirmed";
                    using (var cmd = new SqlCommand(@"INSERT INTO Bookings(CustomerId,TotalAmount,RoomAmount,ServiceAmount,DiscountAmount,CouponCode,CouponDiscountAmount,PaymentMethod,TransactionId,Status,CustomerNotificationShown) OUTPUT INSERTED.BookingId VALUES(@Cust,@Total,@Room,@Service,@Discount,@Coupon,@CouponDiscount,@Method,@Txn,@Status,@Shown)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Cust", Session.UserId); cmd.Parameters.AddWithValue("@Total", grandTotal); cmd.Parameters.AddWithValue("@Room", roomTotal); cmd.Parameters.AddWithValue("@Service", serviceTotal); cmd.Parameters.AddWithValue("@Discount", totalDiscount); cmd.Parameters.AddWithValue("@Coupon", string.IsNullOrWhiteSpace(appliedCouponCode) ? (object)DBNull.Value : appliedCouponCode); cmd.Parameters.AddWithValue("@CouponDiscount", couponDiscount); cmd.Parameters.AddWithValue("@Method", method); cmd.Parameters.AddWithValue("@Txn", online ? (object)transactionId : DBNull.Value); cmd.Parameters.AddWithValue("@Status", status); cmd.Parameters.AddWithValue("@Shown", online ? 1 : 0); bookingId = (int)cmd.ExecuteScalar();
                    }
                    foreach (var line in lines)
                    {
                        using var item = new SqlCommand(@"INSERT INTO BookingItems(BookingId,RoomId,CheckInDate,CheckOutDate,Nights,Quantity,Guests,UnitPrice,Subtotal) OUTPUT INSERTED.BookingItemId VALUES(@B,@R,@I,@O,@N,@Q,@G,@U,@S)", conn, tx);
                        item.Parameters.AddWithValue("@B", bookingId); item.Parameters.AddWithValue("@R", line.RoomId); item.Parameters.AddWithValue("@I", line.CheckIn); item.Parameters.AddWithValue("@O", line.CheckOut); item.Parameters.AddWithValue("@N", line.Nights); item.Parameters.AddWithValue("@Q", line.Quantity); item.Parameters.AddWithValue("@G", line.Guests); item.Parameters.AddWithValue("@U", line.UnitPrice); item.Parameters.AddWithValue("@S", line.RoomSubtotal); int itemId = (int)item.ExecuteScalar();
                        using var svc = new SqlCommand("INSERT INTO BookingItemServices(BookingItemId,ServiceId,Quantity,UnitPrice) SELECT @Item,ServiceId,Quantity,UnitPrice FROM CartServices WHERE CartId=@Cart", conn, tx); svc.Parameters.AddWithValue("@Item", itemId); svc.Parameters.AddWithValue("@Cart", line.CartId); svc.ExecuteNonQuery();
                    }
                    using var clearCoupon = new SqlCommand("DELETE FROM CartCoupons WHERE CustomerId=@Cust", conn, tx); clearCoupon.Parameters.AddWithValue("@Cust", Session.UserId); clearCoupon.ExecuteNonQuery();
                    using var clearCart = new SqlCommand("DELETE FROM Cart WHERE CustomerId=@Cust", conn, tx); clearCart.Parameters.AddWithValue("@Cust", Session.UserId); clearCart.ExecuteNonQuery();
                }
                );
                ShowInvoice(bookingId, roomTotal, serviceTotal, totalDiscount, grandTotal, method, transactionId, online, appliedCouponCode, couponDiscount);
            }
            catch (Exception ex)
            {
                btnConfirm.Enabled = true;
                ShowError(ex.Message);
            }
        }
        private void ShowInvoice(int id, decimal room, decimal services, decimal discount, decimal total, string method, string transactionId, bool pendingApproval, string couponCode, decimal couponDiscountAmount)
        {
            bottomPanel.Visible = true;
            bottomPanel.Height = 0;
            var box = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(16),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            box.Controls.Add(new Label
            {
                Text = $"BOOKING REQUEST SUBMITTED  •  #{id}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader
            }
            , 0, 0);
            box.SetColumnSpan(box.GetControlFromPosition(0, 0), 2);
            box.Controls.Add(new Label
            {
                Text = pendingApproval ? $"Guest: {Session.FullName}\nPayment option: {method}\nTransaction ID: {transactionId}\nCoupon: {(string.IsNullOrWhiteSpace(couponCode) ? "None" : couponCode)}\nStatus: Pending transaction validation\n\nYour cart has been cleared. After the hotel validates your transaction ID, the booking will be confirmed and the dashboard will notify you.\nConfirmation email will be sent to your registered email after validation." : $"Guest: {Session.FullName}\nPayment option: {method}\nCoupon: {(string.IsNullOrWhiteSpace(couponCode) ? "None" : couponCode)}\nStatus: Confirmed\n\nYour Pay at Hotel booking is confirmed. Your cart has been cleared.",
                Dock = DockStyle.Fill
            }
            , 0, 1);
            box.SetColumnSpan(box.GetControlFromPosition(0, 1), 2);
            box.Controls.Add(new Label
            {
                Text = "Room amount",
                Dock = DockStyle.Fill
            }
            , 0, 2);
            box.Controls.Add(new Label
            {
                Text = $"${room:N2}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            }
            , 1, 2);
            box.Controls.Add(new Label
            {
                Text = "Services",
                Dock = DockStyle.Fill
            }
            , 0, 3);
            box.Controls.Add(new Label
            {
                Text = $"${services:N2}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            }
            , 1, 3);
            box.Controls.Add(new Label
            {
                Text = $"Discount {(string.IsNullOrWhiteSpace(couponCode) ? "" : "(coupon " + couponCode + ")")}",
                Dock = DockStyle.Fill
            }
            , 0, 4);
            box.Controls.Add(new Label
            {
                Text = $"-${discount:N2}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            }
            , 1, 4);
            box.Controls.Add(new Label
            {
                Text = "Grand total",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            }
            , 0, 5);
            box.Controls.Add(new Label
            {
                Text = $"${total:N2}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.CustomerColor
            }
            , 1, 5);
            var done = UIHelper.MakeButton("View My Bookings", UIHelper.CustomerColor, 200, 36);
            done.Anchor = AnchorStyles.None;
            done.Click += (s, e) => {
                new BookingHistoryForm().Show();
                Close();
            }
            ;
            box.Controls.Add(done, 0, 6);
            box.SetColumnSpan(done, 2);
            bottomPanel.Controls.Add(box);
            bottomPanel.Height = 260;
        }
        private void ShowError(string text)
        {
            lblError.Text = text;
            lblError.Visible = true;
        }
    }
}
