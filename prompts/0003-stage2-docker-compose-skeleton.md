# 0003 - Stage 2 (docker-compose + runnable skeleton)

Date: 2026-05-07  
Stage: Stage 2 — Docker & infrastructure baseline

## Goal
Create a runnable baseline using `docker-compose` with Redis, RabbitMQ, SQL Server, and minimal service containers (each with a Dockerfile) so future stages can build incrementally.

## Prompt(s) used
User prompt:
> "Stage 1 Approved"

Assistant work performed:
- Added `docker-compose.yml`
- Added minimal C# service skeletons and Dockerfiles:
  - `services/rest-api`
  - `services/sql-data-service`
  - `services/telemetry-service`
- Added minimal React+TS UI skeleton and Dockerfile under `ui/`

## Notes
- Local `docker`/`.NET` CLIs were not available in the terminal environment; Docker builds are intended to compile inside containers.

