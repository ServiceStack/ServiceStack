using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack
{
    public class OrmLiteValidationSource : IValidationSource, IRequiresSchema, IValidationSourceAdmin, IClearable
    {
        public IDbConnectionFactory DbFactory { get; }
        public ICacheClient Cache { get; }
        public TimeSpan? CacheDuration { get; }
        
        public string NamedConnection { get; set; }

        public OrmLiteValidationSource(IDbConnectionFactory dbFactory=null, ICacheClient cache=null, TimeSpan? cacheDuration=null)
        {
            DbFactory = dbFactory 
                ?? HostContext.TryResolve<IDbConnectionFactory>() 
                ?? throw new ArgumentNullException(nameof(dbFactory));
            Cache = cache;
        }

        public IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type)
        {
            if (Cache == null)
                return GetDbValidationRules(type);
            if (CacheDuration == null)
                return Cache.GetOrCreate(nameof(IValidationSource) + "." + type.Name, () => GetDbValidationRules(type));
            
            return Cache.GetOrCreate(nameof(IValidationSource) + "." + type.Name, CacheDuration.Value, () => GetDbValidationRules(type));
        }

        private IEnumerable<KeyValuePair<string, IValidateRule>> GetDbValidationRules(Type type)
        {
            using var db = OpenDbConnection();
            var rows = db.Select<ValidationRule>(x => x.Type == type.Name && x.SuspendedDate == null);
            var to = rows.Map(x => new KeyValuePair<string, IValidateRule>(x.Field, x));
            return to;
        }

        private IDbConnection OpenDbConnection()
        {
            var db = NamedConnection == null
                ? DbFactory.OpenDbConnection()
                : DbFactory.OpenDbConnection(NamedConnection);
            return db;
        }

        public void InitSchema()
        {
            using var db = OpenDbConnection();
            db.CreateTableIfNotExists<ValidationRule>();
        }

        public async Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName)
        {
            using var db = OpenDbConnection();
            var rows = await db.SelectAsync<ValidationRule>(x => x.Type == typeName);
            return rows;
        }

        public async Task SaveValidationRulesAsync(List<ValidationRule> validateRules)
        {
            using var db = OpenDbConnection();
            await db.SaveAllAsync(validateRules);
            ClearValidationSourceCache();
        }

        public async Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids)
        {
            using var db = OpenDbConnection();
            var rows = await db.SelectByIdsAsync<ValidationRule>(ids);
            return rows;
        }

        private void ClearValidationSourceCache() => Cache?.RemoveByPattern(nameof(IValidationSource) + ".*");

        public async Task DeleteValidationRulesAsync(params int[] ids)
        {
            using var db = OpenDbConnection();
            await db.DeleteByIdsAsync<ValidationRule>(ids);
            ClearValidationSourceCache();
        }

        public Task ClearCacheAsync()
        {
            ClearValidationSourceCache();
            return TypeConstants.EmptyTask;
        }

        public void Clear()
        {
            using var db = OpenDbConnection();
            db.DeleteAll<ValidationRule>();
            Cache?.RemoveByPattern(nameof(IValidationSource) + ".*");
        }
    }
}