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