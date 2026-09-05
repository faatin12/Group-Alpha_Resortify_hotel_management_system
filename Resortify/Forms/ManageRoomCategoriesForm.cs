using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class ManageRoomCategoriesForm : Form
    {
        private DataGridView grid;
        private TextBox txtCategory;
        private Label lblError;

        public ManageRoomCategoriesForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Manage Room Categories";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Manage Room Categories", UIHelper.SuperAdmin,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(552, 280) };
            UIHelper.StyleGrid(grid);
            grid.SelectionChanged += (s, e) =>
            {
                if (grid.CurrentRow != null)
                    txtCategory.Text = grid.CurrentRow.Cells["CategoryName"].Value?.ToString();
            };

            var lblNew = new Label { Text = "Category name", Location = new Point(24, 392), AutoSize = true, Font = UIHelper.BaseFont };
            txtCategory = new TextBox { Location = new Point(24, 414), Width = 300, Font = UIHelper.BaseFont };

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(24, 442);

            var btnAdd = UIHelper.MakeButton("Add", Color.FromArgb(46, 125, 50), 100);
            btnAdd.Location = new Point(340, 412);
            btnAdd.Click += (s, e) => AddCategory();

            var btnEdit = UIHelper.MakeButton("Rename", UIHelper.SuperAdmin, 100);
            btnEdit.Location = new Point(340, 448);
            btnEdit.Click += (s, e) => RenameCategory();

            var btnDelete = UIHelper.MakeButton("Delete", Color.FromArgb(120, 20, 20), 100);
            btnDelete.Location = new Point(448, 412);
            btnDelete.Click += (s, e) => DeleteCategory();

            Controls.AddRange(new Control[] { grid, lblNew, txtCategory, lblError, btnAdd, btnEdit, btnDelete });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable("SELECT CategoryId, CategoryName FROM RoomCategories ORDER BY CategoryName");
            if (grid.Columns["CategoryId"] != null) grid.Columns["CategoryId"].Visible = false;
        }

        private bool NameIsDuplicate(string name, int? excludingId = null)
        {
            string sql = "SELECT COUNT(*) FROM RoomCategories WHERE CategoryName = @Name";
            if (excludingId.HasValue) sql += " AND CategoryId <> @Id";
            var parms = excludingId.HasValue
                ? new[] { new SqlParameter("@Name", name), new SqlParameter("@Id", excludingId.Value) }
                : new[] { new SqlParameter("@Name", name) };
            return Convert.ToInt32(DbHelper.ExecuteScalar(sql, parms)) > 0;
        }

        private void AddCategory()
        {
            string name = txtCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { ShowError("Category name cannot be empty."); return; }
            if (NameIsDuplicate(name)) { ShowError("That category already exists."); return; }

            DbHelper.ExecuteNonQuery("INSERT INTO RoomCategories (CategoryName) VALUES (@Name)",
                new SqlParameter("@Name", name));
            txtCategory.Clear();
            LoadData();
        }

        private void RenameCategory()
        {
            if (grid.CurrentRow == null) { ShowError("Select a category to rename."); return; }
            int id = Convert.ToInt32(grid.CurrentRow.Cells["CategoryId"].Value);
            string name = txtCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { ShowError("Category name cannot be empty."); return; }
            if (NameIsDuplicate(name, id)) { ShowError("That category already exists."); return; }

            DbHelper.ExecuteNonQuery("UPDATE RoomCategories SET CategoryName = @Name WHERE CategoryId = @Id",
                new SqlParameter("@Name", name), new SqlParameter("@Id", id));
            LoadData();
        }

        private void DeleteCategory()
        {
            if (grid.CurrentRow == null) { ShowError("Select a category to delete."); return; }
            int id = Convert.ToInt32(grid.CurrentRow.Cells["CategoryId"].Value);
            string name = grid.CurrentRow.Cells["CategoryName"].Value.ToString();

            int inUse = Convert.ToInt32(DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Rooms WHERE RoomType = @Name", new SqlParameter("@Name", name)));
            if (inUse > 0)
            {
                ShowError("Cannot delete: at least one room still uses this category.");
                return;
            }

            DbHelper.ExecuteNonQuery("DELETE FROM RoomCategories WHERE CategoryId = @Id", new SqlParameter("@Id", id));
            txtCategory.Clear();
            LoadData();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
