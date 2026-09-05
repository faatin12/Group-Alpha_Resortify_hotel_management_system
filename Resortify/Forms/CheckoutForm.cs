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
    // Final booking and payment screen.
    public partial class CheckoutForm : Form
    {
        private class CartLine
        {
            public int CartId;
            public int RoomId;
            public int Nights;
            public int Quantity;
            public int Guests;
            public string HotelName;
            public string City;
            public string RoomType;
            public string Services;
            public DateTime CheckIn;
            public DateTime CheckOut;
            public decimal UnitPrice;
            public decimal RoomSubtotal;
            public decimal ServiceAmount;
            public decimal DiscountAmount;
        }

        private List<CartLine> cartLines = new List<CartLine>();

        private ListBox summaryList;
        private ComboBox paymentBox;
        private TextBox transactionText;
        private TextBox couponText;

        private Label roomTotalLabel;
        private Label serviceTotalLabel;
        private Label discountTotalLabel;
        private Label grandTotalLabel;
        private Label couponStatusLabel;
        private Label transactionLabel;
        private Label errorLabel;
        private Label customerLabel;

        private Button confirmButton;
        private Button applyCouponButton;
        private Button removeCouponButton;

        private Panel invoicePanel;

        private string appliedCouponCode = "";
        private TableLayoutPanel root;
        private Panel summaryPanel;
        private Label summaryTitle;
        private TableLayoutPanel paymentPanel;
        private Label paymentTitle;
        private Label paymentInfo;
        private decimal couponDiscount = 0m;

        public CheckoutForm()
        {
            InitializeComponent();
            LoadCustomerDetails();
            LoadCart();
        }

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            summaryPanel = new Panel();
            summaryList = new ListBox();
            customerLabel = new Label();
            summaryTitle = new Label();
            paymentPanel = new TableLayoutPanel();
            paymentTitle = new Label();
            paymentBox = new ComboBox();
            transactionLabel = new Label();
            transactionText = new TextBox();
            paymentInfo = new Label();
            invoicePanel = new Panel();
            root.SuspendLayout();
            summaryPanel.SuspendLayout();
            paymentPanel.SuspendLayout();
            SuspendLayout();
            // 
            // root
            // 
            root.ColumnCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            root.Controls.Add(summaryPanel, 0, 0);
            root.Controls.Add(paymentPanel, 1, 0);
            root.Dock = DockStyle.Fill;
            root.Location = new Point(0, 0);
            root.Name = "root";
            root.Padding = new Padding(24, 102, 24, 20);
            root.RowCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            root.Size = new Size(1120, 720);
            root.TabIndex = 0;
            // 
            // summaryPanel
            // 
            summaryPanel.BackColor = Color.White;
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            summaryPanel.Controls.Add(summaryList);
            summaryPanel.Controls.Add(customerLabel);
            summaryPanel.Controls.Add(summaryTitle);
            summaryPanel.Dock = DockStyle.Fill;
            summaryPanel.Location = new Point(27, 105);
            summaryPanel.Name = "summaryPanel";
            summaryPanel.Padding = new Padding(14);
            summaryPanel.Size = new Size(594, 592);
            summaryPanel.TabIndex = 0;
            // 
            // summaryList
            // 
            summaryList.BorderStyle = BorderStyle.None;
            summaryList.Dock = DockStyle.Fill;
            summaryList.Font = new Font("Segoe UI", 9F);
            summaryList.HorizontalScrollbar = true;
            summaryList.IntegralHeight = false;
            summaryList.ItemHeight = 15;
            summaryList.Location = new Point(14, 110);
            summaryList.Name = "summaryList";
            summaryList.Size = new Size(564, 466);
            summaryList.TabIndex = 0;
            // 
            // customerLabel
            // 
            customerLabel.Dock = DockStyle.Top;
            customerLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            customerLabel.ForeColor = Color.FromArgb(35, 110, 68);
            customerLabel.Location = new Point(14, 48);
            customerLabel.Name = "customerLabel";
            customerLabel.Size = new Size(564, 62);
            customerLabel.TabIndex = 1;
            customerLabel.Text = "Customer details: Loading...";
            // 
            // summaryTitle
            // 
            summaryTitle.Dock = DockStyle.Top;
            summaryTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            summaryTitle.ForeColor = Color.FromArgb(21, 34, 56);
            summaryTitle.Location = new Point(14, 14);
            summaryTitle.Name = "summaryTitle";
            summaryTitle.Size = new Size(564, 34);
            summaryTitle.TabIndex = 2;
            summaryTitle.Text = "Booking Summary";
            // 
            // paymentPanel
            // 
            paymentPanel.BackColor = Color.White;
            paymentPanel.BorderStyle = BorderStyle.FixedSingle;
            paymentPanel.ColumnCount = 1;
            paymentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            paymentPanel.Controls.Add(paymentTitle, 0, 0);
            paymentPanel.Controls.Add(paymentBox, 0, 1);
            paymentPanel.Controls.Add(transactionLabel, 0, 2);
            paymentPanel.Controls.Add(transactionText, 0, 3);
            paymentPanel.Controls.Add(paymentInfo, 0, 4);
            paymentPanel.Dock = DockStyle.Fill;
            paymentPanel.Location = new Point(627, 105);
            paymentPanel.Name = "paymentPanel";
            paymentPanel.Padding = new Padding(18);
            paymentPanel.RowCount = 9;
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            paymentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            paymentPanel.Size = new Size(466, 592);
            paymentPanel.TabIndex = 1;
            // 
            // paymentTitle
            // 
            paymentTitle.Dock = DockStyle.Fill;
            paymentTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            paymentTitle.ForeColor = Color.FromArgb(21, 34, 56);
            paymentTitle.Location = new Point(21, 18);
            paymentTitle.Name = "paymentTitle";
            paymentTitle.Size = new Size(422, 36);
            paymentTitle.TabIndex = 0;
            paymentTitle.Text = "Payment & Discount";
            // 
            // paymentBox
            // 
            paymentBox.Dock = DockStyle.Fill;
            paymentBox.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentBox.Items.AddRange(new object[] { "Pay at Hotel", "Credit Card", "Debit Card", "bKash", "Nagad", "Rocket", "Mobile Banking" });
            paymentBox.Location = new Point(21, 57);
            paymentBox.Name = "paymentBox";
            paymentBox.Size = new Size(422, 25);
            paymentBox.TabIndex = 1;
            paymentBox.SelectedIndexChanged += PaymentChanged;
            // 
            // transactionLabel
            // 
            transactionLabel.Dock = DockStyle.Fill;
            transactionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            transactionLabel.Location = new Point(21, 96);
            transactionLabel.Name = "transactionLabel";
            transactionLabel.Size = new Size(422, 28);
            transactionLabel.TabIndex = 2;
            // 
            // transactionText
            // 
            transactionText.Dock = DockStyle.Fill;
            transactionText.Location = new Point(18, 127);
            transactionText.Margin = new Padding(0, 3, 0, 3);
            transactionText.Name = "transactionText";
            transactionText.PlaceholderText = "Enter transaction ID";
            transactionText.Size = new Size(428, 24);
            transactionText.TabIndex = 3;
            // 
            // paymentInfo
            // 
            paymentInfo.AutoEllipsis = true;
            paymentInfo.Dock = DockStyle.Fill;
            paymentInfo.ForeColor = Color.DimGray;
            paymentInfo.Location = new Point(21, 162);
            paymentInfo.Name = "paymentInfo";
            paymentInfo.Size = new Size(422, 58);
            paymentInfo.TabIndex = 4;
            paymentInfo.Text = "Pay at Hotel confirms immediately. Online payments require a transaction ID and stay pending until the hotel validates it.";
            // 
            // invoicePanel
            // 
            invoicePanel.Dock = DockStyle.Bottom;
            invoicePanel.Location = new Point(0, 720);
            invoicePanel.Name = "invoicePanel";
            invoicePanel.Size = new Size(1120, 0);
            invoicePanel.TabIndex = 1;
            invoicePanel.Visible = false;
            // 
            // CheckoutForm
            // 
            BackColor = Color.FromArgb(247, 249, 251);
            ClientSize = new Size(1120, 720);
            Controls.Add(root);
            Controls.Add(invoicePanel);
            Font = new Font("Segoe UI", 9.5F);
            MinimumSize = new Size(980, 650);
            Name = "CheckoutForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Resortify - Checkout";
            root.ResumeLayout(false);
            summaryPanel.ResumeLayout(false);
            paymentPanel.ResumeLayout(false);
            paymentPanel.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel CreateCouponPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 3;
            panel.RowCount = 2;

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            couponText = new TextBox();
            couponText.Dock = DockStyle.Fill;
            couponText.Margin = new Padding(0, 4, 6, 4);
            couponText.PlaceholderText = "Coupon code";

            applyCouponButton = UIHelper.MakeButton(
                "Apply", UIHelper.CustomerColor, 80, 30);
            applyCouponButton.Dock = DockStyle.Fill;
            applyCouponButton.Margin = new Padding(0, 4, 6, 4);
            applyCouponButton.Click += ApplyCoupon_Click;

            removeCouponButton = UIHelper.MakeButton(
                "Remove", Color.FromArgb(95, 95, 95), 70, 30);
            removeCouponButton.Dock = DockStyle.Fill;
            removeCouponButton.Margin = new Padding(0, 4, 0, 4);
            removeCouponButton.Click += RemoveCoupon_Click;

            couponStatusLabel = new Label();
            couponStatusLabel.Text = "Have a coupon? Try WELCOME10, RESORT15 or GETAWAY20.";
            couponStatusLabel.Dock = DockStyle.Fill;
            couponStatusLabel.ForeColor = Color.DimGray;
            couponStatusLabel.AutoEllipsis = true;

            panel.Controls.Add(couponText, 0, 0);
            panel.Controls.Add(applyCouponButton, 1, 0);
            panel.Controls.Add(removeCouponButton, 2, 0);
            panel.Controls.Add(couponStatusLabel, 0, 1);
            panel.SetColumnSpan(couponStatusLabel, 3);

            return panel;
        }

        private TableLayoutPanel CreateTotalsPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 4;
            panel.Padding = new Padding(0, 4, 0, 4);

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

            panel.Controls.Add(MakeAmountText("Room subtotal"), 0, 0);
            roomTotalLabel = CreateAmountLabel(false);
            panel.Controls.Add(roomTotalLabel, 1, 0);

            panel.Controls.Add(MakeAmountText("Services / extras"), 0, 1);
            serviceTotalLabel = CreateAmountLabel(false);
            panel.Controls.Add(serviceTotalLabel, 1, 1);

            panel.Controls.Add(MakeAmountText("Total discount"), 0, 2);
            discountTotalLabel = CreateAmountLabel(false);
            panel.Controls.Add(discountTotalLabel, 1, 2);

            Label grandText = MakeAmountText("Grand total");
            grandText.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            panel.Controls.Add(grandText, 0, 3);

            grandTotalLabel = CreateAmountLabel(true);
            panel.Controls.Add(grandTotalLabel, 1, 3);

            return panel;
        }

        private Label MakeAmountText(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private Label CreateAmountLabel(bool strong)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Font = new Font(
                "Segoe UI",
                strong ? 14F : 10F,
                FontStyle.Bold);
            label.ForeColor = UIHelper.CustomerColor;
            return label;
        }

        private void LoadCustomerDetails()
        {
            string sql = @"
                SELECT FullName, Email, Phone, Address
                FROM Users
                WHERE UserId=@Id";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", Session.UserId));

            if (table.Rows.Count == 0)
            {
                customerLabel.Text = "Customer details not found.";
                return;
            }

            DataRow row = table.Rows[0];
            string name = row["FullName"].ToString();
            string email = row["Email"].ToString();
            string phone = row["Phone"] == DBNull.Value ? "Not added" : row["Phone"].ToString();
            string address = row["Address"] == DBNull.Value ? "Not added" : row["Address"].ToString();

            customerLabel.Text =
                "Booking for: " + name +
                "\r\nEmail: " + email +
                "  •  Phone: " + phone +
                "  •  Address: " + address;
        }

        private void LoadCart()
        {
            string sql = @"
                SELECT
                    c.CartId,
                    c.RoomId,
                    h.HotelName,
                    h.City,
                    r.RoomType,
                    c.CheckInDate,
                    c.CheckOutDate,
                    c.Quantity,
                    c.Guests,
                    c.DiscountAmount,
                    DATEDIFF(DAY, c.CheckInDate, c.CheckOutDate) AS Nights,
                    r.PricePerNight,
                    ISNULL(
                        (SELECT SUM(cs.UnitPrice * cs.Quantity)
                         FROM CartServices cs
                         WHERE cs.CartId=c.CartId), 0) AS ServiceAmount,
                    ISNULL(
                        (SELECT STRING_AGG(
                            CONCAT(sc.ServiceName,
                                CASE WHEN sc.IsFree=1 THEN ' (Free)'
                                ELSE CONCAT(' (+$', FORMAT(cs.UnitPrice,'N2'), ')') END),
                            ', ')
                         FROM CartServices cs
                         JOIN ServiceCatalog sc ON sc.ServiceId=cs.ServiceId
                         WHERE cs.CartId=c.CartId), 'None') AS Services
                FROM Cart c
                JOIN Rooms r ON r.RoomId=c.RoomId
                JOIN Hotels h ON h.HotelId=r.HotelId
                WHERE c.CustomerId=@Id
                ORDER BY c.AddedDate";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", Session.UserId));

            cartLines.Clear();
            summaryList.Items.Clear();

            foreach (DataRow row in table.Rows)
            {
                CartLine line = new CartLine();
                line.CartId = Convert.ToInt32(row["CartId"]);
                line.RoomId = Convert.ToInt32(row["RoomId"]);
                line.HotelName = row["HotelName"].ToString();
                line.City = row["City"].ToString();
                line.RoomType = row["RoomType"].ToString();
                line.CheckIn = Convert.ToDateTime(row["CheckInDate"]);
                line.CheckOut = Convert.ToDateTime(row["CheckOutDate"]);
                line.Nights = Convert.ToInt32(row["Nights"]);
                line.Quantity = Convert.ToInt32(row["Quantity"]);
                line.Guests = Convert.ToInt32(row["Guests"]);
                line.UnitPrice = Convert.ToDecimal(row["PricePerNight"]);
                line.ServiceAmount = Convert.ToDecimal(row["ServiceAmount"]);
                line.DiscountAmount = Convert.ToDecimal(row["DiscountAmount"]);
                line.RoomSubtotal = line.UnitPrice * line.Nights * line.Quantity;
                line.Services = row["Services"].ToString();

                cartLines.Add(line);

                string text =
                    line.HotelName + " • " + line.City + "\n" +
                    line.RoomType + " • " +
                    line.CheckIn.ToString("dd MMM yyyy") + " → " +
                    line.CheckOut.ToString("dd MMM yyyy") + " • " +
                    line.Nights + " night(s) • " +
                    line.Quantity + " room(s) • " +
                    line.Guests + " guest(s)\n" +
                    "Room: $" + line.RoomSubtotal.ToString("N2") +
                    " • Services: $" + line.ServiceAmount.ToString("N2") +
                    " • Offer: -$" + line.DiscountAmount.ToString("N2") + "\n" +
                    "Services: " + line.Services;

                summaryList.Items.Add(text);
            }

            LoadAppliedCoupon();
            RecalculateTotals();

            confirmButton.Enabled = cartLines.Count > 0;

            if (cartLines.Count == 0)
            {
                ShowError("Your cart is empty. Return to the dashboard and select a room.");
            }
        }

        private void LoadAppliedCoupon()
        {
            appliedCouponCode = "";
            couponDiscount = 0;

            if (cartLines.Count == 0)
            {
                couponStatusLabel.Text = "Enter a coupon code if you have one.";
                return;
            }

            DataTable table = DbHelper.GetDataTable(
                "SELECT CouponCode, DiscountAmount FROM CartCoupons WHERE CustomerId=@Id",
                new SqlParameter("@Id", Session.UserId));

            if (table.Rows.Count > 0)
            {
                appliedCouponCode = table.Rows[0]["CouponCode"].ToString();
                couponDiscount = Convert.ToDecimal(table.Rows[0]["DiscountAmount"]);
                couponText.Text = appliedCouponCode;

                couponStatusLabel.Text = "Coupon " +
                    appliedCouponCode + " applied: -$" +
                    couponDiscount.ToString("N2");
                couponStatusLabel.ForeColor = UIHelper.CustomerColor;
            }
            else
            {
                couponStatusLabel.Text =
                    "Have a coupon? Try WELCOME10, RESORT15 or GETAWAY20.";
            }
        }

        private decimal GetRoomTotal()
        {
            decimal total = 0;
            foreach (CartLine line in cartLines)
            {
                total += line.RoomSubtotal;
            }
            return total;
        }

        private decimal GetServiceTotal()
        {
            decimal total = 0;
            foreach (CartLine line in cartLines)
            {
                total += line.ServiceAmount;
            }
            return total;
        }

        private decimal GetOfferDiscount()
        {
            decimal total = 0;
            foreach (CartLine line in cartLines)
            {
                total += line.DiscountAmount;
            }
            return total;
        }

        private decimal GetBaseTotal()
        {
            decimal roomTotal = GetRoomTotal();
            decimal serviceTotal = GetServiceTotal();
            decimal offerDiscount = GetOfferDiscount();

            decimal total = roomTotal + serviceTotal - offerDiscount;
            if (total < 0)
            {
                total = 0;
            }

            return total;
        }

        private void RecalculateTotals()
        {
            decimal roomTotal = GetRoomTotal();
            decimal serviceTotal = GetServiceTotal();
            decimal offerDiscount = GetOfferDiscount();
            decimal baseTotal = GetBaseTotal();

            if (couponDiscount > baseTotal)
            {
                couponDiscount = baseTotal;
            }

            decimal totalDiscount = offerDiscount + couponDiscount;
            decimal grandTotal = baseTotal - couponDiscount;

            if (grandTotal < 0)
            {
                grandTotal = 0;
            }

            roomTotalLabel.Text = "$" + roomTotal.ToString("N2");
            serviceTotalLabel.Text = "$" + serviceTotal.ToString("N2");

            if (totalDiscount > 0)
            {
                discountTotalLabel.Text = "-$" + totalDiscount.ToString("N2");
            }
            else
            {
                discountTotalLabel.Text = "$0.00";
            }

            grandTotalLabel.Text = "$" + grandTotal.ToString("N2");
        }

        private void ApplyCoupon_Click(object sender, EventArgs e)
        {
            HideError();

            if (cartLines.Count == 0)
            {
                ShowError("Your cart is empty.");
                return;
            }

            string code = couponText.Text.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Enter a coupon code.");
                return;
            }

            decimal baseTotal = GetBaseTotal();

            string sql = @"
                SELECT TOP 1 CouponId, Code, DiscountPercent,
                       MaxDiscountAmount, MinimumBookingAmount,
                       ValidFrom, ValidTo, MaxUses, UsedCount, Active
                FROM Coupons
                WHERE UPPER(Code)=@Code";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Code", code));

            if (table.Rows.Count == 0)
            {
                RemoveCoupon(false);
                ShowError("Invalid coupon code.");
                return;
            }

            DataRow row = table.Rows[0];
            bool active = Convert.ToBoolean(row["Active"]);
            DateTime validFrom = Convert.ToDateTime(row["ValidFrom"]);
            DateTime validTo = Convert.ToDateTime(row["ValidTo"]);

            if (!active || DateTime.Today < validFrom.Date || DateTime.Today > validTo.Date)
            {
                ShowError("This coupon is not currently valid.");
                return;
            }

            if (row["MaxUses"] != DBNull.Value)
            {
                int maxUses = Convert.ToInt32(row["MaxUses"]);
                int usedCount = Convert.ToInt32(row["UsedCount"]);

                if (usedCount >= maxUses)
                {
                    ShowError("This coupon has reached its usage limit.");
                    return;
                }
            }

            decimal minimum = Convert.ToDecimal(row["MinimumBookingAmount"]);
            if (baseTotal < minimum)
            {
                ShowError("This coupon requires a minimum booking of $" +
                    minimum.ToString("N2") + ".");
                return;
            }

            decimal percent = Convert.ToDecimal(row["DiscountPercent"]);
            decimal discount = Math.Round(
                baseTotal * percent / 100m,
                2);

            if (row["MaxDiscountAmount"] != DBNull.Value)
            {
                decimal maxDiscount = Convert.ToDecimal(row["MaxDiscountAmount"]);
                if (discount > maxDiscount)
                {
                    discount = maxDiscount;
                }
            }

            if (discount > baseTotal)
            {
                discount = baseTotal;
            }

            string saveSql = @"
                MERGE CartCoupons AS target
                USING (SELECT @CustomerId AS CustomerId) AS source
                ON target.CustomerId=source.CustomerId
                WHEN MATCHED THEN
                    UPDATE SET CouponId=@CouponId,
                               CouponCode=@Code,
                               DiscountAmount=@Discount,
                               AppliedAt=GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (CustomerId, CouponId, CouponCode, DiscountAmount)
                    VALUES (@CustomerId, @CouponId, @Code, @Discount);";

            DbHelper.ExecuteNonQuery(
                saveSql,
                new SqlParameter("@CustomerId", Session.UserId),
                new SqlParameter("@CouponId", Convert.ToInt32(row["CouponId"])),
                new SqlParameter("@Code", row["Code"]),
                new SqlParameter("@Discount", discount));

            appliedCouponCode = row["Code"].ToString();
            couponDiscount = discount;

            couponStatusLabel.Text = "Coupon " +
                appliedCouponCode + " applied: -$" +
                discount.ToString("N2");
            couponStatusLabel.ForeColor = UIHelper.CustomerColor;

            RecalculateTotals();
        }

        private void RemoveCoupon_Click(object sender, EventArgs e)
        {
            RemoveCoupon(true);
        }

        private void RemoveCoupon(bool showMessage)
        {
            DbHelper.ExecuteNonQuery(
                "DELETE FROM CartCoupons WHERE CustomerId=@Id",
                new SqlParameter("@Id", Session.UserId));

            appliedCouponCode = "";
            couponDiscount = 0;
            couponText.Clear();
            couponStatusLabel.Text = "No coupon applied.";
            couponStatusLabel.ForeColor = Color.DimGray;

            RecalculateTotals();

            if (showMessage)
            {
                MessageBox.Show(
                    "Coupon removed.",
                    "Coupon",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void PaymentChanged(object sender, EventArgs e)
        {
            UpdatePaymentFields();
        }

        private void UpdatePaymentFields()
        {
            bool online = false;

            if (paymentBox.SelectedItem != null)
            {
                online = paymentBox.SelectedItem.ToString() != "Pay at Hotel";
            }

            if (online)
            {
                transactionLabel.Text = "Transaction ID *  (required for online payment)";
                transactionLabel.ForeColor = Color.Firebrick;
                transactionText.Enabled = true;
                transactionText.Visible = true;
            }
            else
            {
                transactionLabel.Text = "Transaction ID  (not required for Pay at Hotel)";
                transactionLabel.ForeColor = Color.FromArgb(70, 70, 70);
                transactionText.Enabled = false;
                transactionText.Visible = false;
                transactionText.Clear();
            }
        }

        private void ConfirmBooking_Click(object sender, EventArgs e)
        {
            HideError();

            if (cartLines.Count == 0)
            {
                ShowError("Your cart is empty.");
                return;
            }

            if (paymentBox.SelectedItem == null)
            {
                ShowError("Choose a payment option to continue.");
                return;
            }

            string method = paymentBox.SelectedItem.ToString();
            bool online = method != "Pay at Hotel";
            string transactionId = transactionText.Text.Trim();

            if (online && string.IsNullOrWhiteSpace(transactionId))
            {
                ShowError("Please enter the transaction ID for your online payment.");
                transactionText.Focus();
                return;
            }

            decimal roomTotal = GetRoomTotal();
            decimal serviceTotal = GetServiceTotal();
            decimal offerDiscount = GetOfferDiscount();
            decimal baseTotal = GetBaseTotal();

            if (couponDiscount > baseTotal)
            {
                couponDiscount = baseTotal;
            }

            decimal totalDiscount = offerDiscount + couponDiscount;
            decimal grandTotal = baseTotal - couponDiscount;

            if (grandTotal < 0)
            {
                grandTotal = 0;
            }

            string couponName = "None";
            if (!string.IsNullOrWhiteSpace(appliedCouponCode))
            {
                couponName = appliedCouponCode;
            }

            string confirmationText;

            if (online)
            {
                confirmationText =
                    "Please review your booking.\n\n" +
                    "Payment method: " + method + "\n" +
                    "Transaction ID: " + transactionId + "\n" +
                    "Coupon: " + couponName + "\n" +
                    "Discount: -$" + totalDiscount.ToString("N2") + "\n" +
                    "Grand total: $" + grandTotal.ToString("N2") + "\n\n" +
                    "Your booking will stay pending until the hotel validates the transaction ID.\n\n" +
                    "Submit this booking request?";
            }
            else
            {
                confirmationText =
                    "Please review your booking.\n\n" +
                    "Payment method: Pay at Hotel\n" +
                    "Coupon: " + couponName + "\n" +
                    "Discount: -$" + totalDiscount.ToString("N2") + "\n" +
                    "Grand total: $" + grandTotal.ToString("N2") + "\n\n" +
                    "Pay at Hotel bookings are confirmed immediately.\n\n" +
                    "Confirm this booking?";
            }

            DialogResult answer = MessageBox.Show(
                confirmationText,
                online ? "Submit Booking Request" : "Confirm Booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            confirmButton.Enabled = false;

            try
            {
                int bookingId = SaveBooking(
                    roomTotal,
                    serviceTotal,
                    totalDiscount,
                    grandTotal,
                    method,
                    transactionId,
                    online);

                ShowInvoice(
                    bookingId,
                    roomTotal,
                    serviceTotal,
                    totalDiscount,
                    grandTotal,
                    method,
                    transactionId,
                    online,
                    appliedCouponCode,
                    couponDiscount);
            }
            catch (Exception ex)
            {
                confirmButton.Enabled = true;
                ShowError(ex.Message);
            }
        }

        private int SaveBooking(
            decimal roomTotal,
            decimal serviceTotal,
            decimal totalDiscount,
            decimal grandTotal,
            string paymentMethod,
            string transactionId,
            bool online)
        {
            int bookingId = 0;

            DbHelper.RunTransaction((connection, transaction) =>
            {
                // 1. Check room availability again before saving.
                foreach (CartLine line in cartLines)
                {
                    string checkSql = @"
                        SELECT r.TotalRooms - ISNULL(
                            (
                                SELECT SUM(bi.Quantity)
                                FROM BookingItems bi
                                JOIN Bookings b ON b.BookingId=bi.BookingId
                                WHERE bi.RoomId=r.RoomId
                                AND b.Status IN ('Pending','Approved','Confirmed','Completed')
                                AND bi.CheckInDate < c.CheckOutDate
                                AND bi.CheckOutDate > c.CheckInDate
                            ), 0)
                        FROM Cart c
                        JOIN Rooms r WITH (UPDLOCK, HOLDLOCK) ON r.RoomId=c.RoomId
                        WHERE c.CartId=@CartId
                        AND c.CustomerId=@CustomerId";

                    using (SqlCommand command = new SqlCommand(
                        checkSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@CartId", line.CartId);
                        command.Parameters.AddWithValue("@CustomerId", Session.UserId);

                        object result = command.ExecuteScalar();
                        int available = Convert.ToInt32(result ?? 0);

                        if (available < line.Quantity)
                        {
                            throw new InvalidOperationException(
                                line.RoomType +
                                " is no longer available for the selected dates. Only " +
                                available + " room(s) remain.");
                        }
                    }
                }

                // 2. Check the coupon again because another customer may have used it.
                if (!string.IsNullOrWhiteSpace(appliedCouponCode))
                {
                    CheckCouponInsideTransaction(
                        connection,
                        transaction,
                        GetBaseTotal());
                }

                string status;
                if (online)
                {
                    status = "Pending";
                }
                else
                {
                    status = "Confirmed";
                }

                // 3. Create the booking.
                string bookingSql = @"
                    INSERT INTO Bookings
                    (
                        CustomerId,
                        TotalAmount,
                        RoomAmount,
                        ServiceAmount,
                        DiscountAmount,
                        CouponCode,
                        CouponDiscountAmount,
                        PaymentMethod,
                        TransactionId,
                        Status,
                        CustomerNotificationShown
                    )
                    OUTPUT INSERTED.BookingId
                    VALUES
                    (
                        @CustomerId,
                        @TotalAmount,
                        @RoomAmount,
                        @ServiceAmount,
                        @DiscountAmount,
                        @CouponCode,
                        @CouponDiscount,
                        @PaymentMethod,
                        @TransactionId,
                        @Status,
                        @NotificationShown
                    )";

                using (SqlCommand command = new SqlCommand(
                    bookingSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@CustomerId", Session.UserId);
                    command.Parameters.AddWithValue("@TotalAmount", grandTotal);
                    command.Parameters.AddWithValue("@RoomAmount", roomTotal);
                    command.Parameters.AddWithValue("@ServiceAmount", serviceTotal);
                    command.Parameters.AddWithValue("@DiscountAmount", totalDiscount);

                    if (string.IsNullOrWhiteSpace(appliedCouponCode))
                    {
                        command.Parameters.AddWithValue("@CouponCode", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@CouponCode", appliedCouponCode);
                    }

                    command.Parameters.AddWithValue("@CouponDiscount", couponDiscount);
                    command.Parameters.AddWithValue("@PaymentMethod", paymentMethod);

                    if (online)
                    {
                        command.Parameters.AddWithValue("@TransactionId", transactionId);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@TransactionId", DBNull.Value);
                    }

                    command.Parameters.AddWithValue("@Status", status);

                    if (online)
                    {
                        command.Parameters.AddWithValue("@NotificationShown", 1);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@NotificationShown", 0);
                    }

                    bookingId = Convert.ToInt32(command.ExecuteScalar());
                }

                // 4. Add every cart room to BookingItems.
                foreach (CartLine line in cartLines)
                {
                    string itemSql = @"
                        INSERT INTO BookingItems
                        (
                            BookingId,
                            RoomId,
                            CheckInDate,
                            CheckOutDate,
                            Nights,
                            Quantity,
                            Guests,
                            UnitPrice,
                            Subtotal
                        )
                        OUTPUT INSERTED.BookingItemId
                        VALUES
                        (
                            @BookingId,
                            @RoomId,
                            @CheckIn,
                            @CheckOut,
                            @Nights,
                            @Quantity,
                            @Guests,
                            @UnitPrice,
                            @Subtotal
                        )";

                    int bookingItemId;

                    using (SqlCommand command = new SqlCommand(
                        itemSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@BookingId", bookingId);
                        command.Parameters.AddWithValue("@RoomId", line.RoomId);
                        command.Parameters.AddWithValue("@CheckIn", line.CheckIn);
                        command.Parameters.AddWithValue("@CheckOut", line.CheckOut);
                        command.Parameters.AddWithValue("@Nights", line.Nights);
                        command.Parameters.AddWithValue("@Quantity", line.Quantity);
                        command.Parameters.AddWithValue("@Guests", line.Guests);
                        command.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
                        command.Parameters.AddWithValue("@Subtotal", line.RoomSubtotal);

                        bookingItemId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // 5. Copy selected services from CartServices to the booking.
                    string serviceSql = @"
                        INSERT INTO BookingItemServices
                        (BookingItemId, ServiceId, Quantity, UnitPrice)
                        SELECT
                            @BookingItemId,
                            ServiceId,
                            Quantity,
                            UnitPrice
                        FROM CartServices
                        WHERE CartId=@CartId";

                    using (SqlCommand command = new SqlCommand(
                        serviceSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@BookingItemId", bookingItemId);
                        command.Parameters.AddWithValue("@CartId", line.CartId);
                        command.ExecuteNonQuery();
                    }
                }

                // 6. Increase coupon usage.
                if (!string.IsNullOrWhiteSpace(appliedCouponCode))
                {
                    string increaseCouponSql =
                        "UPDATE Coupons SET UsedCount=UsedCount+1 WHERE Code=@Code";

                    using (SqlCommand command = new SqlCommand(
                        increaseCouponSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Code", appliedCouponCode);
                        command.ExecuteNonQuery();
                    }
                }

                // 7. Remove the coupon and cart after successful booking.
                using (SqlCommand command = new SqlCommand(
                    "DELETE FROM CartCoupons WHERE CustomerId=@CustomerId",
                    connection,
                    transaction))
                {
                    command.Parameters.AddWithValue("@CustomerId", Session.UserId);
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand(
                    "DELETE FROM Cart WHERE CustomerId=@CustomerId",
                    connection,
                    transaction))
                {
                    command.Parameters.AddWithValue("@CustomerId", Session.UserId);
                    command.ExecuteNonQuery();
                }
            });

            return bookingId;
        }

        private void CheckCouponInsideTransaction(
            SqlConnection connection,
            SqlTransaction transaction,
            decimal baseTotal)
        {
            string sql = @"
                SELECT DiscountPercent, MaxDiscountAmount,
                       MinimumBookingAmount, MaxUses, UsedCount,
                       Active, ValidFrom, ValidTo
                FROM Coupons WITH (UPDLOCK, HOLDLOCK)
                WHERE Code=@Code";

            using (SqlCommand command = new SqlCommand(
                sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Code", appliedCouponCode);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException(
                            "The coupon is no longer available.");
                    }

                    bool active = reader.GetBoolean(reader.GetOrdinal("Active"));
                    DateTime validFrom = reader.GetDateTime(
                        reader.GetOrdinal("ValidFrom"));
                    DateTime validTo = reader.GetDateTime(
                        reader.GetOrdinal("ValidTo"));

                    if (!active || DateTime.Today < validFrom.Date ||
                        DateTime.Today > validTo.Date)
                    {
                        throw new InvalidOperationException(
                            "The coupon is no longer valid.");
                    }

                    int maxUses = 0;
                    if (!reader.IsDBNull(reader.GetOrdinal("MaxUses")))
                    {
                        maxUses = reader.GetInt32(reader.GetOrdinal("MaxUses"));
                    }

                    int usedCount = reader.GetInt32(
                        reader.GetOrdinal("UsedCount"));

                    if (maxUses > 0 && usedCount >= maxUses)
                    {
                        throw new InvalidOperationException(
                            "The coupon usage limit has been reached.");
                    }

                    decimal minimum = reader.GetDecimal(
                        reader.GetOrdinal("MinimumBookingAmount"));

                    if (baseTotal < minimum)
                    {
                        throw new InvalidOperationException(
                            "The coupon requires a minimum booking of $" +
                            minimum.ToString("N2") + ".");
                    }
                }
            }
        }

        private void ShowInvoice(
            int bookingId,
            decimal roomTotal,
            decimal serviceTotal,
            decimal discount,
            decimal grandTotal,
            string paymentMethod,
            string transactionId,
            bool pendingApproval,
            string couponCode,
            decimal couponDiscountAmount)
        {
            invoicePanel.Controls.Clear();
            invoicePanel.Visible = true;
            invoicePanel.Height = 260;

            TableLayoutPanel box = new TableLayoutPanel();
            box.Dock = DockStyle.Fill;
            box.ColumnCount = 2;
            box.RowCount = 7;
            box.Padding = new Padding(16);
            box.BackColor = Color.White;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            Label title = new Label();
            title.Text = "BOOKING REQUEST SUBMITTED  •  #" + bookingId;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            title.ForeColor = UIHelper.NavyHeader;
            box.Controls.Add(title, 0, 0);
            box.SetColumnSpan(title, 2);

            string information;

            if (pendingApproval)
            {
                information =
                    "Guest: " + Session.FullName + "\n" +
                    "Payment option: " + paymentMethod + "\n" +
                    "Transaction ID: " + transactionId + "\n" +
                    "Coupon: " + (string.IsNullOrWhiteSpace(couponCode) ? "None" : couponCode) + "\n" +
                    "Status: Pending transaction validation\n\n" +
                    "Your cart has been cleared. After the hotel validates your transaction ID, " +
                    "the booking will be confirmed and the dashboard will notify you.\n" +
                    "Confirmation email will be sent to your registered email after validation.";
            }
            else
            {
                information =
                    "Guest: " + Session.FullName + "\n" +
                    "Payment option: " + paymentMethod + "\n" +
                    "Coupon: " + (string.IsNullOrWhiteSpace(couponCode) ? "None" : couponCode) + "\n" +
                    "Status: Confirmed\n\n" +
                    "Your Pay at Hotel booking is confirmed. Your cart has been cleared.";
            }

            Label details = new Label();
            details.Text = information;
            details.Dock = DockStyle.Fill;
            box.Controls.Add(details, 0, 1);
            box.SetColumnSpan(details, 2);

            AddInvoiceRow(box, "Room amount", "$" + roomTotal.ToString("N2"), 2);
            AddInvoiceRow(box, "Services", "$" + serviceTotal.ToString("N2"), 3);
            AddInvoiceRow(box, "Discount", "-$" + discount.ToString("N2"), 4);
            AddInvoiceRow(box, "Grand total", "$" + grandTotal.ToString("N2"), 5);

            Button doneButton = UIHelper.MakeButton(
                "View My Bookings",
                UIHelper.CustomerColor,
                200,
                36);
            doneButton.Anchor = AnchorStyles.None;
            doneButton.Click += ViewBookings_Click;
            box.Controls.Add(doneButton, 0, 6);
            box.SetColumnSpan(doneButton, 2);

            invoicePanel.Controls.Add(box);
        }

        private void AddInvoiceRow(
            TableLayoutPanel panel,
            string name,
            string value,
            int row)
        {
            Label nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.Dock = DockStyle.Fill;
            panel.Controls.Add(nameLabel, 0, row);

            Label valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.TextAlign = ContentAlignment.MiddleRight;
            panel.Controls.Add(valueLabel, 1, row);
        }

        private void ViewBookings_Click(object sender, EventArgs e)
        {
            BookingHistoryForm history = new BookingHistoryForm();
            history.Show();
            Close();
        }

        private void Back_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            errorLabel.Text = message;
            errorLabel.Visible = true;
        }

        private void HideError()
        {
            errorLabel.Text = "";
            errorLabel.Visible = false;
        }
    }
}
