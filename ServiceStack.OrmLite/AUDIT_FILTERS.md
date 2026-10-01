# Connection Filters: Mandatory Query Filters and Write Rules

Plan for connection-scoped mandatory filters (e.g. multi-tenancy, soft deletes) and write rules (e.g. auditing),
registered on a database connection and applied to every typed API OrmLite constructs SQL for.

## Goals

- **Request-scoped without globals**: filters and rules live on the `OrmLiteConnection`, which in ServiceStack is
  opened per request, instead of static delegates like `OrmLiteConfig.InsertFilter` that need to resolve the current
  request.
- **Mandatory**: filters are applied with `Ensure()` semantics, so conditions added later (typed, `Sql.Fmt()` or
  user-supplied, e.g. AutoQuery) can narrow results but never widen them.
- **Complete for typed APIs**: every API where OrmLite constructs the SQL applies the filters and rules, so a filtered
  connection doesn't silently leak through APIs like `SingleById()`. Raw SQL isn't parsed or changed.
- **Explicit opt-out**: `db.WithoutFilters()` returns the same connection without filters or rules.

## API

### Registering filters and rules

```csharp
// Mandatory filters: reads, updates and deletes only see matching rows
db.EnsureFilter<IHasTenantId>(x => x.TenantId == tenantId);  // every table implementing IHasTenantId
db.EnsureFilter<Order>(x => !x.IsDeleted);                   // only the Order table

// A function called for each statement, for filters that change. No filter is applied when it returns null
db.EnsureFilter<IHasTenantId>(() => {
    if (user.IsAdmin) return null;
    return x => x.TenantId == user.TenantId;
});

// Mandatory values: set on insert when unset, throws when set to a different value or changed by an update
db.EnsureWrites<IHasTenantId>(x => x.TenantId, tenantId);

// Write rules: always set the column on inserts / updates
db.OnInsert<IAudit>(x => x.CreatedBy, userId);
db.OnInsert<IAudit>(x => x.CreatedDate, () => DateTime.UtcNow);
db.OnUpdate<IAudit>(x => x.ModifiedBy, userId);
db.OnUpdate<IAudit>(x => x.ModifiedDate, () => DateTime.UtcNow);

// OnWrite is an alias for both OnInsert and OnUpdate, e.g. to also set the modified columns when a row is created
db.OnWrite<IAudit>(x => x.ModifiedBy, userId);

// The same connection and transaction without any filters or rules, e.g. for admin tasks
var adminDb = db.WithoutFilters();
```

- The type argument is a table type or an interface. Interface filters and rules apply to every table implementing the
  interface, with the expression rebound to the table's property of the same name, e.g. `IHasTenantId.TenantId` to
  `Order.TenantId`, so column aliases and naming strategies apply.
- Multiple filters and rules combine, e.g. a tenant filter and a soft delete filter.
- Values are either fixed (`userId`) or a function (`() => DateTime.UtcNow`) evaluated for each statement, or each
  row for object writes. Captured values in filter expressions are sent as params.
- Captured values of a filter are read for each statement, so a filter on a captured `tenantId` variable uses its
  current value. Use the function overload when the filter itself changes, e.g. no filter for admins.
- Registering on a connection that isn't an `OrmLiteConnection` throws, so a filter is never silently ignored.

### Registering more than once

A connection can be configured more than once, e.g. a shared SQLite `:memory:` connection that's opened multiple times
in a request, so registrations are compared with the ones a connection already has:

| Registered again | Result |
|-|-|
| The same filter with the same captured values, or the same filter function | Ignored |
| The same rule for a column with the same value, or the same function | Ignored |
| The same filter with different captured values, e.g. another tenant | Added, with a warning logged as rows need to match both |
| `EnsureWrites` for a column with a different value | Throws `InvalidOperationException` |
| `OnInsert` / `OnUpdate` for a column with a different value | Added, replacing the previous value, with a warning logged |

- Filters are compared by their expression and the values they captured when registered. Captured values that aren't
  simple values, e.g. collections, can't be compared, so the filter is added without a warning.
