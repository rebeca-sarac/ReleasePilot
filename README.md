# ReleasePilot

## 1. Project Overview

ReleasePilot is a promotion pipeline API that manages the controlled promotion of application versions through a fixed sequence of deployment environments: **dev → staging → production**.

Each promotion moves through a defined lifecycle — requested, approved, in-progress, and finally completed or rolled back — with approval gates, environment locking, and a full immutable audit trail driven by domain events.

**Core domain concepts:**

- **Promotion** — the aggregate root; represents a request to deploy a specific application version to a target environment.
- **PromotionState** — the state machine: `Requested → Approved → InProgress → Completed / RolledBack`, with `Cancelled` available from `Requested`.
- **EnvironmentName** — a value object that encodes the fixed promotion order and enforces that environments cannot be skipped.
- **ApplicationVersion** — a value object wrapping the semver string.
- **IssueReferences** — optional list of issue tracker keys (e.g. `PROJ-101`) attached to a promotion and resolved to full work items on query.

---

## 2. Architecture

### Clean Architecture layers

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `ReleasePilot.Domain` | Aggregate, value objects, domain events, invariants |
| Application | `ReleasePilot.Application` | Commands, queries, port interfaces, MediatR handlers |
| Infrastructure | `ReleasePilot.Infrastructure` | EF Core, RabbitMQ publisher, port stubs, repositories |
| API | `ReleasePilot.Api` | ASP.NET Core Minimal API endpoints, JWT auth |
| Worker | `ReleasePilot.Worker` | Background service — audit log consumer |

Dependencies point inward only: Infrastructure and API depend on Application; Application depends on Domain; Domain depends on nothing.

### DDD patterns

- **Aggregate** — `Promotion` is the sole aggregate root. All state transitions go through its methods, which raise domain events and return `ErrorOr` results. No handler mutates state directly.
- **Value objects** — `EnvironmentName`, `ApplicationVersion`, and `ApplicationId` are immutable, self-validating, and constructed via static factory methods returning `ErrorOr`.
- **Domain events** — one event per state transition (`PromotionRequested`, `PromotionApproved`, `DeploymentStarted`, `PromotionCompleted`, `PromotionRolledBack`, `PromotionCancelled`). Events are raised inside aggregate methods and collected on `AggregateRoot.DomainEvents`.

### CQRS with MediatR

Commands and queries are separated at the application layer:

- **Write model** — `IPromotionRepository` loads and saves the full `Promotion` aggregate via EF Core. Command handlers return only an id (`Guid`) or void (`Unit`), never a read DTO.
- **Read model** — `IPromotionReadRepository` bypasses the aggregate entirely and projects flat DTOs directly from `AppDbContext` using `AsNoTracking()`. Query handlers compose responses (including work items from `IIssueTrackerPort`) without touching the write model.

### Event-driven with RabbitMQ

After each command persists a state change, the handler publishes the promotion's domain events to a durable fanout exchange (`promotion.events`) via `RabbitMqEventPublisher`. The API returns its HTTP response immediately — the RabbitMQ publish puts the message on the broker, and the Worker consumes it asynchronously in a separate process. The API has no knowledge of what consumers exist.

### Ports and adapters

All external dependencies are expressed as interfaces in `ReleasePilot.Application.Ports`:

| Port | Purpose |
|---|---|
| `IDeploymentPort` | Triggers the actual deployment on `StartDeployment` |
| `INotificationPort` | Sends notifications on terminal states (Completed, RolledBack, Cancelled) |
| `IIssueTrackerPort` | Resolves issue references to full work items on `GetPromotionById` |
| `IEventPublisher` | Publishes domain events to the message broker |
| `IPromotionRepository` | Write-side aggregate persistence |
| `IPromotionReadRepository` | Read-side DTO projection |

The Infrastructure layer provides all implementations, including stubs for the three external service ports that log to the console without calling a real external system.

### Worker service — decoupled audit log consumer

`ReleasePilot.Worker` is a standalone .NET Generic Host process. It runs `AuditLogConsumer` as an `IHostedService`, which subscribes to the same `promotion.events` fanout exchange and persists two records per event:

- An `AuditLog` row (event type, promotion id, timestamp, acting user).
- A `PromotionStateHistory` row (used by `GetPromotionById` to return the full state timeline).

The Worker shares only the Infrastructure project and a database connection with the API. It could be deployed, scaled, or replaced entirely without touching the API.

---

