//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ServiceStack.OrmLite;

public interface IOrmLiteResultsFilter
{
    long GetLastInsertId(IDbCommand dbCmd);

    List<T> GetList<T>(IDbCommand dbCmd);

    IList GetRefList(IDbCommand dbCmd, Type refType);

    T GetSingle<T>(IDbCommand dbCmd);

    object GetRefSingle(IDbCommand dbCmd, Type refType);

    T GetScalar<T>(IDbCommand dbCmd);

    object GetScalar(IDbCommand dbCmd);

    long GetLongScalar(IDbCommand dbCmd);

    List<T> GetColumn<T>(IDbCommand dbCmd);

    HashSet<T> GetColumnDistinct<T>(IDbCommand dbCmd);

    Dictionary<K, V> GetDictionary<K, V>(IDbCommand dbCmd);
        
    List<KeyValuePair<K, V>> GetKeyValuePairs<K, V>(IDbCommand dbCmd);

    Dictionary<K, List<V>> GetLookup<K, V>(IDbCommand dbCmd);

    int ExecuteSql(IDbCommand dbCmd);
}

public class OrmLiteResultsFilter : IOrmLiteResultsFilter, IDisposable
{
    public IEnumerable Results { get; set; }
    public IEnumerable RefResults { get; set; }
    public IEnumerable ColumnResults { get; set; }
    public IEnumerable ColumnDistinctResults { get; set; }
    public IDictionary DictionaryResults { get; set; }
    public IDictionary LookupResults { get; set; }
    public object SingleResult { get; set; }
    public object RefSingleResult { get; set; }
    public object ScalarResult { get; set; }
    public long LongScalarResult { get; set; }
    public long LastInsertId { get; set; }
    public int ExecuteSqlResult { get; set; }

    public Func<IDbCommand, int> ExecuteSqlFn { get; set; }
    public Func<IDbCommand, Type, IEnumerable> ResultsFn { get; set; }
    public Func<IDbCommand, Type, IEnumerable> RefResultsFn { get; set; }
    public Func<IDbCommand, Type, IEnumerable> ColumnResultsFn { get; set; }
    public Func<IDbCommand, Type, IEnumerable> ColumnDistinctResultsFn { get; set; }
    public Func<IDbCommand, Type, Type, IDictionary> DictionaryResultsFn { get; set; }
    public Func<IDbCommand, Type, Type, IDictionary> LookupResultsFn { get; set; }
    public Func<IDbCommand, Type, object> SingleResultFn { get; set; }
    public Func<IDbCommand, Type, object> RefSingleResultFn { get; set; }
    public Func<IDbCommand, Type, object> ScalarResultFn { get; set; }
    public Func<IDbCommand, long> LongScalarResultFn { get; set; }
    public Func<IDbCommand, long> LastInsertIdFn { get; set; }

    public Action<string> SqlFilter { get; set; }
    public Action<IDbCommand> SqlCommandFilter { get; set; }

    public bool PrintSql { get; set; }

    private readonly IOrmLiteResultsFilter previousFilter;

    public OrmLiteResultsFilter(IEnumerable results = null)
    {
        this.Results = results ?? new object[] { };

        previousFilter = OrmLiteConfig.ResultsFilter;
        OrmLiteConfig.ResultsFilter = this;
    }

    private void Filter(IDbCommand dbCmd)
    {
        SqlFilter?.Invoke(dbCmd.CommandText);

        SqlCommandFilter?.Invoke(dbCmd);

        if (PrintSql)
        {
            Console.WriteLine(dbCmd.CommandText);
        }
    }

    private IEnumerable GetResults<T>(IDbCommand dbCmd)
    {
        return ResultsFn != null ? ResultsFn(dbCmd, typeof(T)) : Results;
    }

    private IEnumerable GetRefResults(IDbCommand dbCmd, Type refType)
    {
        return RefResultsFn != null ? RefResultsFn(dbCmd, refType) : RefResults;
    }

    private IEnumerable GetColumnResults<T>(IDbCommand dbCmd)
    {
        return ColumnResultsFn != null ? ColumnResultsFn(dbCmd, typeof(T)) : ColumnResults;
    }

    private IEnumerable GetColumnDistinctResults<T>(IDbCommand dbCmd)
    {
        return ColumnDistinctResultsFn != null ? ColumnDistinctResultsFn(dbCmd, typeof(T)) : ColumnDistinctResults;
    }

    private IDictionary GetDictionaryResults<K, V>(IDbCommand dbCmd)
    {
        return DictionaryResultsFn != null ? DictionaryResultsFn(dbCmd, typeof(K), typeof(V)) : DictionaryResults;
    }

    private IDictionary GetLookupResults<K, V>(IDbCommand dbCmd)
    {
        return LookupResultsFn != null ? LookupResultsFn(dbCmd, typeof(K), typeof(V)) : LookupResults;
    }

    private object GetSingleResult<T>(IDbCommand dbCmd)
    {
        return SingleResultFn != null ? SingleResultFn(dbCmd, typeof(T)) : SingleResult;
    }

    private object GetRefSingleResult(IDbCommand dbCmd, Type refType)
    {
        return RefSingleResultFn != null ? RefSingleResultFn(dbCmd, refType) : RefSingleResult;
    }

    private object GetScalarResult<T>(IDbCommand dbCmd)
    {
        return ScalarResultFn != null ? ScalarResultFn(dbCmd, typeof(T)) : ScalarResult;
    }

