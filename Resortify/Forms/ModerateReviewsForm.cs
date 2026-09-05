using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class ModerateReviewsForm : Form
    {
        private DataGridView grid;

        public ModerateReviewsForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Moderate Reviews";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Moderate Reviews", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(832, 340) };
            UIHelper.StyleGrid(grid);

            var btnDelete = UIHelper.MakeButton("Delete Review", Color.FromArgb(120, 20, 20), 160);
            btnDelete.Location = new Point(24, 456);
            btnDelete.Click += (s, e) => DeleteSelected();

            Controls.AddRange(new Control[] { grid, btnDelete });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT rv.ReviewId, h.HotelName AS Hotel, u.FullName AS Reviewer,
                         rv.Rating, rv.Comment, rv.ReviewDate
                  FROM Reviews rv
                  JOIN Hotels h ON h.HotelId = rv.HotelId
                  JOIN Users u ON u.UserId = rv.CustomerId
                  ORDER BY rv.ReviewDate DESC");
            if (grid.Columns["ReviewId"] != null) grid.Columns["ReviewId"].Visible = false;
        }

        private void DeleteSelected()
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Select a review first.", "Resortify");
                return;
            }
            if (MessageBox.Show("Permanently delete this review? This affects the hotel's average rating.",
                    "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            int id = Convert.ToInt32(grid.CurrentRow.Cells["ReviewId"].Value);
            DbHelper.ExecuteNonQuery("DELETE FROM Reviews WHERE ReviewId = @Id", new SqlParameter("@Id", id));
            LoadData();
        }
    }
}
