# 0004 - Stage 2 fix (Docker build dependency issues)

Date: 2026-05-09  
Stage: Stage 2 — Docker & infrastructure baseline (bugfix)

## Goal
Make `docker compose up --build` succeed by fixing build-time dependency issues discovered during the first real Docker build run.

## Prompt(s) used
User provided build output showing:
- `telemetry-service` missing `Microsoft.Extensions.*` references
- `sql-data-service` NuGet downgrade error: `Google.Protobuf` version too low vs `Grpc.AspNetCore` dependency

## Outcome / changes
- Added `Microsoft.Extensions.Hosting` reference to `services/telemetry-service/telemetry-service.csproj`
- Bumped `Google.Protobuf` to `3.27.0` in:
  - `services/sql-data-service/sql-data-service.csproj`
  - `services/rest-api/rest-api.csproj`

