using System;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

public class Organization
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; }
}

public class OrganizationMembership
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool HasA { get; set; }
    public bool HasB { get; set; }
    public bool HasC { get; set; }
}

[TestFixtureOrmLiteDialects(Dialect.Sqlite)]
public class MergingNestedSqlExpressionIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Does_merge_subselect_params_correctly()
    {
        // select a group of ids
        var ids = DialectProvider.SqlExpression<OrganizationMembership>();
        ids.Where(x => x.HasA == true && x.HasB == true && x.HasC == true);
        ids.SelectDistinct(x => x.OrganizationId);

        //0 params
        var q = DialectProvider.SqlExpression<Organization>(); 
        q.Where(x => Sql.In(x.Id, ids));
        Assert.That(q.WhereExpression, Is.EqualTo(
            "WHERE \"Id\" IN (SELECT DISTINCT \"OrganizationId\" \nFROM \"OrganizationMembership\"\nWHERE (((\"HasA\" = @0) AND (\"HasB\" = @1)) AND (\"HasC\" = @2)))"));

        //1 param
        q = DialectProvider.SqlExpression<Organization>();
        q.Where(x => x.IsActive == true);
        q.Where(x => Sql.In(x.Id, ids));
        Assert.That(q.WhereExpression, Is.EqualTo(
            "WHERE (\"IsActive\" = @0) AND \"Id\" IN (SELECT DISTINCT \"OrganizationId\" \nFROM \"OrganizationMembership\"\nWHERE (((\"HasA\" = @1) AND (\"HasB\" = @2)) AND (\"HasC\" = @3)))"));

        //2 params
        q = DialectProvider.SqlExpression<Organization>();
        q.Where(x => x.IsActive == true);
        q.Where(x => x.IsActive == true);
        q.Where(x => Sql.In(x.Id, ids));
        Assert.That(q.WhereExpression, Is.EqualTo(
            "WHERE (\"IsActive\" = @0) AND (\"IsActive\" = @1) AND \"Id\" IN (SELECT DISTINCT \"OrganizationId\" \nFROM \"OrganizationMembership\"\nWHERE (((\"HasA\" = @2) AND (\"HasB\" = @3)) AND (\"HasC\" = @4)))"));

        //3 params
        q = DialectProvider.SqlExpression<Organization>();
        q.Where(x => x.IsActive == true);
        q.Where(x => x.IsActive == true);
        q.Where(x => x.IsActive == true);
        q.Where(x => Sql.In(x.Id, ids));
        Assert.That(q.WhereExpression, Is.EqualTo(
            "WHERE (\"IsActive\" = @0) AND (\"IsActive\" = @1) AND (\"IsActive\" = @2) AND \"Id\" IN (SELECT DISTINCT \"OrganizationId\" \nFROM \"OrganizationMembership\"\nWHERE (((\"HasA\" = @3) AND (\"HasB\" = @4)) AND (\"HasC\" = @5)))"));
    }
}