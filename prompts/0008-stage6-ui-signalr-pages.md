# 0008 - Stage 6 (UI: 3 pages + SignalR live telemetry)

Date: 2026-05-10  
Stage: Stage 6 — React + TypeScript UI (exactly 3 pages) with SignalR real-time updates

## Goal
Implement the UI to display real-time telemetry via SignalR (no polling), while keeping exactly 3 pages.

## Work performed
- Added SignalR client hook `ui/src/lib/useTelemetry.ts`
- Added basic pages:
  - Overview (`/`)
  - Sensor details (`/sensor`)
  - Ops (`/ops`)
- Added minimal CORS config in `services/rest-api/Program.cs` to allow browser SignalR connections from `http://localhost:5173`