- Lambdas that capture variables are a new function each time they're created, so filter functions and rule value
  functions like `() => clock.UtcNow` are added again. Functions that don't capture, like `() => DateTime.UtcNow`,
  are the same function. Repeated rules set the same value and repeated filters add the same condition.
- Rule values are compared as the column's type, so `1` and `1L` are the same value. Rules for the same column on an
  interface and a table implementing it are compared too.
- Warnings are logged to the `OrmLiteConnectionFilters` logger.

### App-defined openers

Apps wrap their rules in an extension method so they're defined once:

```csharp
public static IDbConnection OpenForTenant(this IDbConnectionFactory dbFactory, int tenantId, string userId)
{
    var db = dbFactory.OpenDbConnection();
    db.EnsureFilter<IHasTenantId>(x => x.TenantId == tenantId);
    db.EnsureWrites<IHasTenantId>(x => x.TenantId, tenantId);
    db.OnInsert<IAudit>(x => x.CreatedBy, userId);
    db.OnWrite<IAudit>(x => x.ModifiedBy, userId);
    return db;
}
```

### ServiceStack integration

ServiceStack opens connections for requests with `AppHost.GetDbConnection(IRequest)`, used by `Service.Db`, AutoQuery,
AutoCrud and other features. Overriding it applies the filters and rules everywhere the framework opens a connection
for a request, which is the recommended approach in ServiceStack apps and should lead the docs:

```csharp
public override IDbConnection GetDbConnection(IRequest? req = null)
{
    var db = base.GetDbConnection(req);
    if (req?.GetSession() is { } session && session.IsAuthenticated)
    {
        var tenantId = session.GetTenantId(); // app-specific
        db.EnsureFilter<IHasTenantId>(x => x.TenantId == tenantId);
        db.EnsureWrites<IHasTenantId>(x => x.TenantId, tenantId);
        db.OnInsert<IAudit>(x => x.CreatedBy, session.UserAuthId);
        db.OnWrite<IAudit>(x => x.ModifiedBy, session.UserAuthId);
    }
    return db;
}
```

## Where filters are applied

Filters are added to the SQL OrmLite constructs for a filtered table:

| API | How the filter is applied |
|-|-|
| `db.From<T>()`, incl. sub queries, set operations and `SeekAfter()` | `Ensure()` condition when the query is created |
| Typed lambda and anonymous-object APIs: `Select`, `Single`, `Count`, `Exists`, `Scalar`, `Column`, `Dictionary`, `Lookup`, `Where(anon)`, `SelectNonDefaults`, and their async / lazy variants | `Ensure()` condition on the query they build |
| WHERE-clause shorthand APIs with SQL fragments, e.g. `db.Select<T>(Sql.Fmt($"..."))`, `db.Select<T>("Age > @age", ...)` | `WHERE {filter} AND ({fragment})` |
| By-id APIs: `SingleById`, `SelectByIds`, `LoadSingleById`, `DeleteById`, `DeleteByIds`, `ExistsById`, `GetRowVersion` | `AND {filter}` added to their `WHERE Id = @id` |
| Joined tables, e.g. `.Join<Customer>()`, `.LeftJoin<Customer>()` | Added to the join's `ON` clause, so `LEFT JOIN` keeps its meaning |
| `WithRecursive()` | Applied to the seed query and the recursive step, so recursion can't walk into filtered out rows |
| `LoadSelect()`, `LoadSingleById()` and `[Reference]` loading | Applied to the child table queries |
| `TopPerGroup()` | Applied inside the ranked sub query, before rows are ranked |
| Updates and deletes: `Update(obj)`, `UpdateAll`, `UpdateOnly` (all forms), `UpdateOnlyFields`, `UpdateNonDefaults`, `UpdateAdd`, `UpdateFrom`, `Delete(obj)`, `Delete(x => ...)`, `Delete(q)`, `DeleteNonDefaults`, `DeleteWhere`, `DeleteAll`, `UpdateOnlyReturning`, `DeleteReturning` | `AND {filter}` added to their `WHERE`, so rows that don't match aren't changed |
| WHERE-clause deletes and updates with SQL fragments, e.g. `db.Delete<T>("Age > @age", ...)`, `db.UpdateOnly(() => ..., whereExpression, params)` | `WHERE {filter} AND ({fragment})` |
| `Save`, `Upsert` | The existence check, and the update of an existing row, are filtered |
| Raw SQL: complete statements in `SqlList`, `SqlColumn`, `SqlScalar`, `ExecuteSql`, `Select<T>(fullSql)`, `Delete<T>(fullSql)` | **Not applied**: OrmLite doesn't parse user SQL, which is the app's responsibility |
| Legacy `[Obsolete]` APIs: `SelectFmt`, `SelectLazyFmt`, `SingleFmt`, `ExistsFmt`, `UpdateFmt<T>`, `DeleteFmt<T>`, the `Func<SqlExpression<T>, SqlExpression<T>>` overloads and `db.SqlExpression<T>()` | Filtered like their replacements: `WHERE {filter} AND ({fragment})` or a filtered query |
| Legacy APIs without a table type: complete SQL in `ScalarFmt`, `ColumnFmt`, `ColumnDistinctFmt`, `LookupFmt`, `DictionaryFmt`, and `UpdateFmt(table, ...)` / `DeleteFmt(table, ...)` with a table name | **Not applied**: there's no table type to resolve filters for |

