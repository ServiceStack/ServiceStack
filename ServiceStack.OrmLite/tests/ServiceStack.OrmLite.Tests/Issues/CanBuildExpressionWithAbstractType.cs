using NUnit.Framework;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests.Issues;

abstract class ProductBase : IHasIntId
{
    public int Id { get; set; }

    public string ManufacturingMessage { get; set; }
}

class ProductSheet : ProductBase
{

}

[TestFixtureOrmLite]
public class CanBuildExpressionWithAbstractType(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_Update_Property_In_Abstract_Base_Class()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ProductSheet>();
        db.Insert(new ProductSheet { Id = 23, ManufacturingMessage = "test" });
        db.UpdateOnlyFields(new ProductSheet { ManufacturingMessage = "toto" }, p => p.ManufacturingMessage, p => p.Id == 23);
        var sheet = db.SingleById<ProductSheet>(23);
        Assert.That(sheet.ManufacturingMessage, Is.EqualTo("toto"));
    }
}