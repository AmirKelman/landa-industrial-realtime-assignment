# 0011 - Fix: RabbitMQ, gRPC client, sensor seeding, README

Date: 2026-05-11
Stage: Post-review fixes

## Goal

Address four gaps identified by a requirements audit against the assignment PDF:
1. RabbitMQ never used in any service code
2. gRPC client not wired in rest-api (env var existed but no channel/client/endpoint)
3. SQL Sensors table seeded with 0 rows on startup
4. README had two TODO placeholder sections

## Prompt(s) used

User prompt:
> "check if all requirements filled in the github project https://github.com/AmirKelman/landa-industrial-realtime-assignment"

Assistant performed a full requirements audit and identified four gaps.

User prompt:
> "yes, implement all four fixes"

## Changes made

### Fix 1 — RabbitMQ (telemetry-service)
- Added `RabbitMQ.Client 6.8.1` to `telemetry-service.csproj`
- `Worker.ExecuteAsync` now calls `PublishSensorStatusOnlineAsync` before the tick loop
- Publishes `SensorStatusChanged(Online)` for all 20 sensors to exchange `landa.events`
  with routing key `sensor.status.changed` (as defined in `contracts/rabbitmq-messages.md`)
- Exponential backoff retry so the service waits gracefully for RabbitMQ to be ready

### Fix 2 — gRPC client + RabbitMQ consumer (rest-api)
- Added `RabbitMQ.Client 6.8.1` to `rest-api.csproj`
- `Program.cs`: registered `IConnectionFactory`, `GrpcChannel`, and
  `SqlDataService.SqlDataServiceClient` (proto already had `GrpcServices="Client"`)
- Added `GET /sensors` endpoint — calls `sql-data-service` via gRPC and returns sensor list
- Added `RabbitMqSensorStatusConsumer` hosted service — consumes `sensor.status.changed`
  from queue `rest-api.sensor-status`, logs each event and forwards to SignalR as `sensorStatus`

### Fix 3 — Sensor seeding (sql-data-service)
- `Program.cs`: after `EnsureCreatedAsync`, calls `SeedSensorsAsync`
- Seeds sensor-01 through sensor-20 with display names and zone locations if table is empty

### Fix 4 — README
- Replaced "TODO (Stage 2+)" How-to-run section with exact ports, URLs, credentials, and table
- Replaced "TODO (Stage 7+)" How-to-test section with exact dotnet test commands for unit and
  integration tests

## Outcome / what we accepted

All four fixes applied. Communication constraints are now fully satisfied:
- gRPC: sql-data-service exposes server; rest-api calls it via client
- RabbitMQ: telemetry-service publishes; rest-api consumes
