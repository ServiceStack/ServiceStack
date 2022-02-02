using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public class BaseClass
    {
        public int Id { get; set; }
    }

    public class Target : BaseClass
    {
        public string Name { get; set; }
    }

    [TestFixtureOrmLite]
    public class UntypedApiTests : OrmLiteProvidersTestBase
    {
        public UntypedApiTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_table_and_insert_with_untyped_Api()
        {
            using (var db = OpenDbConnection())
            {
                var row = (BaseClass)new Target { Id = 1, Name = "Foo" };

                var useType = row.GetType();
                var typedApi = db.CreateTypedApi(useType);

                db.DropAndCreateTables(useType);

                db.GetLastSql().Print();

                typedApi.Save(row);

                var typedRow = db.SingleById<Target>(1);

                Assert.That(typedRow.Name, Is.EqualTo("Foo"));

                var updateRow = (BaseClass)new Target { Id = 1, Name = "Bar" };

                typedApi.Update(updateRow);

                typedRow = db.SingleById<Target>(1);

                Assert.That(typedRow.Name, Is.EqualTo("Bar"));

                typedApi.Delete(typedRow, new { Id = 1 });

                typedRow = db.SingleById<Target>(1);

                Assert.That(typedRow, Is.Null);

                typedApi.Insert(row);

                typedRow = db.SingleById<Target>(1);

                Assert.That(typedRow.Name, Is.EqualTo("Foo"));
            }
        }
        
        public class UserEntity
        {
            public long Id { get; set; }

            [DataAnnotations.Ignore]
            public AnimalEntity MyPrimaryAnimal
            {
                get => AnimalEntity.FromObjectDictionary(AnimalRef);
                set => AnimalRef = value.ToObjectDictionary();
            }
            
            public Dictionary<string, object> AnimalRef { get; set; }
        }

        public class AnimalEntity
        {
            public string Type => GetType().Name;

            public static AnimalEntity FromObjectDictionary(Dictionary<string, object> props)
            {
                if (props == null) return null;
                var type = props[nameof(Type)];
                switch (type) 
                {
                    case nameof(DogEntity):
                        return props.FromObjectDictionary<DogEntity>();
                    case nameof(CatEntity):
                        return props.FromObjectDictionary<CatEntity>();
                    case nameof(BirdEntity):
                        return props.FromObjectDictionary<BirdEntity>();
                    default:
                        throw new NotSupportedException($"Unknown Animal '{type}'");
                }
            }
        }

        public class CatEntity : AnimalEntity
        {
            public int Id { get; set; }
            public string Cat { get; set; }
        }

        public class DogEntity : AnimalEntity
        {
            public int Id { get; set; }
            public string Dog { get; set; }
        }

        public class BirdEntity : AnimalEntity
        {
            public int Id { get; set; }
            public string Bird { get; set; }
        }

        [Test]
        public void Can_store_different_entities_in_single_field()
        {
            using (var db = OpenDbConnection())
            {
                OrmLiteUtils.PrintSql();
                db.DropAndCreateTable<UserEntity>();
                
                db.Insert(new UserEntity {Id = 1, MyPrimaryAnimal = new BirdEntity {Id = 1, Bird = "B"}});
                db.Insert(new UserEntity {Id = 2, MyPrimaryAnimal = new CatEntity {Id = 1, Cat = "C"}});
                db.Insert(new UserEntity {Id = 3, MyPrimaryAnimal = new DogEntity {Id = 1, Dog = "D"}});

                var results = db.Select<UserEntity>();
                var animals = results.OrderBy(x => x.Id).Map(x => x.MyPrimaryAnimal);
                Assert.That(animals[0] is BirdEntity b && b.Bird == "B");
                Assert.That(animals[1] is CatEntity c && c.Cat == "C");
                Assert.That(animals[2] is DogEntity d && d.Dog == "D");
            }
        }

    }
}