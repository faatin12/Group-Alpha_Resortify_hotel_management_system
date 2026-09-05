using System.Drawing;
using System.Windows.Forms;

namespace Resortify.Helpers
{
    /// <summary>
    /// Every Resortify form shares: a navy app header, a role-coloured title
    /// bar, the same button style/typography, and a Back or Logout control
    /// (Chapter 6). These helpers keep that consistent without a designer.
    /// </summary>
    public static class UIHelper
    {
        public static readonly Color NavyHeader   = Color.FromArgb(21, 34, 56);
        public static readonly Color SuperAdmin   = Color.FromArgb(178, 34, 34);   // red
        public static readonly Color AdminColor   = Color.FromArgb(214, 118, 27);  // orange
        public static readonly Color CustomerColor= Color.FromArgb(35, 110, 68);   // green
        public static readonly Color Neutral      = Color.FromArgb(60, 60, 60);
        public static readonly Color LowStockRed  = Color.FromArgb(198, 40, 40);
        public static readonly Font  TitleFont    = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static readonly Font  BaseFont     = new Font("Segoe UI", 9.5F);

        /// <summary>
        /// Builds the shared navy "RESORTIFY" bar + a role-coloured strip with
        /// the screen title, plus an optional Back and/or Logout button on the
        /// right so no screen is ever a dead end.
        /// </summary>
        public static Panel BuildHeader(string screenTitle, Color roleColor,
            System.EventHandler onBack, System.EventHandler onLogout)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, Height = 84 };

            var navy = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = NavyHeader };
            var brand = new Label
            {
                Text = "RESORTIFY",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 8)
            };
            navy.Controls.Add(brand);

            var strip = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = roleColor };
            var titleLabel = new Label
            {
                Text = screenTitle,
                ForeColor = Color.White,
                Font = TitleFont,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            strip.Controls.Add(titleLabel);

            int rightX = 760;
            if (onLogout != null)
            {
                var logoutBtn = MakeButton("Logout", Color.FromArgb(80, 20, 20));
                logoutBtn.Location = new Point(rightX, 6);
                logoutBtn.Click += onLogout;
                strip.Controls.Add(logoutBtn);
                rightX -= 100;
            }
            if (onBack != null)
            {
                var backBtn = MakeButton("< Back", Color.FromArgb(70, 70, 70));
                backBtn.Location = new Point(rightX, 6);
                backBtn.Click += onBack;
                strip.Controls.Add(backBtn);
            }

            // strip added first so it docks directly under navy (Dock=Top stacks in reverse add order)
            wrapper.Controls.Add(strip);
            wrapper.Controls.Add(navy);
            return wrapper;
        }

        public static Button MakeButton(string text, Color backColor, int width = 90, int height = 30)
        {
            return new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Width = width,
                Height = height,
                Cursor = Cursors.Hand
            };
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = NavyHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 32;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Font = BaseFont;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
        }

        public static Label MakeErrorLabel()
        {
            return new Label
            {
                ForeColor = Color.FromArgb(198, 40, 40),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                AutoSize = true,
                Visible = false
            };
        }
    }
}
