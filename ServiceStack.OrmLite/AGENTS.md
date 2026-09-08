# CONTEXT: ServiceStack.OrmLite

## Purpose

`ServiceStack.OrmLite` is a fast, lightweight, convention-based, code-first POCO Object-Relational Mapper (ORM) for .NET. It extends standard ADO.NET `IDbConnection` instances with typed SQL operations, LINQ-like typed expression builders (`SqlExpression<T>`), automated schema DDL generation, and transparent text blob serialization for complex object graphs.

Key capabilities include:
- **Zero-Config Code-First POCOs**: Data models map directly to RDBMS tables using simple class definitions and optional data annotations (`[AutoIncrement]`, `[Index]`, `[Reference]`, `[Alias]`).
- **Typed SQL Expressions (`SqlExpression<T>`)**: Type-safe query building with compile-time checked column references, lambdas, joins, and parameterized filtering.
- **RDBMS Dialect Agnostic**: Pluggable dialect providers for all major relational database engines (PostgreSQL, SQL Server, MySQL, SQLite, Oracle, Firebird).
- **Asynchronous & Synchronous APIs**: Symmetric async (`db.SelectAsync<T>`, `db.InsertAsync<T>`, `db.UpdateAsync<T>`) and sync methods.
- **Schemaless Text Blobs**: Complex nested properties, lists, and dictionaries automatically serialize to JSON/JSV string columns without requiring relational normalization.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Common     ServiceStack.Text
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                          ServiceStack.OrmLite (Core Engine)
                                    │
    ┌────────────────┬──────────────┼────────────────┬──────────────┐
    ▼                ▼              ▼                ▼              ▼
OrmLite.SqlServer  OrmLite.PostgreSQL  OrmLite.MySql  OrmLite.Sqlite  OrmLite.Oracle
    │
    └───────────────────────────────────────────────────────────────┐
                                                                    ▼
                                                           ServiceStack.Server
                                                        (AutoQuery, OrmLiteAuth)
```

- **Depends on**: `ServiceStack.Common`, `ServiceStack.Interfaces`, `ServiceStack.Text`.
- **Depended on by**:
  - Concrete dialect packages (`ServiceStack.OrmLite.SqlServer`, `ServiceStack.OrmLite.PostgreSQL`, `ServiceStack.OrmLite.MySqlConnector`, `ServiceStack.OrmLite.Sqlite`, etc.).
  - `ServiceStack.Server` (AutoQuery, AutoCrud, `OrmLiteAuthRepository`, `OrmLiteCacheClient`).
  - `ServiceStack.Jobs` (SQLite background jobs and request logger).
- **Role**: The foundational data persistence engine of the ServiceStack framework.

---

## Key Functionality

### 1. Database Connection Extension Methods
- **Querying**: `db.Select<T>()`, `db.Single<T>()`, `db.SingleById<T>()`, `db.Where<T>()`, `db.LoadSelect<T>()` (with child references).
- **Mutations**: `db.Insert<T>()`, `db.InsertAll<T>()`, `db.Update<T>()`, `db.UpdateOnly<T>()`, `db.Delete<T>()`, `db.DeleteById<T>()`, `db.Save<T>()`.
- **Raw SQL & Escaping**: `db.SqlList<T>()`, `db.SqlScalar<T>()`, `db.ExecuteSql()`.
- **DDL & Schemas**: `db.CreateTable<T>()`, `db.CreateTableIfNotExists<T>()`, `db.DropTable<T>()`, `db.TableExists<T>()`.

### 2. Typed Query Builder (`SqlExpression<T>`)
- Fluent query construction: `db.From<Customer>().Where(x => x.City == "London").Join<Order>().OrderByDescending(x => x.CreatedDate)`.
- Translates C# expressions into parameterized SQL dialect queries.
- Prevents SQL injection by parameterizing all lambda constants and variables (`@p_0`, `@p_1`).

### 3. Dialect Provider Architecture (`IOrmLiteDialectProvider`)
- Base class: `OrmLiteDialectProviderBase<TDialect>`.
- Controls SQL syntax generation, identifier quoting (`GetQuotedName`), type converters (`IOrmLiteConverter`), naming conventions, and DDL templates per RDBMS.

### 4. Transactions and SavePoints
- `using var dbTrans = db.OpenTransaction()`
- Nested savepoints via `db.SavePoint("point_name")` with automatic syntax adaptation (`SAVEPOINT` vs `SAVE TRANSACTION`).

---

## Architecture & Design Patterns

### Thin Extension-Method Pattern
OrmLite never wraps `IDbConnection` in heavy context objects or change trackers; all operations are stateless static extension methods on native ADO.NET connections.

### Converter Subsystem (`IOrmLiteConverter`)
Data type conversions between .NET types and ADO.NET `DbType` / `IDataReader` values are delegated to modular type converters (e.g. `StringConverter`, `DateTimeConverter`, `ByteArrayConverter`, `JsonConverter`).

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always enforce these rules:

### 1. SQL Injection Defense in `StripQuotedStrings` (`OrmLiteUtils`)
- **Risk**: Unmatched single quotes (e.g. `'; DROP TABLE Users; --`) can cause `StripQuotedStrings` to leave `inQuotes = true` until EOF, stripping everything and allowing malicious injection fragments through verification regexes.
- **Rule**: `StripQuotedStrings` must return an `out bool inQuotes` flag. Fragments with unclosed quote delimiters must be treated as unsafe and rejected immediately (`throw new ArgumentException(...)`).

### 2. ReDoS Protection in SQL Validation
- `VerifyFragmentRegEx` and `VerifySqlRegEx` must specify a strict timeout (e.g. `TimeSpan.FromSeconds(1)`) to avoid catastrophic backtracking on adversarial input.

### 3. Quoted Identifier Breakout
- In `GetQuotedName(name)` across all dialect providers, embedded quote characters inside table, column, or schema names must be escaped by doubling the quote character (e.g. `"` -> `""` or `` ` `` -> `` `` ``), preventing identifier breakout attacks.

### 4. Catalog Query Sanitization
- In catalog queries (`DoesTableExist`, `DoesSchemaExist`, `ToResetSequenceStatement`), never interpolate unescaped strings into SQL literals; sanitize using `.SqlParam()` or proper parameterized queries.

### 5. SavePoint Name Verification
- Savepoint identifiers must be validated via `SqlVerifyFragment(name)` before string interpolation into DDL statements.

---

## Common Modification Scenarios

1. **Adding or Extending a Dialect Provider**
   - Subclass `OrmLiteDialectProviderBase<T>`.
   - Override database-specific DDL generators, naming conventions, and type converters.
   - Ensure `GetQuotedName` and schema verification rules adhere to dialect quote escaping.

2. **Custom Type Converters**
   - Implement `IOrmLiteConverter` or inherit from `OrmLiteConverter`.
   - Register the converter on the dialect provider: `MySqlDialect.Provider.RegisterConverter<MyType>(new MyConverter())`.

3. **Executing Safe Custom SQL Fragments**
   - Always sanitize raw user input via `SqlVerifyFragment` or pass parameters through `new { Param = value }` argument objects.
