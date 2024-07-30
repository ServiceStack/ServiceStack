#if !NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack;

/// <summary>
/// Common High-level App utils covering popular App scenarios
/// </summary>
public static class AppUtils
{
    public static T NewScope<T>(this IServiceProvider services, Func<IServiceScope, T> fn)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using (var scope = scopeFactory.CreateScope())
        {
            return fn(scope);
        }
    }

    public static T DbContextExec<TApplicationDbContext, T>(this IServiceProvider services, 
        Func<TApplicationDbContext, IDbConnection> dbResolver, Func<IDbConnection, T> fn)
    {
        return services.NewScope(scope =>
        {
            using (var dbContext = scope.ServiceProvider.GetRequiredService<TApplicationDbContext>() as IDisposable)
            {
                var db = dbResolver((TApplicationDbContext)dbContext);
                return fn(db);
            }
        });
    }

    const string IdentityUserRolesByIdSql = @"SELECT r.Name 
      FROM AspNetUsers u 
           INNER JOIN AspNetUserRoles ur ON (u.Id = ur.UserId) 
           INNER JOIN AspNetRoles r ON (r.Id = ur.RoleId) 
     WHERE u.Id = @userId";

    public static List<string> GetIdentityUserRolesById(this IDbConnection db, string userId) =>
        db.GetIdentityUserRolesById(userId, IdentityUserRolesByIdSql);
    
    public static List<string> GetIdentityUserRolesById(this IDbConnection db, string userId, string sqlGetUserRoles)
    {
        var roles = new List<string>();
        using (var cmd = db.CreateCommand())
        {
            cmd.AddUserIdParameter(userId, sqlGetUserRoles);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    roles.Add(reader.GetString(0));
                }
            }
        }

        return roles;
    }

    public static long GetIdentityUsersCount(this IDbConnection db) => db.GetTableRowsCount("AspNetUsers");
    public static long GetIdentityRolesCount(this IDbConnection db) => db.GetTableRowsCount("AspNetRoles");
    public static long GetTableRowsCount(this IDbConnection db, string sqlTable)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM " + sqlTable;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            return reader.GetInt64(0);
        }
        return 0;
    }

    const string IdentityUserByIdSql = @"SELECT * FROM AspNetUsers WHERE Id = @userId";

    public static Dictionary<string, object> GetIdentityUserById(this IDbConnection db, string userId) =>
        db.GetIdentityUserById(userId, IdentityUserByIdSql);

    public static Dictionary<string, object> GetIdentityUserById(this IDbConnection db, string userId,
        string sqlGetUser)
    {
        using (var cmd = db.CreateCommand())
        {
            cmd.AddUserIdParameter(userId, sqlGetUser);
            using (var reader = cmd.ExecuteReader())
            {
                var to = reader.ToObjectDictionary();
                return to;
            }
        }
    }

    public static Dictionary<string, object> ToObjectDictionary(
        this IDataReader reader,
        Func<string, object, object> mapper = null)
    {
        Dictionary<string, object> to = null;
        var fieldCount = reader.FieldCount;
        while (reader.Read())
        {
            if (to == null)
                to = new Dictionary<string, object>();

            for (var i = 0; i < fieldCount; i++)
            {
                var value = reader.GetValue(i);
                if (value == DBNull.Value)
                    continue;
                string key = reader.GetName(i);
                value = mapper?.Invoke(key, value) ?? value;
                to[key] = value;
            }
        }
        return to;
    }

    public static T GetIdentityUserById<T>(this IDbConnection db, string userId) =>
        db.GetIdentityUserById<T>(userId, IdentityUserByIdSql);
    
    public static T GetIdentityUserById<T>(this IDbConnection db, string userId, string sqlGetUser)
    {
        var values = db.GetIdentityUserById(userId, sqlGetUser);
        if (values == null)
            return default;
        var to = values.FromObjectDictionary<T>();
        return to;
    }

    private static void AddUserIdParameter(this IDbCommand cmd, string userId, string sql)
    {
        cmd.CommandText = sql;
        var p = cmd.CreateParameter();
        p.ParameterName = nameof(userId);
        p.DbType = DbType.String;
        p.Value = userId;
        cmd.Parameters.Add(p);
    }

}

#endif
