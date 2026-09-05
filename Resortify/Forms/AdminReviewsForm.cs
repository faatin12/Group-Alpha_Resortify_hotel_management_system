using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class AdminReviewsForm : Form
    {
        public AdminReviewsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Reviews on My Hotel";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Reviews on My Hotel (Read-Only)", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            var grid = new DataGridView { Location = new Point(24, 100), Size = new Size(732, 370) };
            UIHelper.StyleGrid(grid);
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT u.FullName AS Reviewer, rv.Rating, rv.Comment, rv.ReviewDate
                  FROM Reviews rv JOIN Users u ON u.UserId = rv.CustomerId
                  WHERE rv.HotelId = @Id ORDER BY rv.ReviewDate DESC",
                new SqlParameter("@Id", Session.HotelId));

            Controls.Add(grid);
        }
    }
}
