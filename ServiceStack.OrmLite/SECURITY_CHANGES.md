# Security Changes & Remediation Reference (`ServiceStack.OrmLite`)

This document details security vulnerabilities identified and remediated across `ServiceStack.OrmLite`.

---

## 1. SQL Injection Filter Bypass via Unclosed Quotes in `StripQuotedStrings` (`OrmLiteUtils`)
- **Severity**: High
- **Description**:
  - `isUnsafeSql` sanitizes SQL fragments before regex checking by calling `StripQuotedStrings` for single quotes `'`, double quotes `"`, and backticks `` ` ``.
  - `StripQuotedStrings` previously toggled `inQuotes = !inQuotes` upon encountering each quote delimiter. If an input fragment contained an unmatched single quote (e.g. `'; DROP TABLE Users; --`), `inQuotes` remained `true` until EOF. Everything after the initial `'` was stripped out, yielding an empty string `""` as the fragment to verify.
  - Because `""` passed `verifySql.Match()`, `isUnsafeSql` returned `false`, and `SqlVerifyFragment()` permitted the malicious injection payload to pass directly into query construction (e.g., `q.OrderBy(userInput)`).
- **Change**:
  - Updated `StripQuotedStrings` to output an `out bool inQuotes` flag and correctly account for escaped quotes (`''` and `""`).
  - Hardened `isUnsafeSql` and `SqlVerifyFragment` to treat fragments with unclosed quote delimiters as unsafe, immediately throwing an `ArgumentException`.

---

## 2. Regular Expression Denial of Service (ReDoS) in SQL Validation Regexes (`OrmLiteUtils`)
- **Severity**: Medium
- **Description**:
  - `VerifyFragmentRegEx` and `VerifySqlRegEx` used patterns with overlapping/nested repetitions `([^\\w]|^)+` without an explicit regex `matchTimeout`.
  - Malicious inputs containing repeated sequences of spaces or non-word characters could trigger catastrophic backtracking, causing thread starvation and high CPU utilization.
- **Change**:
  - Added `DefaultRegexTimeout = TimeSpan.FromSeconds(1)` to both `VerifyFragmentRegEx` and `VerifySqlRegEx`.
  - Handled `RegexMatchTimeoutException` in `isUnsafeSql` to safely fail closed (treating timed-out evaluations as unsafe).

---

## 3. Quoted Identifier Breakout in `GetQuotedName` across Dialect Providers (`OrmLiteDialectProviderBase`, `PostgreSQLDialectProvider`)
- **Severity**: Medium
- **Description**:
  - `GetQuotedName(name)` previously prepended and appended `QuoteChar` without escaping internal quote characters (e.g., `"` -> `""` in ANSI SQL / PostgreSQL / SQLite / Oracle, or `` ` `` -> `` `` `` in MySQL).
  - Any identifier containing a quote character (e.g. `table"; DROP TABLE Users; --`) could prematurely close the quoted identifier and execute arbitrary SQL.
- **Change**:
  - Hardened `GetQuotedName` in `OrmLiteDialectProviderBase` to escape embedded `QuoteChar` by doubling it.
  - Hardened `PostgreSQLDialectProvider.GetQuotedName` to quote and escape each segment individually when encountering compound identifiers (e.g. `schema.table`).

---

## 4. Unquoted / Improperly Quoted Schema Statements (`PostgreSQLDialectProvider`, `OracleOrmLiteDialectProvider`, `FirebirdOrmLiteDialectProvider`, `SqlServerOrmLiteDialectProvider`)
- **Severity**: Medium
- **Description**:
  - In `PostgreSQLDialectProvider`, `OracleOrmLiteDialectProvider`, and `FirebirdOrmLiteDialectProvider`, `ToCreateSchemaStatement` generated `CREATE SCHEMA {schemaName}` without quoting the schema name, causing syntax errors and injection vulnerabilities on schemas containing hyphens, spaces, or mixed case.
  - In `OracleOrmLiteDialectProvider` and `FirebirdOrmLiteDialectProvider`, `DoesSchemaExist` used `.Quoted()` (wrapping the schema name in double quotes `"`), which treated the schema name as an identifier column name rather than comparing against a string literal.
  - In `SqlServerOrmLiteDialectProvider`, `ToCreateSchemaStatement` used `[{schemaName}]` without escaping `]`.
- **Change**:
  - Standardized `ToCreateSchemaStatement` to quote schema identifiers using `GetQuotedName(...)` (and `]]` escaping for SQL Server bracketed identifiers).
  - Updated `DoesSchemaExist` to compare single-quoted string literals sanitized with `.SqlParam()`.

---

## 5. Unescaped Table & Column Literals in Catalog Queries (`FirebirdOrmLiteDialectProvider`, `PostgreSQLDialectProvider`)
- **Severity**: Medium
- **Description**:
  - In `FirebirdOrmLiteDialectProvider`, `DoesTableExist` interpolated `'{tableName}'` into raw SQL without `.SqlParam()` escaping.
  - In `FirebirdOrmLiteDialectProvider.ToChangeColumnNameStatement`, `QuoteTable` was erroneously invoked twice (`QuoteTable(QuoteTable(tableRef))`), producing corrupt table identifiers (`""TABLE""`).
  - In `PostgreSQLDialectProvider.ToResetSequenceStatement`, `useTable` and `useColumn` were interpolated into single-quoted string arguments in `pg_get_serial_sequence` without `.SqlParam()` escaping.
- **Change**:
  - Added `.SqlParam()` escaping to table and column literals in catalog queries across Firebird and PostgreSQL dialect providers.
  - Corrected `ToChangeColumnNameStatement` in Firebird to invoke `QuoteTable` once.

---

## 6. SavePoint Identifier Verification (`SavePoint`)
- **Severity**: Low
- **Description**:
  - Savepoint names in `SavePoint` were interpolated directly into `$"SAVEPOINT {name}"` and `$"SAVE TRANSACTION {name}"` without quotation or verification.
- **Change**:
  - Validated `name` with `SqlVerifyFragment()` upon `SavePoint` construction.

---

## 7. Base `ByteArrayConverter` Missing Hex Formatting (`ByteArrayConverter`)
- **Severity**: Low
- **Description**:
  - Base `ByteArrayConverter` did not override `ToQuotedString`, causing it to fall back to `value.ToString()` which output `'System.Byte[]'`.
- **Change**:
  - Implemented `ToQuotedString` in `ByteArrayConverter` to format byte arrays as standard hex literals (`0x...`).
