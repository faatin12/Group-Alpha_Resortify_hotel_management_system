using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class AdminDashboardForm : Form
    {
        private FlowLayoutPanel cardsPanel;

        public AdminDashboardForm()
        {
            InitializeComponent();
            LoadSummary();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Hotel Owner Dashboard";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader($"{Session.HotelName} - Dashboard", UIHelper.AdminColor,
                onBack: null, onLogout: (s, e) => LogoutClicked()));

            cardsPanel = new FlowLayoutPanel { Location = new Point(24, 100), Size = new Size(832, 110), FlowDirection = FlowDirection.LeftToRight };
            Controls.Add(cardsPanel);

            var lblNav = new Label { Text = "Manage My Hotel", Location = new Point(24, 226), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            Controls.Add(lblNav);

            var navPanel = new FlowLayoutPanel { Location = new Point(24, 260), Size = new Size(832, 250), FlowDirection = FlowDirection.LeftToRight };
            AddNav(navPanel, "Hotel Profile", () => new HotelProfileForm(isFirstTimeSetup: false));
            AddNav(navPanel, "Room / Package\nManagement", () => new RoomManagementForm());
            AddNav(navPanel, "Availability\nDashboard", () => new AvailabilityDashboardForm());
            AddNav(navPanel, "Earnings &&\nBooking Report", () => new EarningsReportForm());
            AddNav(navPanel, "Create Discount\nOffer", () => new CreateOfferForm());
            AddNav(navPanel, "Reviews on My\nHotel", () => new AdminReviewsForm());
            AddNav(navPanel, "Update Profile", () => new UpdateProfileForm());
            Controls.Add(navPanel);
        }

        private void AddNav(FlowLayoutPanel panel, string text, Func<Form> factory)
        {
            var btn = UIHelper.MakeButton(text, UIHelper.AdminColor, 260, 100);
            btn.Margin = new Padding(8);
            btn.Click += (s, e) => factory().Show();
            panel.Controls.Add(btn);
        }

        private void LoadSummary()
        {
            cardsPanel.Controls.Clear();
            int hotelId = Session.HotelId ?? 0;

            int roomTypes = Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Rooms WHERE HotelId = @Id", new SqlParameter("@Id", hotelId)));
            int bookings = Convert.ToInt32(DbHelper.ExecuteScalar(
                @"SELECT COUNT(DISTINCT bi.BookingId) FROM BookingItems bi
                  JOIN Rooms r ON r.RoomId = bi.RoomId WHERE r.HotelId = @Id",
                new SqlParameter("@Id", hotelId)));
            decimal earnings = DbHelper.ExecuteScalar(
                @"SELECT ISNULL(SUM(bi.Subtotal),0) * 0.90 FROM BookingItems bi
                  JOIN Rooms r ON r.RoomId = bi.RoomId WHERE r.HotelId = @Id",
                new SqlParameter("@Id", hotelId)) is decimal d ? d : 0m;
            object avgObj = DbHelper.ExecuteScalar(
                "SELECT AVG(CAST(Rating AS DECIMAL(3,2))) FROM Reviews WHERE HotelId = @Id",
                new SqlParameter("@Id", hotelId));
            string avgRating = avgObj == DBNull.Value || avgObj == null ? "-" : Convert.ToDecimal(avgObj).ToString("N1");

            AddCard("Room Types", roomTypes.ToString());
            AddCard("Bookings", bookings.ToString());
            AddCard("My Earnings (after 10% fee)", $"${earnings:N2}");
            AddCard("Average Rating", avgRating);
        }

        private void AddCard(string title, string value)
        {
            var card = new Panel { Size = new Size(190, 90), BackColor = Color.FromArgb(245, 245, 245), Margin = new Padding(6) };
            card.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UIHelper.AdminColor, Location = new Point(10, 12), AutoSize = true });
            card.Controls.Add(new Label { Text = title, Font = UIHelper.BaseFont, Location = new Point(10, 52), AutoSize = true, MaximumSize = new Size(170, 0) });
            cardsPanel.Controls.Add(card);
        }

        private void LogoutClicked()
        {
            Session.SignOut();
            new LoginForm().Show();
            Close();
        }
    }
}
