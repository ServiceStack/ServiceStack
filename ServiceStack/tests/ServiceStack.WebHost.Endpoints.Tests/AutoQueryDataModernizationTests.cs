using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class AutoQueryDataModernizationTests
{
    [SetUp]
    public void SetUp() => HostContext.Reset();

    [TearDown]
    public void TearDown() => HostContext.Reset();

    [OneTimeTearDown]
    public void OneTimeTearDown() => HostContext.Reset();

    public class SimplePoco
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EmptyPoco { }

    public class TestDataDto : QueryData<SimplePoco>
    {
        public int? Id { get; set; }
    }

    [Test]
    public void PocoDataSource_TryDeleteByIds_Correctly_Counts_And_Removes_Items()
    {
        var items = new List<SimplePoco>
        {
            new() { Id = 1, Name = "Item 1" },
            new() { Id = 2, Name = "Item 2" },
            new() { Id = 3, Name = "Item 3" },
        };

        var source = new PocoDataSource<SimplePoco>(items, nextIdSequence: 3);

        // Delete existing IDs 1 and 3, plus non-existent ID 99
        var deletedCount = source.TryDeleteByIds(new[] { 1, 99, 3 });

        // Exactly 2 items were removed (IDs 1 and 3)
        Assert.That(deletedCount, Is.EqualTo(2));
        var remaining = source.GetAll();
        Assert.That(remaining.Count, Is.EqualTo(1));
        Assert.That(remaining[0].Id, Is.EqualTo(2));

        // If trying to delete non-existent IDs again, count must be 0
        var secondDeleted = source.TryDeleteByIds(new[] { 1, 99, 100 });
        Assert.That(secondDeleted, Is.EqualTo(0));
    }

    [Test]
    public void PocoDataSource_Save_With_Default_Value_Assigns_New_Id()
    {
        var items = new List<SimplePoco>
        {
            new() { Id = 1, Name = "Item 1" },
        };

        var source = new PocoDataSource<SimplePoco>(items, nextIdSequence: 1);

        // New item with default Id 0 should be assigned NextId (2)
        var newItem = new SimplePoco { Id = 0, Name = "Item 2" };
        var saved = source.Save(newItem);

        Assert.That(saved.Id, Is.EqualTo(2));
        Assert.That(source.GetAll().Count, Is.EqualTo(2));

        // Existing item with Id 1 should be updated in place
        var updatedItem = new SimplePoco { Id = 1, Name = "Item 1 Updated" };
        source.Save(updatedItem);
        Assert.That(source.GetAll().Count, Is.EqualTo(2));
        Assert.That(source.GetAll().First(x => x.Id == 1).Name, Is.EqualTo("Item 1 Updated"));
    }

    [Test]
    public void PocoDataSource_Null_Handling()
    {
        Assert.Throws<ArgumentNullException>(() => new PocoDataSource<SimplePoco>(null));

        var source = new PocoDataSource<SimplePoco>(new List<SimplePoco>());
        Assert.Throws<ArgumentNullException>(() => source.Add(null));
        Assert.Throws<ArgumentNullException>(() => source.Save(null));

        Assert.That(source.TryUpdate(null), Is.False);
        Assert.That(source.TryDelete(null), Is.False);
        Assert.That(source.TryDeleteById(null), Is.False);
        Assert.That(source.TryDeleteByIds<int>(null), Is.EqualTo(0));
    }

    [Test]
    public void CompareTypeUtils_CoerceDouble_And_CoerceLong_Null_Safety_And_Cast()
    {
        Assert.That(CompareTypeUtils.CoerceDouble(null), Is.Null);
        Assert.That(CompareTypeUtils.CoerceLong(null), Is.Null);
        Assert.That(CompareTypeUtils.CoerceString(null), Is.Null);

        // Double coercion must not throw InvalidCastException
        var d = CompareTypeUtils.CoerceDouble(12.34);
        Assert.That(d, Is.EqualTo(12.34));

        var f = CompareTypeUtils.CoerceDouble(10.5f);
        Assert.That(f, Is.EqualTo(10.5));

        var l = CompareTypeUtils.CoerceLong(42);
        Assert.That(l, Is.EqualTo(42L));

        // Add with real numbers
        var sum = CompareTypeUtils.Add(1.5, 2.5);
        Assert.That(sum, Is.EqualTo(4.0));
    }

    [Test]
    public void InCollectionCondition_Excludes_String_As_Enumerable()
    {
        // "foo" compared against "foobar": should NOT iterate over chars
        var condition = InCollectionCondition.Instance;
        Assert.That(condition.Match("foo", "foobar"), Is.False);
        Assert.That(condition.Match("foo", "foo"), Is.True);
        Assert.That(condition.Match("foo", new[] { "bar", "foo" }), Is.True);

        var ciCondition = CaseInsensitiveInCollectionCondition.Instance;
        Assert.That(ciCondition.Match("FOO", "foobar"), Is.False);
        Assert.That(ciCondition.Match("FOO", "foo"), Is.True);
        Assert.That(ciCondition.Match("FOO", new[] { "bar", "foo" }), Is.True);
    }

    [Test]
    public void InBetweenCondition_Excludes_String_And_Invalid_Counts()
    {
        var condition = InBetweenCondition.Instance;

        // String input should return false instead of throwing ArgumentException or iterating chars
        Assert.That(condition.Match(25, "20,30"), Is.False);

        // Single element list should return false instead of throwing ArgumentException
        Assert.That(condition.Match(25, new[] { 20 }), Is.False);

        // Null should return false
        Assert.That(condition.Match(25, null), Is.False);

        // Valid 2-element collection
        Assert.That(condition.Match(25, new[] { 20, 30 }), Is.True);
        Assert.That(condition.Match(35, new[] { 20, 30 }), Is.False);
    }

    [Test]
    public void DataQuery_Limit_Clamps_Negative_Values()
    {
        var q = new DataQuery<SimplePoco>(null);
        q.Limit(-10, -5);
        Assert.That(q.Offset, Is.EqualTo(0));
        Assert.That(q.Rows, Is.EqualTo(0));

        q.Take(-10);
        Assert.That(q.Rows, Is.EqualTo(0));

        q.Limit(10, 20);
        Assert.That(q.Offset, Is.EqualTo(10));
        Assert.That(q.Rows, Is.EqualTo(20));
    }

    [Test]
    public void DataQuery_OrderByPrimaryKey_Safely_Handles_Type_Without_Properties()
    {
        var q = new DataQuery<EmptyPoco>(null);
        // Should not throw NullReferenceException
        Assert.DoesNotThrow(() => q.OrderByPrimaryKey());
        Assert.That(q.OrderBy, Is.Null);
    }

    [Test]
    public void AutoCrudOperation_Null_Guards()
    {
        Assert.That(AutoCrudOperation.ToHttpMethod((Type)null), Is.Null);
        Assert.That(AutoCrudOperation.GetAutoQueryGenericDefTypes(null, null), Is.Null);
        Assert.That(AutoCrudOperation.GetAutoQueryDtoType(null), Is.Null);
        Assert.That(AutoCrudOperation.GetAutoCrudDtoType(null), Is.Null);
        Assert.That(AutoCrudOperation.GetModelType(null), Is.Null);
        Assert.That(AutoCrudOperation.GetViewModelType(null, null), Is.Null);
        Assert.That(((MetadataType)null).HasNamedConnection("conn"), Is.False);
        Assert.That(((MetadataType)null).IsRequestDto(), Is.False);
    }

    [Test]
    public void AutoQueryDataServiceSource_GetResults_Null_Safety()
    {
        Assert.That(AutoQueryDataServiceSource.GetResults<SimplePoco>(null), Is.Null);
        Assert.That(AutoQueryDataServiceSource.GetResults(null), Is.Null);
    }

    [Test]
    public void AutoQueryDataFeature_Register_Null_Safety()
    {
        var feature = new AutoQueryDataFeature();
        Assert.DoesNotThrow(() => feature.Register(null));

        using var appHost = new BasicAppHost();
        appHost.Init();
        Assert.DoesNotThrow(() => feature.Register(appHost));
    }

    [Test]
    public void AutoQueryData_Filter_Null_Dto_Safety()
    {
        var aq = new AutoQueryData();
        var q = new DataQuery<SimplePoco>(null);
        Assert.DoesNotThrow(() => aq.Filter<SimplePoco>(q, null, null));
        Assert.DoesNotThrow(() => aq.Filter(q, null, null));
    }
}
