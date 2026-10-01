#nullable enable
using System;
using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite;

/// <summary>
/// The original values of an object's properties that were set by the connection's write rules, to restore them
/// if the object is rejected by a rule. Objects that are written keep the values that were saved.
/// </summary>
internal readonly struct WriteRuleValues(object obj, List<KeyValuePair<FieldDefinition, object?>> original)
{
    public void Restore()
    {
        if (original == null)
            return;
        // In reverse, so a property set by multiple rules is restored to its original value
        for (var i = original.Count - 1; i >= 0; i--)
            original[i].Key.SetValue(obj, original[i].Value);
    }
}

/// <summary>
/// Applies a connection's EnsureWrites, OnInsert and OnUpdate rules to the rows being written
/// </summary>
internal static class OrmLiteConnectionWriteRules
{
    internal static TableWriteRules? GetWriteRules(this IDbCommand dbCmd, Type tableType)
    {
        var filters = dbCmd.GetFilters();
        return filters.HasWriteRules
            ? filters.GetWriteRules(tableType)
            : null;
    }

    internal static bool HasWriteRules<T>(this IDbCommand dbCmd) => dbCmd.GetWriteRules(typeof(T)) != null;

    /// <summary>
    /// Set the values of the connection's rules on an object that's about to be inserted
    /// </summary>
    internal static WriteRuleValues SetInsertRuleValues<T>(this IDbCommand dbCmd, object? obj) =>
        SetInsertRuleValues(dbCmd.GetWriteRules(typeof(T)), obj);

    private static WriteRuleValues SetInsertRuleValues(TableWriteRules? rules, object? obj)
    {
        if (rules == null || obj == null)
            return default;

        var original = new List<KeyValuePair<FieldDefinition, object?>>();
        try
        {
            foreach (var rule in rules.EnsureWrites)
                EnsureWriteValue(rule, obj, original, setWhenDefault: true);
            foreach (var rule in rules.OnInsert)
                SetValue(rule, obj, original);
        }
        catch
        {
            new WriteRuleValues(obj, original).Restore();
            throw;
        }
        return new WriteRuleValues(obj, original);
    }

    /// <summary>
    /// Set the values of the connection's rules on an object that's about to be updated
    /// </summary>
    /// <param name="updateFields">The fields being updated, or null when all fields are</param>
    /// <param name="excludeDefaults">Whether fields with default values aren't updated</param>
    internal static WriteRuleValues SetUpdateRuleValues<T>(this IDbCommand dbCmd, object? obj,
        ICollection<string>? updateFields = null, bool excludeDefaults = false)
    {
        var rules = dbCmd.GetWriteRules(typeof(T));
        if (rules == null || obj == null)
            return default;

        var original = new List<KeyValuePair<FieldDefinition, object?>>();
        try
        {
            foreach (var rule in rules.EnsureWrites)
            {
                if (updateFields == null || updateFields.Count == 0 || Contains(updateFields, rule.Field))
                    EnsureWriteValue(rule, obj, original, setWhenDefault: !excludeDefaults);
            }
            foreach (var rule in rules.OnUpdate)
                SetValue(rule, obj, original);
        }
        catch
        {
            new WriteRuleValues(obj, original).Restore();
            throw;
        }
        return new WriteRuleValues(obj, original);
    }

    /// <summary>
    /// The fields to insert or update, including the fields set by the connection's rules
    /// </summary>
    internal static ICollection<string> WithRuleFields<T>(this IDbCommand dbCmd, ICollection<string> fields, bool forInsert) =>
        WithRuleFields(dbCmd.GetWriteRules(typeof(T)), fields, forInsert);

    private static ICollection<string> WithRuleFields(TableWriteRules? rules, ICollection<string>? fields, bool forInsert)
    {
        if (rules == null || fields == null || fields.Count == 0)
            return fields!;

        var to = new List<string>(fields);
        if (forInsert)
        {
            foreach (var rule in rules.EnsureWrites)
                AddField(to, rule.Field);
        }
        foreach (var rule in forInsert ? rules.OnInsert : rules.OnUpdate)
            AddField(to, rule.Field);
        return to;
    }

    /// <summary>
    /// The values to insert with the values of the connection's rules, in a new dictionary if any apply
    /// </summary>
    internal static Dictionary<string, object> WithInsertRuleValues<T>(this IDbCommand dbCmd, Dictionary<string, object> values)
    {
        var rules = dbCmd.GetWriteRules(typeof(T));
        if (rules == null || values == null)
            return values!;

        var to = new Dictionary<string, object>(values, values.Comparer);
        foreach (var rule in rules.EnsureWrites)
        {
            var expected = rule.GetValue();
            var key = FindKey(to, rule.Field);
            if (key != null && !rule.IsDefault(to[key]))
                AssertValue(rule, to[key], expected);
            else
                Set(to, key, rule.Field, expected);
        }
        foreach (var rule in rules.OnInsert)
            Set(to, FindKey(to, rule.Field), rule.Field, rule.GetValue());
        return to;
    }

