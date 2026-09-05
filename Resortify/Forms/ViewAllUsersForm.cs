using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class ViewAllUsersForm : Form
    {
        private TextBox txtSearch;
        private ComboBox cboRole;
        private DataGridView grid;

        public ViewAllUsersForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - View All Users";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("View All Users", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            var lblSearch = new Label { Text = "Search name/email", Location = new Point(24, 100), AutoSize = true, Font = UIHelper.BaseFont };
            txtSearch = new TextBox { Location = new Point(24, 122), Width = 260, Font = UIHelper.BaseFont };
            txtSearch.TextChanged += (s, e) => LoadData();

            var lblRole = new Label { Text = "Role", Location = new Point(300, 100), AutoSize = true, Font = UIHelper.BaseFont };
            cboRole = new ComboBox { Location = new Point(300, 122), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };
            cboRole.Items.AddRange(new object[] { "All", "Admin", "Customer" });
            cboRole.SelectedIndex = 0;
            cboRole.SelectedIndexChanged += (s, e) => LoadData();

            grid = new DataGridView { Location = new Point(24, 164), Size = new Size(772, 320) };
            UIHelper.StyleGrid(grid);

            Controls.AddRange(new Control[] { lblSearch, txtSearch, lblRole, cboRole, grid });
        }

        private void LoadData()
        {
            string sql = @"SELECT UserId, FullName, Email, Phone, UserType, Status, CreatedAt
                            FROM Users WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                sql += " AND (FullName LIKE @Search OR Email LIKE @Search)";
            if (cboRole.SelectedIndex > 0)
                sql += " AND UserType = @Role";
            sql += " ORDER BY FullName";

            var cmdParams = new System.Collections.Generic.List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                cmdParams.Add(new SqlParameter("@Search", $"%{txtSearch.Text.Trim()}%"));
            if (cboRole.SelectedIndex > 0)
                cmdParams.Add(new SqlParameter("@Role", cboRole.SelectedItem.ToString()));

            grid.DataSource = DbHelper.GetDataTable(sql, cmdParams.ToArray());
            if (grid.Columns["UserId"] != null) grid.Columns["UserId"].Visible = false;
        }
    }
}
