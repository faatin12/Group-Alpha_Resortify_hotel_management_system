using System;
using System.Windows.Forms;
using Resortify.Data;
using Resortify.Forms;

namespace Resortify
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration_Initialize();

            // Make sure the database exists and has the current schema before any form opens.
            try
            {
                DbHelper.EnsureDatabaseCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not connect to / initialise the Resortify database.\n\n" +
                    "Details: " + ex.Message + "\n\n" +
                    "Check the connection string in Data\\DbHelper.cs (ConnectionString field).",
                    "Database Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new LoginForm());
        }

        // Small wrapper so this file works whether or not the target SDK
        // generated ApplicationConfiguration.Initialize() for you.
        private static void ApplicationConfiguration_Initialize()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }
}