Notes:

- Updating or deleting a row that the filter excludes affects 0 rows, the same as a row that doesn't exist, so
  `Update(obj)` returns 0 and `[RowVersion]` updates and deletes throw `OptimisticConcurrencyException`. Apps check the
  returned row count to handle rows that weren't changed.
- `Save` and `Upsert` of a row that the filter excludes don't see it as an existing row, so they insert it, which fails
  on its primary key instead of overwriting it.
- `Upsert` on a filtered table uses a filtered existence check followed by an insert or a filtered update, instead of
  a single upsert statement, as not every RDBMS can filter the row a single upsert statement updates.
- `Where()`, which clears the WHERE conditions, keeps ensured conditions.
- Filters only apply to queries created from the filtered connection. A `SqlExpression` created from another connection
  or `OrmLiteConfig.DialectProvider.SqlExpression<T>()` isn't filtered, which the docs should call out.

## Where write rules are applied

| Rule | APIs |
|-|-|
| `OnInsert` | `Insert` (objects and dictionaries), `InsertAll`, `InsertUsingDefaults`, `InsertOnly` (all forms), `BulkInsert`, `InsertIntoSelect`, and the inserts of `Save` / `SaveAll` and `Upsert` / `UpsertAll` |
| `OnUpdate` | `Update(obj)`, `UpdateAll`, `UpdateOnly` (all forms), `UpdateOnlyFields`, `UpdateNonDefaults`, `UpdateAdd`, `UpdateFrom`, `UpdateOnlyReturning`, and the updates of `Save` / `SaveAll` and `Upsert` / `UpsertAll` |
| `EnsureWrites` | Every insert and update API above |
| Raw SQL, e.g. `ExecuteSql`, and the legacy `UpdateFmt()` | **Not applied** |

### Which rule to use

| | `EnsureWrites` | `OnInsert` / `OnUpdate` |
|-|-|-|
| Use for | A column that's the same for everything the connection writes, e.g. `TenantId` | A column recording an insert or update, e.g. `CreatedBy`, `ModifiedDate` |
| Applies to | Inserts and updates | Only inserts, or only updates |
| App sets a different value | Throws, as it's a bug | Replaced with the rule's value |
| App doesn't set a value | Inserts set it, updates leave the column alone | Always set |

Rule of thumb: `EnsureWrites` for who owns the row, `OnInsert` / `OnUpdate` for who changed it and when. The docs should
lead the write rules section with this table.

