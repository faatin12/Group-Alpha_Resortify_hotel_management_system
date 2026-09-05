using System;
using System.Security.Cryptography;
using System.Text;

namespace Resortify.Helpers
{
    /// <summary>
    /// Passwords are never stored in plain text (see Users.Password in
    /// Schema.sql: "NOT NULL -- hashed"). We use a plain SHA-256 hash, which
    /// matches the level of the CSC 2210 lab work this project builds on;
    /// swap in BCrypt/PBKDF2 with a per-user salt for anything real-world.
    /// </summary>
    public static class PasswordHelper
    {
        public static string HashPassword(string plainText)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainText ?? string.Empty));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public static bool Verify(string plainText, string storedHash)
        {
            return string.Equals(HashPassword(plainText), storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
