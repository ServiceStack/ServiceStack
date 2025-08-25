using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

public class DepartmentEntity
{
    [PrimaryKey]
    [StringLength(250)]
    public string Name { get; set; }

    public string Description { get; set; }

    [StringLength(254)]
    public string Email { get; set; }

    [References(typeof(UserAuth))]
    public int? ManagerId { get; set; }

    [Reference]
    public UserAuth Manager { get; set; }
}

[TestFixtureOrmLite]
public class LoadReferencesNullReferenceIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Does_not_load_references_when_RefId_is_null()
    {
        using var db = OpenDbConnection();
        //db.DropAndCreateTable<UserAuth>(); //This test shouldn't query this table
        db.DropAndCreateTable<UserAuth>();
        db.DropAndCreateTable<DepartmentEntity>();

        db.Insert(new DepartmentEntity { Name = "Dept A", Email = "asif@depta.com" });

        var result = db.LoadSingleById<DepartmentEntity>("Dept A");

        db.DropTable<DepartmentEntity>();

        Assert.That(result, Is.Not.Null);
    }
}