# Mullai Web Task Runtime API

## Base Route

All runtime task endpoints are exposed under:

- `/api/mullai/tasks`

Implemented in:

- `src/Mullai.TaskRuntime/TaskRuntime/MullaiTaskEndpoints.cs`

## Authentication Note

Current implementation does not apply endpoint-specific auth policy yet. In production, add authn/authz for task submission and status access.

## 1) Submit Task

### Request

- `POST /api/mullai/tasks/`

Body schema (`MullaiTaskSubmitRequest`):

```json
{
  "sessionKey": "web-session-001",
  "agentName": "Assistant",
  "prompt": "Summarize today's production incidents",
  "maxAttempts": 3,
  "source": 0,
  "metadata": {
    "tenantId": "acme",
    "requestId": "req-123"
  }
}
```

Field behavior:

- `sessionKey` (required): logical conversation lane.
- `prompt` (required): task payload.
- `agentName` (optional): defaults to `Assistant`.
- `maxAttempts` (optional): falls back to `Mullai:TaskRuntime:DefaultMaxAttempts`.
- `source` (optional): enum `Client=0`, `Cron=1`, `System=2`.
- `metadata` (optional): extension key/value map.

### Success Response

- `202 Accepted`

```json
{
  "taskId": "9f07b2e3c2e74810a9f0b8b5f6f67de2"
}
```

Location semantics:

- accepted URI: `/api/mullai/tasks/{taskId}`

### Validation Errors

- `400 Bad Request` when:
- `prompt` is null/empty/whitespace
- `sessionKey` is null/empty/whitespace

## 2) Get Task Status

### Request

- `GET /api/mullai/tasks/{taskId}`

### Success Response

- `200 OK`

```json
{
  "taskId": "9f07b2e3c2e74810a9f0b8b5f6f67de2",
  "sessionKey": "web-session-001",
  "agentName": "Assistant",
  "source": 0,
  "state": 3,
  "attempt": 0,
  "maxAttempts": 3,
  "response": "Here is a summary...",
  "error": null,
  "updatedAtUtc": "2026-03-22T10:23:45.1234567+00:00"
}
```

State enum (`MullaiTaskState`):

- `Queued = 0`
- `Running = 1`
- `RetryScheduled = 2`
- `Succeeded = 3`
- `Failed = 4`

### Not Found

- `404 Not Found` when `taskId` does not exist in status store.

## 3) Get Recent Tasks

### Request

- `GET /api/mullai/tasks?take=50`

Query:

- `take` (optional): number of snapshots to return; defaults to `50`.

### Success Response

- `200 OK`
- Returns array ordered by most recent `UpdatedAtUtc`.

Example:

```json
[
  {
    "taskId": "...",
    "state": 1,
    "updatedAtUtc": "..."
  },
  {
    "taskId": "...",
    "state": 0,
    "updatedAtUtc": "..."
  }
]
```

## Retry Semantics

Retry is runtime-managed by workers:

- task starts at `Attempt = 0`
- on failure, worker computes `nextAttempt = Attempt + 1`
- retries while `nextAttempt < MaxAttempts`
- when retrying:
- mark `RetryScheduled`
- wait `RetryDelaySeconds`
- re-enqueue and mark `Queued`
- when retries are exhausted:
- mark `Failed`

## Idempotency Guidance (Recommended)

Current API always creates a new task id. For strict idempotency in production:

1. accept an idempotency key header (for example `Idempotency-Key`)
2. store request hash + generated task id
3. return existing task id for duplicate submits

## Versioning Guidance (Recommended)

When contracts evolve:

- move to `/api/v1/mullai/tasks`
- preserve `v1` behavior for existing clients
- add `v2` for breaking payload/state changes
