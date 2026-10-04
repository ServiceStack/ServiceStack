#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

/// <summary>
/// Returns the differences between the models of a database and their tables, and a migration that makes the
/// database the same as its models
/// </summary>
[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminSchemaDiff : IGet, IReturn<AdminSchemaDiffResponse>
{
    /// <summary>
    /// The named connection of the database, or main for the default connection
    /// </summary>
    public string? Db { get; set; }

    /// <summary>
    /// The class name of the migration, defaults to the one after the App's last migration, e.g. Migration1005
    /// </summary>
    public string? Migration { get; set; }

    /// <summary>
    /// The namespace of the migration, defaults to the namespace of the App's migrations
    /// </summary>
    public string? Namespace { get; set; }
}

public class AdminSchemaDiffResponse : IHasResponseStatus
{
    /// <summary>
    /// The models that were compared with their tables
    /// </summary>
    public List<string> Models { get; set; } = [];

    /// <summary>
    /// The changes that make the database the same as its models
    /// </summary>
    public List<AdminSchemaChange> Results { get; set; } = [];

    /// <summary>
    /// What couldn't be compared
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// The tables of models that weren't compared, as set by OrmLiteConfig.SchemaDiff, e.g. AspNetUsers
    /// </summary>
    public List<string> Ignored { get; set; } = [];

    /// <summary>
    /// The class name and namespace of the migration
    /// </summary>
    public string? MigrationName { get; set; }
    public string? MigrationNamespace { get; set; }

    /// <summary>
    /// The source code of a migration that makes the changes, null if there aren't any
    /// </summary>
    public string? Migration { get; set; }

    public ResponseStatus? ResponseStatus { get; set; }
}

public class AdminSchemaChange
{
    /// <summary>
    /// CreateTable, AddColumn, AlterColumn, DropColumn, CreateIndex, AlterIndex, DropIndex, AlterDefault,
    /// AddForeignKey, AlterForeignKey, DropForeignKey, AddConstraint, AlterConstraint, DropConstraint, AlterPrimaryKey
    /// or RebuildTable
    /// </summary>
    public string Type { get; set; } = "";
    public string Model { get; set; } = "";
    public string Table { get; set; } = "";

    /// <summary>
    /// The name of the column, index, foreign key or constraint
    /// </summary>
    public string? Name { get; set; }
    public string? ModelColumn { get; set; }
    public string? DatabaseColumn { get; set; }

    /// <summary>
    /// The column that's likely the same column renamed
    /// </summary>
    public string? LikelyRename { get; set; }
    public string Description { get; set; } = "";
    public string? Sql { get; set; }
    public bool? IsDestructive { get; set; }

    /// <summary>
    /// Whether it's made by the RebuildTable change of its table, as SQLite can't make it by itself
    /// </summary>
    public bool? IsRebuilt { get; set; }
}

/// <summary>
/// Compares the models of the AdminDatabaseFeature with their tables. It doesn't change the database, other than
/// the temporary tables that column types are read from.
/// </summary>
public class AdminSchemaDiffService : Service
{
    public async Task<object> Any(AdminSchemaDiff request)
    {
        var feature = AssertPlugin<AdminDatabaseFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole).ConfigAwait();

        var dbName = request.Db is null or "main" ? null : request.Db;
        var modelTypes = feature.GetModelTypes(HostContext.AppHost, dbName);

        var migrationName = request.Migration ?? feature.GetNextMigrationName(HostContext.AppHost);
        if (!Regex.IsMatch(migrationName, "^[A-Za-z_][A-Za-z0-9_]*$"))
            throw new ArgumentException("Not a valid class name", nameof(request.Migration));
        var migrationNamespace = request.Namespace ?? feature.GetMigrationNamespace(HostContext.AppHost);
        if (!Regex.IsMatch(migrationNamespace, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$"))
            throw new ArgumentException("Not a valid namespace", nameof(request.Namespace));

        var dbFactory = TryResolve<IDbConnectionFactory>()
            ?? throw new NotSupportedException("IDbConnectionFactory is not registered");
        using var db = dbName != null
            ? dbFactory.Open(dbName, AdminDatabaseFeature.ConfigureDb)
            : dbFactory.Open(AdminDatabaseFeature.ConfigureDb);

        var diff = db.GetSchemaDiff(modelTypes.ToArray());
        var dialect = db.GetDialectProvider();

        return new AdminSchemaDiffResponse {
            Models = modelTypes.Where(x => !OrmLiteConfig.SchemaDiff.IsIgnored(x, dialect)).Map(x => x.Name),
            Results = diff.Changes.Map(x => new AdminSchemaChange {
                Type = x.Type.ToString(),
                Model = x.ModelType.Name,
                Table = x.Table,
                Name = x.Name,
                ModelColumn = x.ModelColumn,
                DatabaseColumn = x.DatabaseColumn,
                LikelyRename = x.LikelyRename,
                Description = x.Description,
                Sql = x.Sql,
                IsDestructive = x.IsDestructive ? true : null,
                IsRebuilt = x.IsRebuilt ? true : null,
            }),
            Warnings = diff.Warnings,
            Ignored = diff.Ignored,
            MigrationName = migrationName,
            MigrationNamespace = migrationNamespace,
            Migration = diff.HasChanges
                ? diff.ToMigration(migrationName, migrationNamespace)
                : null,
        };
    }
}
