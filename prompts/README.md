# AI Prompt Log (Mandatory)

The assignment requires documenting **all** AI-assisted development prompts under `/prompts`.

## Rules
- Every time we ask the AI to do something that influences the solution, we add a new file here.
- Files are numbered in order: `0001-...md`, `0002-...md`, etc.
- Each prompt log must include:
  - Date/time
  - Goal / task stage
  - The exact prompt(s) we used
  - A short summary of what we accepted/changed from the response

## Index
- `0001-kickoff.md`: Stage 0 initialization (repo skeleton + prompt logging rules)
- `0002-stage1-architecture-contracts.md`: Architecture lock + gRPC/RabbitMQ contracts
- `0003-stage2-docker-compose-skeleton.md`: Docker Compose skeleton for all services
- `0004-stage2-fix-docker-build-deps.md`: Fix Docker build dependency issues
- `0005-stage3-sql-data-efcore-tests.md`: SQL Data service with EF Core + gRPC server + tests
- `0006-stage4-telemetry-redis-streams-tests.md`: Telemetry service with Redis Streams + tests
- `0007-stage5-rest-api-redis-to-signalr.md`: REST API Redis consumer → SignalR push + tests
- `0008-stage6-ui-signalr-pages.md`: React UI with 3 pages + SignalR integration
- `0009-stage7-integration-tests.md`: End-to-end integration tests for all 20 sensors
- `0010-stage8-ci-cd.md`: GitHub Actions CI/CD pipeline
- `0011-fix-rabbitmq-grpc-seeding-readme.md`: Post-review fixes (RabbitMQ, gRPC client, seeding, README)

