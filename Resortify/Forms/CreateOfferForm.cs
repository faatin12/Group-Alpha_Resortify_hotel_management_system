using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Resortify.Data;
using Resortify.Helpers;

namespace Resortify.Forms
{
    public partial class CreateOfferForm : Form
    {
        private ComboBox cboRoom;
        private NumericUpDown numDiscount;
        private DateTimePicker dtStart, dtEnd;
        private Label lblError;
        private DataGridView grid;

        public CreateOfferForm()
        {
            InitializeComponent();
            LoadRooms();
            LoadOffers();
        }

        private void InitializeComponent()
        {
            Text = "Resortify - Create Discount Offer";
            ClientSize = new Size(880, 560);
            AutoScroll = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(UIHelper.BuildHeader("Create Discount Offer", UIHelper.AdminColor,
                onBack: (s, e) => Close(), onLogout: null));

            var lblRoom = new Label { Text = "Room", Location = new Point(24, 100), AutoSize = true, Font = UIHelper.BaseFont };
            cboRoom = new ComboBox { Location = new Point(24, 122), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = UIHelper.BaseFont };

            var lblDiscount = new Label { Text = "Discount %", Location = new Point(340, 100), AutoSize = true, Font = UIHelper.BaseFont };
            numDiscount = new NumericUpDown { Location = new Point(340, 122), Width = 100, Minimum = 1, Maximum = 100, Value = 10 };

            var lblStart = new Label { Text = "Start Date", Location = new Point(24, 164), AutoSize = true, Font = UIHelper.BaseFont };
            dtStart = new DateTimePicker { Location = new Point(24, 186), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var lblEnd = new Label { Text = "End Date", Location = new Point(190, 164), AutoSize = true, Font = UIHelper.BaseFont };
            dtEnd = new DateTimePicker { Location = new Point(190, 186), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30) };

            lblError = UIHelper.MakeErrorLabel();
            lblError.Location = new Point(24, 222);

            var btnCreate = UIHelper.MakeButton("Create Offer", UIHelper.AdminColor, 160);
            btnCreate.Location = new Point(24, 250);
            btnCreate.Click += BtnCreate_Click;

            var lblExisting = new Label { Text = "Active / Upcoming Offers", Location = new Point(24, 300), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            grid = new DataGridView { Location = new Point(24, 330), Size = new Size(652, 200) };
            UIHelper.StyleGrid(grid);

            Controls.AddRange(new Control[] { lblRoom, cboRoom, lblDiscount, numDiscount, lblStart, dtStart, lblEnd, dtEnd, lblError, btnCreate, lblExisting, grid });
        }

        private void LoadRooms()
        {
            var table = DbHelper.GetDataTable(
                "SELECT RoomId, RoomType, PricePerNight FROM Rooms WHERE HotelId = @Id ORDER BY RoomType",
                new SqlParameter("@Id", Session.HotelId));
            cboRoom.DisplayMember = "Display";
            cboRoom.ValueMember = "RoomId";
            table.Columns.Add("Display", typeof(string));
            foreach (System.Data.DataRow row in table.Rows)
                row["Display"] = $"{row["RoomType"]} (${Convert.ToDecimal(row["PricePerNight"]):N2}/night)";
            cboRoom.DataSource = table;
        }

        private void LoadOffers()
        {
            grid.DataSource = DbHelper.GetDataTable(
                @"SELECT o.OfferId, r.RoomType, o.DiscountPercent, o.StartDate, o.EndDate
                  FROM Offers o JOIN Rooms r ON r.RoomId = o.RoomId
                  WHERE r.HotelId = @Id AND o.EndDate >= CAST(GETDATE() AS DATE)
                  ORDER BY o.StartDate",
                new SqlParameter("@Id", Session.HotelId));
            if (grid.Columns["OfferId"] != null) grid.Columns["OfferId"].Visible = false;
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            if (cboRoom.SelectedValue == null)
            {
                ShowError("Add a room before creating an offer.");
                return;
            }
            if (dtEnd.Value.Date <= dtStart.Value.Date)
            {
                ShowError("End date must be after start date.");
                return;
            }

            DbHelper.ExecuteNonQuery(
                "INSERT INTO Offers (RoomId, DiscountPercent, StartDate, EndDate) VALUES (@Room, @Pct, @Start, @End)",
                new SqlParameter("@Room", cboRoom.SelectedValue),
                new SqlParameter("@Pct", numDiscount.Value),
                new SqlParameter("@Start", dtStart.Value.Date),
                new SqlParameter("@End", dtEnd.Value.Date));

            LoadOffers();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
