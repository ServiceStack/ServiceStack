using System;
using System.Collections;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

public interface IUntypedApi
{
    IDbConnection Db { get; set; }
    IDbCommand DbCmd { get; set; }

    /// <summary>
    /// Every row of the table, as a List of its Type
    /// </summary>
    IList Select();
    Task<IList> SelectAsync(CancellationToken token = default);
    /// <summary>
    /// The rows matching a SQL filter, as a List of the table's Type, e.g:
    /// <para>api.Select("Age > @age", new { age = 40 })</para>
    /// </summary>
    IList Select(string sqlFilter, object anonType = null);
    Task<IList> SelectAsync(string sqlFilter, object anonType = null, CancellationToken token = default);
    /// <summary>
    /// The row with the primary key, or null if it doesn't exist
    /// </summary>
    object SingleById(object id);
    Task<object> SingleByIdAsync(object id, CancellationToken token = default);
    /// <summary>
    /// How many rows the table has
    /// </summary>
    long Count();
    Task<long> CountAsync(CancellationToken token = default);

    int SaveAll(IEnumerable objs);
    Task<int> SaveAllAsync(IEnumerable objs, CancellationToken token);
    bool Save(object obj);
    Task<bool> SaveAsync(object obj, CancellationToken token);

    void InsertAll(IEnumerable objs);
    void InsertAll(IEnumerable objs, Action<IDbCommand> commandFilter);
    long Insert(object obj, bool selectIdentity = false);
    long Insert(object obj, Action<IDbCommand> commandFilter, bool selectIdentity = false);

    int UpdateAll(IEnumerable objs);
    int UpdateAll(IEnumerable objs, Action<IDbCommand> commandFilter);
    int Update(object obj);
    Task<int> UpdateAsync(object obj, CancellationToken token);

    int DeleteAll();
    int Delete(object obj, object anonType);
    int DeleteNonDefaults(object obj, object filter);
    int DeleteById(object id);
    int DeleteByIds(IEnumerable idValues);
    IEnumerable Cast(IEnumerable results);
}