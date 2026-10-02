# FUTURE: ServiceStack.OrmLite Roadmap Ideas

Potential features and improvements that would add value to OrmLite as a typed, code-first, low-ceremony ORM.
Each idea keeps OrmLite's existing design principles:

- **Stateless extension methods** on `IDbConnection` with no context object and no change tracker.
- **Typed first, SQL-friendly**: typed APIs that compile to predictable SQL, with an easy escape hatch to raw SQL.
- **Dialect-agnostic**: works across providers, with graceful fallbacks (or clear `NotSupportedException`s) where an RDBMS lacks a feature.
- **Symmetric sync and async APIs**.

Effort: **S** = days, **M** = 1-2 weeks, **L** = multi-week.

---

## 1. Modelling

### 1.1 More JSON Queries (M)
Complex type properties stored as JSON are queried directly, e.g. `x.Address.City == "London"`,
`x.Tags.Contains("vip")`, `x.Lines.Count` and `x.Lines[0].Quantity`, and JSON in `string` columns with `Sql.Json<T>()`.
Remaining JSON features:
- Matching the items of a list by a condition, e.g. `x.Lines.Any(l => l.Sku == "A-1" && l.Quantity > 1)`
- `[JsonIndex(nameof(Address.City))]` to create generated-column or expression indexes of a JSON property
- Recognizing JSON by property, e.g. `[PgSqlJsonB]` columns and custom JSON serializers when the dialect doesn't use JSON
- PostgreSQL's native arrays, e.g. `x.Aliases.Contains("Al")` on a `string[]` column as `= ANY(aliases)`

### 1.2 More Vector Support (S/M)
`[Vector]` columns and `Sql.CosineDistance()`, `Sql.L2Distance()` and `Sql.NegativeInnerProduct()` are supported on
PostgreSQL (pgvector), SQL Server 2025, MariaDB, MySQL and SQLite (sqlite-vec). Remaining vector features:
- SQL Server's `DiskANN` vector index, once it's no longer a preview feature that has to be enabled per database
- Reading vectors in a custom `Select()` on PostgreSQL, which are only read as text when all columns are selected
- `ReadOnlyMemory<float>` properties, half-precision and sparse vectors
- Index options, e.g. HNSW `m` and `ef_construction`, and IVFFlat indexes

### 1.3 More Temporal Tables (M)
`[SystemVersioned]` tables with `q.AsOf(time)`, `q.VersionsBetween(from, to)` and `q.AllVersions()` are supported by
SQL Server and MariaDB, which have them natively. Remaining temporal features:
- System-versioned tables in PostgreSQL, when it supports system time natively. They aren't emulated with history
  tables and triggers on RDBMS that don't have them
- Reading joined tables as of a time, which are read as they are now
- Application-time tables, where the App says when a row is valid, e.g. a price from March to June. Supported by
  PostgreSQL 18+ (`WITHOUT OVERLAPS` keys and `PERIOD` foreign keys) and MariaDB
- Retention of previous versions, e.g. SQL Server's `HISTORY_RETENTION_PERIOD`

---

## 2. Performance

### 2.1 Compiled / Cached Queries (M)
Expression visiting and SQL generation run on every call. A compiled-query API caches the SQL text once and only re-binds parameters:
```csharp
static readonly var ByCustomer = OrmLite.Compile((IDbConnection db, int customerId) =>
    db.From<Order>().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.Id));
var orders = db.Select(ByCustomer, 42);
```
Prepared-statement reuse (`DbCommand.Prepare()` / `DbBatch`) could be layered on top.

### 2.2 `DbBatch` Support (.NET 6+) (M)
Use ADO.NET `DbBatch` for `InsertAll`, `UpdateAll`, `DeleteAll`, `SaveAll` and `UpsertAll`. This cuts one round-trip per row to one per batch on providers that support it: Npgsql, SqlClient and MySqlConnector. The upsert SQL, which `UpsertAll` currently
generates for each row, would then be prepared once for each distinct set of insert fields.

---

## 3. Schema and Migrations

### 3.1 Schema Diff and Migration Scaffolding (L)
Compare `ModelDefinition`s against the live schema (columns, types, nullability, indexes, foreign keys) and emit:
```csharp
var diff = db.GetSchemaDiff<Order>();      // added/removed/changed columns & indexes
db.ApplySchemaDiff(diff, allowDestructive: false);
```
The existing `Migrator` could also generate a new migration class from the diff. This bridges the gap between `CreateTableIfNotExists` and hand-written migrations.

### 3.2 Database-First Model Generation (M)
Replace the legacy T4 templates with a `dotnet` tool (or `x` tool command) that generates OrmLite POCOs from an existing database, reusing the dialect catalog queries.

---

## 4. Resilience

### 4.1 Transient-Fault Retry Policies (S/M)
```csharp
dbFactory.RetryPolicy = OrmLiteRetry.Exponential(maxRetries: 3)
    .Handle(SqlServerTransient.IsTransient)   // deadlocks (1205), Azure throttling, failover
    .Handle(PostgresTransient.IsTransient);   // serialization failures (40001), connection resets
```
Retries would only apply outside explicit transactions, or re-run a whole `db.InTransaction(fn)` block. Serializable isolation and cloud databases make this important.

### 4.2 Read/Write Connection Routing (S)
Named connections exist, but routing is manual. An `OpenReadOnlyDbConnection()` (or `db.ReadReplica()`) convention would pick a replica connection string automatically, falling back to the primary when no replica is configured.

---

## 5. Security

### 5.1 Roslyn Analyzer Package (M)
A `ServiceStack.OrmLite.Analyzers` package would flag at compile time:
- String concatenation or interpolation passed to `Where(string)`, `OrderBy(string)`, `Unsafe*`, `SqlList(string)` and `ExecuteSql(string)`. The code fix would suggest parameters or `Sql.Fmt()`.
- Use of `Unsafe*` APIs with non-constant arguments.
- `SqlVerifyFragment` used as the only defence on user-supplied values that could instead be allow-listed, e.g. suggesting `OrderBySafe()` for user-supplied `OrderBy` values.

---

## Considered and Not Planned

- **OpenTelemetry `ActivitySource` spans:** OrmLite's diagnostic events already appear in ServiceStack's Profiling UI
  with trace ids, and ADO.NET providers like Npgsql and SqlClient emit their own database spans.
- **Sending large collections in raw SQL as a single array or JSON param:** it requires rewriting user-written SQL,
  which is risky to get right for every dialect. Typed APIs like `SelectByIds` and `Contains()` already handle large
  lists.
- **Allow-list helpers for user-supplied filters (`WhereSafe`):** AutoQuery already resolves and restricts
  user-supplied filters, `OrderBySafe()` covers dynamic sorting, and `ColumnRef` / `Sql.Fmt()` cover hand-written
  dynamic queries.
- **Slow query log, N+1 detection and parameter redaction in logs:** reconsider if users ask for them.
