# ServiceHub – Service Booking & Appointment Management API

A medium-sized ASP.NET Core Web API built as a portfolio / final project. Customers browse services and book
appointments, employees manage the appointments assigned to them, and admins manage everything. Hangfire runs
background jobs (reminders and automatic status updates).

> Target framework: **.NET 8 (LTS)** · Database: **SQL Server** · IDE: Visual Studio 2022 / VS Code / Rider

## Features

- **Auth**: register, login, JWT Bearer tokens, ASP.NET Core Identity password hashing, lockout after 5 failed logins
- **Roles**: `Admin`, `Employee`, `Customer` with role-based + object-level authorization
- **Customer**: browse services, book / list / view / cancel own appointments
- **Employee**: view assigned appointments, confirm / complete / cancel them
- **Admin**: service CRUD + activate/deactivate, all appointments, users, create employees, assign employees
- **Business rules** (in the domain): no booking inactive services, no overlapping slots, valid status transitions
- **Hangfire**: recurring reminder job + automatic status update job, dashboard at `/hangfire` (Development)
- **Cross-cutting**: FluentValidation pipeline, global exception middleware, consistent JSON errors, structured logging, Swagger with JWT

## Architecture

```
API  ──▶  Application  ──▶  Domain
 │             ▲
 └──▶ Infrastructure ─┘   (Infrastructure implements Application/Domain interfaces)
```

| Layer | Contains |
|---|---|
| **Domain** | Entities (`Service`, `Appointment`, `ApplicationUser`), enum, `DomainException`, repository interfaces, business rules inside entities |
| **Application** | CQRS commands/queries + handlers (MediatR), validators, DTOs, pipeline behaviors, `IIdentityService`, `INotificationService`, `ICurrentUserService` |
| **Infrastructure** | EF Core `DbContext`, configurations, repositories, Unit of Work, Identity + JWT, Hangfire jobs, seeding |
| **API** | Thin controllers (only call MediatR), middleware, Swagger, JWT setup, `Program.cs` |

Request flow: `Controller → MediatR → LoggingBehavior → ValidationBehavior → Handler → Repository/UnitOfWork → SQL Server`

## Folder structure

```
ServiceHub.sln
src/
  ServiceHub.Domain/          Constants, Entities, Enums, Exceptions, Interfaces
  ServiceHub.Application/     Behaviors, Common, DTOs, Features/{Auth,Users,Services,Appointments}, Mappings
  ServiceHub.Infrastructure/  BackgroundJobs, Identity, Notifications, Persistence, Repositories
  ServiceHub.API/             Auth, Contracts, Controllers, Extensions, Middleware, Program.cs
docs/                         TESTING_GUIDE.md, GIT_WORKFLOW.md, INTERVIEW_PREP.md
```

## Database design

```
AspNetUsers (ApplicationUser: FullName, IsActive, CreatedAt)  ──< AspNetUserRoles >── AspNetRoles

Services (Id, Name UNIQUE, Description, DurationInMinutes, Price, IsActive, CreatedAt, UpdatedAt)

Appointments (Id, CustomerId FK→Users, ServiceId FK→Services, EmployeeId FK→Users NULL,
              AppointmentDate, EndDate, Status, ReminderSent, CreatedAt, UpdatedAt)
```

- All foreign keys use `Restrict` delete (no accidental cascades).
- Indexes: `(CustomerId, AppointmentDate)`, `(EmployeeId, AppointmentDate)`, `(Status, AppointmentDate)`.
- Unique filtered index on `(ServiceId, AppointmentDate) WHERE Status <> 'Cancelled'` guards against double-booking races.
- Check constraints: `Price > 0`, `DurationInMinutes > 0`, `EndDate > AppointmentDate`.
- `Status` is stored as text (`Pending`, `Confirmed`, `Completed`, `Cancelled`).
- Hangfire creates its own `HangFire` schema in the same database.

Booking rule: a service is a single-capacity resource. A new appointment conflicts with any non-cancelled appointment
that overlaps it in time for the **same service**, or for the **same employee** when one is selected.

Status rules: `Pending → Confirmed → Completed`; `Pending/Confirmed → Cancelled`. Completed can't be cancelled,
cancelled can't be completed.

## NuGet packages

| Project | Packages |
|---|---|
| Domain | Microsoft.Extensions.Identity.Stores |
| Application | MediatR 12.4.1, FluentValidation 11.11.0, FluentValidation.DependencyInjectionExtensions, Microsoft.Extensions.Logging.Abstractions |
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer, Microsoft.AspNetCore.Identity.EntityFrameworkCore, System.IdentityModel.Tokens.Jwt, Hangfire.Core / .SqlServer / .AspNetCore |
| API | Microsoft.AspNetCore.Authentication.JwtBearer, Swashbuckle.AspNetCore 6.9.0, Microsoft.EntityFrameworkCore.Design |

