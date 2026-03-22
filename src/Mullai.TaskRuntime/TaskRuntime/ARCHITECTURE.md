# Mullai Task Runtime Architecture

> Canonical detailed documentation now lives in `docs/architecture/runtime/README.md`.
> Additional runtime references:
> - `docs/architecture/runtime/API.md`
> - `docs/architecture/runtime/OPERATIONS.md`

This task runtime is designed so `Mullai.Web` (and other hosts) can process many concurrent `MullaiAgent` workloads from:

- client/API submissions
- recurring schedules (cron-like jobs)
- future system integrations

## Core Building Blocks

- `IMullaiTaskQueue`: decouples ingestion from execution.
- `IMullaiTaskStatusStore`: tracks lifecycle state (`Queued`, `Running`, `RetryScheduled`, `Succeeded`, `Failed`).
- `IMullaiTaskExecutor`: executes tasks against `IMullaiClient`.
- `IMullaiTaskClientFactory`: provides session-aware Mullai clients.
- `MullaiTaskWorkerService`: N-worker background executor pool.
- `CronTaskSchedulerService`: recurring task producer.

## Concurrency Model

- Global parallelism: controlled by `Mullai:TaskRuntime:WorkerCount`.
- Queue backpressure: bounded channel via `QueueCapacity`.
- Per-session safety: `BaseMullaiClient` serializes execution with an internal lock, preserving session integrity.

## Retry Model

- Each task has `Attempt` and `MaxAttempts`.
- Failed tasks are retried with configurable delay (`RetryDelaySeconds`).
- Final failure is persisted in the status store.

## Current Defaults

- Queue and status are in-memory for simplicity and speed.
- Recurring jobs are interval-based (`IntervalSeconds`) and can run on startup.

## Production Hardening Path

1. Replace `InMemoryMullaiTaskQueue` with a distributed queue (`Azure Service Bus`, `RabbitMQ`, `Kafka`, `SQS`).
2. Replace `InMemoryMullaiTaskStatusStore` with persistent storage (`PostgreSQL`, `Redis`, `Cosmos DB`).
3. Add dead-letter handling for exhausted retries.
4. Add tenant quotas, priority lanes, and adaptive worker autoscaling.
5. Emit queue depth, worker throughput, and failure-rate metrics for SLO-based autoscaling.
