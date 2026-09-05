# Resortify — C# WinForms Implementation

This is the "next phase" implementation described in Chapter 8 of the project
report: a C# Windows Forms desktop app (.NET 8) using ADO.NET
(`Microsoft.Data.SqlClient`) against a SQL Server database that mirrors the
schema in Chapter 4/5.

## Requirements

- Visual Studio 2022 (Community edition is fine) with the **.NET desktop
  development** workload installed.
- SQL Server LocalDB (installed automatically with Visual Studio's "SQL
  Server Express LocalDB" component — usually already present if you've done
  the CSC 2210 labs).

## Run it

1. Open `Resortify.sln` in Visual Studio.
2. Press **F5** (Start).
   - On first launch the app connects to `(localdb)\MSSQLLocalDB` and, if a
     `Resortify` database doesn't exist yet (or looks incomplete from an
     earlier failed run), automatically runs `Data\Schema.sql` to create it
     and seed sample data (3 hotels, 4 rooms, 8 users, sample
     bookings/reviews/offers).
   - Seeded accounts use one of two passwords — the app hashes both with
     SHA-256 into the seeded rows the first time it runs.
3. Log in with, for example:
   - **Quick login — Super Admin:** `super@gmail.com` / password `123456`
   - **Quick login — Hotel Owner (Admin):** `admin@gmail.com` / password
     `123456` (comes with a hotel, "Emerald Hills Retreat", already set up)
   - Original demo accounts, password `Passw0rd!`:
     - Super Admin: `admin@resortify.com`
     - Hotel Owner (Admin): `rahim@bluelagoon.com`
     - Customer: `rafi@gmail.com`

If your SQL Server isn't LocalDB, edit the `ServerPart` connection string
constant near the top of `Data\DbHelper.cs`.

## Project layout

```
Resortify/
  Resortify.sln
  Resortify/
    Program.cs                 entry point; bootstraps the DB, opens LoginForm
    Data/
      DbHelper.cs               connection string + all ADO.NET plumbing
      Schema.sql                CREATE DATABASE/TABLE + seed data (Ch. 4/5)
    Helpers/
      PasswordHelper.cs         SHA-256 hashing
      Session.cs                who's currently logged in
      UIHelper.cs               shared navy header / role colours / button style
    Forms/
      LoginForm.cs, SignUpForm.cs
      SuperAdminDashboardForm.cs, ManageHotelOwnersForm.cs, ViewAllUsersForm.cs,
      PlatformSalesReportForm.cs, LowRatedHotelsForm.cs,
      ManageRoomCategoriesForm.cs, ModerateReviewsForm.cs
      AdminDashboardForm.cs, HotelProfileForm.cs, RoomManagementForm.cs,
      AvailabilityDashboardForm.cs, EarningsReportForm.cs, CreateOfferForm.cs,
      AdminReviewsForm.cs
      CustomerHomeForm.cs, HotelDetailsForm.cs, BookingCartForm.cs,
      CheckoutForm.cs, BookingHistoryForm.cs, ReviewDialog.cs,
      SpecialOffersForm.cs
      UpdateProfileForm.cs (shared by Admin & Customer)
```

## What's implemented, mapped to the report

- **Login / Sign Up (Forms 1–2):** role selector, SHA-256 password check,
  inline min-length and password-mismatch validation, Admin accounts start
  `Pending` and need Super Admin approval; Customers are auto-approved.
- **Super Admin (Forms 3–5 + 3 extra sub-screens):** dashboard with platform
  totals; Manage Hotel Owners (Approve/Reject account, Suspend/Delete hotel,
  delete blocked while active bookings exist); View All Users; Platform
  Sales & Commission report (date-ranged, Query 7); Low-Rated Hotels report
  (`HAVING AVG(Rating) < 2.5`); Manage Room Categories (duplicate- and
  in-use-guarded); Moderate Reviews (delete with confirmation).
- **Admin / Hotel Owner (Forms 6–8 + extras):** Hotel Profile (Save disabled
  until required fields are filled, first submission goes in as `Pending`);
  Room/Package full CRUD (delete blocked if the room has upcoming bookings);
  Availability Dashboard with a red low-stock banner (Query 8, computed
  dynamically — see note below); Earnings & Booking Report with a running
  total; Create Discount Offer; read-only Reviews.
- **Customer (Forms 9–12 + extras):** Home/Browse with search + three
  ComboBox filters over a card list; Hotel Details with a room dropdown that
  shows the discounted price when an offer is active, plus read-only reviews
  (Query 9); Booking Cart with edit/remove and a computed total; Checkout
  that runs the whole booking as **one transaction** (Bookings row +
  one BookingItems row per room, then clears the cart) and shows a CONFIRMED
  invoice; Booking History with Cancel and Leave-a-Review; Special Offers
  list (Query 12).
- Every screen keeps the shared navy header / role-coloured title bar /
  button style from Chapter 6, and always has a Back or Logout so nothing is
  a dead end.

### One deliberate deviation from Chapter 5

Query 6 in the report decrements `Rooms.TotalRooms` directly at checkout.
That works but never lets stock recover once a booking's dates are in the
past. This implementation instead computes availability **dynamically**:
`Available = TotalRooms − (confirmed BookingItems whose CheckOutDate hasn't
passed yet)`. `TotalRooms` itself is never mutated by a booking, only by the
Admin editing the room. This also sidesteps the double-booking race Chapter
8 flags as a concern, since availability is always derived from current
bookings rather than a counter that can drift.

### Two hub screens not in the 12 mockups

The report's Chapter 6 pictures 12 form mockups, but the Super Admin
dashboard mockup ("six navigation buttons") and the "every dashboard exposes
a way to every child screen" navigation rule imply more sub-screens than were
individually mocked up. `AdminDashboardForm` (a landing hub for Hotel
Owners, mirroring the Super Admin dashboard's layout) and a few Super
Admin/Customer sub-screens (View All Users, Manage Room Categories,
Moderate Reviews, Low-Rated Hotels, Booking History, Special Offers, Update
Profile) were added to make every functional requirement in Chapter 2
actually reachable from somewhere.

## Known simplifications (student-project scope, called out honestly)

- Passwords are SHA-256 hashed but not salted — fine for this assignment,
  not what you'd ship in production (BCrypt/PBKDF2 + salt, as Chapter 8
  itself notes as future work).
- Payment is a form field, not a real gateway integration (also flagged as
  future work in Chapter 8).
- Hotel/room photo upload just stores a local file path picked via
  `OpenFileDialog`; it doesn't copy the file anywhere or display a thumbnail.
