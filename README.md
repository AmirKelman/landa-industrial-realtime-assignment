# Industrial Real-Time System (Home Assignment)

This repository implements the �Technical Home Assignment � Industrial / Real?Time System�.

## Quick start

Prerequisites:
- Docker Desktop (compose v2)

Run:
- `docker compose up --build`

Stop:
- `docker compose down -v`

## Services (high level)

The system consists of:
- **UI**: React + TypeScript (exactly 3 pages)
- **REST API**: REST endpoints for the UI + SignalR for real-time push
- **SQL Data service**: C# + Entity Framework (owns SQL schema and access)
- **IoT Telemetry service**: simulates 20 sensors and generates telemetry (1Hz)
- **Infrastructure**: Redis, RabbitMQ, SQL Database

> Note: Per assignment rules, backend services communicate exclusively via gRPC and RabbitMQ.  
> REST is only UI ? REST API, and SignalR is only UI ? REST API.

## How to run (detailed)

Prerequisites: Docker Desktop with Compose v2.

```bash
docker compose up --build
```

Service endpoints once the stack is healthy:

| Service | URL | Notes |
|---|---|---|
| UI | http://localhost:5173 | React app, 3 pages |
| REST API health | http://localhost:5001/health | `{"status":"ok"}` |
| REST API sensors | http://localhost:5001/sensors | Returns all 20 sensors from SQL via gRPC |
| SignalR hub | http://localhost:5001/hubs/telemetry | WebSocket, used by the UI |
| RabbitMQ management | http://localhost:15672 | `guest` / `guest` |
| SQL Server | localhost:1433 | `sa` / `Your_password123`, DB: `LandaHome` |
| Redis | localhost:6379 | No auth |

To stop and remove all volumes:

```bash
docker compose down -v
```

## How to test

**Unit tests** (no running stack required):

```bash
dotnet test services/sql-data-service/tests -c Release
dotnet test services/telemetry-service/tests -c Release
dotnet test services/rest-api/tests -c Release
```

**Integration tests** (requires the full stack to be running):

```bash
# Start the stack first
docker compose up -d --build

# Run end-to-end tests (validates real-time flow for all 20 sensors via SignalR)
dotnet test tests/integration -c Release

# Or set a custom API URL if needed
REST_API_BASE_URL=http://localhost:5001 dotnet test tests/integration -c Release
```

CI runs all of the above automatically on every push (see `.github/workflows/ci.yml`).

## Architecture explanation (mandatory)

### High-level architecture and service boundaries