`OnWrite` registers both an `OnInsert` and an `OnUpdate` rule for a column. With only `OnUpdate`, a row's `ModifiedBy`
and `ModifiedDate` are empty until its first update. The docs example should use `OnInsert` for the created columns,
with `[IgnoreOnUpdate]`, and `OnWrite` for the modified columns.

`EnsureWrites` depends on what's written:

| Write | Column isn't set | Column is set to a different value |
|-|-|-|
| Insert of an object or dictionary, `InsertOnly`, `BulkInsert` | Sets the value | Throws `InvalidOperationException` |
| `InsertIntoSelect` | Sets the value | Throws `NotSupportedException` if the column is selected |
| Update of all an object's fields: `Update(obj)`, `UpdateAll`, `Save`, `Upsert` | Sets the value, so the update keeps it | Throws `InvalidOperationException` |
| Update of some fields: expressions, dictionaries, anonymous objects, `UpdateOnlyFields`, `UpdateNonDefaults` | Not updated | Throws `InvalidOperationException` |
| `UpdateFrom` | Not updated | Throws `NotSupportedException` if the column is updated, as values from other columns can't be verified |

- **Object writes** (`Insert(obj)`, `Update(obj)`, ...) set the rule's values on the object while it's written, then
  restore its original values. Following the write-back convention, `Insert` / `Update` don't modify the object, while
  `Save` and `Upsert`, which keep objects in sync with their rows, keep the values on the object.
- **Dictionary, anonymous object and expression writes** (`UpdateOnly`, `InsertOnly`, ...) add the column to the `SET`
  or `INSERT` column list, replacing a value for the same column. The app's dictionary isn't modified.
- **`UpdateAdd`** sets the values of rules instead of adding them, e.g. a numeric `ModifiedByUserId`.
- **`InsertIntoSelect`** selects the values of rules with the rows of the query:
  `INSERT INTO t (cols, TenantId) SELECT _s.*, @p FROM (query) _s`.
- **Upserts** on a table with rules use a filtered existence check followed by an insert or an update, like tables
  with filters, as a single upsert statement can't use different values for its insert and update. So `OnInsert`
  columns are only set when the row is inserted, and `OnUpdate` columns when it's updated.
- A rule's value always wins over a value from the app, incl. values set by `OrmLiteConfig.InsertFilter` /
  `UpdateFilter` which run first, so audit columns can be trusted, while `EnsureWrites` throws instead of silently
  changing the value.
- **`OnInsert` columns aren't protected from updates**: updating all an object's fields, e.g. `db.Update(obj)`, also
  writes its `CreatedBy`. Add `[IgnoreOnUpdate]` to created columns so they're only set when the row is inserted,
  which the docs should recommend.
- `EnsureWrites` guards the values that are written, `EnsureFilter` guards the rows that are changed. Multi-tenant apps
  need both: without the filter, an update could move another tenant's row into the connection's tenant.
- Rule values are passed as a value or a lambda, e.g. `() => DateTime.UtcNow`. Passing a delegate variable, e.g. a
  `Func<DateTime>`, as a value throws, as it would be used as the value.

## `WithoutFilters()`

- Returns a lightweight `OrmLiteConnection` over the same underlying connection and transaction, with no filters or
  rules, so admin tasks can run in the current transaction. It delegates its connection and transaction to the
  connection it was created from, so a transaction opened from either is used by both.
- Disposing or closing it doesn't close the underlying connection.
- Throws for connections that weren't opened by OrmLite.
- Filters and rules can't be removed from a connection otherwise, and there's no per-query bypass like
  `q.IgnoreFilters()`, so one line can't remove a mandatory filter from a query.

## Shared connections

SQLite `:memory:` databases only exist while their connection is open, so `OpenDbConnection()` returns the same shared
`OrmLiteConnection` every time. Filters and rules on a shared connection are scoped to its outermost open: nested opens,
which are the same connection, use them, and they're cleared when the outermost open is disposed so they never apply
to the next request or test.

## Implementation

