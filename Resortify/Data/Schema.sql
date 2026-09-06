-- =========================================================
-- Resortify database schema
-- Mirrors Chapter 4 / 5 of the project report, plus a
-- RoomCategories table which backs the Super Admin
-- "manage master room category list" feature (FR-SA-6).
-- Safe to re-run: drops and recreates the database.
-- =========================================================

IF DB_ID('Resortify') IS NOT NULL
BEGIN
    ALTER DATABASE Resortify SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Resortify;
END
GO

CREATE DATABASE Resortify;
GO

USE Resortify;
GO

CREATE TABLE Users (
    UserId      INT IDENTITY(1,1) PRIMARY KEY,
    FullName    VARCHAR(100)  NOT NULL,
    Email       VARCHAR(100)  NOT NULL UNIQUE,
    Password    VARCHAR(255)  NOT NULL,               -- SHA-256 hash, never plain text
    Phone       VARCHAR(20)   NULL,
    Address     VARCHAR(255)  NULL,
    UserType    VARCHAR(20)   NOT NULL CHECK (UserType IN ('SuperAdmin','Admin','Customer')),
    Status      VARCHAR(20)   NOT NULL DEFAULT 'Pending'
                              CHECK (Status IN ('Pending','Approved','Suspended','Rejected')),
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE RoomCategories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName VARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Hotels (
    HotelId     INT IDENTITY(1,1) PRIMARY KEY,
    OwnerId     INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    HotelName   VARCHAR(100) NOT NULL,
    Category    VARCHAR(50)  NOT NULL,
    City        VARCHAR(50)  NOT NULL,
    Address     VARCHAR(255) NOT NULL,
    Phone       VARCHAR(20)  NULL,
    StarRating  DECIMAL(2,1) NULL,
    ImagePath   VARCHAR(255) NULL,
    Status      VARCHAR(20)  NOT NULL DEFAULT 'Pending'
                             CHECK (Status IN ('Pending','Approved','Suspended')),
    CreatedAt   DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

-- Rooms: RoomSize / BedType / MaxGuests / Amenities are read by
-- HotelDetailsForm.cs and CustomerHomeForm.cs (via ISNULL(...) so
-- NULL is fine and falls back to a friendly default in the UI).
CREATE TABLE Rooms (
    RoomId          INT IDENTITY(1,1) PRIMARY KEY,
    HotelId         INT NOT NULL FOREIGN KEY REFERENCES Hotels(HotelId),
    RoomType        VARCHAR(50)   NOT NULL,
    PricePerNight   DECIMAL(10,2) NOT NULL CHECK (PricePerNight > 0),
    TotalRooms      INT           NOT NULL CHECK (TotalRooms >= 0),
    MinAvailability INT           NOT NULL DEFAULT 1,
    RoomSize        VARCHAR(50)   NULL,
    BedType         VARCHAR(50)   NULL,
    MaxGuests       INT           NULL,
    Amenities       VARCHAR(500)  NULL,
    Description     VARCHAR(500)  NULL,
    ImagePath       VARCHAR(255)  NULL
);
GO

-- Cart: Guests / DiscountAmount are written by HotelDetailsForm.cs's
-- "Add Booking to Cart" insert and read back by BookingCartForm.cs.
CREATE TABLE Cart (
    CartId         INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId     INT  NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    RoomId         INT  NOT NULL FOREIGN KEY REFERENCES Rooms(RoomId),
    CheckInDate    DATE NOT NULL,
    CheckOutDate   DATE NOT NULL,
    Quantity       INT  NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    Guests         INT  NOT NULL DEFAULT 1,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    AddedDate      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Cart_Dates CHECK (CheckOutDate > CheckInDate)
);
GO

-- ServiceCatalog: master list of add-on services offered at checkout
-- (HotelDetailsForm.cs "Stay Extras & Guest Services" checklist).
CREATE TABLE ServiceCatalog (
    ServiceId   INT IDENTITY(1,1) PRIMARY KEY,
    ServiceName VARCHAR(100)  NOT NULL,
    Price       DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsFree      BIT           NOT NULL DEFAULT 0,
    Active      BIT           NOT NULL DEFAULT 1
);
GO

-- CartServices: services a customer attached to one cart line item.
CREATE TABLE CartServices (
    CartServiceId INT IDENTITY(1,1) PRIMARY KEY,
    CartId        INT NOT NULL FOREIGN KEY REFERENCES Cart(CartId),
    ServiceId     INT NOT NULL FOREIGN KEY REFERENCES ServiceCatalog(ServiceId),
    Quantity      INT NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    UnitPrice     DECIMAL(10,2) NOT NULL
);
GO

-- Coupons: master coupon codes, checked/applied in CheckoutForm.cs.
CREATE TABLE Coupons (
    CouponId              INT IDENTITY(1,1) PRIMARY KEY,
    Code                  VARCHAR(30)   NOT NULL UNIQUE,
    DiscountPercent       DECIMAL(5,2)  NOT NULL CHECK (DiscountPercent BETWEEN 1 AND 100),
    MaxDiscountAmount     DECIMAL(10,2) NULL,
    MinimumBookingAmount  DECIMAL(10,2) NOT NULL DEFAULT 0,
    ValidFrom             DATE          NOT NULL,
    ValidTo               DATE          NOT NULL,
    MaxUses               INT           NULL,
    UsedCount             INT           NOT NULL DEFAULT 0,
    Active                BIT           NOT NULL DEFAULT 1,
    CONSTRAINT CK_Coupons_Dates CHECK (ValidTo > ValidFrom)
);
GO

-- CartCoupons: at most one applied coupon per customer's active cart
-- (CheckoutForm.cs MERGEs into this by CustomerId).
CREATE TABLE CartCoupons (
    CustomerId     INT NOT NULL PRIMARY KEY FOREIGN KEY REFERENCES Users(UserId),
    CouponId       INT NOT NULL FOREIGN KEY REFERENCES Coupons(CouponId),
    CouponCode     VARCHAR(30) NOT NULL,
    DiscountAmount DECIMAL(10,2) NOT NULL,
    AppliedAt      DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Bookings: CustomerNotificationShown flags a one-time "your booking
-- status changed" popup shown on the Customer Dashboard.
CREATE TABLE Bookings (
    BookingId                 INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId                INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    BookingDate               DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount               DECIMAL(10,2) NOT NULL CHECK (TotalAmount >= 0),
    PaymentMethod             VARCHAR(30) NOT NULL,
    Status                    VARCHAR(20) NOT NULL DEFAULT 'Confirmed'
                                          CHECK (Status IN ('Confirmed','Cancelled','Completed')),
    CustomerNotificationShown BIT NOT NULL DEFAULT 0
);
GO

-- BookingItems: Quantity (rooms booked) / Guests are used throughout
-- CheckoutForm.cs, BookingHistoryForm.cs and HotelDetailsForm.cs.
CREATE TABLE BookingItems (
    BookingItemId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId     INT NOT NULL FOREIGN KEY REFERENCES Bookings(BookingId),
    RoomId        INT NOT NULL FOREIGN KEY REFERENCES Rooms(RoomId),
    CheckInDate   DATE NOT NULL,
    CheckOutDate  DATE NOT NULL,
    Nights        INT NOT NULL CHECK (Nights > 0),
    Quantity      INT NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    Guests        INT NOT NULL DEFAULT 1,
    UnitPrice     DECIMAL(10,2) NOT NULL,
    Subtotal      DECIMAL(10,2) NOT NULL
);
GO

-- BookingItemServices: services copied over from CartServices at
-- checkout time, tied to the finalised booking line (CheckoutForm.cs).
CREATE TABLE BookingItemServices (
    BookingItemServiceId INT IDENTITY(1,1) PRIMARY KEY,
    BookingItemId         INT NOT NULL FOREIGN KEY REFERENCES BookingItems(BookingItemId),
    ServiceId             INT NOT NULL FOREIGN KEY REFERENCES ServiceCatalog(ServiceId),
    Quantity              INT NOT NULL DEFAULT 1 CHECK (Quantity > 0),
    UnitPrice             DECIMAL(10,2) NOT NULL
);
GO

CREATE TABLE Reviews (
    ReviewId    INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId  INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    HotelId     INT NOT NULL FOREIGN KEY REFERENCES Hotels(HotelId),
    Rating      INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment     VARCHAR(500) NULL,
    ReviewDate  DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Offers (
    OfferId          INT IDENTITY(1,1) PRIMARY KEY,
    RoomId           INT NOT NULL FOREIGN KEY REFERENCES Rooms(RoomId),
    DiscountPercent  DECIMAL(5,2) NOT NULL CHECK (DiscountPercent BETWEEN 1 AND 100),
    StartDate        DATE NOT NULL,
    EndDate          DATE NOT NULL,
    CONSTRAINT CK_Offers_Dates CHECK (EndDate > StartDate)
);
GO

-- Staff: per-hotel housekeeping / maintenance / front-desk roster,
-- managed from StaffManagementForm.cs / StaffDialog.cs (Admin role).
CREATE TABLE Staff (
    StaffId    INT IDENTITY(1,1) PRIMARY KEY,
    HotelId    INT NOT NULL FOREIGN KEY REFERENCES Hotels(HotelId),
    FullName   VARCHAR(100) NOT NULL,
    Role       VARCHAR(50)  NOT NULL,
    Phone      VARCHAR(20)  NULL,
    Shift      VARCHAR(30)  NOT NULL,
    Status     VARCHAR(20)  NOT NULL DEFAULT 'Active'
                            CHECK (Status IN ('Active','Inactive')),
    HiredDate  DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

-- =========================================================
-- Sample data
-- Password for every seeded account is: Passw0rd!
-- except the two quick-login demo accounts below, which use: 123456
-- (both stored as SHA-256 hashes so LoginForm's hash-compare works out of the box)
-- =========================================================

-- Password hashes below are filled in by DbHelper.EnsureDatabaseCreated the first
-- time the app runs against a freshly-created database:
--   Password = 'PENDING_HASH'     -> SHA-256 hash of DefaultSeedPassword ("Passw0rd!")
--   Password = 'PENDING_HASH_ALT' -> SHA-256 hash of AltSeedPassword ("123456")
-- so every seeded account can log in with the password shown above.

INSERT INTO RoomCategories (CategoryName) VALUES
('Standard'), ('Deluxe'), ('Suite'), ('Villa'), ('Cottage');
GO

INSERT INTO Users (FullName, Email, Password, Phone, Address, UserType, Status) VALUES
('Faatin Muhaimin', 'admin@resortify.com', 'PENDING_HASH', '01710000001', 'Dhaka', 'SuperAdmin', 'Approved'),
('Rahim Uddin', 'rahim@bluelagoon.com', 'PENDING_HASH', '01710000002', 'St. Martin''s', 'Admin', 'Approved'),
('Karim Sheikh', 'karim@coxsbazarbreeze.com', 'PENDING_HASH', '01710000003', 'Cox''s Bazar', 'Admin', 'Approved'),
('Rafi Ahmed', 'rafi@gmail.com', 'PENDING_HASH', '01710000004', 'Dhaka', 'Customer', 'Approved'),
('Nusrat Jahan', 'nusrat@gmail.com', 'PENDING_HASH', '01710000005', 'Chattogram', 'Customer', 'Approved'),
('Tanvir Hasan', 'tanvir@gmail.com', 'PENDING_HASH', '01710000006', 'Sylhet', 'Customer', 'Approved'),
-- Quick-login demo accounts, password '123456' (see PENDING_HASH_ALT note in DbHelper.cs)
('Super Admin', 'super@gmail.com', 'PENDING_HASH_ALT', '01700000000', 'Dhaka', 'SuperAdmin', 'Approved'),
('Demo Admin', 'admin@gmail.com', 'PENDING_HASH_ALT', '01700000001', 'Dhaka', 'Admin', 'Approved');
GO

INSERT INTO Hotels (OwnerId, HotelName, Category, City, Address, Phone, StarRating, Status) VALUES
(2, 'Blue Lagoon Resort', 'Resort', 'St. Martin''s', 'Beach Road, St. Martin''s Island', '01810000001', 4.7, 'Approved'),
(3, 'Cox''s Bazar Breeze', 'Hotel', 'Cox''s Bazar', 'Marine Drive, Cox''s Bazar', '01810000002', 4.3, 'Approved'),
((SELECT UserId FROM Users WHERE Email = 'admin@gmail.com'), 'Emerald Hills Retreat', 'Hotel', 'Dhaka', 'Gulshan Avenue, Dhaka', '01810000003', 4.5, 'Approved');
GO

INSERT INTO Rooms (HotelId, RoomType, PricePerNight, TotalRooms, MinAvailability, RoomSize, BedType, MaxGuests, Amenities, Description) VALUES
(1, 'Deluxe Sea View', 120.00, 10, 2, '32 sqm', 'King', 2, 'Free WiFi, Sea view balcony, Air conditioning', 'Beachfront room with private balcony'),
(1, 'Family Suite', 180.00, 6, 1, '55 sqm', 'Two Queen', 5, 'Free WiFi, Kitchenette, Two bathrooms', 'Two-bedroom suite for up to 5 guests'),
(2, 'Standard Twin', 70.00, 20, 4, '24 sqm', 'Twin', 2, 'Free WiFi, Air conditioning', 'Twin bed room with city view'),
((SELECT HotelId FROM Hotels WHERE HotelName = 'Emerald Hills Retreat'), 'Standard Room', 90.00, 12, 2, '26 sqm', 'Queen', 2, 'Free WiFi, Air conditioning, Work desk', 'Comfortable city-view room for the demo Admin account');
GO

INSERT INTO ServiceCatalog (ServiceName, Price, IsFree, Active) VALUES
('WiFi', 0.00, 1, 1),
('Airport Pickup', 25.00, 0, 1),
('Breakfast Buffet', 15.00, 0, 1),
('Spa Access', 40.00, 0, 1),
('Late Checkout', 20.00, 0, 1);
GO

INSERT INTO Coupons (Code, DiscountPercent, MaxDiscountAmount, MinimumBookingAmount, ValidFrom, ValidTo, MaxUses, UsedCount, Active) VALUES
('WELCOME10', 10.00, 50.00, 50.00, '2026-01-01', '2026-12-31', 500, 0, 1),
('SUMMER25', 25.00, 100.00, 100.00, '2026-06-01', '2026-09-30', 200, 0, 1);
GO

INSERT INTO Bookings (CustomerId, TotalAmount, PaymentMethod, Status) VALUES
(4, 360.00, 'Credit Card', 'Confirmed'),
(5, 280.00, 'Mobile Banking', 'Confirmed'),
(6, 360.00, 'Credit Card', 'Completed');
GO

INSERT INTO BookingItems (BookingId, RoomId, CheckInDate, CheckOutDate, Nights, Quantity, Guests, UnitPrice, Subtotal) VALUES
(1, 1, '2026-08-20', '2026-08-23', 3, 1, 2, 120.00, 360.00),
(2, 3, '2026-09-01', '2026-09-03', 2, 2, 3, 70.00, 280.00),
(3, 2, '2026-07-10', '2026-07-12', 2, 1, 4, 180.00, 360.00);
GO

INSERT INTO Reviews (CustomerId, HotelId, Rating, Comment) VALUES
(4, 1, 5, 'Amazing beach view, great service!'),
(5, 2, 4, 'Clean rooms, friendly staff.'),
(6, 1, 5, 'Best family vacation ever.');
GO

INSERT INTO Offers (RoomId, DiscountPercent, StartDate, EndDate) VALUES
(1, 15.00, '2026-09-01', '2026-09-30'),
(3, 10.00, '2026-08-25', '2026-09-15');
GO

INSERT INTO Staff (HotelId, FullName, Role, Phone, Shift, Status) VALUES
(1, 'Jamal Hossain', 'Housekeeping', '01910000001', 'Morning', 'Active'),
(1, 'Farida Begum', 'Front Desk', '01910000002', 'Evening', 'Active'),
(2, 'Milon Khan', 'Maintenance', '01910000003', 'Morning', 'Active');
GO

-- =========================================================
-- Chapter 5 feature queries (kept here as reference / for
-- SSMS use -- the C# app runs equivalent parameterised
-- versions of these itself).
-- =========================================================

-- Query 7: Platform-wide sales & commission per hotel (Super Admin dashboard)
-- SELECT h.HotelName, COUNT(bi.BookingItemId) AS Bookings, SUM(bi.Subtotal) AS Revenue,
--        SUM(bi.Subtotal) * 0.10 AS Commission
-- FROM Hotels h
-- JOIN Rooms r ON r.HotelId = h.HotelId
-- JOIN BookingItems bi ON bi.RoomId = r.RoomId
-- GROUP BY h.HotelName;

-- Low-rated hotels report (Super Admin)
-- SELECT h.HotelId, h.HotelName, AVG(CAST(rv.Rating AS DECIMAL(3,2))) AS AvgRating
-- FROM Hotels h JOIN Reviews rv ON rv.HotelId = h.HotelId
-- GROUP BY h.HotelId, h.HotelName
-- HAVING AVG(CAST(rv.Rating AS DECIMAL(3,2))) < 2.5;

-- Query 8: Availability dashboard (Admin)
-- SELECT r.RoomId, r.RoomType, r.TotalRooms,
--        r.TotalRooms - ISNULL(SUM(bi.Nights * 0 + 1), 0) AS BookedCount -- see AvailabilityRepository for real logic
-- FROM Rooms r LEFT JOIN BookingItems bi ON bi.RoomId = r.RoomId
-- WHERE r.HotelId = @HotelId
-- GROUP BY r.RoomId, r.RoomType, r.TotalRooms;

-- Query 9: Reviews for a hotel, with reviewer name
-- SELECT u.FullName, rv.Rating, rv.Comment, rv.ReviewDate
-- FROM Reviews rv JOIN Users u ON u.UserId = rv.CustomerId
-- WHERE rv.HotelId = @HotelId
-- ORDER BY rv.ReviewDate DESC;