Services:
- **`telemetry-service`** (C#): simulates exactly 20 sensors and produces 1Hz telemetry. Telemetry is written to **Redis** (the system-of-record for the real-time stream).
- **`sql-data-service`** (C# + EF Core + gRPC): owns the SQL schema and exposes a **gRPC** interface for sensor metadata and persisted operational data.
- **`rest-api`** (C# ASP.NET Core): the only service exposed to the UI. Provides **REST** endpoints (bootstrap/config) and a **SignalR** hub for real-time telemetry push. Calls `sql-data-service` via **gRPC**.
- **`ui`** (React + TypeScript): exactly 3 pages. Uses REST for initial data and SignalR for live updates.

Infrastructure:
- **Redis**: real-time telemetry origin (Redis Streams)
- **RabbitMQ**: inter-service events/commands (and any async decoupling) between backend services
- **SQL Server**: persisted operational data owned by `sql-data-service`

Communication allowed (and enforced by design):
- UI ? `rest-api`: **REST** + **SignalR**
- Backend ? Backend: **gRPC** + **RabbitMQ** only

### Diagram

```mermaid
flowchart LR
  UI[React + TS UI\n(3 pages)]
  API[REST API\nASP.NET Core\nREST + SignalR]
  SQLS[SQL Data Service\nC# + EF Core\n(gRPC server)]
  TEL[IoT Telemetry Service\nC#\n20 sensors @ 1Hz]

  REDIS[(Redis\nStreams)]
  RABBIT[(RabbitMQ)]
  SQLDB[(SQL Server)]

  UI <-- REST --> API
  UI <-- SignalR --> API

  API <-- gRPC --> SQLS
  SQLS --> SQLDB

  TEL --> REDIS

  TEL <--> RABBIT
  API <--> RABBIT
  SQLS <--> RABBIT
```

### Data flow (mandatory)

- **SQL ? API**:
  - The UI calls `rest-api` (REST) to fetch sensor metadata / persisted views.
  - `rest-api` calls `sql-data-service` via gRPC to read/write operational data.
  - `sql-data-service` is the only owner of the SQL schema and database access.

- **Redis ? API ? UI**:
  - `telemetry-service` writes telemetry events to Redis Streams (one stream per sensor or a shared stream with `sensorId` field; we�ll lock this in implementation).
  - `rest-api` consumes the Redis stream using **blocking** reads (consumer group + `BLOCK`) and immediately pushes updates via SignalR to connected UI clients.
  - UI never polls for telemetry.

### Why gRPC, RabbitMQ, and SignalR are used where they are

- **gRPC (backend ? backend)**: strongly-typed, efficient service-to-service calls (sensor metadata, persisted queries) with explicit contracts under `contracts/`.
- **RabbitMQ (backend ? backend)**: async decoupling for events/commands (e.g., sensor online/offline, �persist snapshot�, backpressure-friendly workflows). It�s also the mandated broker option.
- **SignalR (API ? UI only)**: push-based real-time delivery to the browser without polling. This is the only allowed real-time channel to the UI.

### Key trade-offs and constraints considered

- **No polling constraint**: telemetry consumption uses Redis Streams blocking reads; UI uses SignalR push.
- **Redis as telemetry origin**: even if we emit RabbitMQ events, the authoritative telemetry stream starts in Redis.
- **Determinism vs. simplicity**: 1Hz per sensor is generated by a single scheduler loop to avoid timer drift across many timers.
- **Service ownership**: SQL access is isolated to `sql-data-service` to keep boundaries clean and align with �industrial� reliability expectations.

### Scaling for more sensors / higher update rates

- Partition Redis Streams by sensor groups; run multiple consumers (horizontal scale) using consumer groups.
- Push fan-out: scale out `rest-api` with a SignalR backplane (Redis backplane) if needed, or route via a dedicated realtime gateway.
- Reduce bandwidth by sending deltas, batching per tick, or sampling (if requirements allow).
- Persist only necessary telemetry summaries (min/max/avg) rather than every raw point.

### Failure scenarios and behavior (mandatory)
Examples to cover:
- Redis unavailable
- RabbitMQ delayed/unavailable
- Service restarts

- **Redis unavailable**: `rest-api` stops emitting telemetry updates, exposes a degraded status, and resumes consumption when Redis returns (consumer group continues from last acknowledged entry). UI shows disconnected/degraded state.
- **RabbitMQ delayed/unavailable**: async event flows are retried with bounded backoff; core telemetry flow (Redis ? API ? UI) continues if Redis is healthy.
- **Service restarts**:
  - `rest-api` restarts: SignalR clients reconnect and resume streaming; Redis consumer group continues without losing ordering.
  - `telemetry-service` restarts: it resumes producing at 1Hz; gaps are acceptable and visible as missing timestamps.
  - `sql-data-service` restarts: gRPC calls fail fast and are retried by `rest-api` where appropriate.

### Logs, metrics, and operational signals for production

- Logs: structured logs per service (correlation ids where relevant), Redis consumer lag, RabbitMQ publish/consume failures, SignalR connection lifecycle.
- Metrics: telemetry ingest rate (per sensor), end-to-end latency (Redis timestamp ? UI receive), Redis stream lag, RabbitMQ queue depth, gRPC latencies and error rates.
- Health: readiness/liveness endpoints for containers; UI indicators for degraded real-time.

### Handling different sensor behaviors and performance optimizations
Cover sensors that:
- change very frequently
- change very slowly
- remain constant for long periods

- Frequently changing: batch updates per tick; apply backpressure by dropping intermediate updates (if allowed) and keeping last-value semantics.
- Slowly changing/constant: send only changes (dedupe) to reduce SignalR bandwidth, while still preserving �1Hz generation� requirement at the source.
- If performance is a concern: binary payloads, compression, reduce JSON allocations, and move formatting to the UI.

### Stable vs. likely-to-change parts

- Stable: service boundaries, comms constraints, contracts in `contracts/`, Redis-stream-based real-time ingestion.
- Likely to change: telemetry schema fields, UI visualization needs, which events are routed through RabbitMQ, persistence strategy.

### Alternative designs / what we'd do differently

- If allowed, a dedicated real-time gateway could separate SignalR concerns from the REST API.
- Kafka could replace RabbitMQ for high-throughput streaming semantics (not allowed here).
- Persisting raw telemetry in a time-series store could simplify analytics (not required here).

## AI prompt documentation (mandatory)

All AI prompts used during development are documented under [`/prompts`](./prompts).

