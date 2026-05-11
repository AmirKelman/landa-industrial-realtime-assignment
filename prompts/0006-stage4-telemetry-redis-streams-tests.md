# 0006 - Stage 4 (Telemetry service: 20 sensors @ 1Hz → Redis origin) + tests

Date: 2026-05-10  
Stage: Stage 4 — IoT Telemetry service + unit tests

## Goal
Implement the mandatory IoT Telemetry service to simulate **exactly 20 sensors**, generating telemetry **once per second**, where telemetry **originates in Redis**, and prepare the service for later real-time push to the UI.

## Work performed
- Implemented a deterministic `SensorSimulator` producing 20 readings per tick
- Implemented a 1Hz `Worker` using `PeriodicTimer`
- Wrote telemetry into Redis Streams (`telemetry:stream`) using `StackExchange.Redis`
- Added a unit test project under `services/telemetry-service/tests`

## Notes
- Redis Stream entries include: `sensorId`, `tsUtc` (ISO-8601), `value` (invariant culture).

