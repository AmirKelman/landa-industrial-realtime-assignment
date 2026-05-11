# RabbitMQ message contracts (backend ↔ backend)

Backend services may communicate **only** using gRPC and RabbitMQ.

This document defines the RabbitMQ message shapes (JSON) and routing conventions.

## Exchanges / routing

Proposed:
- Exchange: `landa.events` (type: topic)
- Routing key patterns:
  - `sensor.status.changed`
  - `telemetry.snapshot.requested`

## Message: SensorStatusChanged (v1)

- Routing key: `sensor.status.changed`
- Producer: `telemetry-service`
- Consumers: `rest-api` (for UI status), optionally `sql-data-service` (to persist operational status)

JSON:
```json
{
  "schemaVersion": 1,
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-07T12:00:00Z",
  "sensorId": "sensor-01",
  "status": "Online",
  "details": "optional string"
}
```

## Message: TelemetrySnapshotRequested (v1)

- Routing key: `telemetry.snapshot.requested`
- Producer: `rest-api`
- Consumer: `sql-data-service` (optional: persist aggregates/snapshots)

JSON:
```json
{
  "schemaVersion": 1,
  "requestId": "uuid",
  "requestedAtUtc": "2026-05-07T12:00:00Z",
  "reason": "Startup|Manual|Periodic",
  "sensorIds": ["sensor-01", "sensor-02"]
}
```

