# Interview preparation – ServiceHub

## The 30-second pitch
"ServiceHub is a booking API built with ASP.NET Core 8, Clean Architecture and CQRS. Customers book appointments, employees
and admins manage them. It uses Identity + JWT with role-based and object-level authorization, FluentValidation in a MediatR
pipeline, global exception handling, EF Core with SQL Server, and Hangfire for reminders and automatic status updates."

## Why each technology

**Clean Architecture** – dependencies point inward: Domain has no dependencies, Application depends on Domain, Infrastructure and API depend on Application. Business rules don't depend on EF, ASP.NET or Hangfire, so they are easy to test and infrastructure can be swapped (e.g. `INotificationService` log → email).

**CQRS** – every use case is either a command (changes state) or a query (reads). Each is small, has one handler, and is easy to find, test and extend. Read and write paths can be optimized independently.

**MediatR** – controllers only do `_sender.Send(request)`. It gives one entry point per use case and lets pipeline behaviors (logging, validation) run for every request without repeating code.

**EF Core** – productive LINQ-to-SQL, Code First migrations, change tracking, relationships and constraints configured in code (`IEntityTypeConfiguration`).

**Identity** – battle-tested user/role management: password hashing (PBKDF2), lockout, normalized emails, role tables. Writing this myself would be insecure.

**JWT** – stateless authentication: the API validates the signature, issuer, audience and expiry without a session store; role claims drive `[Authorize(Roles = ...)]`.

**Hangfire** – persistent background jobs stored in SQL Server, retries, a dashboard and cron scheduling, with no extra infrastructure.

## How things work

**Booking flow** – `POST /api/appointments` → JWT validated → `[Authorize(Roles="Customer")]` → `CreateAppointmentCommand` → ValidationBehavior (future date, ids) → handler loads the service (404 if missing), checks it is active (400), optionally checks the employee, calculates end time from the service duration, checks overlap with `HasConflictAsync` (409), creates the `Appointment` as `Pending`, saves, logs, notifies and returns a DTO.

**Background jobs** – `Program.cs` registers two recurring jobs. Hangfire stores them in SQL Server; a Hangfire server thread runs them on schedule, creating a DI scope per job. The reminder job finds appointments within 24 h with `ReminderSent = false`, notifies, and sets the flag so reminders are idempotent. The status job completes finished confirmed appointments and expires unconfirmed past ones.

**Authorization** – two levels. (1) Endpoint level: roles on controllers. (2) Object level: `AppointmentAccessPolicy` checks that a customer owns the appointment, or the employee is assigned to it, or the caller is admin. Roles alone can't prevent customer A reading customer B's appointment.

**Validation** – FluentValidation validators are discovered by assembly scanning; `ValidationBehavior` runs them before the handler and throws `ValidationException`, which the middleware maps to a 400 with errors grouped by field. Rules that need the database (service exists, slot free) live in handlers because they need consistent data and give better status codes (404/409).

**Exception handling** – one `GlobalExceptionMiddleware` maps exceptions to status codes (Validation/BadRequest 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict/Domain rule/DbUpdate 409, others 500 with a generic message and full logging). Controllers have no try/catch.

**Database relationships** – `Appointment` has required FKs to the customer (`ApplicationUser`) and `Service`, and an optional FK to an employee (`ApplicationUser`). Delete behavior is `Restrict` to protect history. Users ↔ roles is many-to-many through Identity tables.

## 15 interview questions with sample answers

1. **Why not put the logic in controllers?** Controllers handle HTTP only. Logic in handlers/domain is reusable, unit-testable without HTTP, and keeps controllers thin (SRP).
2. **What is the difference between a command and a query?** A command changes state and returns little (or the created DTO); a query only reads and must not change state.
3. **How do you prevent double booking?** An overlap query (`existing.Start < newEnd && newStart < existing.End`, ignoring cancelled) for the same service/employee, plus a unique filtered index on (ServiceId, AppointmentDate) as a safety net for race conditions; a `DbUpdateException` is turned into 409.
4. **Is the overlap check race-safe by itself?** No: two requests can pass the check at the same time. The unique index catches identical start times. For full safety I'd use a serializable transaction or a `RowVersion`/distributed lock.
5. **Why store dates in UTC?** It avoids time-zone and daylight-saving bugs; the client converts for display. Jobs compare against `DateTime.UtcNow`.
6. **How are passwords stored?** Identity hashes them with PBKDF2 plus a per-user salt; the app never stores or logs plain passwords, and the logging behavior never logs request bodies.
7. **What is inside the JWT and how is it validated?** Claims `sub`, `email`, `name`, `role`, `jti`, `exp`. The API validates signature (HMAC-SHA256 with a secret), issuer, audience and lifetime with zero clock skew.
8. **How would you revoke a JWT?** Use short lifetimes plus refresh tokens, or keep a deny-list / token version on the user. Currently a deactivated user can't log in again, but an existing token lives until expiry (60 min).
9. **What is a pipeline behavior?** MediatR middleware around handlers. I use one for logging and one for validation, so no handler repeats that code.
10. **Why a repository and Unit of Work on top of EF Core?** EF's `DbContext` already is a unit of work, but the interfaces keep the Application layer independent from EF, make handlers easy to mock, and give named, intention-revealing queries. I kept them small to avoid over-engineering.
11. **Why do entities have private setters?** To protect invariants: state changes only through methods like `Cancel()` or `Confirm()` that enforce the rules, so the state machine can't be bypassed.
12. **How do Hangfire jobs get their dependencies?** Hangfire.AspNetCore uses the ASP.NET Core container, creating a scope per job, so scoped services like repositories work.
13. **What happens if a job fails or runs twice?** `[AutomaticRetry]` retries it; `[DisableConcurrentExecution]` prevents overlapping runs; the reminder is idempotent through the `ReminderSent` flag.
14. **How would you scale this?** Add pagination, caching for services, read-optimized queries, run Hangfire workers as a separate process, and use a distributed lock/optimistic concurrency for bookings.
15. **How would you test it?** Unit test entities (state transitions) and handlers with mocked repositories; validator tests; integration tests with `WebApplicationFactory` and a test SQL Server (Testcontainers); test authorization for each role.

## Limitations you should admit honestly
No working-hours calendar, no pagination, access tokens can't be revoked before expiry, notifications only log, Hangfire dashboard is enabled only in Development, and overlap checking isn't fully race-proof without a serializable transaction.
