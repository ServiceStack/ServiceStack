using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class EnumAsIntAsPkTests : OrmLiteProvidersTestBase
    {
        public EnumAsIntAsPkTests(DialectContext context) : base(context) {}
       
        [Test]
        public void CanCreateTable()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<LookupTypeWithEnumAsIntAsPk>();
            }
        }

        [Test]
        public void CanStoreEnumValue()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<LookupTypeWithEnumAsIntAsPk>();
                db.Save(new LookupTypeWithEnumAsIntAsPk { EnumAsIntAsPkId = LookupTypeEnum.Value1, EnumValueThatWouldGoInAsString = SomeEnum.Value1 });
            }
        }

        [Test]
        public void CanGetEnumValue()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<LookupTypeWithEnumAsIntAsPk>();

                var obj = new LookupTypeWithEnumAsIntAsPk { EnumAsIntAsPkId = LookupTypeEnum.Value1, EnumValueThatWouldGoInAsString = SomeEnum.Value1 };
                db.Save(obj);

                var target = db.SingleById<LookupTypeWithEnumAsIntAsPk>(obj.EnumAsIntAsPkId);
                Assert.AreEqual(obj.EnumAsIntAsPkId, target.EnumAsIntAsPkId);
                Assert.AreEqual(obj.EnumValueThatWouldGoInAsString, target.EnumValueThatWouldGoInAsString);
            }
        }
    }

    /// <summary>
    /// We store all enum values in the db for 
    /// </summary>
    [EnumAsInt]
    public enum LookupTypeEnum
    {
        Value1 = 0,
        Value2 = 1,
        Value3 = 2
    }

    [Alias("LookupTypeWithEnumAsIntAsPk")]
    public class LookupTypeWithEnumAsIntAsPk
    {
        [PrimaryKey]
        public LookupTypeEnum EnumAsIntAsPkId { get; set; }


        public SomeEnum EnumValueThatWouldGoInAsString { get; set; }

        /// <summary>
        /// Allow this lookup type to be soft deleted in the future, but retaining referential integrity
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}