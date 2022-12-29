using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    [TestFixture]
    public class JoinWithHintUseCase : OrmLiteTestBase
    {
        [Alias("car")]
        class Car
        {

            [PrimaryKey]
            [Alias("car_id")]
            [AutoIncrement]
            public int CarId { get; set; }

            [Alias("car_name")]
            public string Name { get; set; }
        }

        [Alias("car_type")]
        class CarType
        {
            [Alias("car_id")]
            public int CarId { get; set; }

            [Alias("car_type_name")]
            public string CarTypeName { get; set; }
        }

        class CarTypeJoin
        {
            public int CarId { get; set; }
            public string CarTypeName { get; set; }
        }

        private static void InitTables(IDbConnection db)
        {
            db.DropTable<Car>();
            db.DropTable<CarType>();

            db.CreateTable<Car>();
            db.CreateTable<CarType>();


            var id1 = db.Insert(new Car { Name = "Subaru" }, true);
            var id2 = db.Insert(new Car { Name = "BMW" }, true);
            var id3 = db.Insert(new Car { Name = "Nissan" }, true);

            db.Insert(new CarType { CarId = (int)id1, CarTypeName = "Sedan" });
            db.Insert(new CarType { CarId = (int)id2, CarTypeName = "Coupe" });
            db.Insert(new CarType { CarId = (int)id3, CarTypeName = "SUV" });
        }

        [Test]
        public void can_join_with_readuncommitted()
        {
            using (var db = OpenDbConnection())
            {
                InitTables(db);

                var join = db.From<Car>()
                    .Join<Car, CarType>((l, r) => l.CarId == r.CarId, SqlServerTableHint.ReadUncommitted);

                var selectStatement = join.ToSelectStatement();
                selectStatement.Print();

                var data = db.Select<CarTypeJoin>(join);

                Assert.That(selectStatement.Contains("READUNCOMMITTED"));
            }
        }
    }
}
