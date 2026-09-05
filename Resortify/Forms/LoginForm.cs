```csharp
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
        // One item from cart
        class CartItem
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

            public decimal Price;
            public decimal RoomTotal;
            public decimal ServiceTotal;
            public decimal Discount;
        }

        List<CartItem> cart = new List<CartItem>();

        ListBox listBox;

        Label roomLabel;
        Label serviceLabel;
        Label discountLabel;
        Label totalLabel;
        Label couponLabel;
        Label errorLabel;
        Label transactionLabel;

        ComboBox paymentBox;

        TextBox transactionText;
        TextBox couponText;

        Button applyCouponButton;
        Button removeCouponButton;
        Button confirmButton;

        string couponCode = "";
        decimal couponDiscount = 0;


        public CheckoutForm()
        {
            InitializeComponent();
            LoadCart();
        }


        // =========================================================
        // FORM
        // =========================================================

        private void InitializeComponent()
        {
            Text = "Resortify - Checkout";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;

            BackColor = Color.White;


            // -------------------------
            // LEFT SIDE
            // -------------------------

            Panel leftPanel = new Panel();

            leftPanel.Location = new Point(20, 100);
            leftPanel.Size = new Size(550, 520);
            leftPanel.BackColor = Color.White;
            leftPanel.BorderStyle = BorderStyle.FixedSingle;


            Label title = new Label();

            title.Text = "Booking Summary";
            title.Location = new Point(20, 15);
            title.AutoSize = true;

            title.Font =
                new Font("Segoe UI", 12, FontStyle.Bold);


            listBox = new ListBox();

            listBox.Location = new Point(20, 55);
            listBox.Size = new Size(500, 440);

            listBox.HorizontalScrollbar = true;


            leftPanel.Controls.Add(title);
            leftPanel.Controls.Add(listBox);


            // -------------------------
            // RIGHT SIDE
            // -------------------------

            Panel rightPanel = new Panel();

            rightPanel.Location = new Point(590, 100);
            rightPanel.Size = new Size(480, 520);
            rightPanel.BackColor = Color.White;
            rightPanel.BorderStyle = BorderStyle.FixedSingle;


            Label paymentTitle = new Label();

            paymentTitle.Text = "Payment & Discount";
            paymentTitle.Location = new Point(20, 15);
            paymentTitle.AutoSize = true;

            paymentTitle.Font =
                new Font("Segoe UI", 12, FontStyle.Bold);


            // Payment
            paymentBox = new ComboBox();

            paymentBox.Location = new Point(20, 55);
            paymentBox.Size = new Size(430, 30);

            paymentBox.DropDownStyle =
                ComboBoxStyle.DropDownList;

            paymentBox.Items.Add("Pay at Hotel");
            paymentBox.Items.Add("Credit Card");
            paymentBox.Items.Add("Debit Card");
            paymentBox.Items.Add("bKash");
            paymentBox.Items.Add("Nagad");
            paymentBox.Items.Add("Rocket");
            paymentBox.Items.Add("Mobile Banking");

            paymentBox.SelectedIndex = 0;

            paymentBox.SelectedIndexChanged +=
                PaymentChanged;


            // Transaction
            transactionLabel = new Label();

            transactionLabel.Text =
                "Transaction ID (not required)";

            transactionLabel.Location =
                new Point(20, 95);

            transactionLabel.AutoSize = true;


            transactionText = new TextBox();

            transactionText.Location =
                new Point(20, 120);

            transactionText.Size =
                new Size(430, 30);


            // Coupon
            couponText = new TextBox();

            couponText.Location =
                new Point(20, 170);

            couponText.Size =
                new Size(270, 30);

            couponText.PlaceholderText =
                "Coupon code";


            applyCouponButton =
                UIHelper.MakeButton(
                    "Apply",
                    UIHelper.CustomerColor,
                    70,
                    30);

            applyCouponButton.Location =
                new Point(300, 170);

            applyCouponButton.Click +=
                ApplyCoupon;


            removeCouponButton =
                UIHelper.MakeButton(
                    "Remove",
                    Color.Gray,
                    70,
                    30);

            removeCouponButton.Location =
                new Point(375, 170);

            removeCouponButton.Click +=
                RemoveCoupon;


            couponLabel = new Label();

            couponLabel.Text =
                "Try WELCOME10, RESORT15 or GETAWAY20";

            couponLabel.Location =
                new Point(20, 205);

            couponLabel.AutoSize = true;

            couponLabel.ForeColor =
                Color.Gray;


            // -------------------------
            // TOTALS
            // -------------------------

            Label roomText = new Label();

            roomText.Text = "Room Total:";
            roomText.Location =
                new Point(20, 250);

            roomText.AutoSize = true;


            roomLabel = CreateAmountLabel();

            roomLabel.Location =
                new Point(300, 250);


            Label serviceText = new Label();

            serviceText.Text = "Services:";
            serviceText.Location =
                new Point(20, 285);

            serviceText.AutoSize = true;


            serviceLabel = CreateAmountLabel();

            serviceLabel.Location =
                new Point(300, 285);


            Label discountText = new Label();

            discountText.Text = "Discount:";
            discountText.Location =
                new Point(20, 320);

            discountText.AutoSize = true;


            discountLabel = CreateAmountLabel();

            discountLabel.Location =
                new Point(300, 320);


            Label grandText = new Label();

            grandText.Text = "Grand Total:";
            grandText.Location =
                new Point(20, 360);

            grandText.AutoSize = true;

            grandText.Font =
                new Font("Segoe UI", 11, FontStyle.Bold);


            totalLabel = CreateAmountLabel(true);

            totalLabel.Location =
                new Point(280, 355);


            // Error
            errorLabel = new Label();

            errorLabel.Location =
                new Point(20, 400);

            errorLabel.Size =
                new Size(430, 40);

            errorLabel.ForeColor =
                Color.Red;

            errorLabel.Visible = false;


            // Confirm
            confirmButton =
                UIHelper.MakeButton(
                    "Confirm Booking",
                    UIHelper.CustomerColor,
                    220,
                    40);

            confirmButton.Location =
                new Point(230, 450);

            confirmButton.Click +=
                ConfirmBooking;


            rightPanel.Controls.Add(paymentTitle);

            rightPanel.Controls.Add(paymentBox);

            rightPanel.Controls.Add(transactionLabel);
            rightPanel.Controls.Add(transactionText);

            rightPanel.Controls.Add(couponText);
            rightPanel.Controls.Add(applyCouponButton);
            rightPanel.Controls.Add(removeCouponButton);
            rightPanel.Controls.Add(couponLabel);

            rightPanel.Controls.Add(roomText);
            rightPanel.Controls.Add(roomLabel);

            rightPanel.Controls.Add(serviceText);
            rightPanel.Controls.Add(serviceLabel);

            rightPanel.Controls.Add(discountText);
            rightPanel.Controls.Add(discountLabel);

            rightPanel.Controls.Add(grandText);
            rightPanel.Controls.Add(totalLabel);

            rightPanel.Controls.Add(errorLabel);
            rightPanel.Controls.Add(confirmButton);


            Controls.Add(leftPanel);
            Controls.Add(rightPanel);


            Controls.Add(
                UIHelper.BuildHeader(
                    "Checkout",
                    UIHelper.CustomerColor,
                    (s, e) => Close(),
                    null));
        }


        private Label CreateAmountLabel(
            bool big = false)
        {
            Label label = new Label();

            label.Size =
                new Size(150, 30);

            label.TextAlign =
                ContentAlignment.MiddleRight;

            label.Font =
                new Font(
                    "Segoe UI",
                    big ? 13 : 10,
                    FontStyle.Bold);

            label.ForeColor =
                UIHelper.CustomerColor;

            return label;
        }


        // =========================================================
        // LOAD CART
        // =========================================================

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
                    DATEDIFF(
                        DAY,
                        c.CheckInDate,
                        c.CheckOutDate
                    ) AS Nights,
                    r.PricePerNight,

                    ISNULL(
                        (
                            SELECT SUM(
                                cs.UnitPrice * cs.Quantity
                            )
                            FROM CartServices cs
                            WHERE cs.CartId = c.CartId
                        ), 0
                    ) AS ServiceAmount,

                    ISNULL(
                        (
                            SELECT STRING_AGG(
                                sc.ServiceName,
                                ', '
                            )
                            FROM CartServices cs
                            JOIN ServiceCatalog sc
                            ON sc.ServiceId = cs.ServiceId
                            WHERE cs.CartId = c.CartId
                        ),
                        'None'
                    ) AS Services

                FROM Cart c

                JOIN Rooms r
                ON r.RoomId = c.RoomId

                JOIN Hotels h
                ON h.HotelId = r.HotelId

                WHERE c.CustomerId = @CustomerId";


            DataTable data =
                DbHelper.GetDataTable(
                    sql,
                    new SqlParameter(
                        "@CustomerId",
                        Session.UserId));


            cart.Clear();
            listBox.Items.Clear();


            foreach (DataRow row in data.Rows)
            {
                CartItem item = new CartItem();


                item.CartId =
                    Convert.ToInt32(
                        row["CartId"]);


                item.RoomId =
                    Convert.ToInt32(
                        row["RoomId"]);


                item.HotelName =
                    row["HotelName"].ToString();


                item.City =
                    row["City"].ToString();


                item.RoomType =
                    row["RoomType"].ToString();


                item.CheckIn =
                    Convert.ToDateTime(
                        row["CheckInDate"]);


                item.CheckOut =
                    Convert.ToDateTime(
                        row["CheckOutDate"]);


                item.Nights =
                    Convert.ToInt32(
                        row["Nights"]);


                item.Quantity =
                    Convert.ToInt32(
                        row["Quantity"]);


                item.Guests =
                    Convert.ToInt32(
                        row["Guests"]);


                item.Price =
                    Convert.ToDecimal(
                        row["PricePerNight"]);


                item.RoomTotal =
                    item.Price *
                    item.Nights *
                    item.Quantity;


                item.ServiceTotal =
                    Convert.ToDecimal(
                        row["ServiceAmount"]);


                item.Discount =
                    Convert.ToDecimal(
                        row["DiscountAmount"]);


                item.Services =
                    row["Services"].ToString();


                cart.Add(item);


                string summary =
                    item.HotelName +
                    " - " +
                    item.City +
                    Environment.NewLine +

                    item.RoomType +
                    Environment.NewLine +

                    "Check In: " +
                    item.CheckIn.ToString(
                        "dd MMM yyyy") +

                    Environment.NewLine +

                    "Check Out: " +
                    item.CheckOut.ToString(
                        "dd MMM yyyy") +

                    Environment.NewLine +

                    "Nights: " +
                    item.Nights +

                    " | Rooms: " +
                    item.Quantity +

                    " | Guests: " +
                    item.Guests +

                    Environment.NewLine +

                    "Room: $" +
                    item.RoomTotal.ToString("N2") +

                    " | Services: $" +
                    item.ServiceTotal.ToString("N2") +

                    Environment.NewLine +

                    "Services: " +
                    item.Services;


                listBox.Items.Add(summary);
            }


            LoadSavedCoupon();

            CalculateTotal();


            if (cart.Count == 0)
            {
                ShowError(
                    "Your cart is empty.");
            }
        }


        // =========================================================
        // TOTAL CALCULATIONS
        // =========================================================

        private decimal RoomTotal()
        {
            decimal total = 0;


            foreach (CartItem item in cart)
            {
                total =
                    total +
                    item.RoomTotal;
            }


            return total;
        }


        private decimal ServiceTotal()
        {
            decimal total = 0;


            foreach (CartItem item in cart)
            {
                total =
                    total +
                    item.ServiceTotal;
            }


            return total;
        }


        private decimal OfferDiscount()
        {
            decimal total = 0;


            foreach (CartItem item in cart)
            {
                total =
                    total +
                    item.Discount;
            }


            return total;
        }


        private void CalculateTotal()
        {
            decimal room =
                RoomTotal();

            decimal services =
                ServiceTotal();

            decimal offer =
                OfferDiscount();


            decimal beforeCoupon =
                room +
                services -
                offer;


            if (beforeCoupon < 0)
                beforeCoupon = 0;


            if (couponDiscount >
                beforeCoupon)
            {
                couponDiscount =
                    beforeCoupon;
            }


            decimal totalDiscount =
                offer +
                couponDiscount;


            decimal grandTotal =
                beforeCoupon -
                couponDiscount;


            roomLabel.Text =
                "$" + room.ToString("N2");


            serviceLabel.Text =
                "$" + services.ToString("N2");


            discountLabel.Text =
                "-$" +
                totalDiscount.ToString("N2");


            totalLabel.Text =
                "$" +
                grandTotal.ToString("N2");
        }


        // =========================================================
        // LOAD SAVED COUPON
        // =========================================================

        private void LoadSavedCoupon()
        {
            string sql = @"
                SELECT
                    CouponCode,
                    DiscountAmount

                FROM CartCoupons

                WHERE CustomerId = @CustomerId";


            DataTable data =
                DbHelper.GetDataTable(
                    sql,
                    new SqlParameter(
                        "@CustomerId",
                        Session.UserId));


            if (data.Rows.Count > 0)
            {
                couponCode =
                    data.Rows[0]
                    ["CouponCode"]
                    .ToString();


                couponDiscount =
                    Convert.ToDecimal(
                        data.Rows[0]
                        ["DiscountAmount"]);


                couponText.Text =
                    couponCode;


                couponLabel.Text =
                    "Coupon applied: -$" +
                    couponDiscount.ToString("N2");


                couponLabel.ForeColor =
                    UIHelper.CustomerColor;
            }
        }


        // =========================================================
        // APPLY COUPON
        // =========================================================

        private void ApplyCoupon(
            object sender,
            EventArgs e)
        {
            HideError();


            string code =
                couponText.Text
                .Trim()
                .ToUpper();


            if (code == "")
            {
                ShowError(
                    "Please enter a coupon code.");

                return;
            }


            decimal bookingAmount =
                RoomTotal() +
                ServiceTotal() -
                OfferDiscount();


            string sql = @"
                SELECT TOP 1
                    CouponId,
                    Code,
                    DiscountPercent,
                    MaxDiscountAmount,
                    MinimumBookingAmount,
                    ValidFrom,
                    ValidTo,
                    MaxUses,
                    UsedCount,
                    Active

                FROM Coupons

                WHERE UPPER(Code) = @Code";


            DataTable data =
                DbHelper.GetDataTable(
                    sql,
                    new SqlParameter(
                        "@Code",
                        code));


            if (data.Rows.Count == 0)
            {
                ShowError(
                    "Invalid coupon code.");

                return;
            }


            DataRow row =
                data.Rows[0];


            bool active =
                Convert.ToBoolean(
                    row["Active"]);


            if (!active)
            {
                ShowError(
                    "This coupon is not active.");

                return;
            }


            DateTime start =
                Convert.ToDateTime(
                    row["ValidFrom"]);


            DateTime end =
                Convert.ToDateTime(
                    row["ValidTo"]);


            if (DateTime.Today < start.Date ||
                DateTime.Today > end.Date)
            {
                ShowError(
                    "This coupon is expired.");

                return;
            }


            if (row["MaxUses"] != DBNull.Value)
            {
                int max =
                    Convert.ToInt32(
                        row["MaxUses"]);


                int used =
                    Convert.ToInt32(
                        row["UsedCount"]);


                if (used >= max)
                {
                    ShowError(
                        "Coupon usage limit reached.");

                    return;
                }
            }


            decimal minimum =
                Convert.ToDecimal(
                    row["MinimumBookingAmount"]);


            if (bookingAmount < minimum)
            {
                ShowError(
                    "Minimum booking amount is $" +
                    minimum.ToString("N2"));

                return;
            }


            decimal percent =
                Convert.ToDecimal(
                    row["DiscountPercent"]);


            decimal discount =
                bookingAmount *
                percent /
                100;


            discount =
                Math.Round(
                    discount,
                    2);


            if (row["MaxDiscountAmount"]
                != DBNull.Value)
            {
                decimal maxDiscount =
                    Convert.ToDecimal(
                        row["MaxDiscountAmount"]);


                if (discount >
                    maxDiscount)
                {
                    discount =
                        maxDiscount;
                }
            }


            couponCode =
                row["Code"].ToString();


            couponDiscount =
                discount;


            // Save coupon
            string saveSql = @"
                MERGE CartCoupons AS target

                USING
                (
                    SELECT
                        @CustomerId AS CustomerId
                ) AS source

                ON target.CustomerId =
                   source.CustomerId

                WHEN MATCHED THEN

                    UPDATE SET
                        CouponId = @CouponId,
                        CouponCode = @Code,
                        DiscountAmount = @Discount,
                        AppliedAt = GETDATE()

                WHEN NOT MATCHED THEN

                    INSERT
                    (
                        CustomerId,
                        CouponId,
                        CouponCode,
                        DiscountAmount
                    )

                    VALUES
                    (
                        @CustomerId,
                        @CouponId,
                        @Code,
                        @Discount
                    );";


            DbHelper.ExecuteNonQuery(
                saveSql,

                new SqlParameter(
                    "@CustomerId",
                    Session.UserId),

                new SqlParameter(
                    "@CouponId",
                    Convert.ToInt32(
                        row["CouponId"])),

                new SqlParameter(
                    "@Code",
                    couponCode),

                new SqlParameter(
                    "@Discount",
                    couponDiscount));


            couponLabel.Text =
                "Coupon " +
                couponCode +
                " applied: -$" +
                couponDiscount.ToString("N2");


            couponLabel.ForeColor =
                UIHelper.CustomerColor;


            CalculateTotal();
        }


        // =========================================================
        // REMOVE COUPON
        // =========================================================

        private void RemoveCoupon(
            object sender,
            EventArgs e)
        {
            string sql =
                "DELETE FROM CartCoupons " +
                "WHERE CustomerId = @CustomerId";


            DbHelper.ExecuteNonQuery(
                sql,
                new SqlParameter(
                    "@CustomerId",
                    Session.UserId));


            couponCode = "";
            couponDiscount = 0;


            couponText.Clear();


            couponLabel.Text =
                "No coupon applied.";

            couponLabel.ForeColor =
                Color.Gray;


            CalculateTotal();
        }


        // =========================================================
        // PAYMENT CHANGE
        // =========================================================

        private void PaymentChanged(
            object sender,
            EventArgs e)
        {
            string payment =
                paymentBox.SelectedItem
                .ToString();


            if (payment ==
                "Pay at Hotel")
            {
                transactionLabel.Text =
                    "Transaction ID (not required)";

                transactionText.Enabled =
                    false;

                transactionText.Clear();
            }
            else
            {
                transactionLabel.Text =
                    "Transaction ID *";

                transactionText.Enabled =
                    true;
            }
        }


        // =========================================================
        // CONFIRM BOOKING
        // =========================================================

        private void ConfirmBooking(
            object sender,
            EventArgs e)
        {
            HideError();


            if (cart.Count == 0)
            {
                ShowError(
                    "Your cart is empty.");

                return;
            }


            string payment =
                paymentBox.SelectedItem
                .ToString();


            string transaction =
                transactionText.Text.Trim();


            bool online =
                payment != "Pay at Hotel";


            if (online &&
                transaction == "")
            {
                ShowError(
                    "Enter your transaction ID.");

                return;
            }


            decimal room =
                RoomTotal();


            decimal services =
                ServiceTotal();


            decimal offer =
                OfferDiscount();


            decimal totalDiscount =
                offer +
                couponDiscount;


            decimal total =
                room +
                services -
                totalDiscount;


            if (total < 0)
                total = 0;


            string message =
                "Payment: " +
                payment +

                Environment.NewLine +

                "Coupon: " +
                (couponCode == ""
                    ? "None"
                    : couponCode) +

                Environment.NewLine +

                "Discount: -$" +
                totalDiscount.ToString("N2") +

                Environment.NewLine +

                "Grand Total: $" +
                total.ToString("N2") +

                Environment.NewLine +
                Environment.NewLine +

                "Confirm booking?";


            DialogResult answer =
                MessageBox.Show(
                    message,
                    "Confirm Booking",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);


            if (answer !=
                DialogResult.Yes)
            {
                return;
            }


            try
            {
                int bookingId =
                    SaveBooking(
                        room,
                        services,
                        totalDiscount,
                        total,
                        payment,
                        transaction,
                        online);


                MessageBox.Show(
                    "Booking successful!\nBooking ID: " +
                    bookingId,
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);


                Close();
            }
            catch (Exception ex)
            {
                ShowError(
                    ex.Message);
            }
        }


        // =========================================================
        // SAVE BOOKING
        // =========================================================

        private int SaveBooking(
            decimal room,
            decimal services,
            decimal discount,
            decimal total,
            string payment,
            string transaction,
            bool online)
        {
            int bookingId = 0;


            DbHelper.RunTransaction(
                (connection, trans) =>
                {
                    // -----------------------------
                    // Check room availability
                    // -----------------------------

                    foreach (CartItem item in cart)
                    {
                        string checkSql = @"
                            SELECT
                                r.TotalRooms -

                                ISNULL(
                                (
                                    SELECT SUM(
                                        bi.Quantity
                                    )

                                    FROM BookingItems bi

                                    JOIN Bookings b
                                    ON b.BookingId =
                                       bi.BookingId

                                    WHERE bi.RoomId =
                                          r.RoomId

                                    AND b.Status IN
                                    (
                                        'Pending',
                                        'Approved',
                                        'Confirmed',
                                        'Completed'
                                    )

                                    AND bi.CheckInDate <
                                        c.CheckOutDate

                                    AND bi.CheckOutDate >
                                        c.CheckInDate

                                ), 0)

                            FROM Cart c

                            JOIN Rooms r
                            ON r.RoomId =
                               c.RoomId

                            WHERE c.CartId =
                                  @CartId

                            AND c.CustomerId =
                                @CustomerId";


                        using (SqlCommand command =
                            new SqlCommand(
                                checkSql,
                                connection,
                                trans))
                        {
                            command.Parameters.AddWithValue(
                                "@CartId",
                                item.CartId);

                            command.Parameters.AddWithValue(
                                "@CustomerId",
                                Session.UserId);


                            int available =
                                Convert.ToInt32(
                                    command.ExecuteScalar());


                            if (available <
                                item.Quantity)
                            {
                                throw new Exception(
                                    item.RoomType +
                                    " is no longer available.");
                            }
                        }
                    }


                    // -----------------------------
                    // Increase coupon usage
                    // -----------------------------

                    if (couponCode != "")
                    {
                        string couponSql = @"
                            UPDATE Coupons

                            SET UsedCount =
                                UsedCount + 1

                            WHERE Code = @Code";


                        using (SqlCommand command =
                            new SqlCommand(
                                couponSql,
                                connection,
                                trans))
                        {
                            command.Parameters.AddWithValue(
                                "@Code",
                                couponCode);

                            command.ExecuteNonQuery();
                        }
                    }


                    // -----------------------------
                    // Create booking
                    // -----------------------------

                    string status;

                    if (online)
                        status = "Pending";
                    else
                        status = "Confirmed";


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
                            @Total,
                            @Room,
                            @Services,
                            @Discount,
                            @Coupon,
                            @CouponDiscount,
                            @Payment,
                            @Transaction,
                            @Status,
                            @Notification
                        )";


                    using (SqlCommand command =
                        new SqlCommand(
                            bookingSql,
                            connection,
                            trans))
                    {
                        command.Parameters.AddWithValue(
                            "@CustomerId",
                            Session.UserId);

                        command.Parameters.AddWithValue(
                            "@Total",
                            total);

                        command.Parameters.AddWithValue(
                            "@Room",
                            room);

                        command.Parameters.AddWithValue(
                            "@Services",
                            services);

                        command.Parameters.AddWithValue(
                            "@Discount",
                            discount);


                        if (couponCode == "")
                        {
                            command.Parameters.AddWithValue(
                                "@Coupon",
                                DBNull.Value);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(
                                "@Coupon",
                                couponCode);
                        }


                        command.Parameters.AddWithValue(
                            "@CouponDiscount",
                            couponDiscount);

                        command.Parameters.AddWithValue(
                            "@Payment",
                            payment);


                        if (online)
                        {
                            command.Parameters.AddWithValue(
                                "@Transaction",
                                transaction);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(
                                "@Transaction",
                                DBNull.Value);
                        }


                        command.Parameters.AddWithValue(
                            "@Status",
                            status);


                        if (online)
                        {
                            command.Parameters.AddWithValue(
                                "@Notification",
                                1);
                        }
                        else
                        {
                            command.Parameters.AddWithValue(
                                "@Notification",
                                0);
                        }


                        bookingId =
                            Convert.ToInt32(
                                command.ExecuteScalar());
                    }


                    // -----------------------------
                    // Add rooms to booking
                    // -----------------------------

                    foreach (CartItem item in cart)
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
                                @Price,
                                @Subtotal
                            )";


                        int bookingItemId;


                        using (SqlCommand command =
                            new SqlCommand(
                                itemSql,
                                connection,
                                trans))
                        {
                            command.Parameters.AddWithValue(
                                "@BookingId",
                                bookingId);

                            command.Parameters.AddWithValue(
                                "@RoomId",
                                item.RoomId);

                            command.Parameters.AddWithValue(
                                "@CheckIn",
                                item.CheckIn);

                            command.Parameters.AddWithValue(
                                "@CheckOut",
                                item.CheckOut);

                            command.Parameters.AddWithValue(
                                "@Nights",
                                item.Nights);

                            command.Parameters.AddWithValue(
                                "@Quantity",
                                item.Quantity);

                            command.Parameters.AddWithValue(
                                "@Guests",
                                item.Guests);

                            command.Parameters.AddWithValue(
                                "@Price",
                                item.Price);

                            command.Parameters.AddWithValue(
                                "@Subtotal",
                                item.RoomTotal);


                            bookingItemId =
                                Convert.ToInt32(
                                    command.ExecuteScalar());
                        }


                        // -----------------------------
                        // Add services
                        // -----------------------------

                        string serviceSql = @"
                            INSERT INTO BookingItemServices
                            (
                                BookingItemId,
                                ServiceId,
                                Quantity,
                                UnitPrice
                            )

                            SELECT
                                @BookingItemId,
                                ServiceId,
                                Quantity,
                                UnitPrice

                            FROM CartServices

                            WHERE CartId = @CartId";


                        using (SqlCommand command =
                            new SqlCommand(
                                serviceSql,
                                connection,
                                trans))
                        {
                            command.Parameters.AddWithValue(
                                "@BookingItemId",
                                bookingItemId);

                            command.Parameters.AddWithValue(
                                "@CartId",
                                item.CartId);

                            command.ExecuteNonQuery();
                        }
                    }


                    // -----------------------------
                    // Clear coupon
                    // -----------------------------

                    string deleteCoupon =
                        "DELETE FROM CartCoupons " +
                        "WHERE CustomerId = @CustomerId";


                    using (SqlCommand command =
                        new SqlCommand(
                            deleteCoupon,
                            connection,
                            trans))
                    {
                        command.Parameters.AddWithValue(
                            "@CustomerId",
                            Session.UserId);

                        command.ExecuteNonQuery();
                    }


                    // -----------------------------
                    // Clear cart
                    // -----------------------------

                    string deleteCart =
                        "DELETE FROM Cart " +
                        "WHERE CustomerId = @CustomerId";


                    using (SqlCommand command =
                        new SqlCommand(
                            deleteCart,
                            connection,
                            trans))
                    {
                        command.Parameters.AddWithValue(
                            "@CustomerId",
                            Session.UserId);

                        command.ExecuteNonQuery();
                    }
                });


            return bookingId;
        }


        // =========================================================
        // ERROR
        // =========================================================

        private void ShowError(
            string message)
        {
            errorLabel.Text =
                message;

            errorLabel.Visible =
                true;
        }


        private void HideError()
        {
            errorLabel.Text = "";

            errorLabel.Visible =
                false;
        }
    }
}
```

This version is intentionally written in a **simple C# student style**: normal `if/else`, `foreach`, separate small methods, straightforward SQL, and no complicated expressions.

One thing to note: your uploaded `Pasted code(1).cs` did not contain readable file text on inspection, so I based this directly on the full `CheckoutForm` code you pasted in the chat.

If you want it **even more beginner-level**, I can make the SQL and `SaveBooking()` section simpler too, so it looks more like a normal **CSE lab/project student's code** rather than production-style code.
