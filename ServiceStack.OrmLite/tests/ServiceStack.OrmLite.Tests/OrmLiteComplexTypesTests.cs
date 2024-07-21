using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

using System.Collections.Generic;

[TestFixtureOrmLite]
public class OrmLiteComplexTypesTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_insert_into_ModelWithComplexTypes_table()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithComplexTypes>(true);

            var row = ModelWithComplexTypes.Create(1);

            db.Insert(row);
        }
    }

    [Test]
    public void Can_insert_and_select_from_ModelWithComplexTypes_table()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<ModelWithComplexTypes>(true);

            var row = ModelWithComplexTypes.Create(1);

            db.Insert(row);

            var rows = db.Select<ModelWithComplexTypes>();

            Assert.That(rows, Has.Count.EqualTo(1));

            ModelWithComplexTypes.AssertIsEqual(rows[0], row);
        }
    }

    [Test]
    public void Can_insert_and_select_from_OrderLineData()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<SampleOrderLine>(true);

            var orderIds = new[] { 1, 2, 3, 4, 5 }.ToList();

            orderIds.ForEach(x => db.Insert(
                SampleOrderLine.Create(Guid.NewGuid(), x, 1)));

            var rows = db.Select<SampleOrderLine>();
            Assert.That(rows, Has.Count.EqualTo(orderIds.Count));
        }
    }

    [Test]
    public void Lists_Of_Guids_Are_Formatted_Correctly()
    {
        LogManager.LogFactory = new ConsoleLogFactory();
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<WithAListOfGuids>();

            JsConfig<Guid>.RawSerializeFn = x => x.ToString();

            var item = new WithAListOfGuids
            {
                GuidOne = new Guid("32cb0acb-db43-4061-a6aa-7f4902a7002a"),
                GuidTwo = new Guid("13083231-b005-4ff4-ab62-41bdc7f50a4d"),
                TheGuids = new[] { new Guid("18176030-7a1c-4288-82df-a52f71832381"), new Guid("017f986b-f7be-4b6f-b978-ff05fba3b0aa") },
            };

            db.Insert(item);

            var tbl = "WithAListOfGuids".SqlTable(DialectProvider);
            var savedGuidOne = db.Select<Guid>("SELECT {0} FROM {1}".Fmt("GuidOne".SqlColumn(DialectProvider), tbl)).First();
            Assert.That(savedGuidOne, Is.EqualTo(new Guid("32cb0acb-db43-4061-a6aa-7f4902a7002a")));

            var savedGuidTwo = db.Select<Guid>("SELECT {0} FROM {1}".Fmt("GuidTwo".SqlColumn(DialectProvider), tbl)).First();
            Assert.That(savedGuidTwo, Is.EqualTo(new Guid("13083231-b005-4ff4-ab62-41bdc7f50a4d")));

            var savedGuidList = db.Select<string>("SELECT {0} FROM {1}".Fmt("TheGuids".SqlColumn(DialectProvider), tbl)).First();
            Assert.That(savedGuidList, Is.EqualTo("[18176030-7a1c-4288-82df-a52f71832381,017f986b-f7be-4b6f-b978-ff05fba3b0aa]"));

            JsConfig.Reset();
        }
    }

    [Test]
    public void Can_insert_Contact_with_Complex_NameDetail()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Contact>();

            var contact = new Contact
            {
                FullName = new NameDetail("Sinéad", "O'Connor"),
                Email = "Sinéad@O'Connor.com",
                Age = 10
            };
            db.Save(contact);

            var dbContact = db.Single<Contact>(q => q.Email == contact.Email);

            Assert.That(dbContact.Email, Is.EqualTo(contact.Email));
            Assert.That(dbContact.Age, Is.EqualTo(contact.Age));
            Assert.That(dbContact.FullName.First, Is.EqualTo(contact.FullName.First));
            Assert.That(dbContact.FullName.Last, Is.EqualTo(contact.FullName.Last));
        }
    }
}

public class WithAListOfGuids
{
    public Guid GuidOne { get; set; }

    public Guid GuidTwo { get; set; }

    public IEnumerable<Guid> TheGuids { get; set; }
}

public class Contact
{
    public Contact()
    {
        Tags = new List<AvailableTags>();
        FullName = new NameDetail();
    }

    [AutoIncrement]
    public int Id { get; set; }

    public string Email { get; set; }
    public int Age { get; set; }
    public List<AvailableTags> Tags { get; set; }

    public NameDetail FullName { get; set; }

    [DataAnnotations.Ignore]
    public string Name
    {
        get { return FullName.ToString(); }
    }
}

public class NameDetail
{
    public NameDetail() { }

    public NameDetail(string first, string last)
    {
        First = first;
        Last = last;
    }

    public string First { get; set; }

    public string Last { get; set; }

    public override string ToString()
    {
        return string.Format("{0} {1}", First, Last).Trim();
    }
}

public enum AvailableTags
{
    other,
    glam,
    hiphop,
    grunge,
    funk
}