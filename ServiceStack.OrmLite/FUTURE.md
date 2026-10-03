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

### 2.1 More Compiled Queries (M)
`OrmLiteQuery.Compile()` generates the SQL of a typed query once, then only creates db params from its arguments each
time it's run. Remaining:
- Reuse SQL when `OrmLiteConfig.SqlExpressionSelectFilter` is used, e.g. opted in for filters without values that
  change, like soft deletes
- Vector arguments, e.g. `Sql.CosineDistance(x.Embedding, vector)`, and arguments in join conditions
- Compiled `UPDATE` statements: `UpdateOnly()` and `UpdateAdd()` reuse the SQL of a compiled query's `WHERE` clause,
  but generate their `SET` clause each time, with the connection's write rules
- Prepared-statement reuse (`DbCommand.Prepare()`) layered on top

### 2.2 More `DbBatch` Support (M)
`InsertAll`, `UpdateAll`, `UpsertAll` and `SaveAll` send their statements together with an ADO.NET `DbBatch` on
Npgsql, Microsoft.Data.SqlClient and MySqlConnector. Remaining:
- Batch the rows that read a result back, by reading a result set for each statement: new `[AutoIncrement]` rows in
  `SaveAll` and `UpsertAll`, `[ReturnOnInsert]` columns and row versions
- Batch `UpsertAll` on connections with filters or rules, which use an existence check for each row
- Batches in OrmLite's diagnostics events, the dialect's `OnBeforeExecuteNonQuery` hooks and results filters, which
  turn batching off when they're used
- Name the row that failed in the exception, which only SqlClient reports
- Prepare the SQL once for each distinct set of insert fields

---

## 3. Schema and Migrations

### 3.1 More Schema Diff (M)
`db.GetSchemaDiff(types)` finds the differences between models and their tables (tables, columns and indexes),
`diff.ToMigration()` writes them as a migration and `db.ApplySchemaDiff(diff)` applies them. Tables that aren't
managed by OrmLite are ignored with `OrmLiteConfig.SchemaDiff` (`AspNet*` by default), and
`AdminDatabaseFeature.LogSchemaDiff` logs the differences when an App starts. The Admin UI compares the data models
of AutoQuery APIs and the App's models of the tables migrations create, found by `Migrator.GetMigrationTables()`,
and the `migrate.new` App Task writes the next migration to the App's migrations. Remaining:
- Compare default values, foreign keys, check and unique constraints, primary keys and the columns of indexes
- Indexes that are in the database and not in the model
- Detect likely renames: a column that's not in the model and a new property of the same type
- Rebuild a SQLite table to alter its columns
- Oracle and Firebird, which haven't been tested and don't compare indexes

---

## 4. Resilience

### 4.1 More Retries (S)
A `RetryPolicy`, global in `OrmLiteConfig` or for each dialect, runs statements and connections again after a
temporary error, and `db.RunInTransaction()` runs a whole transaction again. SQLite doesn't retry. The APIs that write many rows in a transaction of their own, e.g. `InsertAll`, are run again
as a whole. Remaining:
- Retry queries that fail while reading their rows, before any are returned
- Retry queries run with OrmLite's Dapper APIs

### 4.2 More Read Replicas (S)
`dbFactory.OpenReadOnlyDbConnection()` opens the read replica of a connection registered with `AddReadReplica()`, or the
primary when it doesn't have one. Remaining:
- A read-only `Db` in ServiceStack Services, configured by the AppHost's `DbConnectionRequestFilters` like `Db`, so
  multi-tenant Apps can read from replicas with their tenant's filters

---

## 5. Security

### 5.1 Roslyn Analyzer Package (M)
A `ServiceStack.OrmLite.Analyzers` package would flag at compile time:
- String concatenation or interpolation passed to `Where(string)`, `OrderBy(string)`, `Unsafe*`, `SqlList(string)` and `ExecuteSql(string)`. The code fix would suggest parameters or `Sql.Fmt()`.
- Use of `Unsafe*` APIs with non-constant arguments.
- `SqlVerifyFragment` used as the only defence on user-supplied values that could instead be allow-listed, e.g. suggesting `OrderBySafe()` for user-supplied `OrderBy` values.

---

## Considered and Not Planned

- **Database-first model generation:** [okai](https://docs.servicestack.net/autoquery/okai-db) already generates
  data models, AutoQuery APIs and migrations from an existing database's tables, and
  [AutoGen](https://docs.servicestack.net/autoquery/autogen) creates AutoQuery APIs for them at runtime.
- **OpenTelemetry `ActivitySource` spans:** OrmLite's diagnostic events already appear in ServiceStack's Profiling UI
  with trace ids, and ADO.NET providers like Npgsql and SqlClient emit their own database spans.
- **Sending large collections in raw SQL as a single array or JSON param:** it requires rewriting user-written SQL,
  which is risky to get right for every dialect. Typed APIs like `SelectByIds` and `Contains()` already handle large
  lists.
- **Allow-list helpers for user-supplied filters (`WhereSafe`):** AutoQuery already resolves and restricts
  user-supplied filters, `OrderBySafe()` covers dynamic sorting, and `ColumnRef` / `Sql.Fmt()` cover hand-written
  dynamic queries.
- **Slow query log, N+1 detection and parameter redaction in logs:** reconsider if users ask for them.
