# Mullai Web Task Runtime Operations Guide

## Operational Scope

This guide covers runtime operations for:

- worker capacity management
- reliability tuning
- observability and troubleshooting
- safe scaling roadmap from in-memory to distributed infrastructure

## Runtime Startup Checklist

- Verify `Mullai.Web` host starts with both hosted services: `MullaiTaskWorkerService`, `CronTaskSchedulerService`.
- Verify runtime configuration is loaded from: `Mullai:TaskRuntime`, `Mullai:RecurringTasks`.
- Verify middleware tool-call callback is wired: `FunctionCallingMiddleware.OnToolCallObserved`.
- Verify at least one provider/API key is configured for agent execution.

## Capacity Planning

### Core Knobs

- `QueueCapacity`: max buffered tasks waiting for execution.
- `WorkerCount`: concurrent worker loops.
- `DefaultMaxAttempts`: default retry cap when submitter does not set one.
- `RetryDelaySeconds`: delay between retry attempts.

### Tuning Strategy

- If queue grows and tasks remain `Queued` for long periods: increase `WorkerCount`.
- If process memory grows due to backlog: reduce `QueueCapacity` and scale out hosts.
- If transient provider failures are frequent: increase `RetryDelaySeconds` slightly and keep bounded `MaxAttempts`.

## Recommended Production Baselines

For moderate traffic:

- `QueueCapacity`: `2000`
- `WorkerCount`: `2 x vCPU` (start conservative and measure)
- `DefaultMaxAttempts`: `3`
- `RetryDelaySeconds`: `5`

For high traffic:

- move queue/status store to durable distributed infrastructure
- run multiple web/runtime replicas behind load balancer
- enable autoscaling on queue depth and processing latency

## Reliability Model

### Current Guarantees (In-Memory Mode)

- at-least-once within process lifetime
- retry on task execution failure
- per-session serialized execution (via client execution lock)

### Current Non-Guarantees

- no restart durability for queue/status/tool feed
- no cross-node state coherence in-memory
- no dead-letter persistence yet

## Observability

### Signals To Collect

- queue depth
- dequeue rate / completion rate
- success rate and failed rate
- retry rate
- median and p95 task completion latency
- tool call count per task

## Logging Expectations

From worker logs:

- worker startup with configured worker count
- task processing start (`TaskId`, attempt, max attempts)
- failure with retry decision
- recurring scheduler enqueue logs (`JobName`, `TaskId`)

## Tool Call Correlation

Tool call events are correlated with:

- `TaskId`
- `SessionKey`

using `MullaiTaskExecutionContext` (`AsyncLocal`) around execution scope.

## Troubleshooting Playbook

### Symptom: Tasks stay `Queued`

Checks:

1. verify `MullaiTaskWorkerService` started
2. verify no repeated startup exceptions
3. verify provider credentials/configuration are valid
4. verify queue is not saturated beyond sustainable throughput

Actions:

- increase `WorkerCount`
- scale out runtime replicas
- inspect provider-side latency and rate limiting

### Symptom: Many `RetryScheduled` tasks

Checks:

1. inspect failure messages in status snapshots
2. confirm upstream provider health and quotas
3. verify tool dependencies (filesystem/CLI/network) are healthy

Actions:

- tune retry delay to avoid immediate retrial storms
- reduce max attempts for non-transient failures
- route affected workload to fallback provider configuration

### Symptom: Tool calls not visible in UI

Checks:

1. verify `FunctionCallingMiddleware` is in the agent pipeline
2. verify callback registration in `Program.cs`
3. verify feed polling in UI session matches `SessionKey`

Actions:

- rewire callback registration
- ensure execution context scope wraps executor invocation

## Safe Migration to Durable Infrastructure

Recommended sequence:

1. introduce durable `IMullaiTaskStatusStore`
2. introduce durable `IMullaiTaskQueue`
3. add dead-letter queue for exhausted retries
4. add idempotency keys on submit endpoint
5. add periodic cleanup jobs / retention policy

## Security and Multi-Tenant Hardening

- require authn/authz on task endpoints
- enforce per-tenant rate limits and quotas
- validate and constrain `metadata` fields
- avoid exposing sensitive prompt/result content in shared logs
- encrypt durable status payloads if they include sensitive data

## SLO Suggestions

Candidate service objectives:

- p95 submit-to-complete latency under target (for example 5s/15s by class)
- failure rate below threshold (for example <1%)
- queue starvation incidents = 0 over rolling window

## Source References

- `src/Mullai.TaskRuntime/TaskRuntime/Services/MullaiTaskWorkerService.cs`
- `src/Mullai.TaskRuntime/TaskRuntime/Services/CronTaskSchedulerService.cs`
- `src/Mullai.TaskRuntime/TaskRuntime/MullaiTaskRuntimeServiceCollectionExtensions.cs`
- `src/Mullai.TaskRuntime/TaskRuntime/Execution/MullaiTaskExecutionContext.cs`
- `src/Mullai.Web/Program.cs`
