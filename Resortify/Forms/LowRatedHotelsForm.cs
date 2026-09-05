using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class LowRatedHotelsForm : Form
    {
        private DataGridView grid;
        private const decimal Threshold = 2.5m;

        public LowRatedHotelsForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Low-Rated Hotels Report";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Low-Rated Hotels Report", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            var lblInfo = new Label
            {
                Text = $"Hotels averaging below {Threshold} stars across all reviews:",
                Location = new Point(24, 100),
                AutoSize = true,
                Font = UIHelper.BaseFont
            };

            grid = new DataGridView { Location = new Point(24, 130), Size = new Size(712, 280) };
            UIHelper.StyleGrid(grid);
            grid.CellFormatting += (s, e) =>
            {
                if (grid.Columns[e.ColumnIndex].Name == "AvgRating")
                    e.CellStyle.ForeColor = UIHelper.LowStockRed;
            };

            var btnSuspend = UIHelper.MakeButton("Suspend Selected Hotel", Color.FromArgb(214, 118, 27), 200);
            btnSuspend.Location = new Point(24, 424);
            btnSuspend.Click += (s, e) => SuspendSelected();

            Controls.AddRange(new Control[] { lblInfo, grid, btnSuspend });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT h.HotelId, h.HotelName, h.City, h.Status,
                         COUNT(rv.ReviewId) AS ReviewCount,
                         CAST(AVG(CAST(rv.Rating AS DECIMAL(3,2))) AS DECIMAL(3,2)) AS AvgRating
                  FROM Hotels h
                  JOIN Reviews rv ON rv.HotelId = h.HotelId
                  GROUP BY h.HotelId, h.HotelName, h.City, h.Status
                  HAVING AVG(CAST(rv.Rating AS DECIMAL(3,2))) < @Threshold
                  ORDER BY AvgRating",
                new SqlParameter("@Threshold", Threshold));
            if (grid.Columns["HotelId"] != null) grid.Columns["HotelId"].Visible = false;
        }

        private void SuspendSelected()
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Select a hotel first.", "Resortify");
                return;
            }
            int hotelId = Convert.ToInt32(grid.CurrentRow.Cells["HotelId"].Value);
            DbHelper.ExecuteNonQuery("UPDATE Hotels SET Status = 'Suspended' WHERE HotelId = @Id",
                new SqlParameter("@Id", hotelId));
            LoadData();
        }
    }
}
