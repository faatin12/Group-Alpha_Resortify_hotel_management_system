using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class RoomManagementForm : Form
    {
        private DataGridView grid;
        private TextBox txtType, txtPrice, txtTotal, txtMinAvail, txtDesc;
        private Label lblError;
        private int? editingRoomId = null;

        public RoomManagementForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Room / Package Management";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Room / Package Management", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            grid = new DataGridView { Location = new Point(24, 100), Size = new Size(892, 260) };
            UIHelper.StyleGrid(grid);
            grid.SelectionChanged += (s, e) => PopulateFormFromSelection();

            int y = 380;
            var lblForm = new Label { Text = "Add / Update Room", Location = new Point(24, y), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            y += 28;

            (Label, TextBox) Field(string label, int width = 180)
            {
                var l = new Label { Text = label, AutoSize = true };
                var t = new TextBox { Width = width, Font = UIHelper.BaseFont };
                return (l, t);
            }

            var (l1, t1) = Field("Room Type"); txtType = t1;
            var (l2, t2) = Field("Price/Night", 100); txtPrice = t2;
            var (l3, t3) = Field("Total Rooms", 100); txtTotal = t3;
            var (l4, t4) = Field("Min Availability", 100); txtMinAvail = t4;

            l1.Location = new Point(24, y); t1.Location = new Point(24, y + 20);
            l2.Location = new Point(224, y); t2.Location = new Point(224, y + 20);
            l3.Location = new Point(344, y); t3.Location = new Point(344, y + 20);
            l4.Location = new Point(464, y); t4.Location = new Point(464, y + 20);
            y += 54;

            var l5 = new Label { Text = "Description", Location = new Point(24, y), AutoSize = true };
            txtDesc = new TextBox { Location = new Point(24, y + 20), Width = 540, Font = UIHelper.BaseFont };
            y += 54;

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(24, y);
            y += 26;

            var btnAdd = UIHelper.MakeButton("Add New", Color.FromArgb(46, 125, 50), 120);
            btnAdd.Location = new Point(24, y);
            btnAdd.Click += (s, e) => SaveRoom(isNew: true);

            var btnUpdate = UIHelper.MakeButton("Update Selected", UIHelper.AdminColor, 140);
            btnUpdate.Location = new Point(154, y);
            btnUpdate.Click += (s, e) => SaveRoom(isNew: false);

            var btnDelete = UIHelper.MakeButton("Delete Selected", Color.FromArgb(120, 20, 20), 140);
            btnDelete.Location = new Point(304, y);
            btnDelete.Click += (s, e) => DeleteRoom();

            var btnClear = UIHelper.MakeButton("Clear Form", Color.Gray, 110);
            btnClear.Location = new Point(454, y);
            btnClear.Click += (s, e) => ClearForm();

            Controls.AddRange(new Control[]
            {
                grid, lblForm, l1, t1, l2, t2, l3, t3, l4, t4, l5, txtDesc, lblError,
                btnAdd, btnUpdate, btnDelete, btnClear
            });
        }

        private void LoadData()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT RoomId, RoomType, PricePerNight, TotalRooms, MinAvailability, Description
                  FROM Rooms WHERE HotelId = @Id ORDER BY RoomType",
                new SqlParameter("@Id", Session.HotelId));
            if (grid.Columns["RoomId"] != null) grid.Columns["RoomId"].Visible = false;
        }

        private void PopulateFormFromSelection()
        {
            if (grid.CurrentRow == null) return;
            editingRoomId = Convert.ToInt32(grid.CurrentRow.Cells["RoomId"].Value);
            txtType.Text = grid.CurrentRow.Cells["RoomType"].Value.ToString();
            txtPrice.Text = grid.CurrentRow.Cells["PricePerNight"].Value.ToString();
            txtTotal.Text = grid.CurrentRow.Cells["TotalRooms"].Value.ToString();
            txtMinAvail.Text = grid.CurrentRow.Cells["MinAvailability"].Value.ToString();
            txtDesc.Text = grid.CurrentRow.Cells["Description"].Value?.ToString();
        }

        private void ClearForm()
        {
            editingRoomId = null;
            txtType.Clear(); txtPrice.Clear(); txtTotal.Clear(); txtMinAvail.Clear(); txtDesc.Clear();
            lblError.Visible = false;
        }

        private bool ValidateFields(out decimal price, out int total, out int minAvail)
        {
            price = 0; total = 0; minAvail = 1;
            if (string.IsNullOrWhiteSpace(txtType.Text))
            {
                ShowError("Room type is required."); return false;
            }
            if (!decimal.TryParse(txtPrice.Text, out price) || price <= 0)
            {
                ShowError("Price per night must be a positive number."); return false;
            }
            if (!int.TryParse(txtTotal.Text, out total) || total < 0)
            {
                ShowError("Total rooms must be zero or a positive whole number."); return false;
            }
            if (!string.IsNullOrWhiteSpace(txtMinAvail.Text) && (!int.TryParse(txtMinAvail.Text, out minAvail) || minAvail < 0))
            {
                ShowError("Minimum availability must be zero or a positive whole number."); return false;
            }
            if (string.IsNullOrWhiteSpace(txtMinAvail.Text)) minAvail = 1;
            return true;
        }

        private void SaveRoom(bool isNew)
        {
            if (!ValidateFields(out decimal price, out int total, out int minAvail)) return;

            if (isNew)
            {
                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO Rooms (HotelId, RoomType, PricePerNight, TotalRooms, MinAvailability, Description)
                      VALUES (@Hotel, @Type, @Price, @Total, @Min, @Desc)",
                    new SqlParameter("@Hotel", Session.HotelId),
                    new SqlParameter("@Type", txtType.Text.Trim()),
                    new SqlParameter("@Price", price),
                    new SqlParameter("@Total", total),
                    new SqlParameter("@Min", minAvail),
                    new SqlParameter("@Desc", (object)txtDesc.Text.Trim() ?? DBNull.Value));
            }
            else
            {
                if (editingRoomId == null) { ShowError("Select a room to update first."); return; }
                DbHelper.ExecuteNonQuery(
                    @"UPDATE Rooms SET RoomType=@Type, PricePerNight=@Price, TotalRooms=@Total,
                      MinAvailability=@Min, Description=@Desc WHERE RoomId = @Id",
                    new SqlParameter("@Type", txtType.Text.Trim()),
                    new SqlParameter("@Price", price),
                    new SqlParameter("@Total", total),
                    new SqlParameter("@Min", minAvail),
                    new SqlParameter("@Desc", (object)txtDesc.Text.Trim() ?? DBNull.Value),
                    new SqlParameter("@Id", editingRoomId.Value));
            }

            ClearForm();
            LoadData();
        }

        private void DeleteRoom()
        {
            if (editingRoomId == null) { ShowError("Select a room to delete first."); return; }

            int upcoming = Convert.ToInt32(DbHelper.ExecuteScalar(
                @"SELECT COUNT(*) FROM BookingItems bi
                  JOIN Bookings b ON b.BookingId = bi.BookingId
                  WHERE bi.RoomId = @Id AND b.Status = 'Confirmed' AND bi.CheckOutDate >= CAST(GETDATE() AS DATE)",
                new SqlParameter("@Id", editingRoomId.Value)));

            if (upcoming > 0)
            {
                ShowError("Cannot delete: this room has upcoming bookings.");
                return;
            }

            if (MessageBox.Show("Delete this room type?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            DbHelper.ExecuteNonQuery("DELETE FROM Rooms WHERE RoomId = @Id", new SqlParameter("@Id", editingRoomId.Value));
            ClearForm();
            LoadData();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
