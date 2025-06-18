using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

public class OrmLiteValidationSource : IValidationSource, IRequiresSchema, IValidationSourceAdmin, IClearable
{
    public IDbConnectionFactory DbFactory { get; }
    static void ConfigureDb(IDbConnection db) => db.WithName(nameof(OrmLiteValidationSource));
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
            ? DbFactory.Open(ConfigureDb)
            : DbFactory.Open(NamedConnection,ConfigureDb);
        return db;
    }

    public void InitSchema()
    {
        using var db = OpenDbConnection();
        db.CreateTableIfNotExists<ValidationRule>();
    }

    public List<ValidationRule> GetAllValidateRules()
    {
        using var db = OpenDbConnection();
        return db.Select<ValidationRule>();
    }

    public async Task<List<ValidationRule>> GetAllValidateRulesAsync()
    {
        using var db = OpenDbConnection();
        var rows = await db.SelectAsync<ValidationRule>().ConfigAwait();
        return rows;
    }

    public async Task<List<ValidationRule>> GetAllValidateRulesAsync(string typeName)
    {
        using var db = OpenDbConnection();
        var rows = await db.SelectAsync<ValidationRule>(x => x.Type == typeName).ConfigAwait();
        return rows;
    }

    public async Task SaveValidationRulesAsync(List<ValidationRule> validateRules)
    {
        using var db = OpenDbConnection();
        await db.SaveAllAsync(validateRules).ConfigAwait();
        ClearValidationSourceCache();
    }

    public void SaveValidationRules(List<ValidationRule> validateRules)
    {
        using var db = OpenDbConnection();
        db.SaveAll(validateRules);
        ClearValidationSourceCache();
    }

    public async Task<List<ValidationRule>> GetValidateRulesByIdsAsync(params int[] ids)
    {
        using var db = OpenDbConnection();
        var rows = await db.SelectByIdsAsync<ValidationRule>(ids).ConfigAwait();
        return rows;
    }

    private void ClearValidationSourceCache() => Cache?.RemoveByPattern(nameof(IValidationSource) + ".*");

    public async Task DeleteValidationRulesAsync(params int[] ids)
    {
        using var db = OpenDbConnection();
        await db.DeleteByIdsAsync<ValidationRule>(ids).ConfigAwait();
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