    private long GetLongScalarResult(IDbCommand dbCmd)
    {
        return LongScalarResultFn?.Invoke(dbCmd) ?? LongScalarResult;
    }

    public long GetLastInsertId(IDbCommand dbCmd)
    {
        return LastInsertIdFn?.Invoke(dbCmd) ?? LastInsertId;
    }

    public List<T> GetList<T>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return (from object result in GetResults<T>(dbCmd) select (T)result).ToList();
    }

    public IList GetRefList(IDbCommand dbCmd, Type refType)
    {
        Filter(dbCmd);
        var list = (IList)typeof(List<>).GetCachedGenericType(refType).CreateInstance();
        foreach (object result in GetRefResults(dbCmd, refType).Safe())
        {
            list.Add(result);
        }
        return list;
    }

    public T GetSingle<T>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        if (SingleResult != null || SingleResultFn != null)
            return (T)GetSingleResult<T>(dbCmd);

        foreach (var result in GetResults<T>(dbCmd))
        {
            return (T)result;
        }
        return default(T);
    }

    public object GetRefSingle(IDbCommand dbCmd, Type refType)
    {
        Filter(dbCmd);
        if (RefSingleResult != null || RefSingleResultFn != null)
            return GetRefSingleResult(dbCmd, refType);

        foreach (var result in GetRefResults(dbCmd, refType).Safe())
        {
            return result;
        }
        return null;
    }

    public T GetScalar<T>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return ConvertTo<T>(GetScalarResult<T>(dbCmd));
    }

    public long GetLongScalar(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return GetLongScalarResult(dbCmd);
    }

    private T ConvertTo<T>(object value)
    {
        if (value == null)
            return default(T);

        if (value is T)
            return (T)value;

        var typeCode = typeof(T).GetUnderlyingTypeCode();
        var strValue = value.ToString();
        switch (typeCode)
        {
            case TypeCode.Boolean:
                return (T)(object)Convert.ToBoolean(strValue);
            case TypeCode.Byte:
                return (T)(object)Convert.ToByte(strValue);
            case TypeCode.Int16:
                return (T)(object)Convert.ToInt16(strValue);
            case TypeCode.Int32:
                return (T)(object)Convert.ToInt32(strValue);
            case TypeCode.Int64:
                return (T)(object)Convert.ToInt64(strValue);
            case TypeCode.Single:
                return (T)(object)Convert.ToSingle(strValue);
            case TypeCode.Double:
                return (T)(object)Convert.ToDouble(strValue);
            case TypeCode.Decimal:
                return (T)(object)Convert.ToDecimal(strValue);
        }

        return (T)value;
    }

    public object GetScalar(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return GetScalarResult<object>(dbCmd) ?? GetResults<object>(dbCmd).Cast<object>().FirstOrDefault();
    }

    public List<T> GetColumn<T>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return (from object result in GetColumnResults<T>(dbCmd).Safe() select (T)result).ToList();
    }

    public HashSet<T> GetColumnDistinct<T>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        var results = GetColumnDistinctResults<T>(dbCmd) ?? GetColumnResults<T>(dbCmd);
        return (from object result in results select (T)result).ToSet();
    }

    public Dictionary<K, V> GetDictionary<K, V>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        var to = new Dictionary<K, V>();
        var map = GetDictionaryResults<K, V>(dbCmd);
        if (map == null)
            return to;

        foreach (DictionaryEntry entry in map)
        {
            to.Add((K)entry.Key, (V)entry.Value);
        }

        return to;
    }

    public List<KeyValuePair<K, V>> GetKeyValuePairs<K, V>(IDbCommand dbCmd) => GetDictionary<K, V>(dbCmd).ToList();

    public Dictionary<K, List<V>> GetLookup<K, V>(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        var to = new Dictionary<K, List<V>>();
        var map = GetLookupResults<K, V>(dbCmd);
        if (map == null)
            return to;

        foreach (DictionaryEntry entry in map)
        {
            var key = (K)entry.Key;

            if (!to.TryGetValue(key, out var list))
            {
                to[key] = list = new List<V>();
            }

            list.AddRange(from object item in (IEnumerable)entry.Value select (V)item);
        }

        return to;
    }

    public int ExecuteSql(IDbCommand dbCmd)
    {
        Filter(dbCmd);
        return ExecuteSqlFn?.Invoke(dbCmd)
               ?? ExecuteSqlResult;
    }

    public void Dispose()
    {
        OrmLiteConfig.ResultsFilter = previousFilter;
    }
}

public class CaptureSqlFilter : OrmLiteResultsFilter
{
    public CaptureSqlFilter()
    {
        SqlCommandFilter = CaptureSqlCommand;
        SqlCommandHistory = new List<SqlCommandDetails>();
    }

    private void CaptureSqlCommand(IDbCommand command)
    {
        SqlCommandHistory.Add(new SqlCommandDetails(command));
    }

    public List<SqlCommandDetails> SqlCommandHistory { get; set; }

    public List<string> SqlStatements
    {
        get { return SqlCommandHistory.Map(x => x.Sql); }
    }
}

public class SqlCommandDetails
{
    public SqlCommandDetails(IDbCommand command)
    {
        if (command == null)
            return;

        Sql = command.CommandText;
        if (command.Parameters.Count <= 0)
            return;

        Parameters = new Dictionary<string, object>();

        foreach (IDataParameter parameter in command.Parameters)
        {
            if (!Parameters.ContainsKey(parameter.ParameterName))
                Parameters.Add(parameter.ParameterName, parameter.Value);
        }
    }

    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}