    /// <summary>
    /// The values to update with the values of the connection's rules, in a new dictionary if any apply
    /// </summary>
    internal static Dictionary<string, object> WithUpdateRuleValues<T>(this IDbCommand dbCmd, Dictionary<string, object> values)
    {
        var rules = dbCmd.GetWriteRules(typeof(T));
        if (rules == null || values == null)
            return values!;

        var to = new Dictionary<string, object>(values, values.Comparer);
        foreach (var rule in rules.EnsureWrites)
        {
            // Only the columns in the dictionary are updated
            var key = FindKey(to, rule.Field);
            if (key != null)
                AssertValue(rule, to[key], rule.GetValue());
        }
        foreach (var rule in rules.OnUpdate)
            Set(to, FindKey(to, rule.Field), rule.Field, rule.GetValue());
        return to;
    }

    /// <summary>
    /// The values of an anonymous object or dictionary to update, keyed by field name, with the values of the
    /// connection's rules, or the same object if no rules apply
    /// </summary>
    internal static object WithUpdateRuleValues<T>(this IDbCommand dbCmd, object updateOnly)
    {
        if (updateOnly == null || dbCmd.GetWriteRules(typeof(T)) == null)
            return updateOnly!;

        var modelDef = typeof(T).GetModelDefinition();
        var values = new Dictionary<string, object>();
        void Add(string name, object value)
        {
            var fieldDef = modelDef.GetFieldDefinition(name)
                ?? Array.Find(modelDef.FieldDefinitionsArray, x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            values[fieldDef?.Name ?? name] = value;
        }

        if (updateOnly is System.Collections.IDictionary d)
        {
            foreach (System.Collections.DictionaryEntry entry in d)
                Add((string)entry.Key, entry.Value!);
        }
        else
        {
            foreach (var entry in updateOnly.ToObjectDictionary())
                Add(entry.Key, entry.Value);
        }
        return dbCmd.WithUpdateRuleValues<T>(values);
    }

    /// <summary>
    /// UPDATE statement that adds to numeric columns, where the values of the connection's OnUpdate rules are set
    /// instead of added
    /// </summary>
    internal static void PrepareUpdateRowAddStatement<T>(this IDbCommand dbCmd, Dictionary<string, object> values, string whereExpression)
    {
        var dialect = dbCmd.GetDialectProvider();
        var rules = dbCmd.GetWriteRules(typeof(T));
        if (rules == null)
        {
            dialect.PrepareUpdateRowAddStatement<T>(dbCmd, values, whereExpression);
            return;
        }

        var addValues = dbCmd.WithUpdateRuleValues<T>(values);
        var setValues = new Dictionary<string, object>();
        foreach (var rule in rules.OnUpdate)
        {
            if (addValues.TryGetValue(rule.Field.Name, out var value))
            {
                setValues[rule.Field.Name] = value;
                addValues.Remove(rule.Field.Name);
            }
        }
        if (setValues.Count == 0)
        {
            dialect.PrepareUpdateRowAddStatement<T>(dbCmd, addValues, whereExpression);
            return;
        }
        if (addValues.Count == 0)
        {
            dialect.PrepareUpdateRowStatement<T>(dbCmd, setValues, whereExpression);
            return;
        }

        dialect.PrepareUpdateRowAddStatement<T>(dbCmd, addValues, sqlFilter: null);
        var sql = new System.Text.StringBuilder(dbCmd.CommandText.TrimEnd());
        var modelDef = typeof(T).GetModelDefinition();
        foreach (var entry in setValues)
        {
            var fieldDef = modelDef.AssertFieldDefinition(entry.Key);
            sql.Append(", ").Append(dialect.GetQuotedColumnName(fieldDef)).Append('=')
                .Append(dialect.GetUpdateParam(dbCmd, entry.Value, fieldDef));
        }
        if (!string.IsNullOrEmpty(whereExpression))
            sql.Append(' ').Append(whereExpression);
        dbCmd.CommandText = sql.ToString();
    }

    /// <summary>
    /// Set the values of the connection's rules on the rows of a bulk insert, returning their original values,
    /// or null if no rules apply
    /// </summary>
    internal static List<WriteRuleValues>? SetBulkInsertRuleValues<T>(this IDbConnection db,
        ref IEnumerable<T> objs, ref BulkInsertConfig? config)
    {
        var filters = db.GetFilters();
        var rules = filters.HasWriteRules ? filters.GetWriteRules(typeof(T)) : null;
        if (rules == null || rules.EnsureWrites.Length + rules.OnInsert.Length == 0 || objs == null)
            return null;

        var rows = new List<T>(objs);
        objs = rows;
        if (config?.InsertFields != null)
        {
            config = new BulkInsertConfig {
                BatchSize = config.BatchSize,
                Mode = config.Mode,
                InsertFields = WithRuleFields(rules, config.InsertFields, forInsert: true),
            };
        }

        var ruleValues = new List<WriteRuleValues>(rows.Count);
        try
        {
            foreach (var row in rows)
                ruleValues.Add(SetInsertRuleValues(rules, row));
        }
        catch
        {
            ruleValues.Restore();
            throw;
        }
        return ruleValues;
    }

