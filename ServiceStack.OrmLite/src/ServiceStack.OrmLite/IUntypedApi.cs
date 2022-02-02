using System;
using System.Collections;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite
{
    public interface IUntypedApi
    {
        IDbConnection Db { get; set; }
        IDbCommand DbCmd { get; set; }

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

        int DeleteAll();
        int Delete(object obj, object anonType);
        int DeleteNonDefaults(object obj, object filter);
        int DeleteById(object id);
        int DeleteByIds(IEnumerable idValues);
        IEnumerable Cast(IEnumerable results);
    }
}