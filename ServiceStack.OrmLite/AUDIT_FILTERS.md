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

### Declaring filters and rules

Filters and rules are declared once in a `FilterSet`, with the type of the scope they read their values from, then
used by each connection with the scope's value:

```csharp
public record TenantUser(int TenantId, string UserId);

public static readonly FilterSet<TenantUser> UserRules = FilterSet.Create<TenantUser>(f => {
    // Rows have the value: reads, updates and deletes only see rows with it, inserts set it when unset,
    // and writing a different value throws
    f.Ensure<IHasTenantId>(x => x.TenantId, s => s.TenantId);

    // Write rules: always set the column on inserts / updates. OnWrite is both
    f.OnInsert<IAudit>(x => x.CreatedBy, s => s.UserId);
    f.OnInsert<IAudit>(x => x.CreatedDate, _ => DateTime.UtcNow);
    f.OnWrite<IAudit>(x => x.ModifiedBy, s => s.UserId);
    f.OnWrite<IAudit>(x => x.ModifiedDate, _ => DateTime.UtcNow);
});

// Filters without a write rule: reads, updates and deletes only see matching rows
public static readonly FilterSet SoftDeletes = FilterSet.Create(f => f.Filter<Order>(x => !x.IsDeleted));

db.UseFilters(UserRules.For(new TenantUser(tenantId, userId)));
db.UseFilters(SoftDeletes);

// The same connection and transaction without any filters or rules, e.g. for admin tasks
var adminDb = db.WithoutFilters();
```

- The type argument is a table type, an interface or a base class. Interface filters and rules apply to every table
  implementing the interface, with the expression rebound to the table's property of the same name, e.g.
  `IHasTenantId.TenantId` to `Order.TenantId`, so column aliases and naming strategies apply.
- A connection can use multiple sets, whose filters and rules combine, e.g. a tenant set and a soft delete set.
- Values are read from the scope each time they're used: for each statement, or each row for object writes. Values
  in filter conditions are sent as params. A condition that only reads the scope decides if the rest of the filter
  applies, e.g. `(x, s) => s.IsAdmin || x.TenantId == s.TenantId`.
- Filter conditions and `Ensure` values can only read values from the scope: a variable captured from outside the
  rule throws an `ArgumentException` when the set is created, so a set is the same for every connection using it.
- `set.For(scope)` pairs a set with its scope, so a scope of the wrong type doesn't compile, with an error naming it.
- Using a set on a connection that isn't an `OrmLiteConnection` throws, so a filter is never silently ignored.
- `Ensure` combines the filter and the write rule of a column, so a tenant column can't be filtered without also
  being enforced on writes.

### Using a set more than once

A connection can be configured more than once, e.g. a shared SQLite `:memory:` connection that's opened multiple times
in a request, so sets are compared with the ones a connection already uses:

| Used again | Result |
|-|-|
| The same set with the same scope, the same object or an equal one, e.g. a `record` | Ignored |
| The same set with a different scope, e.g. another tenant | Throws `InvalidOperationException`, as rows would need to match both |

### App-defined openers

Apps wrap their sets in an extension method so connections are configured the same way:

```csharp
public static IDbConnection OpenForTenant(this IDbConnectionFactory dbFactory, int tenantId, string userId) =>
    dbFactory.OpenDbConnection().UseFilters(UserRules.For(new TenantUser(tenantId, userId)));
```

### ServiceStack integration

ServiceStack opens connections for requests with `AppHost.GetDbConnection(IRequest)`, used by `Service.Db`, AutoQuery,
AutoCrud and other features. `DbConnectionRequestFilters` apply the sets everywhere the framework opens a connection
for a request, which is the recommended approach in ServiceStack apps and leads the docs:

