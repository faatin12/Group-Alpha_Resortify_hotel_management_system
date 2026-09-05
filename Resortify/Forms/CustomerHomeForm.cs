using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;
namespace Resortify.Forms
{
    public partial class CustomerHomeForm : Form
    {
        private TextBox txtSearch;
        private ComboBox cboPrice, cboStars, cboCity, cboRoomType;
        private FlowLayoutPanel hotelPanel;
        private Label lblResultCount, lblWelcome, lblCartCount, lblBookingCount, lblOfferCount;
        private bool initializing = true;
        private System.Windows.Forms.Timer notificationTimer;
        private readonly string[] destinations = {
            "Any location","Dhaka","Cox's Bazar","St. Martin's","Sylhet","Bandarban","Rangamati"
        }
        ;
        private readonly string[] roomTypes = {
            "All room types","Standard Room","Standard Twin","Superior Room","Deluxe Room","Deluxe Sea View","Super Deluxe Room","Executive Room","Junior Suite","Family Suite","Executive Suite","Presidential Suite","Cottage","Villa"
        }
        ;
        public CustomerHomeForm()
        {
            InitializeComponent();
            LoadFilters();
            initializing = false;
            Activated += (s, e) => RefreshDashboard();
            notificationTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            }
            ;
            notificationTimer.Tick += (s, e) => PollBookingNotifications();
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
            Controls.Add(UIHelper.BuildHeader("Customer Dashboard", UIHelper.CustomerColor, null, (s, e) => {
                Session.SignOut(); new LoginForm().Show(); Close();
            }
            ));
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 24),
                ColumnCount = 1,
                RowCount = 4
            }
            ;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 195));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            }
            ;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            string displayName = string.IsNullOrWhiteSpace(Session.FullName) ? "Guest" : Session.FullName.Trim();
            string firstName = displayName.Split(new[] {
                ' '
            }
            , StringSplitOptions.RemoveEmptyEntries)[0];
            string greeting = DateTime.Now.Hour < 12 ? "Good morning" : DateTime.Now.Hour < 17 ? "Good afternoon" : "Good evening";
            var welcomeBox = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 12, 0)
            }
            ;
            lblWelcome = new Label
            {
                Text = $"{greeting}, {firstName}!",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader
            }
            ;
            welcomeBox.Controls.Add(lblWelcome);
            welcomeBox.Controls.Add(new Label
            {
                Text = $"Welcome to Resortify, {firstName}. We hope you find the perfect stay for your next getaway.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                AutoEllipsis = true
            }
            );
            top.Controls.Add(welcomeBox, 0, 0);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 5, 0, 0)
            }
            ;
            AddAction(actions, "Update Profile", Color.FromArgb(80, 80, 80), () => new UpdateProfileForm().Show());
            AddAction(actions, "Special Offers", UIHelper.CustomerColor, () => new SpecialOffersForm().Show());
            AddAction(actions, "My Cart", UIHelper.CustomerColor, () => new BookingCartForm().Show());
            top.Controls.Add(actions, 1, 0);
            var bookingAction = UIHelper.MakeButton("My Bookings  •  View all reservations", UIHelper.NavyHeader, 245, 32);
            bookingAction.Margin = new Padding(0, 2, 0, 2);
            bookingAction.Click += (s, e) => new BookingHistoryForm().ShowDialog(this);
            top.Controls.Add(bookingAction, 0, 1);
            top.SetColumnSpan(bookingAction, 2);
            var stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            }
            ;
            for (int i = 0; i < 3; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            stats.Controls.Add(Stat("MY CART", "0", out lblCartCount), 0, 0);
            stats.Controls.Add(Stat("UPCOMING BOOKINGS", "0", out lblBookingCount), 1, 0);
            stats.Controls.Add(Stat("ACTIVE OFFERS", "0", out lblOfferCount), 2, 0);
            top.Controls.Add(stats, 0, 2);
            top.SetColumnSpan(stats, 2);
            root.Controls.Add(top, 0, 0);
            var filter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12),
                ColumnCount = 6,
                RowCount = 2,
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            float[] widths = {
                28,17,15,15,15,10
            }
            ;
            for (int i = 0; i < 6; i++) filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, widths[i]));
            AddFilter(filter, "Search hotel / city", txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Hotel name or city"
            }
            , 0);
            AddFilter(filter, "Location", cboCity = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            }
            , 1);
            AddFilter(filter, "Room type", cboRoomType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            }
            , 2);
            AddFilter(filter, "Price", cboPrice = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            }
            , 3);
            AddFilter(filter, "Rating", cboStars = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            }
            , 4);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false
            }
            ;
            var search = UIHelper.MakeButton("Search", UIHelper.CustomerColor, 82, 30);
            search.Click += (s, e) => LoadHotels();
            var reset = UIHelper.MakeButton("Reset", Color.FromArgb(95, 95, 95), 72, 30);
            reset.Click += (s, e) => ResetFilters(null, null);
            buttons.Controls.Add(search);
            buttons.Controls.Add(reset);
            filter.Controls.Add(buttons, 5, 1);
            root.Controls.Add(filter, 0, 1);
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    LoadHotels();
                }
            }
            ;
            cboCity.SelectedIndexChanged += FilterChanged;
            cboRoomType.SelectedIndexChanged += FilterChanged;
            cboPrice.SelectedIndexChanged += FilterChanged;
            cboStars.SelectedIndexChanged += FilterChanged;
            var heading = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            }
            ;
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            heading.Controls.Add(new Label
            {
                Text = "Available stays",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader,
                TextAlign = ContentAlignment.MiddleLeft
            }
            , 0, 0);
            lblResultCount = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray
            }
            ;
            heading.Controls.Add(lblResultCount, 1, 0);
            root.Controls.Add(heading, 0, 2);
            hotelPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4),
                BackColor = Color.Transparent
            }
            ;
            hotelPanel.Resize += (s, e) => ResizeCards();
            root.Controls.Add(hotelPanel, 0, 3);
            Controls.Add(root);
        }
        private void LoadFilters()
        {
            cboCity.Items.AddRange(destinations);
            cboCity.SelectedIndex = 0;
            cboRoomType.Items.AddRange(roomTypes);
            cboRoomType.SelectedIndex = 0;
            cboPrice.Items.AddRange(new object[] {
                "Any price","Under $75","$75 - $150","$150 - $250","Over $250"
            }
            );
            cboPrice.SelectedIndex = 0;
            cboStars.Items.AddRange(new object[] {
                "Any rating","5-star hotels","4+ stars","3+ stars"
            }
            );
            cboStars.SelectedIndex = 0;
        }
        private void AddFilter(TableLayoutPanel p, string label, Control input, int col)
        {
            p.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 70)
            }
            , col, 0);
            p.Controls.Add(input, col, 1);
        }
        private void AddAction(FlowLayoutPanel p, string text, Color color, Action action)
        {
            var b = UIHelper.MakeButton(text, color, 108, 32);
            b.Margin = new Padding(4, 0, 0, 0);
            b.Click += (s, e) => action();
            p.Controls.Add(b);
        }
        private Panel Stat(string title, string value, out Label valueLabel)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            p.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(10, 6, 0, 0),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.DimGray
            }
            );
            valueLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = UIHelper.CustomerColor
            }
            ;
            p.Controls.Add(valueLabel);
            return p;
        }
        private void RefreshDashboard()
        {
            try
            {
                lblCartCount.Text = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Cart WHERE CustomerId=@Id", new SqlParameter("@Id", Session.UserId))).ToString();
                lblBookingCount.Text = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Bookings WHERE CustomerId=@Id AND Status IN ('Approved','Confirmed','Completed')", new SqlParameter("@Id", Session.UserId))).ToString();
                CheckBookingNotifications();
                lblOfferCount.Text = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Offers o JOIN Rooms r ON r.RoomId=o.RoomId JOIN Hotels h ON h.HotelId=r.HotelId WHERE h.Status='Approved' AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate")).ToString();
                LoadHotels();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load dashboard data.\n\n" + ex.Message, "Resortify", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void PollBookingNotifications()
        {
            try
            {
                CheckBookingNotifications();
                lblBookingCount.Text = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Bookings WHERE CustomerId=@Id AND Status IN ('Approved','Confirmed','Completed')", new SqlParameter("@Id", Session.UserId))).ToString();
            }
            catch
            {
            }
        }
        private void CheckBookingNotifications()
        {
            var table = DbHelper.GetDataTable(@"SELECT TOP 10 BookingId,Status,TotalAmount FROM Bookings WHERE CustomerId=@Id AND Status IN('Approved','Rejected') AND CustomerNotificationShown=0 ORDER BY BookingDate", new SqlParameter("@Id", Session.UserId));
            foreach (DataRow row in table.Rows)
            {
                int id = Convert.ToInt32(row["BookingId"]);
                string status = row["Status"].ToString();
                decimal total = Convert.ToDecimal(row["TotalAmount"]);
                string message = status == "Approved"
                ? $"Great news! Your booking #{id} has been approved after transaction validation.\n\nTotal: ${total:N2}\n\nYour reservation is now confirmed. A confirmation email will be sent to your registered email."
                : $"Your booking #{id} was not approved by the hotel admin.\n\nTotal requested: ${total:N2}\n\nPlease check My Bookings for the latest status.";
                MessageBox.Show(message, status == "Approved" ? "Booking Confirmed" : "Booking Update", MessageBoxButtons.OK, status == "Approved" ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                DbHelper.ExecuteNonQuery("UPDATE Bookings SET CustomerNotificationShown=1 WHERE BookingId=@Id AND CustomerId=@Cust", new SqlParameter("@Id", id), new SqlParameter("@Cust", Session.UserId));
            }
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
        private void FilterChanged(object sender, EventArgs e)
        {
            if (!initializing) LoadHotels();
        }
        private void ResetFilters(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cboCity.SelectedIndex = 0;
            cboRoomType.SelectedIndex = 0;
            cboPrice.SelectedIndex = 0;
            cboStars.SelectedIndex = 0;
            LoadHotels();
        }
        private void LoadHotels()
        {
            hotelPanel.SuspendLayout();
            hotelPanel.Controls.Clear();
            string sql = @"SELECT h.HotelId,h.HotelName,h.City,h.StarRating,r.RoomId,r.RoomType,r.PricePerNight,r.TotalRooms,ISNULL(r.RoomSize,'') RoomSize,ISNULL(r.BedType,'') BedType,ISNULL(r.MaxGuests,2) MaxGuests,ISNULL(r.Amenities,'') Amenities,
                r.TotalRooms-ISNULL((SELECT ISNULL(SUM(bi.Quantity),0) FROM BookingItems bi JOIN Bookings b ON b.BookingId=bi.BookingId WHERE bi.RoomId=r.RoomId AND b.Status IN('Pending','Approved','Confirmed','Completed') AND bi.CheckInDate<@Out AND bi.CheckOutDate>@In),0) Available
                FROM Hotels h CROSS APPLY(SELECT TOP 1 * FROM Rooms rr WHERE rr.HotelId=h.HotelId
                    AND (@Type IS NULL OR rr.RoomType=@Type) ORDER BY rr.PricePerNight,rr.RoomId) r WHERE h.Status='Approved'";
            var p = new System.Collections.Generic.List<SqlParameter> {
                new SqlParameter("@In",DateTime.Today.AddDays(1)),new SqlParameter("@Out",DateTime.Today.AddDays(2)),new SqlParameter("@Type",cboRoomType.SelectedIndex>0?(object)cboRoomType.SelectedItem.ToString():DBNull.Value)
            }
            ;
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                sql += " AND (h.HotelName LIKE @S OR h.City LIKE @S OR h.Address LIKE @S)";
                p.Add(new SqlParameter("@S", "%" + txtSearch.Text.Trim() + "%"));
            }
            if (cboCity.SelectedIndex > 0)
            {
                sql += " AND h.City=@City";
                p.Add(new SqlParameter("@City", cboCity.SelectedItem.ToString()));
            }
            if (cboStars.SelectedIndex == 1) sql += " AND h.StarRating>=5";
            else if (cboStars.SelectedIndex == 2) sql += " AND h.StarRating>=4";
            else if (cboStars.SelectedIndex == 3) sql += " AND h.StarRating>=3";
            if (cboPrice.SelectedIndex == 1) sql += " AND r.PricePerNight<75";
            else if (cboPrice.SelectedIndex == 2) sql += " AND r.PricePerNight BETWEEN 75 AND 150";
            else if (cboPrice.SelectedIndex == 3) sql += " AND r.PricePerNight>150 AND r.PricePerNight<=250";
            else if (cboPrice.SelectedIndex == 4) sql += " AND r.PricePerNight>250";
            sql += " ORDER BY ISNULL(h.StarRating,0) DESC,r.PricePerNight ASC";
            DataTable table = DbHelper.GetDataTable(sql, p.ToArray());
            lblResultCount.Text = table.Rows.Count + " propert" + (table.Rows.Count == 1 ? "y" : "ies") + " found";
            foreach (DataRow row in table.Rows) hotelPanel.Controls.Add(BuildHotelCard(row));
            if (table.Rows.Count == 0) hotelPanel.Controls.Add(EmptyState());
            ResizeCards();
            hotelPanel.ResumeLayout();
        }
        private Panel BuildHotelCard(DataRow row)
        {
            int hotelId = Convert.ToInt32(row["HotelId"]);
            string name = row["HotelName"].ToString(), city = row["City"].ToString(), type = row["RoomType"].ToString(), amenities = row["Amenities"].ToString();
            decimal price = Convert.ToDecimal(row["PricePerNight"]);
            int available = Math.Max(0, Convert.ToInt32(row["Available"]));
            string rating = row["StarRating"] == DBNull.Value ? "New" : Convert.ToDecimal(row["StarRating"]).ToString("0.0") + " ★";
            var card = new Panel
            {
                Width = 350,
                Height = 315,
                BackColor = Color.White,
                Margin = new Padding(7),
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(226, 238, 232),
                Padding = new Padding(14, 8, 12, 8)
            }
            ;
            var hotelNameLabel = new Label
            {
                Text = name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UIHelper.CustomerColor,
                AutoEllipsis = true
            }
            ;
            banner.Controls.Add(hotelNameLabel);
            card.Controls.Add(banner);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            }
            ;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            for (int i = 0; i < 7; i++) body.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28f));
            body.Controls.Add(new Label
            {
                Text = city,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            }
            , 0, 0);
            body.Controls.Add(new Label
            {
                Text = rating,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UIHelper.CustomerColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            }
            , 1, 0);
            body.Controls.Add(new Label
            {
                Text = type,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader
            }
            , 0, 1);
            body.Controls.Add(new Label
            {
                Text = available > 0 ? available + " available" : "Sold out",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = available > 0 ? UIHelper.CustomerColor : Color.Firebrick
            }
            , 1, 1);
            body.Controls.Add(new Label
            {
                Text = $"${price:N2} / night",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.CustomerColor
            }
            , 0, 2);
            body.SetColumnSpan(body.GetControlFromPosition(0, 2), 2);
            body.Controls.Add(new Label
            {
                Text = $"{row["RoomSize"]} • {row["BedType"]} • Up to {row["MaxGuests"]} guests",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.DimGray
            }
            , 0, 3);
            body.SetColumnSpan(body.GetControlFromPosition(0, 3), 2);
            body.Controls.Add(new Label
            {
                Text = "Amenities: " + amenities,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.DimGray
            }
            , 0, 4);
            body.SetColumnSpan(body.GetControlFromPosition(0, 4), 2);
            body.Controls.Add(new Label
            {
                Text = "Dates can be selected after opening the room details.",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            }
            , 0, 5);
            body.SetColumnSpan(body.GetControlFromPosition(0, 5), 2);
            var btn = UIHelper.MakeButton(available > 0 ? "View Details / Book Now" : "View Details", UIHelper.CustomerColor, 190, 30);
            btn.Anchor = AnchorStyles.Right;
            btn.Click += (s, e) => new HotelDetailsForm(hotelId).ShowDialog(this);
            body.Controls.Add(btn, 1, 6);
            card.Controls.Add(body);
            return card;
        }
        private Panel EmptyState()
        {
            var p = new Panel
            {
                Width = 520,
                Height = 150,
                BackColor = Color.White,
                Margin = new Padding(7),
                Padding = new Padding(20),
                BorderStyle = BorderStyle.FixedSingle
            }
            ;
            p.Controls.Add(new Label
            {
                Text = "No properties match your filters",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = UIHelper.NavyHeader
            }
            );
            p.Controls.Add(new Label
            {
                Text = "Try another destination, room type, price range or rating.",
                Dock = DockStyle.Top,
                Height = 32,
                ForeColor = Color.DimGray
            }
            );
            var b = UIHelper.MakeButton("Clear Filters", UIHelper.CustomerColor, 130, 30);
            b.Click += (s, e) => ResetFilters(s, e);
            p.Controls.Add(b);
            return p;
        }
        private void ResizeCards()
        {
            if (hotelPanel == null) return;
            int width = Math.Max(300, hotelPanel.ClientSize.Width / 3 - 16);
            foreach (Control c in hotelPanel.Controls) if (c is Panel p && p.Height == 315)
            {
                p.Width = width;
                foreach (Control child in p.Controls) if (child is TableLayoutPanel t) t.Width = width - 2;
            }
        }
    }
}
