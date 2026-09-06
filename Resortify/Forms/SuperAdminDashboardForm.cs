using System;
using System.Drawing;
using System.Windows.Forms;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class SuperAdminDashboardForm : Form
    {
        public SuperAdminDashboardForm()
        {
            InitializeComponent();
            LoadSummary();
        }

        private FlowLayoutPanel cardsPanel;

        private void InitializeComponent()
        {
            Text = "Resortify - Super Admin Dashboard";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Super Admin Dashboard", UIHelper.SuperAdmin,
                onBack: null, onLogout: (s, e) => LogoutClicked()));

            cardsPanel = new FlowLayoutPanel
            {
                Location = new Point(24, 100),
                Size = new Size(832, 120),
                FlowDirection = FlowDirection.LeftToRight
            };
            Controls.Add(cardsPanel);

            var lblNav = new Label { Text = "Manage Platform", Location = new Point(24, 240), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            Controls.Add(lblNav);

            var navPanel = new FlowLayoutPanel
            {
                Location = new Point(24, 274),
                Size = new Size(832, 360),
                FlowDirection = FlowDirection.LeftToRight
            };

            AddNav(navPanel, "Add New Staff", () => new AddStaffForm());
            AddNav(navPanel, "Manage Hotel Owners", () => new ManageHotelOwnersForm());
            AddNav(navPanel, "View All Users", () => new ViewAllUsersForm());
            AddNav(navPanel, "Platform Sales &&\nCommission Report", () => new PlatformSalesReportForm());
            AddNav(navPanel, "Low-Rated Hotels\nReport", () => new LowRatedHotelsForm());
            AddNav(navPanel, "Manage Room\nCategories", () => new ManageRoomCategoriesForm());
            AddNav(navPanel, "Moderate Reviews", () => new ModerateReviewsForm());

            Controls.Add(navPanel);
        }

        private void AddNav(FlowLayoutPanel panel, string text, Func<Form> factory)
        {
            var btn = UIHelper.MakeButton(text, UIHelper.SuperAdmin, 260, 100);
            btn.Margin = new Padding(8);
            btn.Click += (s, e) => { factory().Show(); };
            panel.Controls.Add(btn);
        }

        private void LoadSummary()
        {
            cardsPanel.Controls.Clear();
            int hotels = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Hotels WHERE Status = 'Approved'"));
            int customers = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE UserType = 'Customer'"));
            int bookings = Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Bookings"));
            decimal revenue = DbHelper.ExecuteScalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Bookings") is decimal d ? d : 0m;
            decimal commission = revenue * 0.10m;

            AddCard("Approved Hotels", hotels.ToString());
            AddCard("Customers", customers.ToString());
            AddCard("Total Bookings", bookings.ToString());
            AddCard("Platform Revenue", $"${revenue:N2}");
            AddCard("Commission Earned (10%)", $"${commission:N2}");
        }

        private void AddCard(string title, string value)
        {
            var card = new Panel { Size = new Size(155, 100), BackColor = Color.FromArgb(245, 245, 245), Margin = new Padding(6) };
            card.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UIHelper.SuperAdmin, Location = new Point(10, 14), AutoSize = true });
            card.Controls.Add(new Label { Text = title, Font = UIHelper.BaseFont, Location = new Point(10, 56), AutoSize = true, MaximumSize = new Size(135, 0) });
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