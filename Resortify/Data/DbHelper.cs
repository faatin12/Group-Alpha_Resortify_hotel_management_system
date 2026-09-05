using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Resortify.Helpers;

namespace Resortify.Data
{
    /// <summary>
    /// Central place for the connection string and every raw ADO.NET call in
    /// the app. Forms call the static helpers here instead of opening their
    /// own SqlConnection, which keeps connection lifetime handling in one spot.
    /// </summary>
    public static class DbHelper
    {
        // ---- EDIT THIS if your SQL Server instance is not LocalDB ----
        // Examples:
        //   Full SQL Server:   "Data Source=localhost;Initial Catalog=Resortify;Integrated Security=True;TrustServerCertificate=True;"
        //   SQL auth:          "Data Source=localhost;Initial Catalog=Resortify;User Id=sa;Password=yourpassword;TrustServerCertificate=True;"
        private const string ServerPart = @"Data Source=(localdb)\MSSQLLocalDB;TrustServerCertificate=True;Integrated Security=True;";

        public static string ConnectionString => ServerPart + "Initial Catalog=Resortify;";
        private static string MasterConnectionString => ServerPart + "Initial Catalog=master;";

        public const string DefaultSeedPassword = "Passw0rd!";

        /// <summary>Password for the two quick-login demo accounts (super@gmail.com / admin@gmail.com).</summary>
        public const string AltSeedPassword = "123456";

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Creates the Resortify database from Data\Schema.sql if it does not
        /// already exist *and look complete* (checked via the Cart table --
        /// a leftover half-built DB from an earlier failed run would exist
        /// but be missing tables, which we want to detect and fix rather
        /// than silently use), then patches the seeded users' password
        /// hashes so they can log in with DefaultSeedPassword. Safe to call
        /// every startup.
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            bool looksComplete;
            using (var conn = new SqlConnection(MasterConnectionString))
            {
                conn.Open();
                using var cmd = new SqlCommand(
                    "SELECT CASE WHEN DB_ID('Resortify') IS NULL THEN CAST(NULL AS INT) " +
                    "ELSE OBJECT_ID('Resortify.dbo.Cart') END", conn);
                var result = cmd.ExecuteScalar();
                looksComplete = result != DBNull.Value && result != null;
            }

            if (looksComplete) return;

            string scriptPath = Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql");
            if (!File.Exists(scriptPath))
            {
                // fall back to the source-tree copy when running from a build
                // output folder that didn't copy the file for some reason
                throw new FileNotFoundException(
                    "Schema.sql not found next to the executable. Make sure Data\\Schema.sql " +
                    "has 'Copy to Output Directory' = 'Copy if newer' in its file properties.",
                    scriptPath);
            }

            string script = File.ReadAllText(scriptPath);
            string[] batches = script.Split(new[] { "\nGO", "\r\nGO", "\nGO\r", "\ngo\n" },
                StringSplitOptions.RemoveEmptyEntries);

            using (var conn = new SqlConnection(MasterConnectionString))
            {
                conn.Open();
                foreach (var rawBatch in batches)
                {
                    string batch = rawBatch.Trim();
                    if (batch.Length == 0) continue;
                    using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 60 };
                    cmd.ExecuteNonQuery();
                }
            }

            // Patch every seeded row that has a placeholder password so the
            // sample accounts can actually log in.
            string hash = PasswordHelper.HashPassword(DefaultSeedPassword);
            ExecuteNonQuery("UPDATE Users SET Password = @Hash WHERE Password = 'PENDING_HASH'",
                new SqlParameter("@Hash", hash));

            string altHash = PasswordHelper.HashPassword(AltSeedPassword);
            ExecuteNonQuery("UPDATE Users SET Password = @Hash WHERE Password = 'PENDING_HASH_ALT'",
                new SqlParameter("@Hash", altHash));
        }

        public static DataTable GetDataTable(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>Runs several statements as one transaction; all-or-nothing (used for checkout).</summary>
        public static void RunTransaction(Action<SqlConnection, SqlTransaction> work)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                work(conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
