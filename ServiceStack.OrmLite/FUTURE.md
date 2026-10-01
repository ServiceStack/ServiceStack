# FUTURE: ServiceStack.OrmLite Roadmap Ideas

Potential features and improvements that would add value to OrmLite as a typed, code-first, low-ceremony ORM.
Each idea keeps OrmLite's existing design principles:

- **Stateless extension methods** on `IDbConnection` with no context object and no change tracker.
- **Typed first, SQL-friendly**: typed APIs that compile to predictable SQL, with an easy escape hatch to raw SQL.
- **Dialect-agnostic**: works across providers, with graceful fallbacks (or clear `NotSupportedException`s) where an RDBMS lacks a feature.
- **Symmetric sync and async APIs**.

Effort: **S** = days, **M** = 1-2 weeks, **L** = multi-week.

---

## 1. Query Expressiveness

### 1.1 More Common Table Expressions (S/M)
Recursive CTEs are supported with `q.WithRecursive(seed, recurse)`. Remaining CTE features:
- Non-recursive `q.With(name, subQuery)` for naming sub queries that are referenced multiple times
- A depth column and max depth for recursive queries, e.g. to limit how many levels are returned
- Cycle protection for data with loops, e.g. PostgreSQL 14+ `CYCLE` or tracking visited ids

---

## 2. Data Access

### 2.1 Returning Only Selected Columns (S)
`UpdateOnlyReturning()`, `DeleteReturning()` and `Upsert` use `RETURNING` / `OUTPUT` to return all columns of the
affected rows. Allow returning only selected columns, e.g. `returning: x => new { x.Id, x.Status }`, to reduce the data
read back for wide tables.

### 2.2 Bulk Upsert / Merge (M)
`BulkInsert` exists. Add `BulkUpsert<T>(rows, updateOnly)` that bulk-loads into a temp table (COPY / SqlBulkCopy / multi-row VALUES), then runs one `MERGE` / `ON CONFLICT` / `ON DUPLICATE KEY` statement.

---

## 3. Modelling

### 3.1 Connection Filters and Write Rules (in progress)
Connection-scoped mandatory filters for multi-tenancy and soft deletes (`db.EnsureFilter<T>()`), and write rules for
auditing columns (`db.EnsureWrites<T>()`, `db.OnInsert<T>()`, `db.OnUpdate<T>()`). Filters on reads, updates and deletes
are done. See [AUDIT_FILTERS.md](AUDIT_FILTERS.md) for the plan and remaining stages.

### 3.2 LINQ Queries Into JSON / Complex-Type Columns (L)
Complex properties are already stored as JSON/JSV text blobs, but querying them needs `Sql.JsonValue("path")` strings. Translate member access directly:
```csharp
db.Select<Customer>(x => x.Address.City == "London" && x.Tags.Contains("vip"));
```
- PostgreSQL uses `jsonb` operators, SQL Server uses `JSON_VALUE` / `OPENJSON`, SQLite uses `json_extract`, and MySQL uses `->>`.
- Includes optional `[JsonIndex(nameof(Address.City))]` to create generated-column or expression indexes.
- Requires JSON (not JSV) serialization for the column, so this would be opt-in via `[Json]` / `[PgSqlJsonB]`.

### 3.3 Vector Columns and Similarity Search (M)
First-class `float[]` / `ReadOnlyMemory<float>` vector columns for AI and RAG apps. Supported natively by pgvector, SQL Server 2025 `VECTOR`, sqlite-vec and MySQL 9 `VECTOR`:
```csharp
public class Doc { public int Id { get; set; } [Vector(1536)] public float[] Embedding { get; set; } }
var nearest = db.Select(db.From<Doc>().OrderBy(x => Sql.CosineDistance(x.Embedding, queryVec)).Take(5));
```
- Includes index DDL (`HNSW` / `IVFFLAT`) through attributes.
- Would pair naturally with ServiceStack's AI features.

### 3.4 Temporal / System-Versioned Tables (M)
`[SystemVersioned]` DDL support, plus `q.AsOf(timestamp)` / `q.Between(from, to)` for SQL Server temporal tables and MariaDB system-versioned tables. On other dialects this would be emulated with history tables and triggers.

---

## 4. Performance

### 4.1 Compiled / Cached Queries (M)
Expression visiting and SQL generation run on every call. A compiled-query API caches the SQL text once and only re-binds parameters:
```csharp
static readonly var ByCustomer = OrmLite.Compile((IDbConnection db, int customerId) =>
    db.From<Order>().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.Id));
var orders = db.Select(ByCustomer, 42);
```
Prepared-statement reuse (`DbCommand.Prepare()` / `DbBatch`) could be layered on top.

### 4.2 `DbBatch` Support (.NET 6+) (M)
Use ADO.NET `DbBatch` for `InsertAll`, `UpdateAll`, `DeleteAll`, `SaveAll` and `UpsertAll`. This cuts one round-trip per row to one per batch on providers that support it: Npgsql, SqlClient and MySqlConnector. The upsert SQL, which `UpsertAll` currently
generates for each row, would then be prepared once for each distinct set of insert fields.

---

## 5. Schema and Migrations

### 5.1 Schema Diff and Migration Scaffolding (L)
Compare `ModelDefinition`s against the live schema (columns, types, nullability, indexes, foreign keys) and emit:
```csharp
var diff = db.GetSchemaDiff<Order>();      // added/removed/changed columns & indexes
db.ApplySchemaDiff(diff, allowDestructive: false);
```
The existing `Migrator` could also generate a new migration class from the diff. This bridges the gap between `CreateTableIfNotExists` and hand-written migrations.

### 5.2 Database-First Model Generation (M)
Replace the legacy T4 templates with a `dotnet` tool (or `x` tool command) that generates OrmLite POCOs from an existing database, reusing the dialect catalog queries.

### 5.3 Richer DDL Attributes (S)
Would cover:
- Partial and filtered indexes: `[Index(Where = "IsDeleted = 0")]`.
- Covering indexes: `INCLUDE`.
- Descending index columns.
- Generated / stored columns.
- `CHECK` constraints for enums (`[EnumAsCheck]`).
- Table and column comments (`[Description]` → `COMMENT ON`).

---

## 6. Observability and Diagnostics

### 6.1 `db.Explain(q)` (S)
Returns the provider's query plan (`EXPLAIN [ANALYZE]`, `SET SHOWPLAN_XML`, `EXPLAIN QUERY PLAN`), which helps with index tuning from tests or admin UIs.

---

## 7. Resilience

### 7.1 Transient-Fault Retry Policies (S/M)
```csharp
dbFactory.RetryPolicy = OrmLiteRetry.Exponential(maxRetries: 3)
    .Handle(SqlServerTransient.IsTransient)   // deadlocks (1205), Azure throttling, failover
    .Handle(PostgresTransient.IsTransient);   // serialization failures (40001), connection resets
```
Retries would only apply outside explicit transactions, or re-run a whole `db.InTransaction(fn)` block. Serializable isolation and cloud databases make this important.

### 7.2 Read/Write Connection Routing (S)
Named connections exist, but routing is manual. An `OpenReadOnlyDbConnection()` (or `db.ReadReplica()`) convention would pick a replica connection string automatically, falling back to the primary when no replica is configured.

---

## 8. Security

### 8.1 Roslyn Analyzer Package (M)
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
