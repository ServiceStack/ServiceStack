using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests
{
    public class UpdateTests : OrmLiteTestBase
    {
        [Test]
        public void Can_execute_update_using_expression()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleType>(true);
                var obj = new SimpleType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                obj.Id = storedObj.Id;
                obj.Name = "Someothername";
                con.Update(obj, q => q.Id == storedObj.Id);

                var target = con.SingleById<SimpleType>(storedObj.Id);

                Assert.AreEqual(obj.Name, target.Name);
            }
        }

        [Test]
        public void Can_execute_update_only()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleType>(true);
                var obj = new SimpleType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                var ev = OrmLiteConfig.DialectProvider.SqlExpression<SimpleType>();
                ev.Update();
                ev.Where(q => q.Id == storedObj.Id); 
                storedObj.Name = "Someothername";

                con.UpdateOnlyFields(storedObj, ev);

                var target = con.SingleById<SimpleType>(storedObj.Id);

                Assert.AreEqual("Someothername", target.Name);
            }
        }


        [Test]
        public void Can_execute_update()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleType>(true);
                var obj = new SimpleType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                obj.Id = storedObj.Id;
                obj.Name = "Someothername";
                con.Update(obj);

                var target = con.SingleById<SimpleType>(storedObj.Id);

                Assert.AreEqual(obj.Name, target.Name);
            }
        }

        [Test]
        public void Can_execute_update_using_aliased_columns()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleAliasedType>(true);
                var obj = new SimpleAliasedType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleAliasedType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                obj.Id = storedObj.Id;
                obj.Name = "Someothername";
                con.Update(obj);

                var target = con.SingleById<SimpleAliasedType>(storedObj.Id);

                Assert.AreEqual(obj.Name, target.Name);
            }
        }

        [Test]
        public void Can_execute_updateParam()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleType>(true);
                var obj = new SimpleType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                obj.Id = storedObj.Id;
                obj.Name = "Someothername";
                con.Update(obj);

                var target = con.SingleById<SimpleType>(storedObj.Id);

                Assert.AreEqual(obj.Name, target.Name);
            }
        }

        [Test]
        public void Can_execute_updateParam_using_aliased_columns()
        {
            using (var con = OpenDbConnection())
            {
                con.CreateTable<SimpleAliasedType>(true);
                var obj = new SimpleAliasedType { Name = "Somename" };
                con.Save(obj);
                var storedObj = con.SingleById<SimpleAliasedType>(obj.Id);

                Assert.AreEqual(obj.Name, storedObj.Name);

                obj.Id = storedObj.Id;
                obj.Name = "Someothername";
                con.Update(obj);

                var target = con.SingleById<SimpleAliasedType>(storedObj.Id);

                Assert.AreEqual(obj.Name, target.Name);
            }
        }
    }


    public class SimpleType
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SimpleAliasedType
    {
        [AutoIncrement]
        public int Id { get; set; }
        [Alias("NewName")]
        public string Name { get; set; }
      
    }
}
