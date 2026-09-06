using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Resortify.Forms
{
    // Shows one hotel and lets the customer select a room and extra services.
    public partial class HotelDetailsForm : Form
    {
        private int hotelId;

        private ComboBox cboRoom;
        private DateTimePicker dtCheckIn;
        private DateTimePicker dtCheckOut;
        private NumericUpDown numRooms;
        private NumericUpDown numGuests;
        private CheckedListBox serviceList;

        private Label lblHotelInfo;
        private Label lblRoomInfo;
        private Label lblAvailability;
        private Label lblGuestLimit;
        private Label lblNights;
        private Label lblRoomTotal;
        private Label lblServiceTotal;
        private Label lblDiscount;
        private Label lblGrandTotal;
        private Label lblSummaryNights;
        private Label lblError;

        private DataGridView reviewGrid;
        private Label lblReviewSummary;
        private DataTable roomTable;

        private decimal roomPrice;
        private int availableRooms;

        public HotelDetailsForm(int hotelId)
        {
            this.hotelId = hotelId;

            InitializeComponent();
            LoadHotel();
            LoadRooms();
            LoadServices();
            LoadReviews();
            UpdateRoomInfo();
            CalculatePrice();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Room Details & Booking";
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 251);
            Font = UIHelper.BaseFont;

            Controls.Add(UIHelper.BuildHeader(
                "Room Details & Booking",
                UIHelper.CustomerColor,
                Back_Click,
                null));

            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.Padding = new Padding(24, 18, 24, 20);
            main.ColumnCount = 2;
            main.RowCount = 4;
            main.BackColor = Color.FromArgb(247, 249, 251);

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 255));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Hotel information
            Panel hotelCard = new Panel();
            hotelCard.Dock = DockStyle.Fill;
            hotelCard.BackColor = Color.White;
            hotelCard.Padding = new Padding(16);
            hotelCard.BorderStyle = BorderStyle.FixedSingle;

            lblHotelInfo = new Label();
            lblHotelInfo.Dock = DockStyle.Fill;
            lblHotelInfo.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblHotelInfo.ForeColor = UIHelper.NavyHeader;
            hotelCard.Controls.Add(lblHotelInfo);

            main.Controls.Add(hotelCard, 0, 0);
            main.SetColumnSpan(hotelCard, 2);

            // Room selection card
            TableLayoutPanel roomCard = new TableLayoutPanel();
            roomCard.Dock = DockStyle.Fill;
            roomCard.BackColor = Color.White;
            roomCard.Padding = new Padding(16);
            roomCard.ColumnCount = 2;
            roomCard.RowCount = 7;
            roomCard.BorderStyle = BorderStyle.FixedSingle;
            roomCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            roomCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));

            for (int i = 0; i < 7; i++)
            {
                if (i == 5)
                {
                    roomCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                }
                else
                {
                    roomCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
                }
            }

            roomCard.Controls.Add(MakeLabel("Room type"), 0, 0);

            cboRoom = new ComboBox();
            cboRoom.Dock = DockStyle.Fill;
            cboRoom.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRoom.SelectedIndexChanged += RoomChanged;
            roomCard.Controls.Add(cboRoom, 1, 0);

            roomCard.Controls.Add(MakeLabel("Check-in"), 0, 1);

            dtCheckIn = new DateTimePicker();
            dtCheckIn.Dock = DockStyle.Fill;
            dtCheckIn.Format = DateTimePickerFormat.Short;
            dtCheckIn.MinDate = DateTime.Today;
            dtCheckIn.Value = DateTime.Today.AddDays(1);
            dtCheckIn.ValueChanged += DateChanged;
            roomCard.Controls.Add(dtCheckIn, 1, 1);

            roomCard.Controls.Add(MakeLabel("Check-out"), 0, 2);

            dtCheckOut = new DateTimePicker();
            dtCheckOut.Dock = DockStyle.Fill;
            dtCheckOut.Format = DateTimePickerFormat.Short;
            dtCheckOut.MinDate = DateTime.Today.AddDays(2);
            dtCheckOut.Value = DateTime.Today.AddDays(2);
            dtCheckOut.ValueChanged += DateChanged;
            roomCard.Controls.Add(dtCheckOut, 1, 2);

            roomCard.Controls.Add(MakeLabel("Number of rooms"), 0, 3);

            numRooms = new NumericUpDown();
            numRooms.Dock = DockStyle.Fill;
            numRooms.Minimum = 1;
            numRooms.Maximum = 20;
            numRooms.Value = 1;
            numRooms.ValueChanged += RoomsChanged;
            roomCard.Controls.Add(numRooms, 1, 3);

            lblGuestLimit = MakeLabel("Number of guests");
            roomCard.Controls.Add(lblGuestLimit, 0, 4);

            numGuests = new NumericUpDown();
            numGuests.Dock = DockStyle.Fill;
            numGuests.Minimum = 1;
            numGuests.Maximum = 8;
            numGuests.Value = 1;
            numGuests.ValueChanged += GuestsChanged;
            roomCard.Controls.Add(numGuests, 1, 4);

            lblAvailability = new Label();
            lblAvailability.Dock = DockStyle.Fill;
            lblAvailability.AutoEllipsis = true;
            lblAvailability.ForeColor = UIHelper.CustomerColor;
            lblAvailability.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            roomCard.Controls.Add(lblAvailability, 0, 5);

            lblNights = new Label();
            lblNights.Dock = DockStyle.Fill;
            lblNights.ForeColor = UIHelper.NavyHeader;
            lblNights.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            roomCard.Controls.Add(lblNights, 1, 5);

            lblRoomInfo = new Label();
            lblRoomInfo.Dock = DockStyle.Fill;
            lblRoomInfo.AutoEllipsis = true;
            lblRoomInfo.ForeColor = Color.DimGray;
            roomCard.Controls.Add(lblRoomInfo, 0, 6);
            roomCard.SetColumnSpan(lblRoomInfo, 2);

            main.Controls.Add(roomCard, 0, 1);

            // Services
            Panel serviceCard = new Panel();
            serviceCard.Dock = DockStyle.Fill;
            serviceCard.BackColor = Color.White;
            serviceCard.Padding = new Padding(14);
            serviceCard.BorderStyle = BorderStyle.FixedSingle;

            Label serviceTitle = new Label();
            serviceTitle.Text = "Stay Extras & Guest Services";
            serviceTitle.Dock = DockStyle.Top;
            serviceTitle.Height = 38;
            serviceTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            serviceTitle.ForeColor = UIHelper.NavyHeader;

            serviceList = new CheckedListBox();
            serviceList.Dock = DockStyle.Fill;
            serviceList.CheckOnClick = true;
            serviceList.IntegralHeight = false;
            serviceList.Font = new Font("Segoe UI", 9F);
            serviceList.ItemCheck += ServicesChanged;

            serviceCard.Controls.Add(serviceList);
            serviceCard.Controls.Add(serviceTitle);
            main.Controls.Add(serviceCard, 1, 1);

            // Price summary
            TableLayoutPanel summary = new TableLayoutPanel();
            summary.Dock = DockStyle.Fill;
            summary.BackColor = Color.White;
            summary.Padding = new Padding(16);
            summary.ColumnCount = 2;
            summary.RowCount = 5;
            summary.BorderStyle = BorderStyle.FixedSingle;
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            for (int i = 0; i < 5; i++)
            {
                summary.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            }

            summary.Controls.Add(MakeLabel("Room subtotal"), 0, 0);
            lblRoomTotal = MakeAmountLabel(false);
            summary.Controls.Add(lblRoomTotal, 1, 0);

            summary.Controls.Add(MakeLabel("Selected services"), 0, 1);
            lblServiceTotal = MakeAmountLabel(false);
            summary.Controls.Add(lblServiceTotal, 1, 1);

            summary.Controls.Add(MakeLabel("Nights / rooms"), 0, 2);
            lblSummaryNights = MakeAmountLabel(false);
            summary.Controls.Add(lblSummaryNights, 1, 2);

            summary.Controls.Add(MakeLabel("Discount / offer"), 0, 3);
            lblDiscount = MakeAmountLabel(false);
            summary.Controls.Add(lblDiscount, 1, 3);

            summary.Controls.Add(MakeLabel("Grand total"), 0, 4);
            lblGrandTotal = MakeAmountLabel(true);
            summary.Controls.Add(lblGrandTotal, 1, 4);

            main.Controls.Add(summary, 0, 2);

            // Add to cart area
            Panel actionCard = new Panel();
            actionCard.Dock = DockStyle.Fill;
            actionCard.BackColor = Color.White;
            actionCard.Padding = new Padding(16);
            actionCard.BorderStyle = BorderStyle.FixedSingle;

            Label instruction = new Label();
            instruction.Text = "Review the room, dates, guests and services before adding this stay to your cart.";
            instruction.AutoSize = false;
            instruction.Width = 390;
            instruction.Height = 55;
            instruction.ForeColor = Color.DimGray;

            lblError = UIHelper.MakeErrorLabel();
            lblError.AutoSize = false;
            lblError.Width = 390;
            lblError.Height = 42;

            Button addButton = UIHelper.MakeButton(
                "Add Booking to Cart",
                UIHelper.CustomerColor,
                260,
                42);
            addButton.Margin = new Padding(0, 4, 0, 0);
            addButton.Click += AddToCart_Click;

            FlowLayoutPanel actionLayout = new FlowLayoutPanel();
            actionLayout.Dock = DockStyle.Fill;
            actionLayout.FlowDirection = FlowDirection.TopDown;
            actionLayout.WrapContents = false;
            actionLayout.AutoScroll = true;
            actionLayout.Controls.Add(instruction);
            actionLayout.Controls.Add(lblError);
            actionLayout.Controls.Add(addButton);
            actionCard.Controls.Add(actionLayout);

            main.Controls.Add(actionCard, 1, 2);

            // Reviews
            Panel reviewCard = new Panel();
            reviewCard.Dock = DockStyle.Fill;
            reviewCard.BackColor = Color.White;
            reviewCard.Padding = new Padding(10);
            reviewCard.BorderStyle = BorderStyle.FixedSingle;

            Label reviewTitle = new Label();
            reviewTitle.Text = "Guest Reviews";
            reviewTitle.Dock = DockStyle.Top;
            reviewTitle.Height = 28;
            reviewTitle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            reviewTitle.ForeColor = UIHelper.NavyHeader;

            lblReviewSummary = new Label();
            lblReviewSummary.Text = "Loading reviews...";
            lblReviewSummary.Dock = DockStyle.Top;
            lblReviewSummary.Height = 24;
            lblReviewSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblReviewSummary.ForeColor = UIHelper.CustomerColor;

            reviewGrid = new DataGridView();
            reviewGrid.Dock = DockStyle.Fill;
            UIHelper.StyleGrid(reviewGrid);

            reviewCard.Controls.Add(reviewGrid);
            reviewCard.Controls.Add(lblReviewSummary);
            reviewCard.Controls.Add(reviewTitle);
            main.Controls.Add(reviewCard, 0, 3);
            main.SetColumnSpan(reviewCard, 2);

            Controls.Add(main);
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

        private Label MakeAmountLabel(bool strong)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Font = new Font(
                "Segoe UI",
                strong ? 13F : 10F,
                FontStyle.Bold);
            label.ForeColor = UIHelper.CustomerColor;
            return label;
        }

        private void LoadHotel()
        {
            string sql = @"
                SELECT HotelName, Category, City, Address, StarRating
                FROM Hotels
                WHERE HotelId=@Id";

            DataTable table = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", hotelId));

            if (table.Rows.Count == 0)
            {
                lblHotelInfo.Text = "Property not found.";
                return;
            }

            DataRow row = table.Rows[0];
            string stars = "New property";

            if (row["StarRating"] != DBNull.Value)
            {
                stars = Convert.ToDecimal(row["StarRating"]).ToString("N1") + " ★";
            }

            lblHotelInfo.Text = row["HotelName"] + "  •  " + row["Category"] +
                "\n" + row["City"] + "  •  " + row["Address"] +
                "  •  " + stars;
        }

        private void LoadRooms()
        {
            string sql = @"
                SELECT RoomId, RoomType, PricePerNight, TotalRooms,
                       MinAvailability,
                       ISNULL(RoomSize, 'Not specified') AS RoomSize,
                       ISNULL(BedType, 'Not specified') AS BedType,
                       ISNULL(MaxGuests, 2) AS MaxGuests,
                       ISNULL(Amenities, '') AS Amenities,
                       ISNULL(Description, '') AS Description
                FROM Rooms
                WHERE HotelId=@Id
                ORDER BY PricePerNight, RoomType";

            roomTable = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", hotelId));

            if (!roomTable.Columns.Contains("Display"))
            {
                roomTable.Columns.Add("Display", typeof(string));
            }

            foreach (DataRow row in roomTable.Rows)
            {
                decimal price = Convert.ToDecimal(row["PricePerNight"]);
                row["Display"] = row["RoomType"] +
                    "  —  $" + price.ToString("N2") + "/night";
            }

            cboRoom.DisplayMember = "Display";
            cboRoom.ValueMember = "RoomId";
            cboRoom.DataSource = roomTable;
        }

        private void LoadServices()
        {
            string sql = @"
                SELECT ServiceId, ServiceName, Price, IsFree
                FROM ServiceCatalog
                WHERE Active=1
                ORDER BY IsFree DESC, ServiceName";

            DataTable table = DbHelper.GetDataTable(sql);
            serviceList.Items.Clear();

            foreach (DataRow row in table.Rows)
            {
                int id = Convert.ToInt32(row["ServiceId"]);
                string name = row["ServiceName"].ToString();
                decimal price = Convert.ToDecimal(row["Price"]);
                bool free = Convert.ToBoolean(row["IsFree"]);

                if (free)
                {
                    price = 0;
                }

                ServiceChoice service = new ServiceChoice();
                service.ServiceId = id;
                service.Name = name;
                service.Price = price;
                service.IsFree = free;

                serviceList.Items.Add(service, false);
            }
        }

        private void UpdateAvailableRoomsCount()
        {
            if (cboRoom.SelectedValue == null || cboRoom.SelectedValue is DataRowView) return;

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);
            DateTime checkIn = dtCheckIn.Value.Date;
            DateTime checkOut = dtCheckOut.Value.Date;

            // 1. Get total rooms configured by admin
            var roomTable = DbHelper.GetDataTable(
                "SELECT TotalRooms FROM Rooms WHERE RoomId = @RoomId",
                new SqlParameter("@RoomId", roomId));

            if (roomTable.Rows.Count == 0) return;
            int totalRooms = Convert.ToInt32(roomTable.Rows[0]["TotalRooms"]);

            // 2. Count overlapping confirmed bookings safely by counting items
            string overlapQuery = @"
                SELECT ISNULL(COUNT(bi.RoomId), 0) 
                FROM BookingItems bi
                JOIN Bookings b ON b.BookingId = bi.BookingId
                WHERE bi.RoomId = @RoomId 
                  AND b.Status = 'Confirmed'
                  AND bi.CheckInDate < @CheckOut 
                  AND bi.CheckOutDate > @CheckIn";

            int bookedRooms = Convert.ToInt32(DbHelper.ExecuteScalar(overlapQuery,
                new SqlParameter("@RoomId", roomId),
                new SqlParameter("@CheckIn", checkIn),
                new SqlParameter("@CheckOut", checkOut)));

            // 3. Compute available rooms
            int availableRooms = Math.Max(0, totalRooms - bookedRooms);

            // 4. Render output message and adjust constraints
            if (availableRooms > 0)
            {
                lblAvailability.Text = $"✔ Live Status: {availableRooms} rooms available for selected dates.";
                lblAvailability.ForeColor = Color.FromArgb(46, 125, 50); // Green
                numQty.Maximum = Math.Max(1, availableRooms);
            }
            else
            {
                lblAvailability.Text = "✖ Fully Booked for these selected dates!";
                lblAvailability.ForeColor = Color.FromArgb(198, 40, 40); // Red
                numQty.Maximum = 1;
            }
        }

        private void LoadReviews()
        {
            string sql = @"
                SELECT u.FullName AS Reviewer,
                       rv.Rating,
                       rv.Comment,
                       rv.ReviewDate
                FROM Reviews rv
                JOIN Users u ON u.UserId=rv.CustomerId
                WHERE rv.HotelId=@Id
                ORDER BY rv.ReviewDate DESC";

            DataTable reviews = DbHelper.GetDataTable(
                sql,
                new SqlParameter("@Id", hotelId));

            reviewGrid.DataSource = reviews;

            if (reviews.Rows.Count == 0)
            {
                lblReviewSummary.Text = "No reviews yet — be the first to review after your stay.";
                return;
            }

            decimal total = 0;
            foreach (DataRow row in reviews.Rows)
            {
                total += Convert.ToDecimal(row["Rating"]);
            }

            decimal average = total / reviews.Rows.Count;
            lblReviewSummary.Text = average.ToString("N1") +
                " ★ average from " + reviews.Rows.Count + " review(s)";
        }

        private void RoomChanged(object sender, EventArgs e)
        {
            UpdateRoomInfo();
            CalculatePrice();
        }

        private void DateChanged(object sender, EventArgs e)
        {
            if (dtCheckOut.Value.Date <= dtCheckIn.Value.Date)
            {
                dtCheckOut.Value = dtCheckIn.Value.Date.AddDays(1);
            }

            dtCheckOut.MinDate = dtCheckIn.Value.Date.AddDays(1);
            UpdateAvailability();
            CalculatePrice();
        }

        private void RoomsChanged(object sender, EventArgs e)
        {
            UpdateGuestLimit();
            CalculatePrice();
        }

        private void GuestsChanged(object sender, EventArgs e)
        {
            CalculatePrice();
        }

        private void ServicesChanged(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke(new Action(CalculatePrice));
        }

        private void UpdateRoomInfo()
        {
            if (cboRoom.SelectedValue == null || roomTable == null)
            {
                return;
            }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);
            DataRow[] rows = roomTable.Select("RoomId=" + roomId);

            if (rows.Length == 0)
            {
                return;
            }

            DataRow row = rows[0];
            roomPrice = Convert.ToDecimal(row["PricePerNight"]);

            lblRoomInfo.Text = row["RoomSize"] +
                "  •  " + row["BedType"] +
                "  •  Up to " + row["MaxGuests"] + " guests" +
                "\nAmenities: " + row["Amenities"] +
                "  •  " + row["Description"];

            UpdateGuestLimit();
            UpdateAvailability();
        }

        private void UpdateGuestLimit()
        {
            if (cboRoom.SelectedValue == null || roomTable == null)
            {
                return;
            }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);
            DataRow[] rows = roomTable.Select("RoomId=" + roomId);

            if (rows.Length == 0)
            {
                return;
            }

            int guestsPerRoom = Convert.ToInt32(rows[0]["MaxGuests"]);
            if (guestsPerRoom < 1)
            {
                guestsPerRoom = 1;
            }

            int rooms = Convert.ToInt32(numRooms.Value);
            int maxGuests = guestsPerRoom * rooms;

            if (maxGuests > 50)
            {
                maxGuests = 50;
            }

            numGuests.Maximum = maxGuests;

            if (numGuests.Value > maxGuests)
            {
                numGuests.Value = maxGuests;
            }

            lblGuestLimit.Text = "Number of guests (max " +
                guestsPerRoom + " / room)";
        }

        private void UpdateAvailability()
        {
            if (cboRoom.SelectedValue == null)
            {
                return;
            }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);

            string sql = @"
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
                WHERE r.RoomId=@RoomId";

            object result = DbHelper.ExecuteScalar(
                sql,
                new SqlParameter("@OutDate", dtCheckOut.Value.Date),
                new SqlParameter("@InDate", dtCheckIn.Value.Date),
                new SqlParameter("@RoomId", roomId));

            if (result == null || result == DBNull.Value)
            {
                availableRooms = 0;
            }
            else
            {
                availableRooms = Convert.ToInt32(result);
            }

            if (availableRooms < 0)
            {
                availableRooms = 0;
            }

            if (availableRooms > 0)
            {
                lblAvailability.Text = "Available: " + availableRooms + " room(s)";
                lblAvailability.ForeColor = UIHelper.CustomerColor;
            }
            else
            {
                lblAvailability.Text = "Sold out for these dates";
                lblAvailability.ForeColor = Color.Firebrick;
            }

            if (availableRooms > 0 && numRooms.Value > availableRooms)
            {
                decimal value = availableRooms;
                if (value > numRooms.Maximum)
                {
                    value = numRooms.Maximum;
                }
                numRooms.Value = value;
            }
        }

        private decimal GetOfferPercent()
        {
            if (cboRoom.SelectedValue == null)
            {
                return 0;
            }

            string sql = @"
                SELECT ISNULL(MAX(DiscountPercent), 0)
                FROM Offers
                WHERE RoomId=@RoomId
                AND @CheckIn BETWEEN StartDate AND EndDate
                AND @CheckOut <= DATEADD(DAY, 1, EndDate)";

            object result = DbHelper.ExecuteScalar(
                sql,
                new SqlParameter("@RoomId", Convert.ToInt32(cboRoom.SelectedValue)),
                new SqlParameter("@CheckIn", dtCheckIn.Value.Date),
                new SqlParameter("@CheckOut", dtCheckOut.Value.Date));

            if (result == null || result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDecimal(result);
        }

        private decimal GetSelectedServicesTotal()
        {
            decimal total = 0;

            foreach (object item in serviceList.CheckedItems)
            {
                ServiceChoice service = item as ServiceChoice;
                if (service != null)
                {
                    total += service.Price;
                }
            }

            return total;
        }

        private void CalculatePrice()
        {
            if (lblRoomTotal == null || cboRoom.SelectedValue == null)
            {
                return;
            }

            int nights = (dtCheckOut.Value.Date - dtCheckIn.Value.Date).Days;
            if (nights < 1)
            {
                nights = 1;
            }

            int rooms = Convert.ToInt32(numRooms.Value);
            decimal roomTotal = roomPrice * nights * rooms;
            decimal serviceTotal = GetSelectedServicesTotal();
            decimal offerPercent = GetOfferPercent();
            decimal discount = Math.Round(
                roomTotal * offerPercent / 100m,
                2);

            decimal grandTotal = roomTotal + serviceTotal - discount;
            if (grandTotal < 0)
            {
                grandTotal = 0;
            }

            lblNights.Text = nights + " night(s)  •  " + rooms + " room(s)";
            lblRoomTotal.Text = "$" + roomTotal.ToString("N2");
            lblServiceTotal.Text = "$" + serviceTotal.ToString("N2");

            lblSummaryNights.Text = lblNights.Text;

            if (discount > 0)
            {
                lblDiscount.Text = "-$" + discount.ToString("N2") +
                    " (" + offerPercent.ToString("0.#") + "%)";
            }
            else
            {
                lblDiscount.Text = "$0.00";
            }

            lblGrandTotal.Text = "$" + grandTotal.ToString("N2");
        }

        private void AddToCart_Click(object sender, EventArgs e)
        {
            HideError();

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

            if (availableRooms < Convert.ToInt32(numRooms.Value))
            {
                ShowError("Only " + availableRooms +
                    " room(s) are available for these dates.");
                return;
            }

            int roomId = Convert.ToInt32(cboRoom.SelectedValue);

            string otherHotelSql = @"
                SELECT COUNT(*)
                FROM Cart c
                JOIN Rooms r ON r.RoomId=c.RoomId
                WHERE c.CustomerId=@CustomerId
                AND r.HotelId<>@HotelId";

            object otherItems = DbHelper.ExecuteScalar(
                otherHotelSql,
                new SqlParameter("@CustomerId", Session.UserId),
                new SqlParameter("@HotelId", hotelId));

            int otherHotelCount = 0;
            if (otherItems != null && otherItems != DBNull.Value)
            {
                otherHotelCount = Convert.ToInt32(otherItems);
            }

            if (otherHotelCount > 0)
            {
                DialogResult answer = MessageBox.Show(
                    "Your cart contains a room from another property. A booking request can contain rooms from one property at a time.\n\n" +
                    "Clear the existing cart and add this property instead?",
                    "Change Property",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

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
            }

            int maxGuests = GetRoomMaxGuests(roomId);
            int selectedRooms = Convert.ToInt32(numRooms.Value);
            int maximumGuests = maxGuests * selectedRooms;

            if (Convert.ToInt32(numGuests.Value) > maximumGuests)
            {
                ShowError("This room type supports up to " +
                    maximumGuests + " guest(s) for the selected quantity.");
                return;
            }

            int nights = (dtCheckOut.Value.Date - dtCheckIn.Value.Date).Days;
            decimal roomSubtotal = roomPrice * nights * selectedRooms;
            decimal discount = Math.Round(
                roomSubtotal * GetOfferPercent() / 100m,
                2);

            string insertSql = @"
                INSERT INTO Cart
                (CustomerId, RoomId, CheckInDate, CheckOutDate,
                 Quantity, Guests, DiscountAmount)
                OUTPUT INSERTED.CartId
                VALUES
                (@CustomerId, @RoomId, @CheckIn, @CheckOut,
                 @Quantity, @Guests, @Discount)";

            object result = DbHelper.ExecuteScalar(
                insertSql,
                new SqlParameter("@CustomerId", Session.UserId),
                new SqlParameter("@RoomId", roomId),
                new SqlParameter("@CheckIn", dtCheckIn.Value.Date),
                new SqlParameter("@CheckOut", dtCheckOut.Value.Date),
                new SqlParameter("@Quantity", selectedRooms),
                new SqlParameter("@Guests", Convert.ToInt32(numGuests.Value)),
                new SqlParameter("@Discount", discount));

            int cartId = Convert.ToInt32(result);

            foreach (object item in serviceList.CheckedItems)
            {
                ServiceChoice service = item as ServiceChoice;
                if (service == null)
                {
                    continue;
                }

                string serviceSql = @"
                    INSERT INTO CartServices
                    (CartId, ServiceId, Quantity, UnitPrice)
                    VALUES (@CartId, @ServiceId, 1, @Price)";

                DbHelper.ExecuteNonQuery(
                    serviceSql,
                    new SqlParameter("@CartId", cartId),
                    new SqlParameter("@ServiceId", service.ServiceId),
                    new SqlParameter("@Price", service.Price));
            }

            DialogResult answerAfterAdd = MessageBox.Show(
                "Your stay and selected services were added to My Cart.\n\n" +
                "Would you like to view your cart now?",
                "Added to Cart",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (answerAfterAdd == DialogResult.Yes)
            {
                BookingCartForm cart = new BookingCartForm();
                cart.ShowDialog(this);
                cart.Dispose();
            }

            Close();
        }

        private int GetRoomMaxGuests(int roomId)
        {
            object result = DbHelper.ExecuteScalar(
                "SELECT ISNULL(MaxGuests,2) FROM Rooms WHERE RoomId=@Id",
                new SqlParameter("@Id", roomId));

            if (result == null || result == DBNull.Value)
            {
                return 2;
            }

            return Convert.ToInt32(result);
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

        private class ServiceChoice
        {
            public int ServiceId;
            public string Name;
            public decimal Price;
            public bool IsFree;

            public override string ToString()
            {
                if (IsFree)
                {
                    return Name + " — Included / Free";
                }

                return Name + " — +$" + Price.ToString("N2");
            }
        }
    }
}