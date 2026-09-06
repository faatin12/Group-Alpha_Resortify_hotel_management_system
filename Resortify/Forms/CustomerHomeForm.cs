using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    // Customer home page.
    // This form only handles customer browsing and navigation.
    public partial class CustomerHomeForm : Form
    {
        private TextBox txtSearch;
        private ComboBox cboCity;
        private ComboBox cboRoomType;
        private ComboBox cboPrice;
        private ComboBox cboStars;
        private FlowLayoutPanel hotelPanel;

        private Label lblWelcome;
        private Label lblCartCount;
        private Label lblBookingCount;
        private Label lblOfferCount;
        private Label lblResultCount;

        private bool loadingFilters = true;
        private System.Windows.Forms.Timer notificationTimer;

        private string[] cities =
        {
            "Any location",
            "Dhaka",
            "Cox's Bazar",
            "St. Martin's",
            "Sylhet",
            "Bandarban",
            "Rangamati"
        };

        private string[] roomTypes =
        {
            "All room types",
            "Standard Room",
            "Standard Twin",
            "Superior Room",
            "Deluxe Room",
            "Deluxe Sea View",
            "Super Deluxe Room",
            "Executive Room",
            "Junior Suite",
            "Family Suite",
            "Executive Suite",
            "Presidential Suite",
            "Cottage",
            "Villa"
        };

        public CustomerHomeForm()
        {
            InitializeComponent();
            LoadFilters();

            loadingFilters = false;

            Activated += CustomerHomeForm_Activated;

            notificationTimer = new System.Windows.Forms.Timer();
            notificationTimer.Interval = 5000;
            notificationTimer.Tick += NotificationTimer_Tick;
            notificationTimer.Start();

            RefreshDashboard();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Customer Dashboard";
            ClientSize = new Size(1200, 800);
            MinimumSize = new Size(1000, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;

            Panel header = UIHelper.BuildHeader(
                "Customer Dashboard",
                UIHelper.CustomerColor,
                null,
                Logout_Click);

            Controls.Add(header);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(28, 20, 28, 24);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 195));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Top welcome section
            TableLayoutPanel top = CreateTopSection();
            root.Controls.Add(top, 0, 0);

            // Search and filter section
            TableLayoutPanel filters = CreateFilterSection();
            root.Controls.Add(filters, 0, 1);

            // Result heading
            TableLayoutPanel resultHeading = new TableLayoutPanel();
            resultHeading.Dock = DockStyle.Fill;
            resultHeading.ColumnCount = 2;
            resultHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            resultHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            Label availableLabel = new Label();
            availableLabel.Text = "Available stays";
            availableLabel.Dock = DockStyle.Fill;
            availableLabel.TextAlign = ContentAlignment.MiddleLeft;
            availableLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            availableLabel.ForeColor = UIHelper.NavyHeader;

            lblResultCount = new Label();
            lblResultCount.Dock = DockStyle.Fill;
            lblResultCount.TextAlign = ContentAlignment.MiddleRight;
            lblResultCount.ForeColor = Color.DimGray;

            resultHeading.Controls.Add(availableLabel, 0, 0);
            resultHeading.Controls.Add(lblResultCount, 1, 0);
            root.Controls.Add(resultHeading, 0, 2);

            // Hotel cards
            hotelPanel = new FlowLayoutPanel();
            hotelPanel.Dock = DockStyle.Fill;
            hotelPanel.AutoScroll = true;
            hotelPanel.WrapContents = true;
            hotelPanel.FlowDirection = FlowDirection.LeftToRight;
            hotelPanel.Padding = new Padding(4);
            hotelPanel.BackColor = Color.Transparent;
            hotelPanel.Resize += HotelPanel_Resize;
            root.Controls.Add(hotelPanel, 0, 3);

            Controls.Add(root);
        }

        private TableLayoutPanel CreateTopSection()
        {
            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.ColumnCount = 2;
            top.RowCount = 3;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            string name = string.IsNullOrWhiteSpace(Session.FullName)
                ? "Guest"
                : Session.FullName.Trim();

            string firstName = name;
            string[] nameParts = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length > 0)
            {
                firstName = nameParts[0];
            }

            string greeting;
            if (DateTime.Now.Hour < 12)
            {
                greeting = "Good morning";
            }
            else if (DateTime.Now.Hour < 17)
            {
                greeting = "Good afternoon";
            }
            else
            {
                greeting = "Good evening";
            }

            Panel welcomeBox = new Panel();
            welcomeBox.Dock = DockStyle.Fill;
            welcomeBox.Padding = new Padding(0, 2, 12, 0);

            lblWelcome = new Label();
            lblWelcome.Text = greeting + ", " + firstName + "!";
            lblWelcome.Dock = DockStyle.Top;
            lblWelcome.Height = 34;
            lblWelcome.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            lblWelcome.ForeColor = UIHelper.NavyHeader;

            Label welcomeMessage = new Label();
            welcomeMessage.Text = "Welcome to Resortify, " + firstName +
                ". We hope you find the perfect stay for your next getaway.";
            welcomeMessage.Dock = DockStyle.Fill;
            welcomeMessage.ForeColor = Color.DimGray;
            welcomeMessage.AutoEllipsis = true;

            welcomeBox.Controls.Add(welcomeMessage);
            welcomeBox.Controls.Add(lblWelcome);
            top.Controls.Add(welcomeBox, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            actions.AutoScroll = true;
            actions.Padding = new Padding(0, 5, 0, 0);

            AddActionButton(actions, "Update Profile", Color.FromArgb(80, 80, 80), UpdateProfile_Click);
            AddActionButton(actions, "Special Offers", UIHelper.CustomerColor, SpecialOffers_Click);
            AddActionButton(actions, "My Cart", UIHelper.CustomerColor, MyCart_Click);
            top.Controls.Add(actions, 1, 0);

            Button bookingsButton = UIHelper.MakeButton(
                "My Bookings  •  View all reservations",
                UIHelper.NavyHeader,
                245,
                32);
            bookingsButton.Margin = new Padding(0, 2, 0, 2);
            bookingsButton.Click += MyBookings_Click;
            top.Controls.Add(bookingsButton, 0, 1);
            top.SetColumnSpan(bookingsButton, 2);

            TableLayoutPanel stats = new TableLayoutPanel();
            stats.Dock = DockStyle.Fill;
            stats.ColumnCount = 3;
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            stats.Controls.Add(CreateStat("MY CART", out lblCartCount), 0, 0);
            stats.Controls.Add(CreateStat("UPCOMING BOOKINGS", out lblBookingCount), 1, 0);
            stats.Controls.Add(CreateStat("ACTIVE OFFERS", out lblOfferCount), 2, 0);

            top.Controls.Add(stats, 0, 2);
            top.SetColumnSpan(stats, 2);

            return top;
        }

        private TableLayoutPanel CreateFilterSection()
        {
            TableLayoutPanel filter = new TableLayoutPanel();
            filter.Dock = DockStyle.Fill;
            filter.BackColor = Color.White;
            filter.Padding = new Padding(12);
            filter.ColumnCount = 6;
            filter.RowCount = 2;
            filter.BorderStyle = BorderStyle.FixedSingle;

            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

            txtSearch = new TextBox();
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Hotel name or city";
            AddFilter(filter, "Search hotel / city", txtSearch, 0);

            cboCity = new ComboBox();
            cboCity.Dock = DockStyle.Fill;
            cboCity.DropDownStyle = ComboBoxStyle.DropDownList;
            AddFilter(filter, "Location", cboCity, 1);

            cboRoomType = new ComboBox();
            cboRoomType.Dock = DockStyle.Fill;
            cboRoomType.DropDownStyle = ComboBoxStyle.DropDownList;
            AddFilter(filter, "Room type", cboRoomType, 2);

            cboPrice = new ComboBox();
            cboPrice.Dock = DockStyle.Fill;
            cboPrice.DropDownStyle = ComboBoxStyle.DropDownList;
            AddFilter(filter, "Price", cboPrice, 3);

            cboStars = new ComboBox();
            cboStars.Dock = DockStyle.Fill;
            cboStars.DropDownStyle = ComboBoxStyle.DropDownList;
            AddFilter(filter, "Rating", cboStars, 4);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.WrapContents = false;

            Button searchButton = UIHelper.MakeButton("Search", UIHelper.CustomerColor, 82, 30);
            searchButton.Click += Search_Click;

            Button resetButton = UIHelper.MakeButton("Reset", Color.FromArgb(95, 95, 95), 72, 30);
            resetButton.Click += Reset_Click;

            buttons.Controls.Add(searchButton);
            buttons.Controls.Add(resetButton);
            filter.Controls.Add(buttons, 5, 1);

            txtSearch.KeyDown += Search_KeyDown;
            cboCity.SelectedIndexChanged += FilterChanged;
            cboRoomType.SelectedIndexChanged += FilterChanged;
            cboPrice.SelectedIndexChanged += FilterChanged;
            cboStars.SelectedIndexChanged += FilterChanged;

            return filter;
        }

        private void LoadFilters()
        {
            cboCity.Items.AddRange(cities);
            cboCity.SelectedIndex = 0;

            cboRoomType.Items.AddRange(roomTypes);
            cboRoomType.SelectedIndex = 0;

            cboPrice.Items.AddRange(new object[]
            {
                "Any price",
                "Under $75",
                "$75 - $150",
                "$150 - $250",
                "Over $250"
            });
            cboPrice.SelectedIndex = 0;

            cboStars.Items.AddRange(new object[]
            {
                "Any rating",
                "5+ star hotels",
                "4+ stars",
                "3+ stars"
            });
            cboStars.SelectedIndex = 0;
        }

        private void AddFilter(TableLayoutPanel panel, string text, Control input, int column)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(70, 70, 70);

            panel.Controls.Add(label, column, 0);
            panel.Controls.Add(input, column, 1);
        }

        private void AddActionButton(FlowLayoutPanel panel, string text, Color color, EventHandler handler)
        {
            Button button = UIHelper.MakeButton(text, color, 108, 32);
            button.Margin = new Padding(4, 0, 0, 0);
            button.Click += handler;
            panel.Controls.Add(button);
        }

        private Panel CreateStat(string title, out Label valueLabel)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.Margin = new Padding(5);
            panel.BorderStyle = BorderStyle.FixedSingle;

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 24;
            titleLabel.Padding = new Padding(10, 6, 0, 0);
            titleLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            titleLabel.ForeColor = Color.DimGray;

            valueLabel = new Label();
            valueLabel.Text = "0";
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Padding = new Padding(10, 0, 0, 0);
            valueLabel.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            valueLabel.ForeColor = UIHelper.CustomerColor;

            panel.Controls.Add(valueLabel);
            panel.Controls.Add(titleLabel);

            return panel;
        }

        private void RefreshDashboard()
        {
            try
            {
                lblCartCount.Text = GetCount(
                    "SELECT COUNT(*) FROM Cart WHERE CustomerId=@Id");

                lblBookingCount.Text = GetCount(
                    "SELECT COUNT(*) FROM Bookings WHERE CustomerId=@Id " +
                    "AND Status IN ('Approved','Confirmed','Completed')");

                CheckBookingNotifications();

                lblOfferCount.Text = GetCount(
                    "SELECT COUNT(*) FROM Offers o " +
                    "JOIN Rooms r ON r.RoomId=o.RoomId " +
                    "JOIN Hotels h ON h.HotelId=r.HotelId " +
                    "WHERE h.Status='Approved' " +
                    "AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate");

                LoadHotels();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load dashboard data.\n\n" + ex.Message,
                    "Resortify",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string GetCount(string sql)
        {
            object result = DbHelper.ExecuteScalar(
                sql,
                new SqlParameter("@Id", Session.UserId));

            if (result == null || result == DBNull.Value)
            {
                return "0";
            }

            return Convert.ToInt32(result).ToString();
        }

        private void CheckBookingNotifications()
        {
            string sql = @"
                SELECT TOP 10 BookingId, Status, TotalAmount
                FROM Bookings
                WHERE CustomerId=@Id
                AND Status IN ('Approved','Rejected')
                AND CustomerNotificationShown=0
                ORDER BY BookingDate";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", Session.UserId));

            foreach (DataRow row in table.Rows)
            {
                int bookingId = Convert.ToInt32(row["BookingId"]);
                string status = row["Status"].ToString();
                decimal total = Convert.ToDecimal(row["TotalAmount"]);

                string message;
                string title;
                MessageBoxIcon icon;

                if (status == "Approved")
                {
                    message = "Great news! Your booking #" + bookingId +
                        " has been approved after transaction validation.\n\n" +
                        "Total: $" + total.ToString("N2") + "\n\n" +
                        "Your reservation is now confirmed. A confirmation email " +
                        "will be sent to your registered email.";
                    title = "Booking Confirmed";
                    icon = MessageBoxIcon.Information;
                }
                else
                {
                    message = "Your booking #" + bookingId +
                        " was not approved by the hotel admin.\n\n" +
                        "Total requested: $" + total.ToString("N2") + "\n\n" +
                        "Please check My Bookings for the latest status.";
                    title = "Booking Update";
                    icon = MessageBoxIcon.Warning;
                }

                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);

                DbHelper.ExecuteNonQuery(
                    "UPDATE Bookings SET CustomerNotificationShown=1 " +
                    "WHERE BookingId=@Id AND CustomerId=@Cust",
                    new SqlParameter("@Id", bookingId),
                    new SqlParameter("@Cust", Session.UserId));
            }
        }

        private void LoadHotels()
        {
            if (hotelPanel == null)
            {
                return;
            }

            hotelPanel.SuspendLayout();
            hotelPanel.Controls.Clear();

            string sql = @"
                SELECT
                    h.HotelId,
                    h.HotelName,
                    h.City,
                    h.StarRating,
                    r.RoomId,
                    r.RoomType,
                    r.PricePerNight,
                    r.TotalRooms,
                    ISNULL(r.RoomSize, '') AS RoomSize,
                    ISNULL(r.BedType, '') AS BedType,
                    ISNULL(r.MaxGuests, 2) AS MaxGuests,
                    ISNULL(r.Amenities, '') AS Amenities,
                    r.TotalRooms - ISNULL(
                        (
                            SELECT SUM(bi.Quantity)
                            FROM BookingItems bi
                            JOIN Bookings b ON b.BookingId=bi.BookingId
                            WHERE bi.RoomId=r.RoomId
                            AND b.Status IN ('Pending','Approved','Confirmed','Completed')
                            AND bi.CheckInDate < @OutDate
                            AND bi.CheckOutDate > @InDate
                        ), 0) AS Available
                FROM Hotels h
                CROSS APPLY
                (
                    SELECT TOP 1 *
                    FROM Rooms rr
                    WHERE rr.HotelId=h.HotelId
                    AND (@RoomType IS NULL OR rr.RoomType=@RoomType)
                    ORDER BY rr.PricePerNight, rr.RoomId
                ) r
                WHERE h.Status='Approved'";

            System.Collections.Generic.List<SqlParameter> parameters =
                new System.Collections.Generic.List<SqlParameter>();

            parameters.Add(new SqlParameter(
                "@InDate", DateTime.Today.AddDays(1)));
            parameters.Add(new SqlParameter(
                "@OutDate", DateTime.Today.AddDays(2)));

            object roomType = DBNull.Value;
            if (cboRoomType.SelectedIndex > 0)
            {
                roomType = cboRoomType.SelectedItem.ToString();
            }
            parameters.Add(new SqlParameter("@RoomType", roomType));

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                sql += " AND (h.HotelName LIKE @Search OR " +
                       "h.City LIKE @Search OR h.Address LIKE @Search)";

                parameters.Add(new SqlParameter(
                    "@Search",
                    "%" + txtSearch.Text.Trim() + "%"));
            }

            if (cboCity.SelectedIndex > 0)
            {
                sql += " AND h.City=@City";
                parameters.Add(new SqlParameter(
                    "@City", cboCity.SelectedItem.ToString()));
            }

            if (cboStars.SelectedIndex == 1)
            {
                sql += " AND h.StarRating>=5";
            }
            else if (cboStars.SelectedIndex == 2)
            {
                sql += " AND h.StarRating>=4";
            }
            else if (cboStars.SelectedIndex == 3)
            {
                sql += " AND h.StarRating>=3";
            }

            if (cboPrice.SelectedIndex == 1)
            {
                sql += " AND r.PricePerNight<75";
            }
            else if (cboPrice.SelectedIndex == 2)
            {
                sql += " AND r.PricePerNight BETWEEN 75 AND 150";
            }
            else if (cboPrice.SelectedIndex == 3)
            {
                sql += " AND r.PricePerNight>150 AND r.PricePerNight<=250";
            }
            else if (cboPrice.SelectedIndex == 4)
            {
                sql += " AND r.PricePerNight>250";
            }

            sql += " ORDER BY ISNULL(h.StarRating,0) DESC, r.PricePerNight ASC";

            DataTable table = DbHelper.GetDataTable(
                sql,
                parameters.ToArray());

            if (table.Rows.Count == 1)
            {
                lblResultCount.Text = "1 property found";
            }
            else
            {
                lblResultCount.Text = table.Rows.Count + " properties found";
            }

            foreach (DataRow row in table.Rows)
            {
                hotelPanel.Controls.Add(CreateHotelCard(row));
            }

            if (table.Rows.Count == 0)
            {
                hotelPanel.Controls.Add(CreateEmptyState());
            }

            ResizeCards();
            hotelPanel.ResumeLayout();
        }

        private Panel CreateHotelCard(DataRow row)
        {
            int hotelId = Convert.ToInt32(row["HotelId"]);
            string name = row["HotelName"].ToString();
            string city = row["City"].ToString();
            string roomType = row["RoomType"].ToString();
            string amenities = row["Amenities"].ToString();
            decimal price = Convert.ToDecimal(row["PricePerNight"]);
            int available = Convert.ToInt32(row["Available"]);

            if (available < 0)
            {
                available = 0;
            }

            string rating = "New";
            if (row["StarRating"] != DBNull.Value)
            {
                rating = Convert.ToDecimal(row["StarRating"]).ToString("0.0") + " ★";
            }

            Panel card = new Panel();
            card.Width = 350;
            card.Height = 315;
            card.BackColor = Color.White;
            card.Margin = new Padding(7);
            card.BorderStyle = BorderStyle.FixedSingle;

            Panel banner = new Panel();
            banner.Dock = DockStyle.Top;
            banner.Height = 78;
            banner.BackColor = Color.FromArgb(226, 238, 232);
            banner.Padding = new Padding(14, 8, 12, 8);

            Label hotelName = new Label();
            hotelName.Text = name;
            hotelName.Dock = DockStyle.Fill;
            hotelName.TextAlign = ContentAlignment.MiddleLeft;
            hotelName.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            hotelName.ForeColor = UIHelper.CustomerColor;
            hotelName.AutoEllipsis = true;

            banner.Controls.Add(hotelName);
            card.Controls.Add(banner);

            TableLayoutPanel body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.Padding = new Padding(12);
            body.ColumnCount = 2;
            body.RowCount = 7;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

            for (int i = 0; i < 7; i++)
            {
                body.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28F));
            }

            Label cityLabel = new Label();
            cityLabel.Text = city;
            cityLabel.AutoEllipsis = true;
            cityLabel.Dock = DockStyle.Fill;
            cityLabel.ForeColor = Color.DimGray;

            Label ratingLabel = new Label();
            ratingLabel.Text = rating;
            ratingLabel.Dock = DockStyle.Fill;
            ratingLabel.TextAlign = ContentAlignment.MiddleRight;
            ratingLabel.ForeColor = UIHelper.CustomerColor;
            ratingLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            body.Controls.Add(cityLabel, 0, 0);
            body.Controls.Add(ratingLabel, 1, 0);

            Label roomLabel = new Label();
            roomLabel.Text = roomType;
            roomLabel.AutoEllipsis = true;
            roomLabel.Dock = DockStyle.Fill;
            roomLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            roomLabel.ForeColor = UIHelper.NavyHeader;

            Label availableLabel = new Label();
            if (available > 0)
            {
                availableLabel.Text = available + " available";
                availableLabel.ForeColor = UIHelper.CustomerColor;
            }
            else
            {
                availableLabel.Text = "Sold out";
                availableLabel.ForeColor = Color.Firebrick;
            }
            availableLabel.Dock = DockStyle.Fill;
            availableLabel.TextAlign = ContentAlignment.MiddleRight;

            body.Controls.Add(roomLabel, 0, 1);
            body.Controls.Add(availableLabel, 1, 1);

            Label priceLabel = new Label();
            priceLabel.Text = "$" + price.ToString("N2") + " / night";
            priceLabel.Dock = DockStyle.Fill;
            priceLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            priceLabel.ForeColor = UIHelper.CustomerColor;
            body.Controls.Add(priceLabel, 0, 2);
            body.SetColumnSpan(priceLabel, 2);

            Label roomInfo = new Label();
            roomInfo.Text = row["RoomSize"] + " • " + row["BedType"] +
                " • Up to " + row["MaxGuests"] + " guests";
            roomInfo.Dock = DockStyle.Fill;
            roomInfo.AutoEllipsis = true;
            roomInfo.ForeColor = Color.DimGray;
            body.Controls.Add(roomInfo, 0, 3);
            body.SetColumnSpan(roomInfo, 2);

            Label amenitiesLabel = new Label();
            amenitiesLabel.Text = "Amenities: " + amenities;
            amenitiesLabel.Dock = DockStyle.Fill;
            amenitiesLabel.AutoEllipsis = true;
            amenitiesLabel.ForeColor = Color.DimGray;
            body.Controls.Add(amenitiesLabel, 0, 4);
            body.SetColumnSpan(amenitiesLabel, 2);

            Label dateInfo = new Label();
            dateInfo.Text = "Dates can be selected after opening the room details.";
            dateInfo.Dock = DockStyle.Fill;
            dateInfo.AutoEllipsis = true;
            dateInfo.ForeColor = Color.Gray;
            dateInfo.Font = new Font("Segoe UI", 8F);
            body.Controls.Add(dateInfo, 0, 5);
            body.SetColumnSpan(dateInfo, 2);

            Button viewButton;
            if (available > 0)
            {
                viewButton = UIHelper.MakeButton(
                    "View Details / Book Now",
                    UIHelper.CustomerColor,
                    190,
                    30);
            }
            else
            {
                viewButton = UIHelper.MakeButton(
                    "View Details",
                    UIHelper.CustomerColor,
                    190,
                    30);
            }

            viewButton.Anchor = AnchorStyles.Right;
            viewButton.Click += delegate
            {
                HotelDetailsForm details = new HotelDetailsForm(hotelId);
                details.ShowDialog(this);
            };

            body.Controls.Add(viewButton, 1, 6);
            card.Controls.Add(body);

            return card;
        }

        private Panel CreateEmptyState()
        {
            Panel panel = new Panel();
            panel.Width = 520;
            panel.Height = 150;
            panel.BackColor = Color.White;
            panel.Margin = new Padding(7);
            panel.Padding = new Padding(20);
            panel.BorderStyle = BorderStyle.FixedSingle;

            Label title = new Label();
            title.Text = "No properties match your filters";
            title.Dock = DockStyle.Top;
            title.Height = 32;
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.ForeColor = UIHelper.NavyHeader;

            Label message = new Label();
            message.Text = "Try another destination, room type, price range or rating.";
            message.Dock = DockStyle.Top;
            message.Height = 32;
            message.ForeColor = Color.DimGray;

            Button clear = UIHelper.MakeButton(
                "Clear Filters",
                UIHelper.CustomerColor,
                130,
                30);
            clear.Click += Reset_Click;

            panel.Controls.Add(clear);
            panel.Controls.Add(message);
            panel.Controls.Add(title);

            return panel;
        }

        private void ResizeCards()
        {
            if (hotelPanel == null)
            {
                return;
            }

            int width = Math.Max(300, hotelPanel.ClientSize.Width / 3 - 16);

            foreach (Control control in hotelPanel.Controls)
            {
                Panel panel = control as Panel;
                if (panel == null || panel.Height != 315)
                {
                    continue;
                }

                panel.Width = width;

                foreach (Control child in panel.Controls)
                {
                    TableLayoutPanel table = child as TableLayoutPanel;
                    if (table != null)
                    {
                        table.Width = width - 2;
                    }
                }
            }
        }

        private void CustomerHomeForm_Activated(object sender, EventArgs e)
        {
            RefreshDashboard();
        }

        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                CheckBookingNotifications();
                lblBookingCount.Text = GetCount(
                    "SELECT COUNT(*) FROM Bookings WHERE CustomerId=@Id " +
                    "AND Status IN ('Approved','Confirmed','Completed')");
            }
            catch
            {
                // Do not interrupt the customer if a notification check fails.
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (!loadingFilters)
            {
                LoadHotels();
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            LoadHotels();
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadHotels();
            }
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cboCity.SelectedIndex = 0;
            cboRoomType.SelectedIndex = 0;
            cboPrice.SelectedIndex = 0;
            cboStars.SelectedIndex = 0;
            LoadHotels();
        }

        private void MyCart_Click(object sender, EventArgs e)
        {
            BookingCartForm form = new BookingCartForm();
            form.Show();
        }

        private void MyBookings_Click(object sender, EventArgs e)
        {
            BookingHistoryForm form = new BookingHistoryForm();
            form.ShowDialog(this);
        }

        private void SpecialOffers_Click(object sender, EventArgs e)
        {
            SpecialOffersForm form = new SpecialOffersForm();
            form.Show();
        }

        private void UpdateProfile_Click(object sender, EventArgs e)
        {
            UpdateProfileForm form = new UpdateProfileForm();
            form.Show();
        }

        private void Logout_Click(object sender, EventArgs e)
        {
            Session.SignOut();
            LoginForm form = new LoginForm();
            form.Show();
            Close();
        }

        private void HotelPanel_Resize(object sender, EventArgs e)
        {
            ResizeCards();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (notificationTimer != null)
            {
                notificationTimer.Stop();
                notificationTimer.Dispose();
            }

            base.OnFormClosed(e);
        }
    }
}