> MediatR is pinned to 12.x (free / MIT). Newer major versions use a commercial license.

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB comes with Visual Studio, or SQL Server Express/Developer)
- EF Core CLI: `dotnet tool install --global dotnet-ef --version 8.*`

## Configuration

`src/ServiceHub.API/appsettings.json` holds placeholders; `appsettings.Development.json` holds local demo values.

```json
"ConnectionStrings": { "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ServiceHubDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" },
"Jwt": { "Issuer": "ServiceHub", "Audience": "ServiceHubClients", "SecretKey": "CHANGE_THIS_SECRET_...", "ExpiryMinutes": 60 }
```

Using SQL Server Express instead of LocalDB? Change the server to `.\\SQLEXPRESS`.

**Never commit real secrets.** Use user secrets locally:

```bash
cd src/ServiceHub.API
dotnet user-secrets set "Jwt:SecretKey" "<random string, 32+ characters>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>"
```

In production use environment variables: `Jwt__SecretKey`, `ConnectionStrings__DefaultConnection`.
Seed users are only created when the `Seed:*Password` settings exist (they are only in `appsettings.Development.json`).

## Running migrations and the project

Run every command from the **solution root** (the folder that contains `ServiceHub.sln`):

```bash
dotnet restore
dotnet build

dotnet ef migrations add InitialCreate \
  --project src/ServiceHub.Infrastructure \
  --startup-project src/ServiceHub.API \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/ServiceHub.Infrastructure \
  --startup-project src/ServiceHub.API

dotnet run --project src/ServiceHub.API
```

Windows PowerShell: put each `dotnet ef` command on one line (replace `\` line breaks with spaces).
The app also applies pending migrations and seeds data at startup.

Open **https://localhost:7080/swagger** (or http://localhost:5080/swagger). Hangfire dashboard: **/hangfire** (Development only).

## Default users (Development only)

| Role | Email | Password |
|---|---|---|
| Admin | admin@servicehub.com | Admin@12345 |
| Employee | employee@servicehub.com | Employee@12345 |
| Customer | customer@servicehub.com | Customer@12345 |

Five demo services are seeded (Haircut, Full Body Massage, Dental Cleaning, Car Oil Change, Business Consultation).

## Authentication

1. `POST /api/auth/login` → copy `token`
2. Swagger → **Authorize** → paste the token only (Swagger adds `Bearer `)
3. Call protected endpoints

## API overview

| Method | Route | Access |
|---|---|---|
| POST | /api/auth/register | Anonymous |
| POST | /api/auth/login | Anonymous |
| GET | /api/services, /api/services/{id} | Any logged-in user |
| POST / PUT / DELETE | /api/services, /api/services/{id} | Admin |
| PUT | /api/services/{id}/active | Admin |
| POST | /api/appointments | Customer |
| GET | /api/appointments/my | Customer |
| GET | /api/appointments/{id} | Owner, assigned employee, admin |
| PUT | /api/appointments/{id}/cancel | Owner, assigned employee, admin |
| PUT | /api/appointments/{id}/status | Admin, assigned employee |
| GET | /api/employee/appointments | Employee |
| GET | /api/admin/appointments?status= | Admin |
| PUT | /api/admin/appointments/{id}/employee | Admin |
| GET | /api/admin/users?role= | Admin |
| POST | /api/admin/employees | Admin |
| PUT | /api/admin/users/{id}/active | Admin |

Error format (all failures):

```json
{ "statusCode": 409, "message": "The selected time slot is already booked. Please choose another time.", "traceId": "0HN..." }
```

Validation failures add an `errors` object grouped by field. See `docs/TESTING_GUIDE.md` for full request/response examples.

## Hangfire

| Job | Schedule | What it does |
|---|---|---|
| `appointment-reminders` | every 15 min | Sends a reminder (via `INotificationService`) for Pending/Confirmed appointments starting within 24 h, then sets `ReminderSent` |
| `appointment-status-update` | every 5 min | Confirmed appointments that ended → `Completed`; Pending appointments whose start passed → `Cancelled` |

`INotificationService` currently logs. To send real email/SMS, create a new implementation (SMTP, SendGrid, Twilio…)
and change the single registration line in `Infrastructure/DependencyInjection.cs`.

## Future improvements

Working hours and employee availability calendars, pagination and filtering, refresh tokens, email confirmation and
password reset, unit/integration tests (xUnit + Testcontainers), rate limiting, health checks, real email/SMS provider,
optimistic concurrency (`RowVersion`), Docker support.