```csharp
DbConnectionRequestFilters.Add((db, req) => {
    if (req.GetSession() is { IsAuthenticated: true } session)
        db.UseFilters(UserRules.For(new TenantUser(session.GetTenantId(), session.UserAuthId)));
});
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
| `Ensure` | Every insert and update API above |
| Raw SQL, e.g. `ExecuteSql`, and the legacy `UpdateFmt()` | **Not applied** |

### Which rule to use

| | `Ensure` | `OnInsert` / `OnUpdate` |
|-|-|-|
| Use for | A column that's the same for everything the connection writes, e.g. `TenantId` | A column recording an insert or update, e.g. `CreatedBy`, `ModifiedDate` |
| Applies to | Inserts and updates | Only inserts, or only updates |
| App sets a different value | Throws, as it's a bug | Replaced with the rule's value |
| App doesn't set a value | Inserts set it, updates leave the column alone | Always set |

Rule of thumb: `Ensure` for who owns the row, `OnInsert` / `OnUpdate` for who changed it and when. The docs should
lead the write rules section with this table.

`OnWrite` declares both an `OnInsert` and an `OnUpdate` rule for a column. With only `OnUpdate`, a row's `ModifiedBy`
and `ModifiedDate` are empty until its first update. The docs example should use `OnInsert` for the created columns,
with `[IgnoreOnUpdate]`, and `OnWrite` for the modified columns.

`Ensure`'s write rule depends on what's written. A column isn't set when it's `null`, its type's default value or an empty
string, e.g. a property initialized with `string TenantId { get; set; } = ""`:

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
  `UpdateFilter` which run first, so audit columns can be trusted, while `Ensure` throws instead of silently changing
  the value.
- **`OnInsert` columns aren't protected from updates**: updating all an object's fields, e.g. `db.Update(obj)`, also
  writes its `CreatedBy`. Add `[IgnoreOnUpdate]` to created columns so they're only set when the row is inserted,
  which the docs should recommend.
- `Ensure` guards both the rows that are changed and the values that are written. Multi-tenant apps need both:
  without the filter, an update could move another tenant's row into the connection's tenant, and without the write
  rule an insert could be written for another tenant. `Filter` is only for conditions that don't decide what's
  written, e.g. soft deletes.

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

- **Storage**: `OrmLiteConnection.Filters`, an immutable list of the sets a connection uses with their scopes, and the
  filters and rules they bind to the scope, replaced each time a set is used, with a per-connection cache from table
  type to its matching filters and rules. Commands reach it through `OrmLiteCommand.OrmLiteConnection`.
- **Filter SQL** (`FilterTemplate.cs`): each rule has a template for each table type. Parts of its condition that
  don't read the row are evaluated for each statement: `bool` conditions of `&&`, `||`, `!` and `?:` that only read
  the scope are decided in C# with short-circuiting, and other values become db params. The SQL of each combination
  of decided conditions, dialect, table alias and null values is generated once, with the values as the arguments of
  a compiled query (`CompiledQueryBuild`), and kept when it's the same as the filter normally translated with the
  scope's values. Each statement reads the values and adds them as db params renamed for its query. Filters whose
  SQL can't be kept are translated with the scope's values for each statement, see `FilterSet.NotCachedReasons`.
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
6. ✅ **`FilterSet`**: filters and rules declared once with a typed scope, replacing registering them on each
   connection with `EnsureFilter`, `EnsureWrites`, `OnInsert`, `OnUpdate` and `OnWrite` before they shipped. `Ensure`
   combines a column's filter and write rule.
7. ✅ **Filter SQL translated once**: each filter's SQL is reused by every connection that uses its set, with
   conditions that only read the scope decided in C#, e.g. `s.WorkspaceId == null || x.RefIdStr == s.WorkspaceId`
   has no SQL for the null case. `FilterSetBenchmark` measures the time filters add to each statement.

## Docs

In `/home/mythz/src/ServiceStack/docs.servicestack.net/MyApp`:

- Reference docs: `_pages/ormlite/connection-filters.md`, linked from `_pages/ormlite/sidebar.json`, `ensure-apis.md`,
  `filters.md` and `upsert.md`.
- Release notes: "Multi-tenancy, soft deletes and auditing with connection filters" in `_pages/releases/v10_04.md`.
- The SQL in their `<generated-sql>` examples was captured from running the examples on SQLite. Update both when
  behaviour changes.

## Decisions

- `Update(obj)` of a row excluded by a filter returns 0 instead of throwing, apps check the row count.
- Filters and rules are declared once in a `FilterSet` with a typed scope, instead of being registered on each
  connection with closures: a set's shape is fixed when it's created, so it can be validated, compared and its SQL
  reused, and rules can't capture values from outside the scope. Values that change during a connection's lifetime
  are read from the scope for each statement.
- Legacy APIs (`Legacy/`), e.g. `SelectFmt()`, are filtered where they have a table type. They're all marked
  `[Obsolete]`. The ones taking complete SQL or a table name aren't filtered, like other raw SQL.
