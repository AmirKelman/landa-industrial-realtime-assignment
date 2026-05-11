# 0005 - Stage 3 (SQL Data service: EF Core + unit tests)

Date: 2026-05-10  
Stage: Stage 3 — SQL Data service (C# + Entity Framework) + unit tests

## Goal
Implement the mandatory SQL Data service using C# and Entity Framework, expose required gRPC methods, and add unit tests.

## Work performed
- Added EF Core + SQL Server provider to `services/sql-data-service`
- Implemented `SqlDataDbContext` with `Sensors` table
- Implemented gRPC methods:
  - `ListSensors`
  - `UpsertSensors`
- Added unit tests using EF Core InMemory provider under `services/sql-data-service/tests`

## Notes
- DB initialization uses `EnsureCreated()` for now; migrations can be introduced once the schema stabilizes.

