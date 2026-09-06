IF OBJECT_ID('dbo.Staff', 'U') IS NOT NULL
    DROP TABLE dbo.Staff;
GO

CREATE TABLE Staff (
    StaffId     INT IDENTITY(1,1) PRIMARY KEY,
    HotelId     INT NOT NULL FOREIGN KEY REFERENCES Hotels(HotelId),
    FullName    VARCHAR(100)  NOT NULL,
    Role        VARCHAR(30)   NOT NULL CHECK (Role IN ('Cleaning','Housekeeping','Maintenance')),
    Phone       VARCHAR(20)   NULL,
    Shift       VARCHAR(10)   NOT NULL DEFAULT 'Day' CHECK (Shift IN ('Day','Night')),
    Status      VARCHAR(20)   NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Inactive')),
    HiredDate   DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

INSERT INTO Staff (HotelId, FullName, Role, Phone, Shift) VALUES
(1, 'Amina Rahman', 'Housekeeping', '01910000001', 'Day'),
(1, 'Sabbir Hossain', 'Cleaning', '01910000002', 'Night'),
(1, 'Jahid Islam', 'Maintenance', '01910000003', 'Day'),
(2, 'Mitu Akter', 'Housekeeping', '01910000004', 'Night'),
(2, 'Karim Molla', 'Cleaning', '01910000005', 'Day');
GO