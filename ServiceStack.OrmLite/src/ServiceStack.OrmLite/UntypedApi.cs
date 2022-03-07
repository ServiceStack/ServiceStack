using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite
{
    public static class UntypedApiExtensions
    {
        static readonly ConcurrentDictionary<Type, Type> untypedApiMap =
            new ConcurrentDictionary<Type, Type>();

        public static IUntypedApi CreateTypedApi(this IDbConnection db, Type forType)
        {
            var genericType = untypedApiMap.GetOrAdd(forType, key => typeof(UntypedApi<>).GetCachedGenericType(key));
            var unTypedApi = genericType.CreateInstance<IUntypedApi>();
            unTypedApi.Db = db;
            return unTypedApi;
        }

        public static IUntypedApi CreateTypedApi(this IDbCommand dbCmd, Type forType)
        {
            var genericType = untypedApiMap.GetOrAdd(forType, key => typeof(UntypedApi<>).GetCachedGenericType(key));
            var unTypedApi = genericType.CreateInstance<IUntypedApi>();
            unTypedApi.DbCmd = dbCmd;
            return unTypedApi;
        }

        public static IUntypedApi CreateTypedApi(this Type forType)
        {
            var genericType = untypedApiMap.GetOrAdd(forType, key => typeof(UntypedApi<>).GetCachedGenericType(key));
            var unTypedApi = genericType.CreateInstance<IUntypedApi>();
            return unTypedApi;
        }
    }

    public class UntypedApi<T> : IUntypedApi
    {
        public IDbConnection Db { get; set; }
        public IDbCommand DbCmd { get; set; }

        public TReturn Exec<TReturn>(Func<IDbCommand, TReturn> filter)
        {
            return DbCmd != null ? filter(DbCmd) : Db.Exec(filter);
        }

        public Task<TReturn> Exec<TReturn>(Func<IDbCommand, Task<TReturn>> filter)
        {
            return DbCmd != null ? filter(DbCmd) : Db.Exec(filter);
        }

        public void Exec(Action<IDbCommand> filter)
        {
            if (DbCmd != null)
                filter(DbCmd);
            else
                Db.Exec(filter);
        }

        public int SaveAll(IEnumerable objs)
        {
            return Exec(dbCmd => dbCmd.SaveAll((IEnumerable<T>)objs));
        }

        public bool Save(object obj)
        {
            return Exec(dbCmd => dbCmd.Save((T)obj));
        }

#if ASYNC
        public Task<int> SaveAllAsync(IEnumerable objs, CancellationToken token)
        {
            return Exec(dbCmd => dbCmd.SaveAllAsync((IEnumerable<T>)objs, token));
        }

        public Task<bool> SaveAsync(object obj, CancellationToken token)
        {
            return Exec(dbCmd => dbCmd.SaveAsync((T)obj, token));
        }
#else
        public Task<int> SaveAllAsync(IEnumerable objs, CancellationToken token)
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }

        public Task<bool> SaveAsync(object obj, CancellationToken token)
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }
#endif

        public void InsertAll(IEnumerable objs)
        {
            Exec(dbCmd => dbCmd.InsertAll((IEnumerable<T>)objs, commandFilter:null));
        }

        public void InsertAll(IEnumerable objs, Action<IDbCommand> commandFilter)
        {
            Exec(dbCmd => dbCmd.InsertAll((IEnumerable<T>)objs, commandFilter:commandFilter));
        }

        public long Insert(object obj, bool selectIdentity = false)
        {
            return Exec(dbCmd => dbCmd.Insert((T)obj, commandFilter: null, selectIdentity: selectIdentity));
        }

        public long Insert(object obj, Action<IDbCommand> commandFilter, bool selectIdentity = false)
        {
            return Exec(dbCmd => dbCmd.Insert((T)obj, commandFilter: commandFilter, selectIdentity: selectIdentity));
        }

        public int UpdateAll(IEnumerable objs)
        {
            return Exec(dbCmd => dbCmd.UpdateAll((IEnumerable<T>)objs));
        }

        public int UpdateAll(IEnumerable objs, Action<IDbCommand> commandFilter)
        {
            return Exec(dbCmd => dbCmd.UpdateAll((IEnumerable<T>)objs, commandFilter: commandFilter));
        }

        public int Update(object obj)
        {
            return Exec(dbCmd => dbCmd.Update((T)obj));
        }

        public Task<int> UpdateAsync(object obj, CancellationToken token)
        {
            return Exec(dbCmd => dbCmd.UpdateAsync((T)obj, token));
        }

        public int Update(object obj, Action<IDbCommand> commandFilter)
        {
            return Exec(dbCmd => dbCmd.Update((T)obj, commandFilter: commandFilter));
        }

        public int DeleteAll()
        {
            return Exec(dbCmd => dbCmd.DeleteAll<T>());
        }

        public int Delete(object obj, object anonType)
        {
            return Exec(dbCmd => dbCmd.Delete<T>(anonType));
        }

        public int DeleteNonDefaults(object obj, object filter)
        {
            return Exec(dbCmd => dbCmd.DeleteNonDefaults((T)filter));
        }

        public int DeleteById(object id)
        {
            return Exec(dbCmd => dbCmd.DeleteById<T>(id));
        }

        public int DeleteByIds(IEnumerable idValues)
        {
            return Exec(dbCmd => dbCmd.DeleteByIds<T>(idValues));
        }

        public IEnumerable Cast(IEnumerable results)
        {
            return (from object result in results select (T)result).ToList();
        }
    }
}