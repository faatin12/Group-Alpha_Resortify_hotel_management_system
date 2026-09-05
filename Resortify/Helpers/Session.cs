namespace Resortify.Helpers
{
    /// <summary>Holds who is currently logged in for the lifetime of the app run.</summary>
    public static class Session
    {
        public static int UserId { get; private set; }
        public static string FullName { get; private set; }
        public static string Email { get; private set; }
        public static string UserType { get; private set; } // SuperAdmin | Admin | Customer

        /// <summary>Only meaningful when UserType == "Admin". Each Admin owns exactly one Hotel.</summary>
        public static int? HotelId { get; set; }
        public static string HotelName { get; set; }

        public static void SignIn(int userId, string fullName, string email, string userType)
        {
            UserId = userId;
            FullName = fullName;
            Email = email;
            UserType = userType;
            HotelId = null;
            HotelName = null;
        }

        public static void SignOut()
        {
            UserId = 0;
            FullName = null;
            Email = null;
            UserType = null;
            HotelId = null;
            HotelName = null;
        }
    }
}
