# 0007 - Stage 5 (REST API: Redis → SignalR push)

Date: 2026-05-10  
Stage: Stage 5 — REST API real-time telemetry push

## Goal
Consume telemetry that originates in Redis (`telemetry:stream`) and push it to the UI via SignalR (no UI polling).

## Work performed
- Added `StackExchange.Redis` to `services/rest-api`
- Added hosted service `RedisTelemetryStreamWorker` that uses `XREADGROUP ... BLOCK` to consume the Redis Stream
- Added `TelemetryStreamEntryParser` and `TelemetryMessage` used by SignalR
- Added unit tests for the parser under `services/rest-api/tests`

