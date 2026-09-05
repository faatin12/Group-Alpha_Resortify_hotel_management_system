using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    // Shows all rooms currently saved by the customer.
    public partial class BookingCartForm : Form
    {
        private FlowLayoutPanel cartPanel;
        private DateTimePicker dtCheckIn;
        private DateTimePicker dtCheckOut;
        private NumericUpDown numRooms;
        private NumericUpDown numGuests;

        private Label lblError;
        private Label lblTotal;
        private Label lblServices;
        private Label lblEmpty;
        private Label lblCoupon;
        private Label lblCustomerDetails;
        private Label lblPaymentMethods;

        private int selectedCartId = 0;

        public BookingCartForm()
        {
            InitializeComponent();
            LoadCustomerDetails();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - My Cart";
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;

            Controls.Add(UIHelper.BuildHeader(
                "My Cart",
                UIHelper.CustomerColor,
                Back_Click,
                null));

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(24, 18, 24, 20);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            // Customer information and payment methods.
            Panel customerBox = new Panel();
            customerBox.Dock = DockStyle.Fill;
            customerBox.BackColor = Color.White;
            customerBox.BorderStyle = BorderStyle.FixedSingle;
            customerBox.Padding = new Padding(12);

            lblCustomerDetails = new Label();
            lblCustomerDetails.Location = new Point(12, 8);
            lblCustomerDetails.Size = new Size(620, 82);
            lblCustomerDetails.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCustomerDetails.ForeColor = UIHelper.NavyHeader;
            lblCustomerDetails.Text = "Customer: Loading...";

            lblPaymentMethods = new Label();
            lblPaymentMethods.Location = new Point(650, 8);
            lblPaymentMethods.Size = new Size(420, 82);
            lblPaymentMethods.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblPaymentMethods.ForeColor = UIHelper.CustomerColor;
            lblPaymentMethods.Text = "Payment methods available at Checkout:\r\nPay at Hotel • Credit Card • Debit Card\r\nbKash • Nagad • Rocket • Mobile Banking";

            customerBox.Controls.Add(lblCustomerDetails);
            customerBox.Controls.Add(lblPaymentMethods);
            root.Controls.Add(customerBox, 0, 0);

            Panel cartBox = new Panel();
            cartBox.Dock = DockStyle.Fill;
            cartBox.BackColor = Color.White;
            cartBox.BorderStyle = BorderStyle.FixedSingle;
            cartBox.Padding = new Padding(12);

            Label cartTitle = new Label();
            cartTitle.Text = "Your Booking Cart";
            cartTitle.Dock = DockStyle.Top;
            cartTitle.Height = 34;
            cartTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            cartTitle.ForeColor = UIHelper.NavyHeader;
            cartBox.Controls.Add(cartTitle);

            cartPanel = new FlowLayoutPanel();
            cartPanel.Dock = DockStyle.Fill;
            cartPanel.AutoScroll = true;
            cartPanel.WrapContents = false;
            cartPanel.FlowDirection = FlowDirection.TopDown;
            cartPanel.BackColor = Color.FromArgb(250, 251, 253);
            cartPanel.Padding = new Padding(8);
            cartBox.Controls.Add(cartPanel);

            lblEmpty = new Label();
            lblEmpty.Text = "Your cart is empty. Return to the dashboard and choose a room.";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.ForeColor = Color.DimGray;
            lblEmpty.Font = new Font("Segoe UI", 11F);
            lblEmpty.Visible = false;
            cartBox.Controls.Add(lblEmpty);

            root.Controls.Add(cartBox, 0, 1);

            // Editor for the selected cart item.
            TableLayoutPanel editor = new TableLayoutPanel();
            editor.Dock = DockStyle.Fill;
            editor.BackColor = Color.White;
            editor.Padding = new Padding(14);
            editor.ColumnCount = 4;
            editor.RowCount = 3;
            editor.BorderStyle = BorderStyle.FixedSingle;

            for (int i = 0; i < 4; i++)
            {
                editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            for (int i = 0; i < 3; i++)
            {
                editor.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            }

            editor.Controls.Add(MakeLabel("Check-in"), 0, 0);
            dtCheckIn = new DateTimePicker();
            dtCheckIn.Dock = DockStyle.Fill;
            dtCheckIn.Format = DateTimePickerFormat.Short;
            dtCheckIn.MinDate = DateTime.Today;
            editor.Controls.Add(dtCheckIn, 0, 1);

            editor.Controls.Add(MakeLabel("Check-out"), 1, 0);
            dtCheckOut = new DateTimePicker();
            dtCheckOut.Dock = DockStyle.Fill;
            dtCheckOut.Format = DateTimePickerFormat.Short;
            dtCheckOut.MinDate = DateTime.Today.AddDays(1);
            editor.Controls.Add(dtCheckOut, 1, 1);

            editor.Controls.Add(MakeLabel("Rooms"), 2, 0);
            numRooms = new NumericUpDown();
            numRooms.Dock = DockStyle.Fill;
            numRooms.Minimum = 1;
            numRooms.Maximum = 20;
            editor.Controls.Add(numRooms, 2, 1);

            editor.Controls.Add(MakeLabel("Guests"), 3, 0);
            numGuests = new NumericUpDown();
            numGuests.Dock = DockStyle.Fill;
            numGuests.Minimum = 1;
            numGuests.Maximum = 50;
            editor.Controls.Add(numGuests, 3, 1);

            lblServices = new Label();
            lblServices.Dock = DockStyle.Fill;
            lblServices.AutoEllipsis = true;
            lblServices.ForeColor = Color.DimGray;
            lblServices.Text = "Select a booking above to view its services.";
            editor.Controls.Add(lblServices, 0, 2);
            editor.SetColumnSpan(lblServices, 2);

            lblError = UIHelper.MakeErrorLabel();
            lblError.Dock = DockStyle.Fill;
            editor.Controls.Add(lblError, 2, 2);

            root.Controls.Add(editor, 0, 2);

            // Bottom buttons.
            FlowLayoutPanel bottom = new FlowLayoutPanel();
            bottom.Dock = DockStyle.Fill;
            bottom.FlowDirection = FlowDirection.LeftToRight;
            bottom.WrapContents = false;
            bottom.Padding = new Padding(0, 8, 0, 0);

            Button updateButton = UIHelper.MakeButton(
                "Update Booking", UIHelper.CustomerColor, 135, 38);
            updateButton.Click += Update_Click;

            Button removeButton = UIHelper.MakeButton(
                "Remove", Color.FromArgb(120, 20, 20), 110, 38);
            removeButton.Click += Remove_Click;

            Button clearButton = UIHelper.MakeButton(
                "Clear Cart", Color.FromArgb(95, 95, 95), 110, 38);
            clearButton.Click += Clear_Click;

            Button checkoutButton = UIHelper.MakeButton(
                "Proceed to Checkout", UIHelper.CustomerColor, 210, 38);
            checkoutButton.Click += Checkout_Click;

            lblCoupon = new Label();
            lblCoupon.AutoSize = true;
            lblCoupon.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCoupon.ForeColor = UIHelper.CustomerColor;
            lblCoupon.Padding = new Padding(8, 10, 8, 0);

            lblTotal = new Label();
            lblTotal.AutoSize = true;
            lblTotal.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTotal.ForeColor = UIHelper.NavyHeader;
            lblTotal.Padding = new Padding(14, 8, 20, 0);

            bottom.Controls.Add(updateButton);
            bottom.Controls.Add(removeButton);
            bottom.Controls.Add(clearButton);
            bottom.Controls.Add(checkoutButton);
            bottom.Controls.Add(lblCoupon);
            bottom.Controls.Add(lblTotal);

            root.Controls.Add(bottom, 0, 3);
            Controls.Add(root);
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
                lblCustomerDetails.Text = "Customer details not found.";
                return;
            }

            DataRow row = table.Rows[0];

            string name = row["FullName"].ToString();
            string email = row["Email"].ToString();
            string phone = row["Phone"] == DBNull.Value ? "Not added" : row["Phone"].ToString();
            string address = row["Address"] == DBNull.Value ? "Not added" : row["Address"].ToString();

            lblCustomerDetails.Text =
                "Customer Details\r\n" +
                "Name: " + name +
                "  •  Email: " + email +
                "\r\nPhone: " + phone +
                "  •  Address: " + address;
        }

        private Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(70, 70, 70);
            return label;
        }

        private void LoadData()
        {
            selectedCartId = 0;
            cartPanel.Controls.Clear();
            lblError.Visible = false;

            string sql = @"
                SELECT
                    c.CartId,
                    c.RoomId,
                    h.HotelName,
                    h.City,
                    h.StarRating,
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

            decimal total = 0;

            foreach (DataRow row in table.Rows)
            {
                int nights = Convert.ToInt32(row["Nights"]);
                int quantity = Convert.ToInt32(row["Quantity"]);
                decimal price = Convert.ToDecimal(row["PricePerNight"]);
                decimal roomTotal = price * nights * quantity;
                decimal services = Convert.ToDecimal(row["ServiceAmount"]);
                decimal discount = Convert.ToDecimal(row["DiscountAmount"]);

                decimal grandTotal = roomTotal + services - discount;
                if (grandTotal < 0)
                {
                    grandTotal = 0;
                }

                total += grandTotal;

                Panel card = CreateCartCard(
                    row,
                    roomTotal,
                    services,
                    discount,
                    grandTotal);
                cartPanel.Controls.Add(card);
            }

            // Apply the saved coupon to the cart total.
            string couponSql = @"
                SELECT CouponCode, DiscountAmount
                FROM CartCoupons
                WHERE CustomerId=@Id";

            DataTable coupon = DbHelper.GetDataTable(
                couponSql,
                new SqlParameter("@Id", Session.UserId));

            if (coupon.Rows.Count > 0)
            {
                decimal couponDiscount = Convert.ToDecimal(
                    coupon.Rows[0]["DiscountAmount"]);

                total -= couponDiscount;
                if (total < 0)
                {
                    total = 0;
                }

                lblCoupon.Text = "Coupon " +
                    coupon.Rows[0]["CouponCode"] +
                    ": -$" + couponDiscount.ToString("N2");
            }
            else
            {
                lblCoupon.Text = "";
            }

            lblTotal.Text = "Cart Total: $" + total.ToString("N2");
            lblEmpty.Visible = table.Rows.Count == 0;

            if (table.Rows.Count == 0)
            {
                lblServices.Text = "Select a booking above to view its services.";
            }
        }

        private Panel CreateCartCard(
            DataRow row,
            decimal roomTotal,
            decimal services,
            decimal discount,
            decimal grandTotal)
        {
            int cartId = Convert.ToInt32(row["CartId"]);
            string hotel = row["HotelName"].ToString();
            string city = row["City"].ToString();
            string roomType = row["RoomType"].ToString();
            string rating = "New";

            if (row["StarRating"] != DBNull.Value)
            {
                rating = Convert.ToDecimal(row["StarRating"]).ToString("0.0");
            }

            DateTime checkIn = Convert.ToDateTime(row["CheckInDate"]);
            DateTime checkOut = Convert.ToDateTime(row["CheckOutDate"]);
            int nights = Convert.ToInt32(row["Nights"]);
            int quantity = Convert.ToInt32(row["Quantity"]);
            int guests = Convert.ToInt32(row["Guests"]);
            string servicesText = row["Services"].ToString();

            Panel card = new Panel();
            card.Width = Math.Max(760, cartPanel.ClientSize.Width - 35);
            card.Height = 155;
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(4, 4, 4, 10);
            card.Padding = new Padding(14);
            card.Tag = cartId;

            Label title = new Label();
            title.Text = hotel + "  •  " + city;
            title.Location = new Point(14, 10);
            title.AutoSize = true;
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.ForeColor = UIHelper.NavyHeader;

            Label ratingLabel = new Label();
            ratingLabel.Text = "★ " + rating + "/5.0";
            ratingLabel.Location = new Point(Math.Max(500, card.Width - 170), 12);
            ratingLabel.AutoSize = true;
            ratingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            ratingLabel.ForeColor = UIHelper.CustomerColor;

            Label information = new Label();
            information.Text =
                roomType + "\r\n" +
                checkIn.ToString("dd MMM yyyy") +
                " → " + checkOut.ToString("dd MMM yyyy") +
                "  •  " + nights + " night(s)" +
                "  •  " + quantity + " room(s)" +
                "  •  " + guests + " guest(s)\r\n" +
                "Services: " + servicesText;
            information.Location = new Point(14, 42);
            information.Size = new Size(card.Width - 260, 78);
            information.Font = new Font("Segoe UI", 9F);
            information.ForeColor = Color.FromArgb(65, 65, 65);

            Label amount = new Label();
            amount.Text =
                "Room: $" + roomTotal.ToString("N2") + "\r\n" +
                "Services: $" + services.ToString("N2") + "\r\n" +
                "Discount: -$" + discount.ToString("N2") + "\r\n" +
                "TOTAL: $" + grandTotal.ToString("N2");
            amount.Location = new Point(Math.Max(500, card.Width - 250), 42);
            amount.Size = new Size(225, 85);
            amount.TextAlign = ContentAlignment.MiddleRight;
            amount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            amount.ForeColor = UIHelper.NavyHeader;

            card.Controls.Add(title);
            card.Controls.Add(ratingLabel);
            card.Controls.Add(information);
            card.Controls.Add(amount);

            card.Click += CartCard_Click;
            title.Click += CartCard_Click;
            ratingLabel.Click += CartCard_Click;
            information.Click += CartCard_Click;
            amount.Click += CartCard_Click;

            return card;
        }

        private void CartCard_Click(object sender, EventArgs e)
        {
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }

            Control card = control;
            while (card != null && card.Tag == null)
            {
                card = card.Parent;
            }

            if (card == null || card.Tag == null)
            {
                return;
            }

            SelectCard(card);
        }

        private void SelectCard(Control card)
        {
            selectedCartId = Convert.ToInt32(card.Tag);

            foreach (Control control in cartPanel.Controls)
            {
                Panel panel = control as Panel;
                if (panel != null)
                {
                    if (panel == card)
                    {
                        panel.BackColor = Color.FromArgb(235, 246, 250);
                    }
                    else
                    {
                        panel.BackColor = Color.White;
                    }
                }
            }

            string sql = @"
                SELECT c.CheckInDate, c.CheckOutDate,
                       c.Quantity, c.Guests,
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
                WHERE c.CartId=@Id
                AND c.CustomerId=@CustomerId";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", selectedCartId),
                new SqlParameter("@CustomerId", Session.UserId));

            if (table.Rows.Count == 0)
            {
                return;
            }

            DataRow row = table.Rows[0];
            DateTime checkIn = Convert.ToDateTime(row["CheckInDate"]);
            DateTime checkOut = Convert.ToDateTime(row["CheckOutDate"]);

            if (checkIn < DateTime.Today)
            {
                checkIn = DateTime.Today;
            }

            dtCheckIn.Value = checkIn;

            if (checkOut <= checkIn)
            {
                checkOut = checkIn.AddDays(1);
            }

            dtCheckOut.Value = checkOut;
            numRooms.Value = Math.Max(
                1,
                Math.Min(
                    numRooms.Maximum,
                    Convert.ToDecimal(row["Quantity"])));
            numGuests.Value = Math.Max(
                1,
                Math.Min(
                    numGuests.Maximum,
                    Convert.ToDecimal(row["Guests"])));

            lblServices.Text = "Selected services: " + row["Services"];
            lblError.Visible = false;
        }

        private void UpdateSelected()
        {
            HideError();

            if (selectedCartId == 0)
            {
                ShowError("Select a booking from your cart first.");
                return;
            }

            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date)
            {
                ShowError("Check-out must be after check-in.");
                return;
            }

            string availabilitySql = @"
                SELECT r.TotalRooms - ISNULL(
                    (
                        SELECT SUM(bi.Quantity)
                        FROM BookingItems bi
                        JOIN Bookings b ON b.BookingId=bi.BookingId
                        WHERE bi.RoomId=r.RoomId
                        AND b.Status IN ('Pending','Approved','Confirmed','Completed')
                        AND bi.CheckInDate < @OutDate
                        AND bi.CheckOutDate > @InDate
                    ), 0)
                FROM Rooms r
                JOIN Cart c ON c.RoomId=r.RoomId
                WHERE c.CartId=@CartId";

            object availableResult = DbHelper.ExecuteScalar(
                availabilitySql,
                new SqlParameter("@OutDate", dtCheckOut.Value.Date),
                new SqlParameter("@InDate", dtCheckIn.Value.Date),
                new SqlParameter("@CartId", selectedCartId));

            int available = Convert.ToInt32(availableResult ?? 0);

            if (Convert.ToInt32(numRooms.Value) > available)
            {
                ShowError("Only " + available +
                    " room(s) are available for these dates.");
                return;
            }

            object priceResult = DbHelper.ExecuteScalar(
                "SELECT r.PricePerNight FROM Rooms r " +
                "JOIN Cart c ON c.RoomId=r.RoomId " +
                "WHERE c.CartId=@CartId",
                new SqlParameter("@CartId", selectedCartId));

            decimal price = Convert.ToDecimal(priceResult ?? 0m);
            int nights = (dtCheckOut.Value.Date - dtCheckIn.Value.Date).Days;
            int quantity = Convert.ToInt32(numRooms.Value);
            decimal roomTotal = price * nights * quantity;

            object offerResult = DbHelper.ExecuteScalar(
                @"SELECT ISNULL(MAX(o.DiscountPercent),0)
                  FROM Offers o
                  JOIN Cart c ON c.RoomId=o.RoomId
                  WHERE c.CartId=@CartId
                  AND @CheckIn BETWEEN o.StartDate AND o.EndDate",
                new SqlParameter("@CartId", selectedCartId),
                new SqlParameter("@CheckIn", dtCheckIn.Value.Date));

            decimal offerPercent = Convert.ToDecimal(offerResult ?? 0m);
            decimal discount = Math.Round(
                roomTotal * offerPercent / 100m,
                2);

            string updateSql = @"
                UPDATE Cart
                SET CheckInDate=@CheckIn,
                    CheckOutDate=@CheckOut,
                    Quantity=@Quantity,
                    Guests=@Guests,
                    DiscountAmount=@Discount
                WHERE CartId=@CartId
                AND CustomerId=@CustomerId";

            DbHelper.ExecuteNonQuery(
                updateSql,
                new SqlParameter("@CheckIn", dtCheckIn.Value.Date),
                new SqlParameter("@CheckOut", dtCheckOut.Value.Date),
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@Guests", Convert.ToInt32(numGuests.Value)),
                new SqlParameter("@Discount", discount),
                new SqlParameter("@CartId", selectedCartId),
                new SqlParameter("@CustomerId", Session.UserId));

            LoadData();
        }

        private void RemoveSelected()
        {
            if (selectedCartId == 0)
            {
                ShowError("Select a booking from your cart first.");
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Remove this booking from your cart?",
                "Remove Booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            DbHelper.ExecuteNonQuery(
                "DELETE FROM Cart WHERE CartId=@Id AND CustomerId=@CustomerId",
                new SqlParameter("@Id", selectedCartId),
                new SqlParameter("@CustomerId", Session.UserId));

            LoadData();
        }

        private void ClearCart()
        {
            if (cartPanel.Controls.Count == 0)
            {
                ShowError("Your cart is already empty.");
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Clear your entire cart? All selected rooms and services will be removed.",
                "Clear Cart",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            DbHelper.ExecuteNonQuery(
                "DELETE FROM Cart WHERE CustomerId=@CustomerId",
                new SqlParameter("@CustomerId", Session.UserId));

            DbHelper.ExecuteNonQuery(
                "DELETE FROM CartCoupons WHERE CustomerId=@CustomerId",
                new SqlParameter("@CustomerId", Session.UserId));

            LoadData();
        }

        private void Checkout_Click(object sender, EventArgs e)
        {
            if (cartPanel.Controls.Count == 0)
            {
                ShowError("Your cart is empty.");
                return;
            }

            CheckoutForm checkout = new CheckoutForm();
            checkout.ShowDialog(this);
            checkout.Dispose();
            LoadData();
        }

        private void Update_Click(object sender, EventArgs e)
        {
            UpdateSelected();
        }

        private void Remove_Click(object sender, EventArgs e)
        {
            RemoveSelected();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            ClearCart();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private void HideError()
        {
            lblError.Text = "";
            lblError.Visible = false;
        }

        private void Back_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
