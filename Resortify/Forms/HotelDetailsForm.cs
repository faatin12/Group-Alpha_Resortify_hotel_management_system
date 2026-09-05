using System;
using System.Data;
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
        private NumericUpDown numQty, numGuests;
        private CheckedListBox lstServices;
        private Label lblError, lblHotelInfo, lblRoomInfo, lblNights, lblSummaryNights, lblRoomSubtotal, lblExtrasTotal, lblDiscount, lblGrandTotal, lblAvailability, lblGuestLabel;
        private DataGridView gridReviews;
        private DataTable roomTable;
        private decimal currentRoomPrice;
        private int currentAvailability;

        public HotelDetailsForm(int hotelId)
        {
            this.hotelId = hotelId;
            InitializeComponent();
            LoadHotel();
            LoadRooms();
            LoadServices();
            LoadReviews();
            Recalculate();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Room Details & Booking";
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;

            Controls.Add(UIHelper.BuildHeader("Room Details & Booking", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 18, 24, 20),
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.FromArgb(247, 249, 251)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 255));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var hotelCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), BorderStyle = BorderStyle.FixedSingle };
            lblHotelInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UIHelper.NavyHeader };
            hotelCard.Controls.Add(lblHotelInfo);
            main.Controls.Add(hotelCard, 0, 0);
            main.SetColumnSpan(hotelCard, 2);

            var roomCard = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), ColumnCount = 2, RowCount = 7, BorderStyle = BorderStyle.FixedSingle };
            roomCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            roomCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            for (int i = 0; i < 7; i++) roomCard.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 54 : 35));

            roomCard.Controls.Add(MakeLabel("Room type"), 0, 0);
            cboRoom = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRoom.SelectedIndexChanged += (s, e) => { LoadSelectedRoomInfo(); Recalculate(); };
            roomCard.Controls.Add(cboRoom, 1, 0);
            roomCard.Controls.Add(MakeLabel("Check-in"), 0, 1);
            dtCheckIn = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, MinDate = DateTime.Today, Value = DateTime.Today.AddDays(1) };
            dtCheckIn.ValueChanged += DatesChanged;
            roomCard.Controls.Add(dtCheckIn, 1, 1);
            roomCard.Controls.Add(MakeLabel("Check-out"), 0, 2);
            dtCheckOut = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, MinDate = DateTime.Today.AddDays(2), Value = DateTime.Today.AddDays(2) };
            dtCheckOut.ValueChanged += DatesChanged;
            roomCard.Controls.Add(dtCheckOut, 1, 2);
            roomCard.Controls.Add(MakeLabel("Number of rooms"), 0, 3);
            numQty = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 20, Value = 1 };
            numQty.ValueChanged += (s, e) => { UpdateGuestLimit(); Recalculate(); };
            roomCard.Controls.Add(numQty, 1, 3);
            lblGuestLabel = MakeLabel("Number of guests");
            roomCard.Controls.Add(lblGuestLabel, 0, 4);
            numGuests = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 8, Value = 1 };
            numGuests.ValueChanged += (s, e) => Recalculate();
            roomCard.Controls.Add(numGuests, 1, 4);
            lblAvailability = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = UIHelper.CustomerColor, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            roomCard.Controls.Add(lblAvailability, 0, 5);
            lblNights = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UIHelper.NavyHeader, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            roomCard.Controls.Add(lblNights, 1, 5);
            lblRoomInfo = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Color.DimGray };
            roomCard.Controls.Add(lblRoomInfo, 0, 6);
            roomCard.SetColumnSpan(lblRoomInfo, 2);
            main.Controls.Add(roomCard, 0, 1);

            var serviceCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14), BorderStyle = BorderStyle.FixedSingle };
            var serviceLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            serviceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            serviceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var serviceTitle = new Label { Text = "Stay Extras & Guest Services", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = UIHelper.NavyHeader, TextAlign = ContentAlignment.MiddleLeft };
            serviceLayout.Controls.Add(serviceTitle, 0, 0);
            lstServices = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, Font = new Font("Segoe UI", 9F) };
            lstServices.ItemCheck += (s, e) => BeginInvoke(new Action(Recalculate));
            serviceLayout.Controls.Add(lstServices, 0, 1);
            serviceCard.Controls.Add(serviceLayout);
            main.Controls.Add(serviceCard, 1, 1);

            var summaryCard = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), ColumnCount = 2, RowCount = 5, BorderStyle = BorderStyle.FixedSingle };
            summaryCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            summaryCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            for (int i = 0; i < 5; i++) summaryCard.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            summaryCard.Controls.Add(MakeLabel("Room subtotal"), 0, 0);
            lblRoomSubtotal = MakeAmountLabel(); summaryCard.Controls.Add(lblRoomSubtotal, 1, 0);
            summaryCard.Controls.Add(MakeLabel("Selected services"), 0, 1);
            lblExtrasTotal = MakeAmountLabel(); summaryCard.Controls.Add(lblExtrasTotal, 1, 1);
            summaryCard.Controls.Add(MakeLabel("Nights / rooms"), 0, 2);
            lblSummaryNights = MakeAmountLabel(); summaryCard.Controls.Add(lblSummaryNights, 1, 2);
            summaryCard.Controls.Add(MakeLabel("Discount / offer"), 0, 3);
            lblDiscount = MakeAmountLabel(); summaryCard.Controls.Add(lblDiscount, 1, 3);
            summaryCard.Controls.Add(MakeLabel("Grand total"), 0, 4);
            lblGrandTotal = MakeAmountLabel(true); summaryCard.Controls.Add(lblGrandTotal, 1, 4);
            main.Controls.Add(summaryCard, 0, 2);

            var actionCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16), BorderStyle = BorderStyle.FixedSingle };
            var actionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            var info = new Label { Text = "Review the room, dates, guests and services before adding this stay to your cart.", AutoSize = false, Width = 390, Height = 55, ForeColor = Color.DimGray };
            actionLayout.Controls.Add(info);
            lblError = UIHelper.MakeErrorLabel(); lblError.AutoSize = false; lblError.Width = 390; lblError.Height = 42; actionLayout.Controls.Add(lblError);
            var btnAdd = UIHelper.MakeButton("Add Booking to Cart", UIHelper.CustomerColor, 260, 42); btnAdd.Margin = new Padding(0, 4, 0, 0); btnAdd.Click += BtnAddToCart_Click; actionLayout.Controls.Add(btnAdd);
            actionCard.Controls.Add(actionLayout);
            main.Controls.Add(actionCard, 1, 2);

            var reviewCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), BorderStyle = BorderStyle.FixedSingle };
            var reviewTitle = new Label { Text = "Guest Reviews", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = UIHelper.NavyHeader };
            gridReviews = new DataGridView { Dock = DockStyle.Fill };
            UIHelper.StyleGrid(gridReviews);
            reviewCard.Controls.Add(gridReviews); reviewCard.Controls.Add(reviewTitle);
            main.Controls.Add(reviewCard, 0, 3); main.SetColumnSpan(reviewCard, 2);

            Controls.Add(main);
        }

        private Label MakeLabel(string text) => new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(70, 70, 70) };
        private Label MakeAmountLabel(bool strong = false) => new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", strong ? 13F : 10F, FontStyle.Bold), ForeColor = UIHelper.CustomerColor };

        private void LoadHotel()
        {
            var table = DbHelper.GetDataTable("SELECT HotelName, Category, City, Address, StarRating FROM Hotels WHERE HotelId=@Id", new SqlParameter("@Id", hotelId));
            if (table.Rows.Count == 0) { lblHotelInfo.Text = "Property not found."; return; }
            var row = table.Rows[0];
            string stars = row["StarRating"] == DBNull.Value ? "New property" : $"{Convert.ToDecimal(row["StarRating"]):N1} ★";
            lblHotelInfo.Text = $"{row["HotelName"]}  •  {row["Category"]}\n{row["City"]}  •  {row["Address"]}  •  {stars}";
        }

        private void LoadRooms()
        {
            roomTable = DbHelper.GetDataTable(
                @"SELECT r.RoomId, r.RoomType, r.PricePerNight, r.TotalRooms, r.MinAvailability,
                         ISNULL(r.RoomSize,'Not specified') AS RoomSize, ISNULL(r.BedType,'Not specified') AS BedType,
                         ISNULL(r.MaxGuests,2) AS MaxGuests, ISNULL(r.Amenities,'') AS Amenities, ISNULL(r.Description,'') AS Description
                  FROM Rooms r WHERE r.HotelId=@Id ORDER BY r.PricePerNight, r.RoomType", new SqlParameter("@Id", hotelId));
            if (!roomTable.Columns.Contains("Display")) roomTable.Columns.Add("Display", typeof(string));
            foreach (DataRow row in roomTable.Rows)
                row["Display"] = $"{row["RoomType"]}  —  ${Convert.ToDecimal(row["PricePerNight"]):N2}/night";
            cboRoom.DisplayMember = "Display"; cboRoom.ValueMember = "RoomId"; cboRoom.DataSource = roomTable;
        }

        private void LoadServices()
        {
            var table = DbHelper.GetDataTable("SELECT ServiceId, ServiceName, Price, IsFree FROM ServiceCatalog WHERE Active=1 ORDER BY IsFree DESC, ServiceName");
            lstServices.Items.Clear();
            foreach (DataRow row in table.Rows)
            {
                decimal price = Convert.ToDecimal(row["Price"]);
                bool free = Convert.ToBoolean(row["IsFree"]);
                lstServices.Items.Add(new ServiceChoice(Convert.ToInt32(row["ServiceId"]), row["ServiceName"].ToString(), free ? 0m : price, free), false);
            }
        }

        private void LoadReviews()
        {
            gridReviews.DataSource = DbHelper.GetDataTable(
                @"SELECT u.FullName AS Reviewer, rv.Rating, rv.Comment, rv.ReviewDate
                  FROM Reviews rv JOIN Users u ON u.UserId=rv.CustomerId WHERE rv.HotelId=@Id ORDER BY rv.ReviewDate DESC", new SqlParameter("@Id", hotelId));
        }

        private void LoadSelectedRoomInfo()
        {
            if (cboRoom.SelectedValue == null || roomTable == null) return;
            int id = Convert.ToInt32(cboRoom.SelectedValue);
            DataRow[] rows = roomTable.Select("RoomId=" + id);
            if (rows.Length == 0) return;
            DataRow r = rows[0];
            currentRoomPrice = Convert.ToDecimal(r["PricePerNight"]);
            lblRoomInfo.Text = $"{r["RoomSize"]}  •  {r["BedType"]}  •  Up to {r["MaxGuests"]} guests\nAmenities: {r["Amenities"]}  •  {r["Description"]}";
            UpdateGuestLimit();
            UpdateAvailability();
        }


        private void UpdateGuestLimit()
        {
            if (cboRoom == null || numGuests == null || cboRoom.SelectedValue == null || roomTable == null) return;
            int id = Convert.ToInt32(cboRoom.SelectedValue);
            DataRow[] rows = roomTable.Select("RoomId=" + id);
            if (rows.Length == 0) return;
            int guestsPerRoom = Math.Max(1, Convert.ToInt32(rows[0]["MaxGuests"]));
            int rooms = Math.Max(1, (int)numQty.Value);
            int maxGuests = Math.Min(50, guestsPerRoom * rooms);
            numGuests.Maximum = maxGuests;
            if (numGuests.Value > maxGuests) numGuests.Value = maxGuests;
            if (lblGuestLabel != null)
                lblGuestLabel.Text = $"Number of guests (max {guestsPerRoom} / room)";
        }

        private void DatesChanged(object sender, EventArgs e)
        {
            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date) dtCheckOut.Value = dtCheckIn.Value.Date.AddDays(1);
            dtCheckOut.MinDate = dtCheckIn.Value.Date.AddDays(1);
            UpdateAvailability(); Recalculate();
        }

        private void UpdateAvailability()
        {
            if (cboRoom.SelectedValue == null) return;
            int roomId = Convert.ToInt32(cboRoom.SelectedValue);
            currentAvailability = Convert.ToInt32(DbHelper.ExecuteScalar(
                @"SELECT r.TotalRooms - ISNULL((SELECT ISNULL(SUM(bi.Quantity),0) FROM BookingItems bi JOIN Bookings b ON b.BookingId=bi.BookingId
                  WHERE bi.RoomId=r.RoomId AND b.Status IN ('Pending','Approved','Confirmed','Completed')
                  AND bi.CheckInDate < @Out AND bi.CheckOutDate > @In),0) FROM Rooms r WHERE r.RoomId=@Room",
                new SqlParameter("@Out", dtCheckOut.Value.Date), new SqlParameter("@In", dtCheckIn.Value.Date), new SqlParameter("@Room", roomId)) ?? 0);
            lblAvailability.Text = currentAvailability > 0 ? $"Available: {currentAvailability} room(s)" : "Sold out for these dates";
            lblAvailability.ForeColor = currentAvailability > 0 ? UIHelper.CustomerColor : Color.Firebrick;
            if (numQty.Value > Math.Max(1, currentAvailability)) numQty.Value = Math.Max(1, currentAvailability);
        }

        private void Recalculate()
        {
            if (lblRoomSubtotal == null || cboRoom.SelectedValue == null) return;
            int nights = Math.Max(1, (dtCheckOut.Value.Date - dtCheckIn.Value.Date).Days);
            int qty = (int)numQty.Value;
            decimal roomSubtotal = currentRoomPrice * nights * qty;
            decimal extras = 0m;
            foreach (var item in lstServices.CheckedItems) extras += ((ServiceChoice)item).Price;
            decimal discountPercent = GetCurrentDiscountPercent();
            decimal discount = Math.Round(roomSubtotal * discountPercent / 100m, 2);
            decimal grand = Math.Max(0m, roomSubtotal + extras - discount);
            lblNights.Text = $"{nights} night(s)  •  {qty} room(s)";
            if (lblSummaryNights != null) lblSummaryNights.Text = lblNights.Text;
            lblRoomSubtotal.Text = $"${roomSubtotal:N2}";
            lblExtrasTotal.Text = $"${extras:N2}";
            if (lblDiscount != null) lblDiscount.Text = discount > 0 ? $"-${discount:N2} ({discountPercent:0.#}%)" : "$0.00";
            lblGrandTotal.Text = $"${grand:N2}";
        }

        private decimal GetCurrentDiscountPercent()
        {
            if (cboRoom.SelectedValue == null) return 0m;
            object value = DbHelper.ExecuteScalar(@"SELECT ISNULL(MAX(DiscountPercent),0) FROM Offers WHERE RoomId=@Room AND @In BETWEEN StartDate AND EndDate AND @Out <= DATEADD(DAY,1,EndDate)",
                new SqlParameter("@Room", Convert.ToInt32(cboRoom.SelectedValue)), new SqlParameter("@In", dtCheckIn.Value.Date), new SqlParameter("@Out", dtCheckOut.Value.Date));
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private void BtnAddToCart_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;

            if (cboRoom.SelectedValue == null)
            {
                ShowError("Select a room type.");
                return;
            }

            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date)
            {
                ShowError("Check-out must be after check-in.");
                return;
            }

            if (currentAvailability < numQty.Value)
            {
                ShowError("Not enough rooms are available for these dates.");
                return;
            }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);

            int otherHotelItems = Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Cart c JOIN Rooms r ON r.RoomId=c.RoomId " +
                "WHERE c.CustomerId=@Cust AND r.HotelId<>@Hotel",
                new SqlParameter("@Cust", Session.UserId),
                new SqlParameter("@Hotel", hotelId)) ?? 0);

            if (otherHotelItems > 0)
            {
                DialogResult clear = MessageBox.Show(
                    "Your cart contains a room from another property.\n\n" +
                    "Clear the old cart and add this property instead?",
                    "Change Property",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (clear != DialogResult.Yes)
                {
                    return;
                }

                DbHelper.ExecuteNonQuery(
                    "DELETE FROM Cart WHERE CustomerId=@Cust",
                    new SqlParameter("@Cust", Session.UserId));
            }

            int maxGuests = Convert.ToInt32(
                DbHelper.ExecuteScalar(
                    "SELECT ISNULL(MaxGuests,2) FROM Rooms WHERE RoomId=@Id",
                    new SqlParameter("@Id", roomId)) ?? 2);

            if (numGuests.Value > maxGuests * numQty.Value)
            {
                ShowError("Too many guests for the selected rooms.");
                return;
            }

            int nights = (dtCheckOut.Value.Date - dtCheckIn.Value.Date).Days;
            int rooms = (int)numQty.Value;

            decimal roomSubtotal = currentRoomPrice * nights * rooms;
            decimal discountPercent = GetCurrentDiscountPercent();
            decimal discount = Math.Round(roomSubtotal * discountPercent / 100m, 2);

            int cartId = Convert.ToInt32(
                DbHelper.ExecuteScalar(
                    @"INSERT INTO Cart
                      (CustomerId, RoomId, CheckInDate, CheckOutDate, Quantity, Guests, DiscountAmount)
                      OUTPUT INSERTED.CartId
                      VALUES (@Cust, @Room, @In, @Out, @Qty, @Guests, @Discount)",
                    new SqlParameter("@Cust", Session.UserId),
                    new SqlParameter("@Room", roomId),
                    new SqlParameter("@In", dtCheckIn.Value.Date),
                    new SqlParameter("@Out", dtCheckOut.Value.Date),
                    new SqlParameter("@Qty", rooms),
                    new SqlParameter("@Guests", (int)numGuests.Value),
                    new SqlParameter("@Discount", discount)));

            foreach (ServiceChoice service in lstServices.CheckedItems)
            {
                DbHelper.ExecuteNonQuery(
                    "INSERT INTO CartServices (CartId, ServiceId, Quantity, UnitPrice) " +
                    "VALUES (@Cart, @Service, 1, @Price)",
                    new SqlParameter("@Cart", cartId),
                    new SqlParameter("@Service", service.ServiceId),
                    new SqlParameter("@Price", service.Price));
            }

            // Show the success message first. After the user clicks OK, open My Cart.
            MessageBox.Show(
                "Your stay and selected services were added to My Cart.",
                "Added to Cart",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            BookingCartForm cartForm = new BookingCartForm();
            cartForm.ShowDialog(this);

            Close();
        }

        private void ShowError(string message) { lblError.Text = message; lblError.Visible = true; }

        private sealed class ServiceChoice
        {
            public int ServiceId { get; }
            public string Name { get; }
            public decimal Price { get; }
            public bool IsFree { get; }
            public ServiceChoice(int id, string name, decimal price, bool free) { ServiceId = id; Name = name; Price = price; IsFree = free; }
            public override string ToString() => IsFree ? $"{Name} — Included / Free" : $"{Name} — +${Price:N2}";
        }
    }
}
