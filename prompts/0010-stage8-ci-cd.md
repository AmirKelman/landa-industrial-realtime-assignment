# 0010 - Stage 8 (CI/CD pipeline)

Date: 2026-05-11  
Stage: Stage 8 — CI/CD

## Goal
Add CI/CD (GitHub Actions) that builds services, runs unit + integration tests, and builds Docker images, per assignment requirements.

## Work performed
- Added GitHub Actions workflow: `.github/workflows/ci.yml`
  - Runs backend unit tests for:
    - `services/sql-data-service/tests`
    - `services/telemetry-service/tests`
    - `services/rest-api/tests`
  - Builds docker images via `docker compose build`
  - Starts the stack via `docker compose up -d`
  - Waits for `rest-api` health endpoint
  - Runs integration tests `tests/integration` against `http://localhost:5001`

