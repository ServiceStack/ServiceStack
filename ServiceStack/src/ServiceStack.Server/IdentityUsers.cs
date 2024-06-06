#if NET6_0_OR_GREATER
#nullable enable

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

public static class IdentityUsers
{
    public static string TableName { get; set; } = "AspNetUsers";
    
    public static string[] ColumnNames { get; set; } = [
        nameof(IdentityUser.Id),
        nameof(IdentityUser.UserName),
        nameof(IdentityUser.NormalizedUserName),
        nameof(IdentityUser.Email),
        nameof(IdentityUser.NormalizedEmail),
        nameof(IdentityUser.EmailConfirmed),
        nameof(IdentityUser.PasswordHash),
        nameof(IdentityUser.SecurityStamp),
        nameof(IdentityUser.ConcurrencyStamp),
        nameof(IdentityUser.PhoneNumber),
        nameof(IdentityUser.PhoneNumberConfirmed),
        nameof(IdentityUser.TwoFactorEnabled),
        nameof(IdentityUser.LockoutEnd),
        nameof(IdentityUser.LockoutEnabled),
        nameof(IdentityUser.AccessFailedCount)
    ];

    private static string GetSelectFromAspNetUsersSql(IOrmLiteDialectProvider dialect)
    {
        var sqlColumns = string.Join(',', ColumnNames.Select(dialect.GetQuotedName));
        var sqlSelect = $"SELECT {sqlColumns} FROM {dialect.GetQuotedName(TableName)}";
        return sqlSelect;
    }

    private static string GetByUserIdSql(IDbConnection db, string userId)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName("Id")} = {dialect.GetQuotedValue(userId)}";
    }
    public static IdentityUser? GetByUserId(IDbConnection db, string userId) => 
        db.SqlList<IdentityUser>(GetByUserIdSql(db, userId)).FirstOrDefault();
    public static async Task<IdentityUser?> GetByUserIdAsync(IDbConnection db, string userId, CancellationToken token=default) =>
        (await db.SqlListAsync<IdentityUser>(GetByUserIdSql(db, userId), token:token).ConfigAwait()).FirstOrDefault();

    private static string GetByUserIdsSql(IDbConnection db, IEnumerable<string> userIds)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName("Id")} IN ({userIds.ToList().SqlJoin()})";
    }
    public static List<IdentityUser> GetByUserIds(IDbConnection db, IEnumerable<string> userIds) => 
        db.SqlList<IdentityUser>(GetByUserIdsSql(db, userIds));
    public static async Task<List<IdentityUser>> GetByUserIdsAsync(IDbConnection db, IEnumerable<string> userIds, CancellationToken token=default) => 
        await db.SqlListAsync<IdentityUser>(GetByUserIdsSql(db, userIds), token:token).ConfigAwait();
    
    private static string GetByUserNameSql(IDbConnection db, string userName)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName(nameof(IdentityUser.NormalizedUserName))} = {dialect.GetQuotedValue(userName.ToUpper())}";
    }
    public static IdentityUser? GetByUserName(IDbConnection db, string userName) => 
        db.SqlList<IdentityUser>(GetByUserNameSql(db, userName)).FirstOrDefault();
    public static async Task<IdentityUser?> GetByUserNameAsync(IDbConnection db, string userName, CancellationToken token=default) => 
        (await db.SqlListAsync<IdentityUser>(GetByUserNameSql(db, userName), token:token).ConfigAwait()).FirstOrDefault();

    private static string GetByUserNamesSql(IDbConnection db, IEnumerable<string> userNames)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName(nameof(IdentityUser.NormalizedUserName))} IN ({userNames.Map(x => x.ToUpper()).SqlJoin()})";
    }
    public static List<IdentityUser> GetByUserNames(IDbConnection db, IEnumerable<string> userNames) => 
        db.SqlList<IdentityUser>(GetByUserNamesSql(db, userNames));
    public static async Task<List<IdentityUser>> GetByUserNamesAsync(IDbConnection db, IEnumerable<string> userNames, CancellationToken token=default) => 
        await db.SqlListAsync<IdentityUser>(GetByUserNamesSql(db, userNames), token:token).ConfigAwait();
    
    private static string GetByEmailSql(IDbConnection db, string email)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName(nameof(IdentityUser.NormalizedEmail))} = {dialect.GetQuotedValue(email.ToUpper())}";
    }
    public static IdentityUser? GetByEmail(IDbConnection db, string email) => 
        db.SqlList<IdentityUser>(GetByEmailSql(db, email)).FirstOrDefault();
    public static async Task<IdentityUser?> GetByEmailAsync(IDbConnection db, string email, CancellationToken token=default) =>
        (await db.SqlListAsync<IdentityUser>(GetByEmailSql(db, email), token: token).ConfigAwait()).FirstOrDefault();

    private static string GetByEmailsSql(IDbConnection db, IEnumerable<string> emails)
    {
        var dialect = db.GetDialectProvider();
        return $"{GetSelectFromAspNetUsersSql(dialect)} WHERE {dialect.GetQuotedName(nameof(IdentityUser.NormalizedEmail))} IN ({emails.Map(x => x.ToUpper()).SqlJoin()})";
    }
    public static List<IdentityUser> GetByEmails(IDbConnection db, IEnumerable<string> userNames) => 
        db.SqlList<IdentityUser>(GetByEmailsSql(db, userNames));
    public static async Task<List<IdentityUser>> GetByEmailsAsync(IDbConnection db, IEnumerable<string> userNames, CancellationToken token=default) => 
        await db.SqlListAsync<IdentityUser>(GetByEmailsSql(db, userNames), token:token).ConfigAwait();
}
#endif