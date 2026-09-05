using System;
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
        private ComboBox cboPrice, cboStars, cboCity;
        private FlowLayoutPanel hotelPanel;

        public CustomerHomeForm()
        {
            InitializeComponent();
            LoadCityOptions();
            LoadHotels();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Browse Hotels";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Browse Hotels & Resorts", UIHelper.CustomerColor,
                onBack: null, onLogout: (s, e) => { Session.SignOut(); new LoginForm().Show(); Close(); }));

            var toolbar = new FlowLayoutPanel { Location = new Point(24, 92), Size = new Size(932, 36), FlowDirection = FlowDirection.LeftToRight };
            void ToolBtn(string text, EventHandler onClick)
            {
                var b = UIHelper.MakeButton(text, UIHelper.CustomerColor, 130, 30);
                b.Margin = new Padding(4, 0, 0, 0);
                b.Click += onClick;
                toolbar.Controls.Add(b);
            }
            ToolBtn("My Cart", (s, e) => new BookingCartForm().Show());
            ToolBtn("My Bookings", (s, e) => new BookingHistoryForm().Show());
            ToolBtn("Special Offers", (s, e) => new SpecialOffersForm().Show());
            ToolBtn("Update Profile", (s, e) => new UpdateProfileForm().Show());
            Controls.Add(toolbar);

            var lblSearch = new Label { Text = "Search", Location = new Point(24, 136), AutoSize = true, Font = UIHelper.BaseFont };
            txtSearch = new TextBox { Location = new Point(24, 158), Width = 220, Font = UIHelper.BaseFont };
            txtSearch.TextChanged += (s, e) => LoadHotels();

            var lblPrice = new Label { Text = "Price Range", Location = new Point(260, 136), AutoSize = true, Font = UIHelper.BaseFont };
            cboPrice = new ComboBox { Location = new Point(260, 158), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboPrice.Items.AddRange(new object[] { "Any", "Under $75", "$75 - $150", "Over $150" });
            cboPrice.SelectedIndex = 0;
            cboPrice.SelectedIndexChanged += (s, e) => LoadHotels();

            var lblStars = new Label { Text = "Star Rating", Location = new Point(426, 136), AutoSize = true, Font = UIHelper.BaseFont };
            cboStars = new ComboBox { Location = new Point(426, 158), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboStars.Items.AddRange(new object[] { "Any", "4+ stars", "3+ stars" });
            cboStars.SelectedIndex = 0;
            cboStars.SelectedIndexChanged += (s, e) => LoadHotels();

            var lblCity = new Label { Text = "Location", Location = new Point(592, 136), AutoSize = true, Font = UIHelper.BaseFont };
            cboCity = new ComboBox { Location = new Point(592, 158), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboCity.SelectedIndexChanged += (s, e) => LoadHotels();

            var btnReset = UIHelper.MakeButton("Reset", Color.Gray, 90, 26);
            btnReset.Location = new Point(758, 158);
            btnReset.Click += (s, e) =>
            {
                txtSearch.Clear(); cboPrice.SelectedIndex = 0; cboStars.SelectedIndex = 0; cboCity.SelectedIndex = 0;
            };

            hotelPanel = new FlowLayoutPanel
            {
                Location = new Point(24, 196),
                Size = new Size(932, 420),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            Controls.AddRange(new Control[] { lblSearch, txtSearch, lblPrice, cboPrice, lblStars, cboStars, lblCity, cboCity, btnReset, hotelPanel });
        }

        private void LoadCityOptions()
        {
            var table = DbHelper.GetDataTable("SELECT DISTINCT City FROM Hotels WHERE Status = 'Approved' ORDER BY City");
            cboCity.Items.Add("Any");
            foreach (System.Data.DataRow row in table.Rows) cboCity.Items.Add(row["City"].ToString());
            cboCity.SelectedIndex = 0;
        }

        private void LoadHotels()
        {
            hotelPanel.Controls.Clear();

            string sql = @"SELECT h.HotelId, h.HotelName, h.City, h.StarRating, h.ImagePath,
                                  MIN(r.PricePerNight) AS StartingPrice
                           FROM Hotels h
                           JOIN Rooms r ON r.HotelId = h.HotelId
                           WHERE h.Status = 'Approved'";
            var parms = new System.Collections.Generic.List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                sql += " AND (h.HotelName LIKE @Search OR h.City LIKE @Search)";
                parms.Add(new SqlParameter("@Search", $"%{txtSearch.Text.Trim()}%"));
            }
            if (cboCity.SelectedIndex > 0)
            {
                sql += " AND h.City = @City";
                parms.Add(new SqlParameter("@City", cboCity.SelectedItem.ToString()));
            }
            if (cboStars.SelectedIndex == 1) sql += " AND h.StarRating >= 4";
            else if (cboStars.SelectedIndex == 2) sql += " AND h.StarRating >= 3";

            sql += " GROUP BY h.HotelId, h.HotelName, h.City, h.StarRating, h.ImagePath";

            if (cboPrice.SelectedIndex == 1) sql += " HAVING MIN(r.PricePerNight) < 75";
            else if (cboPrice.SelectedIndex == 2) sql += " HAVING MIN(r.PricePerNight) BETWEEN 75 AND 150";
            else if (cboPrice.SelectedIndex == 3) sql += " HAVING MIN(r.PricePerNight) > 150";

            var table = DbHelper.GetDataTable(sql, parms.ToArray());

            foreach (System.Data.DataRow row in table.Rows)
            {
                int hotelId = Convert.ToInt32(row["HotelId"]);
                string name = row["HotelName"].ToString();
                string city = row["City"].ToString();
                object starObj = row["StarRating"];
                string stars = starObj == DBNull.Value ? "Unrated" : $"{Convert.ToDecimal(starObj):N1} ★";
                decimal startingPrice = Convert.ToDecimal(row["StartingPrice"]);

                var card = new Panel { Size = new Size(290, 150), BackColor = Color.FromArgb(248, 248, 248), Margin = new Padding(8) };
                card.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(12, 10), AutoSize = true, MaximumSize = new Size(266, 0) });
                card.Controls.Add(new Label { Text = $"{city}  •  {stars}", Font = UIHelper.BaseFont, Location = new Point(12, 40), AutoSize = true, ForeColor = Color.DimGray });
                card.Controls.Add(new Label { Text = $"From ${startingPrice:N2} / night", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(12, 66), AutoSize = true, ForeColor = UIHelper.CustomerColor });

                var btnView = UIHelper.MakeButton("View Details", UIHelper.CustomerColor, 260, 32);
                btnView.Location = new Point(12, 104);
                btnView.Click += (s, e) => new HotelDetailsForm(hotelId).Show();
                card.Controls.Add(btnView);

                hotelPanel.Controls.Add(card);
            }

            if (table.Rows.Count == 0)
            {
                hotelPanel.Controls.Add(new Label { Text = "No hotels match your filters.", AutoSize = true, ForeColor = Color.Gray, Font = UIHelper.BaseFont });
            }
        }
    }
}
