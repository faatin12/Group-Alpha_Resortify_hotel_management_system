using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class SpecialOffersForm : Form
    {
        private DataGridView grid;

        public SpecialOffersForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Special Offers";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Special Offers", UIHelper.CustomerColor,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(732, 320) };
            UIHelper.StyleGrid(grid);

            var btnView = UIHelper.MakeButton("View Hotel", UIHelper.CustomerColor, 160);
            btnView.Location = new Point(24, 434);
            btnView.Click += (s, e) =>
            {
                if (grid.CurrentRow == null) { MessageBox.Show("Select an offer first.", "Resortify"); return; }
                int hotelId = Convert.ToInt32(grid.CurrentRow.Cells["HotelId"].Value);
                new HotelDetailsForm(hotelId).Show();
            };

            Controls.AddRange(new Control[] { grid, btnView });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT h.HotelId, h.HotelName, h.City, r.RoomType, r.PricePerNight,
                         o.DiscountPercent,
                         CAST(r.PricePerNight * (1 - o.DiscountPercent / 100.0) AS DECIMAL(10,2)) AS DiscountedPrice,
                         o.StartDate, o.EndDate
                  FROM Offers o
                  JOIN Rooms r ON r.RoomId = o.RoomId
                  JOIN Hotels h ON h.HotelId = r.HotelId
                  WHERE h.Status = 'Approved' AND CAST(GETDATE() AS DATE) BETWEEN o.StartDate AND o.EndDate
                  ORDER BY o.DiscountPercent DESC");
            if (grid.Columns["HotelId"] != null) grid.Columns["HotelId"].Visible = false;
        }
    }
}
