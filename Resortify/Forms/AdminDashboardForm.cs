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
        public AdminDashboardForm()
        {
            InitializeComponent();

            // First-Time Registration Check
            if (Session.HotelId == null || Session.HotelId == 0)
            {
                MessageBox.Show("Welcome to Resortify! Please register your hotel details first to continue.",
                                "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

                using (var setupForm = new HotelProfileForm(isFirstTimeSetup: true))
                {
                    setupForm.ShowDialog();
                }

                if (Session.HotelId == null || Session.HotelId == 0)
                {
                    LogoutClicked();
                    return;
                }
            }

            LoadSummary();
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
            var card = new Panel
            {
                Size = new Size(190, 90),
                BackColor = Color.FromArgb(245, 245, 245),
                Margin = new Padding(6)
            };

            card.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = UIHelper.AdminColor,
                Location = new Point(10, 12),
                AutoSize = true
            });

            card.Controls.Add(new Label
            {
                Text = title,
                Font = UIHelper.BaseFont,
                Location = new Point(10, 52),
                AutoSize = true,
                MaximumSize = new Size(170, 0)
            });

            cardsPanel.Controls.Add(card);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadSummary();
        }

        private void LogoutClicked()
        {
            Session.SignOut();
            new LoginForm().Show();
            this.Close();
        }
    }
}