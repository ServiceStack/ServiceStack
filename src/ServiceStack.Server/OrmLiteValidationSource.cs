using System;
using System.Collections.Generic;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack
{
    public class OrmLiteValidationSource : IValidationSource, IRequiresSchema, IValidationSourceWriter
    {
        public IDbConnectionFactory DbFactory { get; }
        public ICacheClient Cache { get; }
        public TimeSpan? CacheDuration { get; }

        public OrmLiteValidationSource(IDbConnectionFactory dbFactory, ICacheClient cache=null, TimeSpan? cacheDuration=null)
        {
            DbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            Cache = cache;
        }

        public IEnumerable<KeyValuePair<string, IValidateRule>> GetValidationRules(Type type)
        {
            var ret = Cache == null
                ? GetDbValidationRules(type)
                : CacheDuration == null 
                    ? Cache.GetOrCreate(nameof(IValidationSource) + "." + type.Name, () => GetDbValidationRules(type))
                    : Cache.GetOrCreate(nameof(IValidationSource) + "." + type.Name, CacheDuration.Value, () => GetDbValidationRules(type));
            return ret;
        }

        private IEnumerable<KeyValuePair<string, IValidateRule>> GetDbValidationRules(Type type)
        {
            using var db = DbFactory.OpenDbConnection();
            var rows = db.Select<ValidateRule>(x => x.Type == type.Name && x.SuspendedDate == null);
            var to = rows.Map(x => new KeyValuePair<string, IValidateRule>(x.Field, x));
            return to;
        }

        public void InitSchema()
        {
            using var db = DbFactory.OpenDbConnection();
            db.CreateTableIfNotExists<ValidateRule>();
        }

        public void SaveValidationRules(List<ValidateRule> validateRules)
        {
            using var db = DbFactory.OpenDbConnection();
            db.SaveAll(validateRules);
            ClearValidationSourceCache();
        }

        private void ClearValidationSourceCache() => Cache?.RemoveByPattern(nameof(IValidationSource) + ".*");

        public void DeleteValidationRules(params int[] ids)
        {
            using var db = DbFactory.OpenDbConnection();
            db.DeleteByIds<ValidateRule>(ids);
            ClearValidationSourceCache();
        }

        public void Clear()
        {
            using var db = DbFactory.OpenDbConnection();
            db.DeleteAll<ValidateRule>();
            Cache?.RemoveByPattern(nameof(IValidationSource) + ".*");
        }
    }
}