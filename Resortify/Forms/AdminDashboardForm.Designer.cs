using System.Drawing;
using System.Windows.Forms;
using Resortify.Helpers;

namespace Resortify.Forms
{
    partial class AdminDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private FlowLayoutPanel cardsPanel;
        private FlowLayoutPanel navPanel;
        private Label lblNav;
        private Button btnRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Resortify - Hotel Owner Dashboard";
            this.ClientSize = new Size(880, 620);
            this.AutoScroll = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Header Banner
            this.Controls.Add(UIHelper.BuildHeader($"{Session.HotelName ?? "Resortify"} - Dashboard", UIHelper.AdminColor,
                onBack: null, onLogout: (s, e) => LogoutClicked()));

            // Metric Cards Container
            this.cardsPanel = new FlowLayoutPanel
            {
                Location = new Point(24, 100),
                Size = new Size(832, 110),
                FlowDirection = FlowDirection.LeftToRight
            };
            this.Controls.Add(this.cardsPanel);

            // Section Heading
            this.lblNav = new Label
            {
                Text = "Manage My Hotel",
                Location = new Point(24, 226),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            this.Controls.Add(this.lblNav);

            // Refresh Button
            this.btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(746, 220),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UIHelper.AdminColor
            };
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            this.Controls.Add(this.btnRefresh);

            // Navigation Grid
            this.navPanel = new FlowLayoutPanel
            {
                Location = new Point(24, 260),
                Size = new Size(832, 320),
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true
            };

            AddNavButton("Hotel Profile", () => new HotelProfileForm(isFirstTimeSetup: false));
            AddNavButton("Room / Package\nManagement", () => new RoomManagementForm());
            AddNavButton("Availability\nDashboard", () => new AvailabilityDashboardForm());
            AddNavButton("Earnings &&\nBooking Report", () => new EarningsReportForm());
            AddNavButton("Create Discount\nOffer", () => new CreateOfferForm());
            AddNavButton("Reviews on My\nHotel", () => new AdminReviewsForm());
            AddNavButton("Update Profile", () => new UpdateProfileForm());

            this.Controls.Add(this.navPanel);
        }

        private void AddNavButton(string text, System.Func<Form> factory)
        {
            var btn = UIHelper.MakeButton(text, UIHelper.AdminColor, 260, 100);
            btn.Margin = new Padding(8);
            btn.Click += (s, e) => factory().Show();
            this.navPanel.Controls.Add(btn);
        }
    }
}