using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class EnumTests : OrmLiteProvidersTestBase
    {
        public EnumTests(DialectContext context) : base(context) {}

        [Test]
        public void CanCreateTable()
        {
            OpenDbConnection().CreateTable<TypeWithEnum>(true);
        }

        [Test]
        public void CanStoreEnumValue()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
            }
        }

        [Test]
        public void CanGetEnumValue()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                var obj = new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 };
                con.Save(obj);
                var target = con.SingleById<TypeWithEnum>(obj.Id);
                Assert.AreEqual(obj.Id, target.Id);
                Assert.AreEqual(obj.EnumValue, target.EnumValue);
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_expression()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var target = con.Select<TypeWithEnum>(q => q.EnumValue == SomeEnum.Value1);

                Assert.AreEqual(2, target.Count());
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_string()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var target = con.Select<TypeWithEnum>("\"enum_value\" = @value", new { value = SomeEnum.Value1 });

                Assert.AreEqual(2, target.Count());
            }
        }

        [Ignore("Where using anon type needs to be fixed")]
        [Test]
        public void CanQueryByEnumValue_using_where_with_AnonType()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var target = con.Where<TypeWithEnum>(new { EnumValue = SomeEnum.Value1 });

                Assert.AreEqual(2, target.Count());
            }
        }

    }

    public enum SomeEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class TypeWithEnum
    {
        public int Id { get; set; }
        public SomeEnum EnumValue { get; set; }
    }

}
