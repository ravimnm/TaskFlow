# TaskFlow API

A small, deliberately focused task-management backend built with **C# / .NET 8 / ASP.NET Core Web API**. It demonstrates transferable backend fundamentals: REST design, dependency injection, JWT authentication, role-based authorization, EF Core, validation, pagination, centralized error handling, unit testing, and Docker.

> **Status:** learning / interview showcase project. It runs on an in-memory database with development-only secrets. See [Known Limitations](#known-limitations) and [Production Hardening Checklist](#production-hardening-checklist) before reusing any of it for real workloads.

---

## Table of Contents

1. [Features](#features)
2. [Tech Stack](#tech-stack)
3. [Project Structure](#project-structure)
4. [Architecture](#architecture)
5. [Getting Started](#getting-started)
6. [Configuration](#configuration)
7. [Authentication & Authorization](#authentication--authorization)
8. [API Reference](#api-reference)
9. [Data Model](#data-model)
10. [Error Handling](#error-handling)
11. [Testing](#testing)
12. [Docker](#docker)
13. [Switching to SQL Server](#switching-to-sql-server)
14. [Known Limitations](#known-limitations)
15. [Production Hardening Checklist](#production-hardening-checklist)
16. [Extension Point: Finishing Update & Status Endpoints](#extension-point-finishing-update--status-endpoints)
17. [Interview Talking Points](#interview-talking-points)

---

## Features

- **JWT bearer authentication** with register and login endpoints
- **Role-based authorization** (`ADMIN` and `USER`)
- **Task CRUD** with ownership rules (users see and manage their own tasks; admins see everything)
- **Server-side pagination** and **status filtering**
- **Request validation** via DataAnnotations on DTOs, plus business-rule validation in the service layer
- **Global exception-handling middleware** that returns consistent JSON errors
- **Layered architecture**: Controller → Service → Repository → EF Core
- **Seeded demo users** on startup
- **Swagger / OpenAPI** documentation (Development environment)
- **xUnit + Moq** unit tests
- **Multi-stage Dockerfile**

## Tech Stack

| Area | Technology |
|---|---|
| Language / runtime | C# 12, .NET 8 |
| Web framework | ASP.NET Core Web API (controllers) |
| ORM | Entity Framework Core 8 |
| Database | EF Core InMemory (default); SQL Server provider included |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| API docs | Swashbuckle (Swagger UI) |
| Testing | xUnit, Moq |
| Packaging | Docker (multi-stage build) |

## Project Structure

```
TaskFlow/
├── Dockerfile
├── TaskFlow.sln
├── TaskFlow.Api/
│   ├── Program.cs                  # Composition root: DI, auth, middleware pipeline
│   ├── appsettings.json            # JWT settings, connection string, logging
│   ├── Controllers/
│   │   ├── AuthController.cs       # POST /api/auth/register, /api/auth/login
│   │   └── TasksController.cs      # /api/tasks endpoints (requires auth)
│   ├── Services/
│   │   ├── ITaskService.cs / TaskService.cs     # Business rules, authorization checks
│   │   └── ITokenService.cs / TokenService.cs   # JWT creation
│   ├── Repositories/
│   │   ├── ITaskRepository.cs / TaskRepository.cs  # Data access over EF Core
│   ├── Data/
│   │   ├── AppDbContext.cs         # EF Core model, indexes, relationships
│   │   └── SeedData.cs             # Demo users
│   ├── Models/                     # Entities: User, TaskItem
│   ├── DTOs/                       # API contracts (requests/responses)
│   └── Middleware/
│       └── ExceptionHandlingMiddleware.cs
└── TaskFlow.Tests/
    └── TaskServiceTests.cs         # Service-layer unit tests (mocked repository)
```

## Architecture

```
HTTP request
   │
   ▼
ExceptionHandlingMiddleware ──► catches exceptions, maps to HTTP status codes
   │
   ▼
Authentication (JWT) ──► Authorization ([Authorize])
   │
   ▼
Controller      – HTTP concerns: routing, binding, status codes
   │
   ▼
Service         – business rules, ownership/role checks, validation
   │
   ▼
Repository      – queries and persistence (EF Core)
   │
   ▼
AppDbContext ──► Database (InMemory by default, SQL Server-ready)
```

**Design decisions**

- **Constructor injection everywhere** (using C# 12 primary constructors). Services are registered in `Program.cs`: repository and task service are `Scoped` (one per request, matching `DbContext` lifetime); the token service is a `Singleton` (stateless).
- **DTOs separate the API contract from entities**, so the database shape can change without breaking clients and sensitive fields (e.g. `PasswordHash`) are never serialized.
- **The service layer owns authorization decisions** that depend on data (e.g. "does this task belong to the caller?"), while the framework handles authentication and endpoint-level `[Authorize]`.
- **Repositories use `AsNoTracking()`** for read paths, which is efficient for read-only queries (and is the reason for the [update extension point](#extension-point-finishing-update--status-endpoints)).

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional) Docker, for containerized runs

### Run locally

```bash
dotnet restore
dotnet build

# Swagger UI is only enabled when the environment is "Development".
# The project ships without a launchSettings.json, so set it explicitly:
export ASPNETCORE_ENVIRONMENT=Development          # macOS / Linux
# $env:ASPNETCORE_ENVIRONMENT = "Development"      # PowerShell

dotnet run --project TaskFlow.Api
```

The console prints the listening URL (by default `http://localhost:5000` when no launch profile is present). Open `<url>/swagger` to explore the API.

> `UseHttpsRedirection()` is enabled. If no HTTPS endpoint is configured you will see a warning that the HTTPS port could not be determined; plain HTTP requests still work. To get HTTPS locally, run `dotnet dev-certs https --trust` and launch with `--urls "https://localhost:5001;http://localhost:5000"`.

### Seeded users

On startup, if the user table is empty, two accounts are created:

| Email | Password | Role |
|---|---|---|
| `admin@taskflow.local` | `Admin@123` | `ADMIN` |
| `user@taskflow.local` | `User@123` | `USER` |

> These credentials are for local development only. Because the database is in-memory, all data (including anything you register) is lost whenever the app restarts, and the seed runs again.

### Quick walkthrough with curl

```bash
BASE=http://localhost:5000

# 1. Log in and capture a token (requires jq; otherwise copy it from the JSON)
TOKEN=$(curl -s -X POST $BASE/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@taskflow.local","password":"User@123"}' | jq -r .token)

# 2. Create a task (regular users can only assign to themselves; seeded user has Id 2)
curl -s -X POST $BASE/api/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Prepare interview","description":"Review .NET","priority":"HIGH","assignedUserId":2}'

# 3. List tasks (page 1, 10 per page, only OPEN)
curl -s "$BASE/api/tasks?page=1&pageSize=10&status=open" \
  -H "Authorization: Bearer $TOKEN"
```

> The seeded users receive database-generated IDs in insertion order, so `admin` is `1` and `user` is `2`. You can also read the `nameidentifier` claim from the JWT (e.g. at jwt.io) to confirm your own ID.

**Using Swagger UI:** the Swagger setup does not currently define a JWT security scheme, so there is no "Authorize" button. Use curl/Postman/an HTTP client for protected endpoints, or add an `AddSecurityDefinition("Bearer", ...)` block to `AddSwaggerGen` in `Program.cs`.

## Configuration

Settings live in `TaskFlow.Api/appsettings.json` and can be overridden by environment variables (use `__` for nesting).

| Key | Environment variable | Default | Purpose |
|---|---|---|---|
| `Jwt:Key` | `Jwt__Key` | `TaskFlowDevelopmentSecretKey-ChangeMe-2026` | HMAC-SHA256 signing key. **Must be replaced outside development.** Use a long random value (32+ bytes). |
| `Jwt:Issuer` | `Jwt__Issuer` | `TaskFlow.Api` | Token issuer; also used as the audience. |
| `Jwt:ExpiryMinutes` | `Jwt__ExpiryMinutes` | `60` | Access-token lifetime. |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | LocalDB-style SQL Server string | **Currently unused** (see [Switching to SQL Server](#switching-to-sql-server)). |
| `ASPNETCORE_ENVIRONMENT` | same | `Production` if unset | `Development` enables Swagger. |
| `ASPNETCORE_URLS` | same | `http://localhost:5000` (local), `http://+:8080` (Docker) | Listening addresses. |

Example:

```bash
Jwt__Key="$(openssl rand -base64 48)" dotnet run --project TaskFlow.Api
```

## Authentication & Authorization

**Authentication (who are you?)** is handled by ASP.NET Core's JWT bearer middleware. `TokenService` issues a signed token containing these claims:

| Claim | Value |
|---|---|
| `NameIdentifier` | User ID |
| `Email` | User email |
| `Role` | `ADMIN` or `USER` |

Token validation checks issuer, audience, lifetime, and signing key. Send the token on every protected request:

```
Authorization: Bearer <token>
```

**Authorization (what may you do?)**

| Rule | Where enforced |
|---|---|
| All `/api/tasks/*` endpoints require a valid token | `[Authorize]` on `TasksController` |
| Non-admins may only create tasks assigned to themselves | `TaskService.CreateAsync` |
| Non-admins may only modify or delete tasks assigned to them | `TaskService.UpdateAsync / UpdateStatusAsync / DeleteAsync` |
| Non-admins only see their own tasks in the list; admins see all | `TaskService.GetPagedAsync` |

**Status code semantics**

- **401 Unauthorized**: missing, expired, or invalid token; also wrong login credentials.
- **403 Forbidden**: authenticated, but not allowed to perform the action (mapped from `UnauthorizedAccessException`).

New users created through `/api/auth/register` always receive the `USER` role. There is no endpoint to promote a user to `ADMIN`.

## API Reference

Base path: `/api`. All request and response bodies are JSON.

### Auth

#### `POST /api/auth/register`

Create a user and receive a token.

```json
{ "email": "new.user@example.com", "password": "secret1" }
```

| Field | Rules |
|---|---|
| `email` | Required, valid email format |
| `password` | Required, minimum 6 characters |

Responses: `200 OK` `{ "token": "...", "role": "USER" }` · `400` validation error · `409` email already registered.

#### `POST /api/auth/login`

```json
{ "email": "user@taskflow.local", "password": "User@123" }
```

Responses: `200 OK` `{ "token": "...", "role": "USER" }` · `400` validation error · `401` invalid credentials.

### Tasks (all require `Authorization: Bearer <token>`)

| Method | Path | Description | Success |
|---|---|---|---|
| `GET` | `/api/tasks` | List tasks (paged, filterable) | `200` |
| `GET` | `/api/tasks/{id}` | Get one task | `200` / `404` |
| `POST` | `/api/tasks` | Create a task | `201` + `Location` header |
| `PUT` | `/api/tasks/{id}` | Update a task | ⚠️ not implemented (see below) |
| `PATCH` | `/api/tasks/{id}/status` | Change task status | ⚠️ not implemented (see below) |
| `DELETE` | `/api/tasks/{id}` | Delete a task | `204` / `404` |

#### `GET /api/tasks`

| Query param | Default | Rules |
|---|---|---|
| `page` | `1` | Must be ≥ 1 |
| `pageSize` | `10` | 1–100 |
| `status` | none | Optional; case-insensitive (`OPEN`, `IN_PROGRESS`, `DONE`) |

Results are ordered by `CreatedAt` descending. Invalid pagination returns `400`.

```json
{
  "items": [
    {
      "id": 1,
      "title": "Prepare interview",
      "description": "Review .NET",
      "status": "OPEN",
      "priority": "HIGH",
      "dueDate": null,
      "assignedUserId": 2,
      "createdAt": "2026-09-26T10:23:00Z"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 10
}
```

#### `POST /api/tasks`

```json
{
  "title": "Prepare interview",
  "description": "Review .NET",
  "priority": "HIGH",
  "dueDate": "2026-10-15T00:00:00Z",
  "assignedUserId": 2
}
```

| Field | Rules |
|---|---|
| `title` | Required, max 100 characters |
| `description` | Optional (defaults to empty) |
| `priority` | Optional; `LOW`, `MEDIUM` (default), or `HIGH`, case-insensitive |
| `dueDate` | Optional |
| `assignedUserId` | Required in practice; must equal your own ID unless you are an admin |

New tasks start with status `OPEN`.

#### `PUT /api/tasks/{id}`

Body: `title` (required, max 100), `description`, `priority`, `dueDate`. See [Known Limitations](#known-limitations): the service currently validates and then throws `NotSupportedException`.

#### `PATCH /api/tasks/{id}/status`

Body: `{ "status": "IN_PROGRESS" }` where status is `OPEN`, `IN_PROGRESS`, or `DONE`. Same limitation as above.

#### `DELETE /api/tasks/{id}`

Returns `204` on success, `404` if the task does not exist, `403` if it belongs to someone else (non-admin).

## Data Model

```
User                          TaskItem
────────────────────          ─────────────────────────────
Id (PK)                       Id (PK)
Email (unique index)          Title
PasswordHash                  Description
Role ("USER" | "ADMIN")       Status   ("OPEN" | "IN_PROGRESS" | "DONE")
                              Priority ("LOW" | "MEDIUM" | "HIGH")
         1 ──────────── *     DueDate (nullable)
                              CreatedAt (UTC)
                              AssignedUserId (FK → User.Id, ON DELETE RESTRICT)
```

**Indexes**

- `Users.Email`: unique, which also speeds up login lookups.
- `Tasks (AssignedUserId, Status, CreatedAt)`: composite index matching the main list query (filter by user and status, sort by newest). Pagination with `Skip/Take` plus a covering index avoids full table scans as data grows.

Deleting a user who still has tasks is blocked (`DeleteBehavior.Restrict`).

## Error Handling

`ExceptionHandlingMiddleware` wraps the pipeline and converts unhandled exceptions into a uniform JSON body:

```json
{ "error": "Status must be OPEN, IN_PROGRESS, or DONE." }
```

| Exception | HTTP status |
|---|---|
| `ArgumentException` | `400 Bad Request` |
| `UnauthorizedAccessException` | `403 Forbidden` |
| Anything else (including `NotSupportedException`) | `500 Internal Server Error` |

DataAnnotation failures on DTOs (e.g. missing title) are handled earlier by `[ApiController]` and return a standard `400` ProblemDetails response. Note that the middleware currently returns `ex.Message` for 500s, which can leak internals; see the hardening checklist.

## Testing

```bash
dotnet test
```

`TaskFlow.Tests/TaskServiceTests.cs` unit-tests `TaskService` with a Moq-mocked `ITaskRepository`, so business rules are verified without a database:

- `CreateAsync_CreatesTaskForCurrentUser`: verifies mapping, priority handling, and that `AddAsync` and `SaveChangesAsync` are each called once.
- `CreateAsync_RejectsUserAssigningToSomeoneElse`: verifies a non-admin gets `UnauthorizedAccessException`.

**Suggested next tests:** pagination bounds, invalid priority/status, delete ownership rules, admin overrides, and controller/integration tests using `WebApplicationFactory<Program>` (the `public partial class Program { }` declaration in `Program.cs` is already there to support this; you would add `Microsoft.AspNetCore.Mvc.Testing` to the test project).

## Docker

The Dockerfile is a multi-stage build: the .NET 8 SDK image restores and publishes, and the smaller ASP.NET 8 runtime image runs the result on port **8080**.

```bash
# From the repository root
docker build -t taskflow-api .

docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Jwt__Key="$(openssl rand -base64 48)" \
  taskflow-api
```

Then visit `http://localhost:8080/swagger`.

Notes:

- The container defaults to the `Production` environment, so Swagger is off unless you set `ASPNETCORE_ENVIRONMENT=Development` as above.
- The image is built from `TaskFlow.Api` only (tests are not included). Add a `.dockerignore` to keep `bin/` and `obj/` out of the build context.
- HTTPS is not configured inside the container; terminate TLS at a reverse proxy or load balancer.

## Switching to SQL Server

The SQL Server provider package and a connection string are already present, but `Program.cs` uses the in-memory provider. To move to SQL Server:

1. In `Program.cs`, replace:
   ```csharp
   options.UseInMemoryDatabase("TaskFlowDb")
   ```
   with:
   ```csharp
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
   ```
2. Install the EF tooling and create the schema:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate --project TaskFlow.Api
   dotnet ef database update --project TaskFlow.Api
   ```
3. Provide the real connection string via environment variable or secret store, not source control.
4. Validate that `AssignedUserId` refers to an existing user before saving (the in-memory provider does not enforce foreign keys, but SQL Server will).

## Known Limitations

These are intentional simplifications or gaps in the current code:

1. **Update and status endpoints are incomplete.** `PUT /api/tasks/{id}` and `PATCH /api/tasks/{id}/status` perform validation and authorization, then throw `NotSupportedException`, which the middleware reports as `500` (not `501`). See the [extension point](#extension-point-finishing-update--status-endpoints).
2. **Password hashing is not production-grade.** Passwords are hashed with a single unsalted SHA-256 pass (`AuthController.Hash`, duplicated in `SeedData.Hash`). Use ASP.NET Core's `PasswordHasher<T>`, BCrypt, or Argon2 instead.
3. **`GET /api/tasks/{id}` has no ownership check.** Any authenticated user can read any task by ID, unlike the list, update, and delete paths.
4. **`assignedUserId` is not checked for existence** when creating a task as an admin.
5. **In-memory database.** All data is lost on restart; the SQL Server connection string is unused.
6. **Development secrets are committed** in `appsettings.json`, and `Program.cs` falls back to a hard-coded key if none is configured.
7. **No refresh tokens, logout/revocation, rate limiting, or account lockout.**
8. **Swagger has no Bearer auth configuration**, so protected endpoints can't be called from the UI without adding it.
9. **Test coverage is minimal** (two unit tests, no integration tests).
10. **Task `Status` and `Priority` are strings**, validated against hard-coded sets; enums would be safer.

## Production Hardening Checklist

- [ ] Replace SHA-256 with a salted, slow password hash
- [ ] Load `Jwt:Key` from a secret store (environment, Key Vault, etc.); remove the hard-coded fallback
- [ ] Move off the in-memory database; add EF migrations
- [ ] Return generic messages for 500 errors and log details server-side; consider `ProblemDetails` (RFC 7807) responses
- [ ] Map `NotSupportedException` to `501`, or finish the endpoints
- [ ] Add ownership checks to `GET /api/tasks/{id}`
- [ ] Enforce HTTPS/HSTS and configure CORS for known origins
- [ ] Add rate limiting, structured logging, health checks, and request correlation IDs
- [ ] Add integration tests and CI (build, test, container scan)
- [ ] Run the container as a non-root user and add a `.dockerignore`

## Extension Point: Finishing Update & Status Endpoints

The repository's `GetByIdAsync` returns an entity loaded with `AsNoTracking()`, so changes made to it are not detected by `SaveChangesAsync`. To complete the feature:

1. Add a tracked read to the repository:
   ```csharp
   // ITaskRepository
   Task<TaskItem?> GetTrackedByIdAsync(int id);

   // TaskRepository
   public Task<TaskItem?> GetTrackedByIdAsync(int id) =>
       db.Tasks.FirstOrDefaultAsync(x => x.Id == id);
   ```
2. In `TaskService`, use it for updates, mutate the entity, and save:
   ```csharp
   var item = await repository.GetTrackedByIdAsync(id);
   if (item is null) return null;
   // ...ownership and validation checks...
   item.Title = request.Title;
   item.Description = request.Description ?? item.Description;
   item.Priority = (request.Priority ?? item.Priority).ToUpperInvariant();
   item.DueDate = request.DueDate;
   await repository.SaveChangesAsync();
   return ToResponse(item);
   ```
   Do the same for `UpdateStatusAsync` (set `item.Status = status.ToUpperInvariant()`).
3. Fix a related mismatch while you're there: `ITaskService.UpdateStatusAsync` returns `bool`, and the controller wraps it in `Ok(...)`. Returning `NoContent()`/`NotFound()` or the updated `TaskResponse` would be a cleaner contract, and `UpdateAsync` returning `null` should map to `404` in the controller.
4. Add unit tests for the new paths (success, not found, forbidden, invalid input).

## Interview Talking Points

- **Why ASP.NET Core?** Cross-platform, high performance, built-in DI, first-class middleware pipeline, and strong tooling. Compare with Spring Boot: controllers ≈ `@RestController`, DI container ≈ Spring IoC, EF Core ≈ JPA/Hibernate, middleware ≈ servlet filters, `[Authorize]` ≈ Spring Security annotations.
- **How JWT auth works in middleware:** the bearer handler reads the `Authorization` header, validates signature/issuer/audience/expiry, and builds a `ClaimsPrincipal` for the request. Controllers and policies then read claims and roles from it.
- **Authentication vs authorization; 401 vs 403:** identity versus permission. 401 means "we don't know who you are"; 403 means "we know, and you can't do this".
- **Why constructor injection?** Explicit dependencies, easy mocking, and lifetimes managed by the container (scoped `DbContext`, singleton stateless services).
- **Why DTOs?** They decouple API contracts from entities, prevent over-posting and leaking fields like password hashes, and allow independent evolution.
- **How EF Core maps to tables:** `DbSet<T>` + conventions + Fluent API (`OnModelCreating`) define keys, indexes, relationships, and delete behavior; LINQ is translated to SQL.
- **Why indexes matter for pagination/filtering:** the composite `(AssignedUserId, Status, CreatedAt)` index serves the exact filter-and-sort pattern of the list endpoint.
- **Tracking vs no-tracking queries:** `AsNoTracking()` is faster for reads but means changes aren't persisted. This project's update extension point is a concrete example.
- **Global exception middleware:** one place to translate exceptions into consistent HTTP responses, with the trade-off of needing care to avoid leaking internal messages.
- **Why mock repositories in unit tests?** It isolates business logic from I/O, so tests are fast and deterministic. Integration tests then cover the wiring.
- **What would you improve first?** Password hashing, the unfinished update endpoints, the missing read-ownership check, and a real database with migrations.
