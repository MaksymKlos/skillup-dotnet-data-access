# Data Access Playground

A .NET 10 reference project that models a small orders/products domain and uses it to demonstrate
two complementary ways of talking to a relational database — **EF Core on the command side**,
**Dapper on the query side** — together with the patterns that hold such a system together:
the transactional Outbox/Inbox, optimistic and pessimistic concurrency, keyset pagination, and a
dual-provider (PostgreSQL + SQL Server) setup, all wired and orchestrated with .NET Aspire.

It is deliberately not a CRUD sample. Every piece is there to show a specific technique and, more
importantly, the reasoning behind it.

## At a glance

```
                                  HTTP  ·  Minimal API (DataAccess.Api)
                                              │
              ┌───────────────────────────────┴───────────────────────────────┐
              │                                                                 │
     WRITE / COMMANDS                                                  READ / QUERIES
     EF Core 10                                                        Dapper 2.1
     ────────────────────────────                                      ───────────────────────────
     • Order / Product aggregates                                      • IOrderQueries → flat DTOs
     • invariants, domain events                                       • multi-mapping (splitOn)
     • IUnitOfWork = DbContext                                         • keyset pagination (N+1 probe)
     • OutboxInterceptor captures                                      • provider SQL dialects
       events in the SAME transaction                                   (quoting, LIMIT / FETCH)
              │                                                                 │
              └───────────────────────────────┬─────────────────────────────────┘
                                               ▼
        ┌──────────────────────────── same schema, two providers ────────────────────────────┐
        │   PostgreSQL  ── optimistic token: xmin   │   SQL Server ── optimistic token: rowversion │
        │   pessimistic: SELECT … FOR UPDATE        │   pessimistic: WITH (UPDLOCK, ROWLOCK)        │
        │   tables: orders · products · outbox_messages · inbox_messages                          │
        └──────────────────────────────────────────────────────────────────────────────────────┘
                                               │
                          outbox_messages  (jsonb  /  nvarchar(max))
                                               │
                              OutboxProcessor  ── relay, at-least-once
                                               │
                                               ▼
                        RabbitMQ  ───►  OrderEventsConsumer
                                        Inbox: idempotent, dedup by MessageId (PK)

  Orchestration: .NET Aspire (AppHost)    ·    Tests: Testcontainers    ·    Bench: BenchmarkDotNet
```

## What it demonstrates

- **CQRS in practice** — writes go through EF Core aggregates (invariants, change tracking,
  transactions); reads go through hand-written Dapper SQL into flat DTOs. Two models over the same tables.
- **Domain-Driven Design building blocks** — aggregates as transaction boundaries, value objects
  compared by value, strongly-typed IDs, domain events, and an invariant-vs-`Result` split for errors.
- **Optimistic concurrency, two ways** — PostgreSQL `xmin` and SQL Server `rowversion`, both as EF
  shadow properties, with a demo that provokes and detects a lost update.
- **Pessimistic locking** — `SELECT … FOR UPDATE` / `UPDLOCK, ROWLOCK`, plus explicit isolation levels.
- **Transactional Outbox + idempotent Inbox** — domain events captured atomically with the state
  change, relayed to RabbitMQ at-least-once, and de-duplicated by a persistent inbox table.
- **Keyset (seek) pagination** — cursor-based paging that stays fast at any depth, with provider-specific SQL.
- **Modern EF Core** — `ExecuteUpdate`/`ExecuteDelete`, `FromSql` with LINQ composition, `AsSplitQuery`,
  optional connection resiliency.
- **A real testing pyramid** — unit tests, a database-free "model builds" check, and integration tests
  on Testcontainers, plus an EF-vs-Dapper micro-benchmark.

## Architecture

Dependencies point inward, toward a domain with zero external references. Commands and queries are
separated into their own infrastructure projects.

```
                         ┌──────────────┐
                         │     Api      │  Minimal API, DI, hosted services
                         └──┬────────┬──┘
              commands →    │        │    ← queries
              ┌─────────────▼──┐  ┌──▼─────────────┐
              │ Infra.EfCore   │  │ Infra.Dapper   │
              └─────────────┬──┘  └──┬─────────────┘
                            │        │
                        ┌───▼────────▼───┐
                        │  Application   │  contracts, commands/handlers, DTOs
                        └───────┬────────┘
                            ┌───▼────┐
                            │ Domain │  aggregates, value objects, events
                            └────────┘
```

| Project | Depends on | Responsibility |
|---|---|---|
| `DataAccess.Domain` | — | Aggregates, value objects, domain events, invariants. No infrastructure references. |
| `DataAccess.Application` | Domain | Contracts (`IUnitOfWork`, repositories, `IOrderQueries`), commands + handlers, `Result`, DTOs, paging. |
| `DataAccess.Infrastructure.EfCore` | Domain, Application | Write side: `DbContext`, mappings, repositories, Outbox/Inbox, migrations, DI. |
| `DataAccess.Infrastructure.Dapper` | Application | Read side: hand-written SQL, provider dialects, connection factory. |
| `DataAccess.Api` | all above | Minimal API endpoints, DI wiring, Outbox relay + consumer hosted services. |
| `DataAccess.AppHost` / `ServiceDefaults` | — | Aspire orchestration; shared telemetry/health-check defaults. |

## Design decisions and why

