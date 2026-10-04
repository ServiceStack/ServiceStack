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

### 1.2 More Vector Support (M)
`[Vector]` `float[]` and `ReadOnlyMemory<float>` columns with `Sql.CosineDistance()`, `Sql.L2Distance()` and
`Sql.NegativeInnerProduct()` are supported on PostgreSQL (pgvector), SQL Server 2025, MariaDB, MySQL and SQLite
(sqlite-vec), with HNSW and IVFFlat index options, `db.SetVectorSearch()` and half-precision vectors. Remaining:
- SQL Server's `DiskANN` vector index, which is only used by queries of its `VECTOR_SEARCH()` table function, so it
  needs a portable query API, e.g. `q.NearestTo(x => x.Embedding, vector, take: 10)`. It's generally available in
  Azure SQL, and a preview feature of SQL Server 2025

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
`diff.ToMigration()` writes them as a migration and `db.ApplySchemaDiff(diff)` applies them. Indexes are compared by
their key columns, uniqueness, INCLUDE columns and WHERE conditions, and default values, foreign keys, unique and check
constraints and primary keys are compared, the indexes, foreign keys and constraints that aren't in the model are found,
and likely column renames are suggested. Tables that aren't
managed by OrmLite are ignored with `OrmLiteConfig.SchemaDiff` (`AspNet*` by default), and
`AdminDatabaseFeature.LogSchemaDiff` logs the differences when an App starts. The Admin UI compares the data models
of AutoQuery APIs and the App's models of the tables migrations create, found by `Migrator.GetMigrationTables()`,
and the `migrate.new` App Task writes the next migration to the App's migrations. Remaining:
- Change a primary key, with the foreign keys that reference it, which is only reported
- Compare the WHERE conditions of indexes that a database rewrites, e.g. PostgreSQL's IN lists, as check constraints
  are, by creating them in a temporary table
- Rebuild a SQLite table to alter its columns, defaults, foreign keys and constraints
- Oracle and Firebird, which haven't been tested and don't compare indexes, defaults, foreign keys or constraints

---

## 4. Security

### 4.1 Roslyn Analyzer Package (M)
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
- **Retrying queries run with OrmLite's Dapper APIs:** OrmLite's embedded copy of Dapper is never modified, so it
  stays the same as Dapper's own implementation. Its queries don't run through OrmLite's commands, so they aren't
  retried. Queries that need retrying use OrmLite's APIs.
- **`[JsonIndex]` to index the JSON properties of a model:** each database only uses an index whose expression is
  the same as the query's, including OrmLite's `CASE WHEN json_type(...)` checks, so the SQL of JSON queries could
  never change without silently leaving existing indexes unused. SQL Server and MariaDB can only index them through
  extra computed or virtual columns with the expression's exact type and collation, which the schema diff would see
  as columns that aren't in the model. Values longer than the index's limit fail inserts on SQL Server and MariaDB,
  and MySQL 8 and MariaDB before 11.8 couldn't be verified. A property that's queried often is better stored in a
  column of its own, and SQLite and PostgreSQL can index the query's expression by hand, see `json.md`.
- **Slow query log, N+1 detection and parameter redaction in logs:** reconsider if users ask for them.
- **Compiling the SET clause of compiled `UPDATE` statements:** `UpdateOnly()` and `UpdateAdd()` reuse the SQL of a
  compiled query's `WHERE` clause. Their `SET` clause is a few columns and db params, and would need write rules'
  values, e.g. `ModifiedBy`, to be bound for each run.
- **Reusing compiled SQL with `OrmLiteConfig.SqlExpressionSelectFilter`:** FilterSets add conditions like soft deletes
  to each connection's queries, which compiled queries reuse their SQL with.
- **Calling `DbCommand.Prepare()`:** OrmLite creates a command for each statement, and drivers that keep prepared
  statements for a connection prepare them by themselves, e.g. Npgsql's `Max Auto Prepare`, while SQL Server reuses
  the plans of parameterized SQL.
- **Sparse vectors:** only PostgreSQL's pgvector has them (`sparsevec`), which would need a C# type of their own.
  Reconsider if users ask for them.
- **Recognizing JSON for each property:** PostgreSQL's dialect always stores complex types as JSON, so `[PgSqlJsonB]`
  columns are already queried, and JSON from custom serializers, e.g. SQL Server's `[SqlJson]` types, is queried with
  `Sql.Json(x.Address).City`.
