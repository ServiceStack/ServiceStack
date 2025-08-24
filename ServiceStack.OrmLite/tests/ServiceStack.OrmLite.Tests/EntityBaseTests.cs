using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

public class EntityBase
{
    public int TenantId { get; set; }
    public bool Deleted { get; set; }
}

public class CustomerEntity : EntityBase
{
    [AutoIncrement]
    public int Id { get; set; }
        
    public string CustomerName { get; set; }
}

public class EmployeeEntity : EntityBase
{
    [AutoIncrement]
    public int Id { get; set; }
        
    public string EmployeeType { get; set; }
}

class TenantRepo
{
    public int tenantId;
    public TenantRepo(int tenantId) => this.tenantId = tenantId;

    public List<T> All<T>(IDbConnection db)
    {
        return db.Where<T>(new { TenantId = tenantId });
    }

    public List<T> AllUntyped<T>(IDbConnection db)
    {
        return db.Where<T>(nameof(EntityBase.TenantId), tenantId);
    }

    private SqlExpression<T> CreateQuery<T>(IDbConnection db)
    {
        return db.From<T>().Where(x => (x as EntityBase).TenantId == tenantId);
    }

    public List<T> AllTyped<T>(IDbConnection db)
    {
        return db.Select(CreateQuery<T>(db));
    }

    public List<T> Where<T>(IDbConnection db, Expression<Func<T, bool>> expr)
    {
        return db.Select(CreateQuery<T>(db).And(expr));
    }
}

abstract class EntityRepoBase<T> where T : EntityBase, new()
{
    public int tenantId;
    public EntityRepoBase(int tenantId) => this.tenantId = tenantId;
        
    public List<T> All(IDbConnection db) => 
        db.Select(db.From<T>().Where(x => x.TenantId == tenantId));

    public List<T> Where(IDbConnection db, Expression<Func<T, bool>> expr) =>
        db.Select(db.From<T>().Where(x => x.TenantId == tenantId).And(expr));
}

class CustomerTenantRepo : EntityRepoBase<CustomerEntity>
{
    public CustomerTenantRepo(int tenantId) : base(tenantId) { }
}
    
[TestFixture]
public class EntityBaseTests : OrmLiteTestBase
{
    public void SeedData(IDbConnection db)
    {
        db.DropAndCreateTable<CustomerEntity>();
        db.Insert(new CustomerEntity {TenantId = 1, CustomerName = "Kurt" });
        db.Insert(new CustomerEntity {TenantId = 1, CustomerName = "Dave" });
        db.Insert(new CustomerEntity {TenantId = 2, CustomerName = "Kurt" });
        db.Insert(new CustomerEntity {TenantId = 2, CustomerName = "Dave" });
    }

    [Test]
    public void Can_generically_query_base_class()
    {
        var repo = new TenantRepo(2);

        using var db = OpenDbConnection();
        SeedData(db);

        var rows = repo.All<CustomerEntity>(db);
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.All(x => x.TenantId == 2));
                
        rows = repo.AllTyped<CustomerEntity>(db);
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.All(x => x.TenantId == 2));
                
        rows = repo.AllUntyped<CustomerEntity>(db);
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.All(x => x.TenantId == 2));

        rows = repo.Where<CustomerEntity>(db, x => x.CustomerName == "Kurt");
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].TenantId, Is.EqualTo(2));
    }

    [Test]
    public void Can_generically_query_base_class_with_constrained_repo()
    {
        var repo = new CustomerTenantRepo(2);

        using var db = OpenDbConnection();
        SeedData(db);

        var rows = repo.All(db);
        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows.All(x => x.TenantId == 2));

        rows = repo.Where(db, x => x.CustomerName == "Kurt");
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows[0].TenantId, Is.EqualTo(2));
    }
}