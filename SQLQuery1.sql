ALTER TABLE Bookings 
ADD BookingType VARCHAR(50) DEFAULT 'Standard Booking',
    PaymentStatus VARCHAR(50) DEFAULT 'Pending',
    BookingStatus VARCHAR(50) DEFAULT 'Confirmed';