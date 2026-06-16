# ActionItems Sharding POC

A .NET proof-of-concept demonstrating **WorkAreaId-based database sharding** shared between a Web API and a background worker. The SDK is structured so you can lift the `ActionItems/` and `Sharding/` folders into a production microservice with minimal changes.

---

## Table of contents

- [What this POC demonstrates](#what-this-poc-demonstrates)
- [Solution layout](#solution-layout)
- [Architecture overview](#architecture-overview)
- [SDK structure](#sdk-structure)
- [Data model](#data-model)
- [Sharding](#sharding)
  - [Shard catalog](#shard-catalog)
  - [Resolution flow](#resolution-flow)
  - [ApplicationIntent (master / read replica)](#applicationintent-master--read-replica)
  - [Round-robin assignment](#round-robin-assignment)
  - [Key Vault secret resolution](#key-vault-secret-resolution)
  - [Shard caching](#shard-caching)
- [Shared DbContext per request](#shared-dbcontext-per-request)
- [Dependency injection](#dependency-injection)
- [Configuration](#configuration)
- [Running locally](#running-locally)
- [API reference](#api-reference)
- [Background worker](#background-worker)
- [Porting to production](#porting-to-production)

---

## What this POC demonstrates

| Concern | Approach |
|---------|----------|
| Sharding key | `WorkAreaId` (GUID) |
| Shard routing | Central catalog DB maps work areas → shard keys |
| Connection strings | **Not** stored in the catalog — Key Vault secret names only |
| Read scaling | `ApplicationIntent.Read` → read replica connection |
| Writes | `ApplicationIntent.ReadWrite` → master connection |
| New work areas | Round-robin shard assignment, mapping persisted to catalog |
| Fast lookups | `IShardCache` (in-memory; swap for Redis) |
| Shared context | One `ActionItemsDbContext` per HTTP request / worker scope |
| Cross-service | Same SDK wired into API and Worker |

Replication and master/slave sync are **out of scope** — handled by DevOps / the DBMS. This application only picks the correct connection string.

---

## Solution layout

```
ActionItems.Poc/
├── ActionItems.Poc.slnx
├── README.md
└── src/
    ├── ActionItems.Sdk/       # Shared library (domain + sharding)
    ├── ActionItems.Api/       # Web API — MediatR + Swagger
    └── ActionItems.Worker/    # Background worker — event consumer
```

| Project | Role |
|---------|------|
| `ActionItems.Sdk` | Entities, repositories, services, sharding infrastructure |
| `ActionItems.Api` | REST endpoints, Swagger UI |
| `ActionItems.Worker` | Processes action-item events (add/update via master) |

---

## Architecture overview

```mermaid
flowchart TB
    subgraph hosts [Hosts]
        API[ActionItems.Api]
        Worker[ActionItems.Worker]
    end

    subgraph sdk [ActionItems.Sdk]
        Services[ActionItems.Services]
        Repos[ActionItems.Repositories]
        Scope[IShardedScope]
        Resolver[IShardResolver]
        Cache[IShardCache]
        KV[IKeyVaultSecretProvider]
    end

    subgraph data [Databases]
        Catalog[(Shard Catalog)]
        Master1[(Shard 1 Master)]
        Read1[(Shard 1 Read)]
        Master2[(Shard 2 Master)]
        Read2[(Shard 2 Read)]
    end

    API --> Services
    Worker --> Services
    Services --> Repos
    Repos --> Scope
    Scope --> Resolver
    Resolver --> Cache
    Resolver --> Catalog
    Resolver --> KV
    KV --> Master1
    KV --> Read1
    KV --> Master2
    KV --> Read2
    Scope --> Master1
    Scope --> Read1
```

**Per-operation flow:**

1. Host receives `WorkAreaId` (route param or event payload).
2. Service calls `IShardedScope.InitializeAsync(workAreaId, applicationIntent)`.
3. `IShardResolver` checks cache → catalog → Key Vault → returns connection string.
4. `ShardedScope` builds one `ActionItemsDbContext` and attaches it to `ShardedDbContextHolder`.
5. Injected repositories (`IEntityRepository`, `IActionItemRepository`) share that context.
6. `SaveChangesAsync()` on any repository commits all pending changes on that shard.

---

## SDK structure

Everything lives in **one project** (`ActionItems.Sdk`), split into two concerns:

```
ActionItems.Sdk/
├── ActionItems/                         # Domain + persistence (shard-agnostic)
│   ├── Entities/
│   │   ├── ActionItem.cs
│   │   └── Entity.cs
│   ├── Data/
│   │   └── ActionItemsDbContext.cs     # Per-shard EF Core context
│   ├── Repositories/
│   │   ├── IActionItemRepository.cs
│   │   ├── ActionItemRepository.cs
│   │   ├── IEntityRepository.cs
│   │   └── EntityRepository.cs
│   ├── Services/
│   │   ├── IActionItemService.cs
│   │   ├── ActionItemService.cs
│   │   ├── IEntityService.cs
│   │   └── EntityService.cs
│   └── DependencyInjection/
│       └── ActionItemsServiceCollectionExtensions.cs   # AddActionItemsPersistence()
│
├── Sharding/                            # Shard routing infrastructure
│   ├── ApplicationIntent.cs
│   ├── IShardResolver.cs / ShardResolver.cs
│   ├── IShardedScope.cs / ShardedScope.cs
│   ├── ShardedDbContextHolder.cs
│   ├── ShardInfo.cs
│   ├── ShardDatabaseInitializer.cs
│   ├── ShardedRepositoryAccess.cs
│   ├── IRoundRobinCounter.cs / RoundRobinCounter.cs
│   ├── Catalog/
│   │   ├── ShardCatalogDbContext.cs
│   │   └── Entities/
│   │       ├── ShardDefinition.cs
│   │       └── WorkAreaShardMapping.cs
│   ├── Caching/
│   │   ├── IShardCache.cs
│   │   └── InMemoryShardCache.cs
│   ├── KeyVault/
│   │   ├── IKeyVaultSecretProvider.cs
│   │   └── FileKeyVaultSecretProvider.cs
│   └── DependencyInjection/
│       └── ShardingServiceCollectionExtensions.cs        # AddActionItemsSharding()
│
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs                    # AddActionItemsSdk()
```

---

## Data model

### Shard databases (`ActionItemsDbContext`)

Each shard has its own database containing:

**Entity**
| Column | Type | Notes |
|--------|------|-------|
| `Id` | GUID | PK |
| `Name` | string | |

**ActionItem**
| Column | Type | Notes |
|--------|------|-------|
| `Id` | GUID | PK |
| `WorkAreaId` | GUID | Sharding key (denormalized) |
| `EntityId` | GUID | FK → Entity |
| `Title` | string | |
| `Status` | string | Default: `Open` |
| `CreatedAtUtc` | DateTime | |
| `UpdatedAtUtc` | DateTime? | |

`ActionItem` holds the FK to `Entity` (not the other way around).

### Shard catalog (`ShardCatalogDbContext`)

**ShardDefinition** — registered shards (no connection strings)
| Column | Type | Notes |
|--------|------|-------|
| `ShardKey` | string | PK, e.g. `shard-1` |
| `MasterKeyVaultSecretName` | string | Key Vault secret for write/master |
| `ReadReplicaKeyVaultSecretName` | string | Key Vault secret for read replica |

**WorkAreaShardMapping** — work area → shard assignment
| Column | Type | Notes |
|--------|------|-------|
| `WorkAreaId` | GUID | PK |
| `ShardKey` | string | FK → ShardDefinition |

### Demo seed data

| WorkAreaId | Shard |
|------------|-------|
| `11111111-1111-1111-1111-111111111111` | `shard-1` |
| `22222222-2222-2222-2222-222222222222` | `shard-2` |

New work areas (not in the table above) are assigned via round-robin on first write.

---

## Sharding

### Shard catalog

The catalog is a **separate database** (`shard-catalog.db` in the POC). It answers one question:

> Given a `WorkAreaId`, which shard key should I use?

Connection strings are **never** stored here — only Key Vault secret names on `ShardDefinition`.

### Resolution flow

```
WorkAreaId
    → IShardCache (hit? return)
    → ShardCatalog.WorkAreaShards (lookup ShardKey)
    → ShardCatalog.Shards (get KeyVault secret name for intent)
    → IKeyVaultSecretProvider (resolve connection string)
    → ShardInfo (cached)
    → ActionItemsDbContext
```

### ApplicationIntent (master / read replica)

```csharp
public enum ApplicationIntent
{
    Read,       // → ReadReplicaKeyVaultSecretName
    ReadWrite   // → MasterKeyVaultSecretName
}
```

| Operation | Intent | Connection |
|-----------|--------|------------|
| GET entities / action items | `Read` | Read replica |
| POST / PATCH / create / update | `ReadWrite` | Master |
| Worker event processing | `ReadWrite` (hardcoded) | Master |

In **SQL Server production**, Key Vault secrets contain the full connection string including `ApplicationIntent=ReadOnly` or `ApplicationIntent=ReadWrite`. DevOps manages failover and replication.

In this **SQLite POC**, master and read replica are separate files (e.g. `shard-1.db` vs `shard-1-read.db`) to simulate the routing behaviour.

### Round-robin assignment

When `IShardResolver.ResolveForCreationAsync` is called for an unmapped `WorkAreaId`:

1. Pick next shard from `ShardDefinition` (thread-safe counter).
2. Insert `WorkAreaShardMapping`.
3. Resolve master connection string.
4. Cache `ShardInfo` with `ApplicationIntent.ReadWrite`.

Used when creating the first entity for a new work area.

### Key Vault secret resolution

**POC:** `FileKeyVaultSecretProvider` reads `keyvault-secrets.json`:

```json
{
  "shard-1-master": "Data Source=shard-1.db",
  "shard-1-read": "Data Source=shard-1-read.db",
  "shard-2-master": "Data Source=shard-2.db",
  "shard-2-read": "Data Source=shard-2-read.db"
}
```

**Production:** Replace `IKeyVaultSecretProvider` with an Azure Key Vault implementation. The catalog schema stays the same — only secret names are stored.

Secrets can also be inlined in `appsettings.json` under `KeyVault:Secrets` for local dev.

### Shard caching

`IShardCache` caches resolved `ShardInfo` per `workAreaId + applicationIntent` (1-hour TTL in the in-memory implementation).

| POC | Production |
|-----|------------|
| `InMemoryShardCache` | Replace with `RedisShardCache` implementing `IShardCache` |

Register your Redis implementation in `ShardingServiceCollectionExtensions` instead of `InMemoryShardCache`.

---

## Shared DbContext per request

This mirrors the standard ASP.NET Core pattern where `AddDbContext` gives one context per request — except the connection string is resolved at runtime from the shard catalog.

```csharp
// Inject both repos + scope in a handler
public sealed class MyHandler(
    IShardedScope shardedScope,
    IEntityRepository entities,
    IActionItemRepository actionItems)
{
    public async Task Handle(Guid workAreaId, ...)
    {
        await shardedScope.InitializeAsync(workAreaId, ApplicationIntent.ReadWrite);

        await entities.AddAsync(new Entity { ... });
        await actionItems.AddAsync(new ActionItem { ... });

        // Either repo commits everything on the shared context
        await entities.SaveChangesAsync();
    }
}
```

**Rules:**
- Call `InitializeAsync` before using repositories.
- `AddAsync` / `Update*` stage changes only — no implicit save.
- `SaveChangesAsync()` on **either** repository flushes all pending changes.
- No auto-save on dispose — you must call `SaveChangesAsync()` explicitly.
- One work area + one intent per scope; scope re-initializes if intent changes.

---

## Dependency injection

Both hosts register the SDK identically:

```csharp
// Program.cs (Api and Worker)
builder.Services.AddActionItemsSdk(builder.Configuration);
await ShardDatabaseInitializer.InitializeAsync(configuration);
```

`AddActionItemsSdk` composes:

```csharp
services.AddActionItemsSharding(configuration);  // catalog, resolver, cache, key vault, scope
services.AddActionItemsPersistence();            // repos + services
```

### Registrations (reference)

| Service | Lifetime | Layer |
|---------|----------|-------|
| `ShardCatalogDbContext` | Scoped | Sharding |
| `IRoundRobinCounter` | Singleton | Sharding |
| `IKeyVaultSecretProvider` | Singleton | Sharding |
| `IShardCache` | Singleton | Sharding |
| `IShardResolver` | Scoped | Sharding |
| `ShardedDbContextHolder` | Scoped | Sharding |
| `IShardedScope` | Scoped | Sharding |
| `IEntityRepository` | Scoped | ActionItems |
| `IActionItemRepository` | Scoped | ActionItems |
| `IEntityService` | Scoped | ActionItems |
| `IActionItemService` | Scoped | ActionItems |

---

## Configuration

### `appsettings.json` (Api / Worker)

```json
{
  "ConnectionStrings": {
    "ShardCatalog": "Data Source=shard-catalog.db"
  },
  "KeyVault": {
    "SecretsFile": "keyvault-secrets.json"
  }
}
```

Only the **catalog** connection string lives in config. Shard connections come from Key Vault secrets.

### Files created at runtime

Created in each host's working directory (`src/ActionItems.Api/` or `src/ActionItems.Worker/`):

| File | Purpose |
|------|---------|
| `shard-catalog.db` | Shard catalog |
| `shard-1.db` | Shard 1 master |
| `shard-1-read.db` | Shard 1 read replica |
| `shard-2.db` | Shard 2 master |
| `shard-2-read.db` | Shard 2 read replica |

`ShardDatabaseInitializer` seeds demo data and recreates schemas when the catalog model changes.

---

## Running locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Option 1 — Visual Studio / Rider

Open `ActionItems.Poc.slnx`, set **ActionItems.Api** as startup project, run. Swagger opens at `/swagger`.

### Option 2 — CLI

```bash
# Terminal 1 — API
dotnet run --project src/ActionItems.Api
# Swagger: https://localhost:7201/swagger  (or http://localhost:5274/swagger)

# Terminal 2 — Worker
dotnet run --project src/ActionItems.Worker
```

### Option 3 — VS Code

Use the **API + Worker** compound launch profile in `.vscode/launch.json`.

---

## API reference

Base route: `/api/workareas/{workAreaId}`

### Entities

| Method | Route | Intent | Body | Description |
|--------|-------|--------|------|-------------|
| `POST` | `/entities` | ReadWrite | `{ "name": "..." }` | Create entity. New work areas trigger round-robin shard assignment. |
| `GET` | `/entities` | Read | — | List all entities on the shard |
| `GET` | `/entities/{entityId}` | Read | — | Get entity by ID |

### Action items

| Method | Route | Intent | Body | Description |
|--------|-------|--------|------|-------------|
| `POST` | `/action-items` | ReadWrite | `{ "entityId": "...", "title": "..." }` | Create action item |
| `GET` | `/action-items/{actionItemId}` | Read | — | Get action item by ID |
| `GET` | `/action-items/by-entity/{entityId}` | Read | — | List action items for an entity |
| `PATCH` | `/action-items/{actionItemId}/status` | ReadWrite | `{ "status": "Done" }` | Update status |

### Swagger walkthrough

1. **POST** `/entities` with `{ "name": "Contract A" }` — copy returned `id`.
2. **POST** `/action-items` with `{ "entityId": "<id>", "title": "Review contract" }`.
3. **GET** `/entities` and `/action-items/by-entity/{entityId}` to verify reads hit the replica path.
4. **PATCH** status to verify writes hit the master path.

### curl examples

```bash
WORK_AREA=11111111-1111-1111-1111-111111111111
BASE=https://localhost:7201/api/workareas/$WORK_AREA

# Create entity
curl -k -X POST "$BASE/entities" \
  -H "Content-Type: application/json" \
  -d '{"name":"Contract A"}'

# Create action item (replace {entityId})
curl -k -X POST "$BASE/action-items" \
  -H "Content-Type: application/json" \
  -d '{"entityId":"{entityId}","title":"Review contract"}'

# Get action item (replace {actionItemId})
curl -k "$BASE/action-items/{actionItemId}"

# Update status
curl -k -X PATCH "$BASE/action-items/{actionItemId}/status" \
  -H "Content-Type: application/json" \
  -d '{"status":"Done"}'
```

---

## Background worker

`ActionItems.Worker` simulates an event consumer that updates action item status.

```
EventConsumerWorker (every 10s)
    → ProcessActionItemStatusChangedCommand
        → IShardedScope.InitializeAsync(workAreaId, ApplicationIntent.ReadWrite)
        → IActionItemService.UpdateStatusAsync(...)
```

The worker always uses **`ApplicationIntent.ReadWrite`** — it only performs mutations.

To wire a real message bus, replace the demo loop in `EventConsumerWorker` with your queue/topic consumer; keep the MediatR handler and SDK registration unchanged.

---

## Porting to production

### 1. Copy the SDK folders

Lift `ActionItems.Sdk/ActionItems/` and `ActionItems.Sdk/Sharding/` into your shared library. Call `AddActionItemsSdk(configuration)` from each microservice host.

### 2. Replace Key Vault provider

```csharp
// ShardingServiceCollectionExtensions.cs
services.AddSingleton<IKeyVaultSecretProvider, AzureKeyVaultSecretProvider>();
```

Store secrets like:
```
shard-1-master  → Server=...;Database=...;Application Intent=ReadWrite;...
shard-1-read    → Server=...;Database=...;Application Intent=ReadOnly;...
```

### 3. Replace shard cache

```csharp
services.AddSingleton<IShardCache, RedisShardCache>();
```

Cache key format: `shard:workarea:{workAreaId}:intent:{Read|ReadWrite}`

### 4. Swap SQLite for SQL Server

- Catalog: `ShardCatalogDbContext` → SQL Server connection
- Shards: connection strings from Key Vault (already routed by intent)
- Use EF Core migrations instead of `EnsureCreated` in production

### 5. What stays the same

- Catalog schema (`ShardDefinition`, `WorkAreaShardMapping`)
- `IShardResolver` / `IShardedScope` / repository pattern
- `ApplicationIntent` routing
- `AddActionItemsSdk` composition
- API and Worker registration pattern

### 6. What you own in production

- Azure Key Vault secret management (DevOps)
- Read replica provisioning and DB replication (DBA / DevOps)
- Redis cache infrastructure
- Real message bus for the worker
- EF migrations and deployment pipelines

---

## Key interfaces (quick reference)

| Interface | Namespace | Purpose |
|-----------|-----------|---------|
| `IShardResolver` | `Sharding` | Resolve `WorkAreaId` → `ShardInfo` |
| `IShardedScope` | `Sharding` | Initialize shared `ActionItemsDbContext` per operation |
| `IShardCache` | `Sharding.Caching` | Cache resolved shard info |
| `IKeyVaultSecretProvider` | `Sharding.KeyVault` | Resolve secret name → connection string |
| `IEntityRepository` | `ActionItems.Repositories` | Entity CRUD (shared context) |
| `IActionItemRepository` | `ActionItems.Repositories` | Action item CRUD (shared context) |
| `IEntityService` | `ActionItems.Services` | Higher-level entity operations |
| `IActionItemService` | `ActionItems.Services` | Higher-level action item operations |
