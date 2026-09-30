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

### 1.2 `UPDATE ... FROM` (S/M)
`INSERT ... SELECT` is already supported with `db.InsertIntoSelect<T>(q)`. Add the equivalent for updating rows from
a joined table without round-tripping rows through .NET:
```csharp
db.UpdateFrom<Order, Customer>((o, c) => o.CustomerId == c.Id, o => new Order { Region = /* c.Region */ });
```

---

## 2. Data Access

### 2.1 Returning Only Selected Columns (S)
`UpdateOnlyReturning()`, `DeleteReturning()` and `Upsert` use `RETURNING` / `OUTPUT` to return all columns of the
affected rows. Allow returning only selected columns, e.g. `returning: x => new { x.Id, x.Status }`, to reduce the data
read back for wide tables.

### 2.2 Bulk Upsert / Merge (M)
`BulkInsert` exists. Add `BulkUpsert<T>(rows, updateOnly)` that bulk-loads into a temp table (COPY / SqlBulkCopy / multi-row VALUES), then runs one `MERGE` / `ON CONFLICT` / `ON DUPLICATE KEY` statement.

### 2.3 Large Collection Params in Raw SQL (S)
Large lists are handled automatically in `SelectByIds`, `DeleteByIds` and `Contains()` expressions, but collection args in raw SQL,
e.g. `db.Select<T>("Id IN (@ids)", new { ids })` or `Sql.Fmt($"Id IN ({ids})")`, still add a param per value. On PostgreSQL and
SQL Server 2016+ these could be sent as a single array or JSON param when the SQL can be safely rewritten.

---

## 3. Modelling

### 3.1 Auditing Columns (S)
Auto-populate columns tagged with `[CreatedDate]`, `[ModifiedDate]`, `[CreatedBy]` and `[ModifiedBy]` on insert, update and upsert. Values come from a pluggable `OrmLiteConfig.AuditUserResolver`, which is cheaper and more discoverable than hand-written `InsertFilter` / `UpdateFilter` code.

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

### 6.1 OpenTelemetry `ActivitySource` (S)
Emit `db.system`, `db.statement` (with an opt-in parameter-redaction policy), `db.operation`, row counts and durations as OTel spans. This would build on the existing `OrmLiteDiagnostics` `DiagnosticListener` events so it works with standard APM tooling without custom listeners.
Lower value than it looks: OrmLite's diagnostic events already appear in ServiceStack's Profiling UI with trace ids, and ADO.NET providers like Npgsql and SqlClient emit their own database spans.

### 6.2 `db.Explain(q)` (S)
Returns the provider's query plan (`EXPLAIN [ANALYZE]`, `SET SHOWPLAN_XML`, `EXPLAIN QUERY PLAN`), which helps with index tuning from tests or admin UIs.

### 6.3 Slow Query Log and N+1 Detection (S)
A configurable threshold that logs the SQL, parameters (redacted) and caller for slow commands. There could also be a debug-mode detector that warns when the same statement shape runs more than N times within one request or connection.

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

### 8.2 Allow-List Helpers for Dynamic Filtering (S)
`OrderBySafe()` covers dynamic sorting. An equivalent helper for user-supplied filters, e.g. `?field=value` pairs, would resolve
field names to quoted columns and only accept allowed fields and operators:
```csharp
q.WhereSafe(request.Filters, allowed: [nameof(Order.Status), nameof(Order.Total)]);
```

### 8.3 Parameter Redaction in Logs (S)
`GetDebugString` / `DebugCommand` currently log every parameter value. Add `OrmLiteConfig.RedactParam` (for example, redact fields marked `[Secret]` / `[PasswordField]` or matching names like `*password*` / `*token*`) so debug logging is safe to enable in production.

---

## Considered and Not Planned

- **Typed global query filters (soft delete / multi-tenancy):** complete coverage means changing nearly every read and
  write path (`SqlExpression` generation, JOIN clauses, the raw `SingleById` / `Where(anon)` APIs, reference loading,
  and every update and delete), which is too disruptive for the value it adds. Soft delete is already supported for
  typed queries with `OrmLiteConfig.SqlExpressionSelectFilter` and `LoadReferenceSelectFilter`. ServiceStack apps
  typically handle multi-tenancy with a database per tenant or with AutoQuery / AutoCrud filters rather than
  ORM-level tenant filters.
