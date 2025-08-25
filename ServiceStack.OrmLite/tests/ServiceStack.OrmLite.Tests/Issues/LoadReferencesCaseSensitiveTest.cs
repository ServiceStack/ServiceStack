using ServiceStack.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

/// <summary>This test set is used to demonstrate that the OrmLite Load methods are unexpectedly case-sensitive for
/// string-based columns. A reference will not be loaded if there is a difference in case between otherwise matching parent
/// value and child values. For example, parent table has RegionCode = "WEST" while it's related lookup table Region has
/// RegionCode = "West".</summary>
/// <remarks>Target database must be setup as case-insensitive or this exercise is pointless. Refer to custom
/// openDbConnection method included in this test set.</remarks>
public class CustomerWithRegion
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }

    [CustomField("VARCHAR(8000) COLLATE Latin1_General_CI_AS")]
    public string RegionId { get; set; }

    [Reference]
    public Region RegionDetail { get; set; }
}

public class Region
{
    [PrimaryKey]
    [CustomField("VARCHAR(8000) COLLATE Latin1_General_CI_AS")]
    public string Id { get; set; }

    public string RegionName { get; set; }
}

[TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
public class LoadReferencesCaseSensitiveTest(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string regionCode = "West";
    const string regionName = "Western Region";

    /// <summary>This test is used to demonstrate that the OrmLite Load methods are unexpectedly case-sensitive for
    /// string-based columns. A reference will not be loaded if there is a difference in case between otherwise matching parent
    /// value and child values. For example, parent table has RegionCode = "WEST" while it's related lookup table Region has
    /// RegionCode = "West".</summary>
    [Test]
    public void LoadReference_with_Case_Variance()
    {
        OrmLiteConfig.IsCaseInsensitive = true;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<CustomerWithRegion>();
        //db.GetLastSql().Print();
        db.DropAndCreateTable<Region>();
        //db.GetLastSql().Print();

        var region = new Region { Id = regionCode, RegionName = regionName };
        db.Save(region);

        var caseInsensitiveRegion = db.Single<Region>(x => x.Id == regionCode.ToUpper());
        Assert.That(caseInsensitiveRegion.Id, Is.EqualTo(regionCode));

        var customers = new List<CustomerWithRegion>
        {
            new CustomerWithRegion { Name = "Acme Anvil Co.", RegionId = regionCode }, 
            new CustomerWithRegion { Name = "Penny's Poodle Emporium", RegionId = regionCode.ToUpper() }, 
        };

        foreach (var customer in customers)
        {
            db.Save(customer);

            var dbCustomer = db.LoadSelect<CustomerWithRegion>(s => s.Name == customer.Name).FirstOrDefault();

            dbCustomer.PrintDump();

            Assert.That(
                dbCustomer.RegionId,
                Is.Not.Null,
                $"Region code missing for {customer.Name}");
            Assert.That(
                dbCustomer.RegionId.ToLower() == regionCode.ToLower(),
                $"Region code incorrect for {customer.Name}");

            // The following assertion will fail because LoadSelect considers CustomWithRegion.RegionCode of "WEST" != to Region.RegionCode of "West".
            Assert.That(
                dbCustomer.RegionDetail,
                Is.Not.Null,
                $"Region detail record missing for {customer.Name}");
            Assert.That(
                dbCustomer.RegionDetail.RegionName == regionName,
                $"Region name incorrect for {customer.Name}");
        }

        OrmLiteConfig.IsCaseInsensitive = false;
    }
}