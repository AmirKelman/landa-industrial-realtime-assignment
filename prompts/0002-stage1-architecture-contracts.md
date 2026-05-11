# 0002 - Stage 1 (Architecture + contracts)

Date: 2026-05-07  
Stage: Stage 1 — Architecture + contracts

## Goal
Lock a compliant architecture and define initial contracts for the only allowed backend communication patterns: gRPC and RabbitMQ (plus SignalR only API↔UI, REST only UI↔API).

## Prompt(s) used
User prompt:
> "Stage 1 Approved"

Assistant work performed:
- Updated `README.md` to document architecture, data flows, allowed channels, scaling/failure/ops notes
- Created gRPC proto contract `contracts/sql_data.proto`
- Created RabbitMQ contract documentation `contracts/rabbitmq-messages.md`

## Outcome / what we accepted
- Established `contracts/` as the source of truth for inter-service contracts.

