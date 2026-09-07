# Resortify — C# WinForms Hotel & Resort Booking System

Resortify is a desktop hotel/resort booking platform built in **C# WinForms (.NET 8)**
with **ADO.NET** against a **SQL Server** database. It supports three roles —
**Customer**, **Hotel Owner (Admin)**, and **Super Admin** — covering the full
flow from browsing and booking a stay to platform-wide revenue reporting.

## Requirements

- Visual Studio 2022 (Community edition is fine) with the **.NET desktop
  development** workload installed.
- SQL Server LocalDB (installed automatically with Visual Studio's "SQL
  Server Express LocalDB" component).

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

## Features & Screenshots

Screenshots are stored in [`screenshots/`](screenshots/)

### Authentication

**Login** — role selector (Customer / Admin / Super Admin), SHA-256 password
check, "Show Password" toggle, quick-login demo credentials shown inline.

![Login](screenshots/login.png)

**Sign Up** — new Customer/Admin registration with inline validation
(min length, password match). Admin accounts start `Pending` and need
Super Admin approval; Customers are auto-approved.

![Sign Up](screenshots/signup.png)

### Customer

**Customer Dashboard** — search bar plus Location / Room type / Price /
Rating filters, a card grid of available hotels, and quick stats for cart
items, upcoming bookings, and active offers.

![Customer Dashboard](screenshots/customer-dashboard.png)

**Room Details & Booking** — pick room type, dates, room/guest counts, and
optional Stay Extras (WiFi, airport pickup, breakfast, late checkout, spa
access); live subtotal with any active discount applied, plus read-only
guest reviews for that hotel.

![Room Details & Booking](screenshots/room-details-booking.png)

**My Cart** — every pending booking with its services and computed total,
editable check-in/out dates, room and guest counts, with Update / Remove /
Clear Cart / Proceed to Checkout actions.

![My Cart](screenshots/my-cart.png)

**Checkout** — booking summary, Pay-at-Hotel or online payment (with
transaction ID) options, coupon code entry, and an itemized breakdown
(room subtotal, extras, discount, grand total) before confirming.

![Checkout](screenshots/checkout.png)

### Hotel Owner (Admin)

**Hotel Owner Dashboard** — per-hotel stats (room types, bookings, earnings
after the platform's 10% fee, average rating) with quick links to every
management screen for that hotel.

![Hotel Owner Dashboard](screenshots/hotel-owner-dashboard.png)

**Room / Package Management** — full CRUD over room types: price/night,
total rooms, minimum availability threshold, and description, with
Add / Update / Delete / Clear Form controls.

![Room / Package Management](screenshots/room-package-management.png)

**Availability Dashboard** — live room-type availability computed from
current bookings (total, booked, available) with a status flag for
low-stock room types.

![Availability Dashboard](screenshots/availability-dashboard.png)

### Super Admin

**Super Admin Dashboard** — platform-wide totals (approved hotels,
customers, bookings, revenue) with navigation into every platform
management screen: staff, hotel owners, users, reports, room categories,
and review moderation.

![Super Admin Dashboard](screenshots/superadmin-dashboard.png)

**Platform Sales & Commission Report** — date-ranged report of bookings,
revenue, and commission per hotel, with running totals for the selected
period.

![Platform Sales & Commission Report](screenshots/platform-sales-commission-report.png)

**Manage Hotel Owners** — one table listing every owner with account and
hotel status; Approve/Reject the account and Approve/Suspend/Delete the
hotel (delete is blocked while active bookings exist).

![Manage Hotel Owners](screenshots/manage-hotel-owners.png)

## What's implemented, mapped to the report

- **Login / Sign Up:** role selector, SHA-256 password check, inline
  min-length and password-mismatch validation, Admin accounts start
  `Pending` and need Super Admin approval; Customers are auto-approved.
- **Super Admin:** dashboard with platform totals; Manage Hotel Owners
  (Approve/Reject account, Suspend/Delete hotel, delete blocked while active
  bookings exist); View All Users; Platform Sales & Commission report
  (date-ranged); Low-Rated Hotels report (`HAVING AVG(Rating) < 2.5`);
  Manage Room Categories (duplicate- and in-use-guarded); Moderate Reviews
  (delete with confirmation).
- **Admin / Hotel Owner:** Hotel Profile (Save disabled until required
  fields are filled, first submission goes in as `Pending`); Room/Package
  full CRUD (delete blocked if the room has upcoming bookings); Availability
  Dashboard with a low-stock status flag, computed dynamically; Earnings &
  Booking Report with a running total; Create Discount Offer; read-only
  Reviews.
- **Customer:** Home/Browse with search + three ComboBox filters over a
  card list; Hotel Details with a room dropdown that shows the discounted
  price when an offer is active, plus read-only reviews; Booking Cart with
  edit/remove and a computed total; Checkout that runs the whole booking as
  **one transaction** (Bookings row + one BookingItems row per room, then
  clears the cart) and shows a CONFIRMED invoice; Booking History with
  Cancel and Leave-a-Review; Special Offers list.
- Every screen keeps the shared navy header / role-coloured title bar /
  button style, and always has a Back or Logout so nothing is a dead end.

### One deliberate deviation from the original spec

The original design decrements `Rooms.TotalRooms` directly at checkout.
That works but never lets stock recover once a booking's dates are in the
past. This implementation instead computes availability **dynamically**:
`Available = TotalRooms − (confirmed BookingItems whose CheckOutDate hasn't
passed yet)`. `TotalRooms` itself is never mutated by a booking, only by the
Admin editing the room. This also sidesteps a double-booking race, since
availability is always derived from current bookings rather than a counter
that can drift.

### Hub screens beyond the original 12 mockups

`AdminDashboardForm` (a landing hub for Hotel Owners, mirroring the Super
Admin dashboard's layout) and a few Super Admin/Customer sub-screens (View
All Users, Manage Room Categories, Moderate Reviews, Low-Rated Hotels,
Booking History, Special Offers, Update Profile) were added so that every
functional requirement is actually reachable from somewhere.

## Known simplifications (student-project scope, called out honestly)

- Passwords are SHA-256 hashed but not salted — fine for this assignment,
  not what you'd ship in production (BCrypt/PBKDF2 + salt would be the
  production choice).
- Payment is a form field, not a real gateway integration.
- Hotel/room photo upload just stores a local file path picked via
  `OpenFileDialog`; it doesn't copy the file anywhere or display a
  thumbnail.
