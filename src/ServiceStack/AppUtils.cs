#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStack
{
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
                cmd.CommandText = sqlGetUserRoles;
                var p = cmd.CreateParameter();
                p.ParameterName = nameof(userId);
                p.DbType = DbType.String;
                p.Value = userId;
                cmd.Parameters.Add(p);

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
    }
}

#endif