## 3. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL and RabbitMQ)
- [Git](https://git-scm.com/)

---

## 4. Getting Started

**Clone the repository**

```bash
git clone <repository-url>
cd ReleasePilot
```

**Start infrastructure**

```bash
docker compose up -d
```

This starts PostgreSQL (port 5432) and RabbitMQ (AMQP 5672, management UI 15672). Wait for both health checks to pass before continuing.

**Apply database migrations**

```bash
dotnet ef database update \
  --project src/ReleasePilot.Infrastructure \
  --startup-project src/ReleasePilot.Api
```

**Run the API** (terminal 1)

```bash
dotnet run --project src/ReleasePilot.Api
```

**Run the Worker** (terminal 2)

```bash
dotnet run --project src/ReleasePilot.Worker
```

**Open the Scalar API UI**

```
http://localhost:5127/scalar
```

---

## 5. Test Users

The stub auth endpoint issues JWTs for two pre-configured users. Pass the returned token as `Authorization: Bearer <token>` on all other requests.

| Username | Role | Can approve promotions? |
|---|---|---|
| `approver` | `Approver` | Yes |
| `developer` | `Developer` | No — returns 401 |

---

## 6. Running Tests

```bash
dotnet test
```

| Project | Covers |
|---|---|
| `ReleasePilot.Domain.Tests` | Promotion aggregate state machine — all valid transitions, all invalid transitions, immutability of terminal states, business rule validation (role enforcement, empty reason rejection, issue reference storage) |
| `ReleasePilot.Application.Tests` | All command and query handlers — happy paths, not-found cases, conflict cases, port interactions verified with NSubstitute mocks |

---

## 7. Example HTTP Requests

### Get a JWT token

```http
POST http://localhost:5127/auth/token
Content-Type: application/json

{
  "username": "approver"
}
```

### RequestPromotion

```http
POST http://localhost:5127/api/promotions
Content-Type: application/json
Authorization: Bearer <token>

{
  "applicationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "version": "1.2.3",
  "targetEnvironment": "dev",
  "issueReferences": ["PROJ-101", "PROJ-102"]
}
```

Returns `201 Created` with `{ "id": "<promotion-id>" }`. Use this id in all subsequent requests.

### ApprovePromotion

```http
PUT http://localhost:5127/api/promotions/{id}/approve
Authorization: Bearer <approver-token>
```

Returns `204 No Content`. Requires the `Approver` role — a `developer` token returns `401`.

### StartDeployment

```http
PUT http://localhost:5127/api/promotions/{id}/start
Authorization: Bearer <token>
```

Returns `204 No Content`. Triggers `IDeploymentPort` (stub logs to console). Transitions state to `InProgress`.

### CompletePromotion

```http
PUT http://localhost:5127/api/promotions/{id}/complete
Authorization: Bearer <token>
```

Returns `204 No Content`. Records `CompletedAt` and sends a notification via `INotificationPort`. Unlocks the next environment for promotion.

### RollbackPromotion

```http
PUT http://localhost:5127/api/promotions/{id}/rollback
Content-Type: application/json
Authorization: Bearer <token>

{
  "reason": "Health checks failed after deployment."
}
```

Returns `204 No Content`. Requires a non-empty reason. Only valid from `InProgress`.

### CancelPromotion

```http
PUT http://localhost:5127/api/promotions/{id}/cancel
Content-Type: application/json
Authorization: Bearer <token>

{
  "reason": "No longer needed."
}
```

Returns `204 No Content`. Only valid from `Requested`. Approved or in-progress promotions cannot be cancelled.

---

## 8. Design Decisions

**Why PostgreSQL over SQL Server**

PostgreSQL's native `text[]` array type maps cleanly to `IReadOnlyList<string>` via EF Core's `PrimitiveCollection`, making `IssueReferences` a first-class column without a join table. It is also the standard choice for .NET open-source projects and runs well in Docker on all platforms.

**Why MediatR for CQRS**

MediatR provides a clean in-process dispatch mechanism that enforces the command/query separation without coupling handlers to the API layer. Each handler has a single, testable responsibility, and adding a new command or query requires no changes to existing wiring.

**Why ErrorOr result pattern over exceptions**

Exceptions are for exceptional conditions, not expected business outcomes like "state transition not permitted" or "environment not ready". `ErrorOr<T>` makes the failure path explicit in the method signature, forces callers to handle errors, and maps naturally to HTTP problem details without try/catch in every endpoint.

**Why ports live in Application not Domain**

The Domain layer should express pure business concepts and have no opinion about how they are delivered. Ports represent the application's needs from the outside world (notifications, deployments, event publishing) — they are application-layer concerns. Placing them in Domain would couple the business model to infrastructure abstractions it has no reason to know about.

**Why Worker is a separate process**

Decoupling the audit consumer from the API means neither affects the other's availability, deployment lifecycle, or scaling. The API publishes fire-and-forget to RabbitMQ and returns immediately; the Worker processes at its own pace. The Worker can be restarted, redeployed, or replaced with a different consumer implementation without touching the API.

**Why state history is driven by domain events via the Worker**

Recording `PromotionStateHistory` inside a command handler would couple the write path to audit concerns and create implicit side effects. By deriving history from published domain events in the Worker, the record is always consistent with what actually happened at the broker level, and the consumer can replay or rebuild history independently.

**Why docker-compose starts infrastructure only**

The assessment does not require the application to run inside Docker. Running the API and Worker as native `dotnet run` processes gives a faster development loop, visible structured log output in the terminal, and straightforward debugger attachment — without the overhead of containerising the application itself.

---

## 9. What I Would Do Next

- **End-to-end tests** with Testcontainers and `WebApplicationFactory` — spin up a real PostgreSQL container and RabbitMQ container per test run to cover the full request-to-audit-log path.
- **Outbox pattern** for guaranteed event delivery — wrap the domain event publish in the same database transaction as the aggregate save, so a RabbitMQ outage cannot cause a state change to be persisted without its event being delivered.
- **Integration tests for AuditLogConsumer** — publish a raw message to the exchange and assert that the correct `AuditLog` and `PromotionStateHistory` rows appear in the database.
- **Separate read database** — materialise a dedicated read replica or projection store for the query side, completing the true CQRS read/write split and allowing the read model to evolve independently of the write schema.
- **.NET Aspire AppHost** to replace docker-compose — define the full distributed application topology (PostgreSQL, RabbitMQ, API, Worker) in C# with first-class health dashboards, structured log aggregation, and OpenTelemetry traces out of the box.
- **Stretch goal: AI Release Notes Agent** — use Semantic Kernel to compose a natural-language release notes summary when a promotion completes, pulling context from `IssueReferences`, the state history timeline, and the application version, then publishing the summary via `INotificationPort`.