| Decision | Rationale |
|---|---|
| **EF Core for writes, Dapper for reads** | Writes need aggregates, invariants and change tracking; reads need speed and flat shapes. Using each tool where it is strongest, over the same schema, is CQRS at its most practical level. |
| **Strongly-typed IDs** (`readonly record struct XxxId`) | The compiler stops you passing an `OrderId` where a `ProductId` is expected. One generic `ValueConverter` + a marker interface + reflection auto-scan means a new ID needs zero extra mapping code. |
| **Optimistic concurrency by default** | Conflicts on this domain are rare, so a version token (`xmin`/`rowversion`) beats holding locks. Pessimistic locking is shown as the deliberate alternative for high-contention paths. |
| **Concurrency token as a shadow property** | The token is an infrastructure concern; keeping it out of the domain class avoids leaking persistence details into the model. |
| **Transactional Outbox** | Writing to the database and publishing to a broker cannot be one atomic operation. Persisting the event in the same transaction, then relaying it, removes the dual-write failure window. |
| **Idempotent Inbox** | At-least-once delivery means duplicates. A persisted `inbox_messages` table (PK = message id) makes the consumer idempotent even under a delivery race — giving an effectively exactly-once outcome. |
| **Keyset pagination over OFFSET** | `OFFSET` reads and discards skipped rows and drifts under concurrent writes. A `(CreatedAt, Id)` cursor seeks straight to the next page and stays stable. |
| **`Result` for expected outcomes, exceptions for invariant violations** | "Not enough stock" is a normal business answer (→ `Result` → HTTP 400); a broken invariant is a bug (→ `DomainException`). |
| **`TimeProvider` instead of `DateTime.UtcNow`** | Time becomes injectable and deterministic in tests. The domain never reads the clock; the Outbox interceptor stamps event time in one place. |
| **Two providers from one codebase** | An abstract `DbContext` with thin provider subclasses and per-provider SQL dialects shows where portability is free and where it genuinely differs. |
| **No MediatR, no Central Package Management** | Handlers are invoked directly (one command + handler per file); dependencies stay explicit and easy to follow in a learning codebase. |

### Provider differences, side by side

| Concern | PostgreSQL | SQL Server |
|---|---|---|
| Concurrency token | system column `xmin` | `rowversion` shadow column |
| Outbox payload column | `jsonb` | `nvarchar(max)` |
| Keyset predicate | row-value comparison `(a, b) > (@a, @b)` | expanded `a > @a OR (a = @a AND b > @b)` |
| Row limit | `LIMIT` | `OFFSET … FETCH NEXT` |
| Pessimistic lock | `SELECT … FOR UPDATE` | `WITH (UPDLOCK, ROWLOCK)` |

## Project layout

```
src/
  DataAccess.Domain/            aggregates, value objects, events, strongly-typed IDs
  DataAccess.Application/       commands + handlers, query contracts, Result, paging
  DataAccess.Infrastructure.EfCore/   DbContext, configurations, repositories, Outbox/Inbox, migrations
  DataAccess.Infrastructure.Dapper/   dialects, connection factory, order queries
  DataAccess.Api/               Minimal API, hosted services (Outbox relay + consumer)
  DataAccess.AppHost/           Aspire orchestration (Postgres, SQL Server, RabbitMQ)
  DataAccess.ServiceDefaults/   OpenTelemetry, health checks, resilience
tests/
  DataAccess.UnitTests/         domain + handlers + model-builds
  DataAccess.IntegrationTests/  Testcontainers: round-trip, concurrency, Outbox/Inbox e2e
benchmarks/
  DataAccess.Benchmarks/        BenchmarkDotNet: EF (tracking/no-tracking) vs Dapper
docs/
  learning-guide.en.pdf         detailed, section-by-section walkthrough
```

## Running

Provider is selected by `Database:Provider` (`Postgres` | `SqlServer`).

```bash
# Full orchestration (starts Postgres, SQL Server, RabbitMQ and the Api)
dotnet run --project src/DataAccess.AppHost

# Migrations (connection string comes from the environment)
$env:ConnectionStrings__orders-postgres = "Host=localhost;Database=ordersdb;Username=postgres;Password=postgres"
dotnet ef migrations add <Name> --project src/DataAccess.Infrastructure.EfCore `
  --startup-project src/DataAccess.Infrastructure.EfCore `
  --context PostgresAppDbContext --output-dir Migrations/Postgres

# Tests (integration tests require a Docker runtime)
dotnet test

# Benchmark (Release + Docker)
dotnet run -c Release --project benchmarks/DataAccess.Benchmarks
```

## HTTP surface

| Endpoint | Purpose |
|---|---|
| `POST /products`, `POST /products/{id}/restock` | Create / restock a product (commands, EF) |
| `POST /orders`, `GET /orders/{id}`, `GET /orders` | Place an order; details (Dapper multi-mapping); list (keyset) |
| `GET /outbox` | Inspect captured outbox messages |
| `POST /demo/concurrency`, `POST /demo/pessimistic`, `GET /demo/isolation` | Optimistic vs pessimistic locking; isolation levels |
| `/ef/*` | `ExecuteUpdate`/`ExecuteDelete`, `FromSql`, `AsSplitQuery` |

## Testing

Three levels with distinct roles: fast **unit** tests for the domain and handlers; a database-free
**model-builds** check that fails fast on a misconfigured EF mapping; and **integration** tests on
Testcontainers that drive real PostgreSQL/SQL Server/RabbitMQ through the same DI the app uses.
Coverage includes the EF-write → Dapper-read round-trip, keyset paging, optimistic concurrency,
transaction rollback, Outbox capture, an end-to-end Outbox relay, and Inbox idempotency.

## Further reading

A detailed, section-by-section walkthrough of every technique and decision lives in
[`docs/learning-guide.en.pdf`](docs/learning-guide.en.pdf).
