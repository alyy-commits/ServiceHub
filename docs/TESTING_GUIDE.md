# API testing guide (Swagger)

Start the API (`dotnet run --project src/ServiceHub.API`) and open `https://localhost:7080/swagger`.
Use dates in the future. Examples below use `2026-10-15` – change them if that date has passed.

Tip: to remove a value use `null` (for optional fields such as `employeeId`), never the placeholder `"string"`.

## 1. Register a customer
`POST /api/auth/register`
```json
{ "fullName": "Sara Ali", "email": "sara@test.com", "password": "Sara@12345" }
```
Expected **200**:
```json
{ "token": "eyJ...", "expiresAtUtc": "2026-09-24T15:00:00Z", "userId": "...", "fullName": "Sara Ali",
  "email": "sara@test.com", "roles": ["Customer"] }
```

## 2. Login (Admin)
`POST /api/auth/login`
```json
{ "email": "admin@servicehub.com", "password": "Admin@12345" }
```

## 3. Copy the JWT
Copy the `token` value (without quotes).

## 4. Authorize Swagger
Click **Authorize** → paste the token → **Authorize** → **Close**. Repeat with another user's token whenever you switch roles
(click Authorize → Logout → paste the new token).

## 5. Create a service as Admin
`POST /api/services`
```json
{ "name": "Hair Coloring", "description": "Full hair coloring session.", "durationInMinutes": 90, "price": 70 }
```
Expected **201** with `id`, `isActive: true`.

## 6. Get services as Customer
Login as `customer@servicehub.com` / `Customer@12345`, authorize, then `GET /api/services` → **200** list (only active services).
Note a service `id` (e.g. `1`).

## 7. Create an appointment (Customer)
`POST /api/appointments`
```json
{ "serviceId": 1, "appointmentDate": "2026-10-15T10:00:00Z", "employeeId": null }
```
Expected **201**:
```json
{ "id": 1, "serviceId": 1, "serviceName": "Haircut", "customerName": "Demo Customer", "employeeId": null,
  "appointmentDate": "2026-10-15T10:00:00Z", "endDate": "2026-10-15T10:30:00Z", "status": "Pending", ... }
```

## 8. View appointments
- `GET /api/appointments/my` → list with your appointments
- `GET /api/appointments/1` → details

## 9. Change status
Get the employee id: login as Admin → `GET /api/admin/users?role=Employee` → copy `id`.
Assign: `PUT /api/admin/appointments/1/employee`
```json
{ "employeeId": "<employee id>" }
```
Login as `employee@servicehub.com` / `Employee@12345`:
- `GET /api/employee/appointments` → shows appointment 1
- `PUT /api/appointments/1/status`
```json
{ "status": "Confirmed" }
```
→ **200**, `status: "Confirmed"`. Then `{ "status": "Completed" }` → **200**.
(Admin can also change any status, and admin can pass `?status=Pending` to `GET /api/admin/appointments`.)

## 10. Cancel an appointment
Create another appointment (different time, e.g. `2026-10-16T10:00:00Z`), then as the Customer:
`PUT /api/appointments/2/cancel` → **200**, `status: "Cancelled"`.
Try cancelling appointment 1 (Completed) → **409** `"A completed appointment cannot be cancelled."`

## 11. Test Hangfire
1. Open `https://localhost:7080/hangfire` → **Recurring Jobs**.
2. Reminder job: create an appointment starting in ~3 hours (e.g. now + 3h in UTC), select `appointment-reminders` → **Trigger now**.
   Check the API console for `NOTIFICATION to user ... | Appointment reminder`. In SQL, `ReminderSent` becomes `1`, so it is not reminded twice.
3. Status job: appointments cannot be created in the past through the API, so simulate time passing in SQL Server:
```sql
UPDATE Appointments
SET AppointmentDate = DATEADD(HOUR, -3, GETUTCDATE()), EndDate = DATEADD(HOUR, -2, GETUTCDATE())
WHERE Id = 3;   -- a Confirmed appointment
```
   Trigger `appointment-status-update` → the appointment becomes `Completed` (a Pending one becomes `Cancelled`).

## 12. Authorization errors
- No token: `GET /api/appointments/my` → **401**
```json
{ "statusCode": 401, "message": "Authentication is required. Provide a valid Bearer token.", "traceId": "..." }
```
- Customer token on `POST /api/services` → **403**
- Customer token on `GET /api/admin/users` → **403**
- Customer B opening Customer A's `GET /api/appointments/{id}` → **403** `"You do not have access to this appointment."`
- Employee changing status of an appointment not assigned to them → **403**

## 13. Validation errors
`POST /api/auth/register`
```json
{ "fullName": "", "email": "not-an-email", "password": "123" }
```
Expected **400**:
```json
{ "statusCode": 400, "message": "One or more validation errors occurred.", "traceId": "...",
  "errors": { "FullName": ["'Full Name' must not be empty."],
              "Email": ["'Email' is not a valid email address."],
              "Password": ["Password must be at least 8 characters long.", "..."] } }
```
Other checks: appointment in the past → 400 `"The appointment date must be in the future."`; inactive service → 400;
unknown service id → 404; `POST /api/services` with `price: 0` → 400.

## 14. Duplicate appointment handling
1. As Admin: `PUT /api/services/1/active` with `{ "isActive": false }`, then as Customer book service 1 → **400** (inactive). Reactivate afterwards.
2. As Customer book service 1 at `2026-10-20T10:00:00Z` → 201.
3. Book the same service at `2026-10-20T10:15:00Z` (overlaps the 30-minute slot) → **409**
```json
{ "statusCode": 409, "message": "The selected time slot is already booked. Please choose another time.", "traceId": "..." }
```
4. Cancel the first appointment, book again → **201** (cancelled appointments free the slot).