    internal static void Restore(this List<WriteRuleValues>? ruleValues)
    {
        if (ruleValues == null)
            return;
        foreach (var values in ruleValues)
            values.Restore();
    }

    /// <summary>
    /// Set the params of an INSERT from the object, with the values of the connection's rules
    /// </summary>
    internal static void SetInsertParameterValues<T>(this IDbCommand dbCmd, object obj)
    {
        dbCmd.SetInsertRuleValues<T>(obj);
        dbCmd.GetDialectProvider().SetParameterValues<T>(dbCmd, obj);
    }

    /// <summary>
    /// Adds the values of the connection's rules to an INSERT INTO ... SELECT, by selecting them with the rows of
    /// the query, returning the columns to add to the INSERT's column list
    /// </summary>
    internal static string AddInsertIntoSelectRuleValues<T>(this IDbCommand dbCmd, ICollection<string> selectFields, ref string selectSql)
    {
        var rules = dbCmd.GetWriteRules(typeof(T));
        if (rules == null || rules.EnsureWrites.Length + rules.OnInsert.Length == 0)
            return "";

        var dialect = dbCmd.GetDialectProvider();

        var ruleValues = new Dictionary<string, KeyValuePair<FieldDefinition, object?>>();
        foreach (var rule in rules.EnsureWrites)
            ruleValues[rule.Field.Name] = new(rule.Field, rule.GetValue());
        foreach (var rule in rules.OnInsert)
            ruleValues[rule.Field.Name] = new(rule.Field, rule.GetValue());

        // The selected columns or their aliases, e.g. "Table"."Column" or "Alias"
        var selectColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var selectField in selectFields)
            selectColumns.Add(selectField.LastRightPart('.').StripDbQuotes());

        var columns = new System.Text.StringBuilder();
        var values = new System.Text.StringBuilder();
        foreach (var entry in ruleValues.Values)
        {
            var fieldDef = entry.Key;
            if (selectColumns.Contains(fieldDef.Name) || selectColumns.Contains(fieldDef.FieldName)
                || selectColumns.Contains(dialect.NamingStrategy.GetColumnName(fieldDef.FieldName)))
                throw new NotSupportedException(
                    $"InsertIntoSelect() can't select '{fieldDef.Name}', which is set by a rule of the connection");

            columns.Append(',').Append(dialect.GetQuotedColumnName(fieldDef));
            values.Append(", ").Append(dialect.AddQueryParam(dbCmd, entry.Value, fieldDef).ParameterName);
        }

        var rows = dialect.GetQuotedName("_s");
        selectSql = $"SELECT {rows}.*{values} FROM ({selectSql}) {rows}";
        return columns.ToString();
    }

    private static void EnsureWriteValue(WriteRule rule, object obj, List<KeyValuePair<FieldDefinition, object?>> original,
        bool setWhenDefault)
    {
        var expected = rule.GetValue();
        var value = rule.Field.GetValue(obj);
        if (!rule.IsDefault(value))
        {
            AssertValue(rule, value, expected);
        }
        else if (setWhenDefault)
        {
            original.Add(new(rule.Field, value));
            rule.Field.SetValue(obj, expected);
        }
    }

    private static void SetValue(WriteRule rule, object obj, List<KeyValuePair<FieldDefinition, object?>> original)
    {
        original.Add(new(rule.Field, rule.Field.GetValue(obj)));
        rule.Field.SetValue(obj, rule.GetValue());
    }

    internal static void AssertValue(WriteRule rule, object? value, object? expected)
    {
        if (!Equals(rule.ToFieldValue(value), expected))
            throw new InvalidOperationException(
                $"{rule.Field.PropertyInfo?.DeclaringType?.Name}.{rule.Field.Name} must be '{expected}' on this connection");
    }

    private static bool Contains(ICollection<string> fields, FieldDefinition fieldDef)
    {
        foreach (var field in fields)
        {
            if (string.Equals(field, fieldDef.Name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void AddField(List<string> fields, FieldDefinition fieldDef)
    {
        if (!Contains(fields, fieldDef))
            fields.Add(fieldDef.Name);
    }

    private static string? FindKey(Dictionary<string, object> values, FieldDefinition fieldDef)
    {
        if (values.ContainsKey(fieldDef.Name))
            return fieldDef.Name;
        foreach (var key in values.Keys)
        {
            if (string.Equals(key, fieldDef.Name, StringComparison.OrdinalIgnoreCase)
                || (fieldDef.Alias != null && string.Equals(key, fieldDef.Alias, StringComparison.OrdinalIgnoreCase)))
                return key;
        }
        return null;
    }

    private static void Set(Dictionary<string, object> values, string? existingKey, FieldDefinition fieldDef, object? value)
    {
        if (existingKey != null && existingKey != fieldDef.Name)
            values.Remove(existingKey);
        values[fieldDef.Name] = value!;
    }
}
