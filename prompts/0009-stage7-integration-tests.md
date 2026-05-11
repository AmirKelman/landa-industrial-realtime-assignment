# 0009 - Stage 7 (Integration tests: end-to-end real-time flow)

Date: 2026-05-11  
Stage: Stage 7 — Integration tests

## Goal
Add integration tests that validate end-to-end behavior and prove real-time flow for all 20 sensors (Redis → REST API → SignalR → client).

## Work performed
- Added `tests/integration/integration.Tests.csproj`
- Added `tests/integration/RealTimeFlowTests.cs` which connects to SignalR and asserts receiving telemetry for all `sensor-01..sensor-20` within a bounded window.

## How to run
- Requires the system running (e.g. `docker compose up -d --build`)
- Run tests via `dotnet test tests/integration` (or via a .NET SDK container in environments without dotnet installed)