- **Storage**: `OrmLiteConnection.Filters`, an immutable set of filters and rules replaced on each registration, with
  a per-connection cache from table type to its matching filters and rules. Commands reach it through
  `OrmLiteCommand.OrmLiteConnection`.
- **Rebinding interface expressions**: an `ExpressionVisitor` that replaces the interface parameter with a parameter of
  the table type and rebinds member access to the table's property of the same name, cached per table type.
- **Filtered query creation**: one internal entry point, e.g. `dbCmd.CreateQuery<T>()` / `dbConn.CreateQuery<T>()`,
  that creates the dialect's `SqlExpression<T>` and applies the connection's filters. `db.From<T>()`
  (`OrmLiteExecFilter.SqlExpression<T>()`) and the ~65 internal `DialectProvider.SqlExpression<T>()` call sites in the
  read, write, returning, load and legacy APIs are changed to use it.
- **Joins**: `SqlExpression` needs a reference to the connection's filters so `Join<TJoin>()` can add the joined table's
  filter to its `ON` clause, and `WithRecursive()` to its recursive step.
- **By-id and object writes**: render the table's filter condition with its params from a filtered `SqlExpression<T>`
  and append it to the statement's `WHERE` clause.
- **Write rules**: `OrmLiteConnectionWriteRules` applies them where each write API invokes `OrmLiteConfig.InsertFilter`
  / `UpdateFilter`: objects have the values set and restored around the statement, which also works for dialect
  specific `BulkInsert` implementations, and dictionaries of values are copied with the rule values.

## Stages

Each stage includes reference tests in `UseCases/` verified on SQLite, PostgreSQL, SQL Server, MySql and
MySqlConnector, and a full test suite run.

1. ✅ **Filters on queries**: connection storage, `EnsureFilter<T>()`, interface rebinding, `db.From<T>()`, sub queries
   and set operations, filters prefixed with their table (or alias) so later joins stay unambiguous, `Where()` keeps
   ensured conditions when clearing WHERE conditions, and filters scoped to the outermost open of shared connections.
2. ✅ **All typed reads**: lambda / anonymous-object APIs, WHERE-clause shorthand APIs, by-id APIs, async and lazy
   variants, joins (`ON` clause), `WithRecursive()`, `TopPerGroup()`, `LoadSelect()` and references. Filter conditions
   added to SQL OrmLite builds on a command use `@_f0` style params so they don't clash with the command's params.
3. ✅ **Updates and deletes**: filters on every update and delete API, incl. `Save`, `Upsert`, `UpdateFrom` and the
   returning APIs, the legacy APIs, and `EnsureFilter<T>()` with a function. Params of filter conditions are skipped when an object's
   values are set on a command's params.
4. ✅ **Write rules**: `EnsureWrites`, `OnInsert` and `OnUpdate` for object writes, expression writes and upserts, then
   `BulkInsert` and `InsertIntoSelect`.
5. ✅ **`WithoutFilters()`** and the reference docs: a new page leading with the `GetDbConnection()` pattern, multi-tenancy
   and auditing examples, the table of covered APIs and the raw SQL caveat, plus release notes.

## Docs

In `/home/mythz/src/ServiceStack/docs.servicestack.net/MyApp`:

- Reference docs: `_pages/ormlite/connection-filters.md`, linked from `_pages/ormlite/sidebar.json`, `ensure-apis.md`,
  `filters.md` and `upsert.md`.
- Release notes: "Multi-tenancy, soft deletes and auditing with connection filters" in `_pages/releases/v10_04.md`.
- The SQL in their `<generated-sql>` examples was captured from running the examples on SQLite. Update both when
  behaviour changes.

## Decisions

- `Update(obj)` of a row excluded by a filter returns 0 instead of throwing, apps check the row count.
- `EnsureFilter<T>()` has an overload with a function called for each statement, for filters that change during a
  connection's lifetime.
- Legacy APIs (`Legacy/`), e.g. `SelectFmt()`, are filtered where they have a table type. They're all marked
  `[Obsolete]`. The ones taking complete SQL or a table name aren't filtered, like other raw SQL.
