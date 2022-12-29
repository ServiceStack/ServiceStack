using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.MySql.Tests
{
    [TestFixture]
    public class BooleanTests : OrmLiteTestBase
    {
        [Test]
        public void Can_store_and_read_bool_values()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Boolies>();

                var boolies = new List<Boolies> {
                  new Boolies { plainBool = true,  netBool = true },
                  new Boolies { plainBool = false, netBool = true },
                  new Boolies { plainBool = true,  netBool = false },
                  new Boolies { plainBool = false, netBool = false },
                };
                db.InsertAll(boolies);

                var target = db.Select<Boolies>();

                Assert.AreEqual(boolies.Count, target.Count);

                for (int i = 0; i < boolies.Count; i++)
                {
                    Assert.AreEqual(boolies[i].netBool, target.ElementAt(i).netBool);
                    Assert.AreEqual(boolies[i].plainBool, target.ElementAt(i).plainBool);
                }
            }
        }

        [Test]
        public void Can_store_and_read_bool_values_mapped_as_bit_column()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<Boolies>(true);

                db.ExecuteSql("ALTER TABLE `Boolies` CHANGE `plainBool` `plainBool` bit( 1 ) NOT NULL, " 
                    + "CHANGE `netBool`   `netBool`   bit( 1 ) NOT NULL");

                var boolies = new List<Boolies> {
                  new Boolies { plainBool = true,  netBool = true },
                  new Boolies { plainBool = false, netBool = true },
                  new Boolies { plainBool = true,  netBool = false },
                  new Boolies { plainBool = false, netBool = false },
                };
                db.InsertAll(boolies);

                var target = db.Select<Boolies>();

                Assert.AreEqual(boolies.Count, target.Count);

                for (int i = 0; i < boolies.Count; i++)
                {
                    Assert.AreEqual(boolies[i].netBool, target.ElementAt(i).netBool);
                    Assert.AreEqual(boolies[i].plainBool, target.ElementAt(i).plainBool);
                }
            }
        }

        public class Boolies : IHasId<int>
        {
            [AutoIncrement]
            [Alias("id")]
            public int Id { get; set; }
            public bool plainBool { get; set; }
            public Boolean netBool { get; set; }
        }
    }

    
}
