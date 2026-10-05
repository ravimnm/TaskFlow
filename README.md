# TaskFlow API — ASP.NET Core Interview Project

A deliberately small C#/.NET backend project for demonstrating transferable backend fundamentals: REST APIs, dependency injection, JWT authentication, RBAC, EF Core, SQL concepts, validation, exception handling, pagination, testing, and Docker.

## Stack
- C# / .NET 8 / ASP.NET Core Web API
- Entity Framework Core
- SQL Server-ready data model (development uses InMemory DB)
- JWT Bearer authentication
- Swagger/OpenAPI
- xUnit + Moq
- Docker

## Run
Install .NET 8 SDK, then:

```bash
dotnet restore
dotnet build
dotnet run --project TaskFlow.Api
```

Swagger is available in Development mode.

Seeded users:
- `admin@taskflow.local` / `Admin@123`
- `user@taskflow.local` / `User@123`

**Development note:** the sample uses an in-memory database and a development JWT secret. Do not use these credentials/secrets in production.

## Tests
```bash
dotnet test
```

## Architecture
Controller -> Service -> Repository -> EF Core -> Database

Authentication is handled by ASP.NET Core JWT middleware; authorization uses roles. Global exception handling is implemented as middleware.

## Important extension point
The update/status service methods intentionally throw `NotSupportedException` because the repository currently exposes a no-tracking read path. This is left explicit rather than pretending the implementation is complete. To finish it, add tracked update methods to the repository (or expose an update callback) and persist changes.

## Interview talking points
- Why ASP.NET Core instead of Spring Boot?
- How JWT authentication works in middleware.
- Authentication vs authorization / 401 vs 403.
- Why constructor dependency injection is useful.
- Why DTOs separate API contracts from entities.
- How EF Core maps objects to relational tables.
- Why indexes help filtering/pagination.
- How global exception middleware provides consistent errors.
- Why unit tests mock repositories.
- How this architecture compares with Spring Boot.
