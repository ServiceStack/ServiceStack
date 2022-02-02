using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests.Issues
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class JsonDataTypeTests : OrmLiteProvidersTestBase
    {
        public JsonDataTypeTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_save_and_restore_JSON_property()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new OrmLiteNamingStrategyBase()))
            {
                var item = new LicenseCheckTemp
                {
                    Body = new CheckHistory
                    {
                        List = {
                        new ItemHistory { AddedOn = DateTime.MaxValue, Note = "Test" }
                    }
                    }
                };

                using (var db = OpenDbConnection())
                {
                    db.DropAndCreateTable<LicenseCheckTemp>();
                    db.GetLastSql().Print();
                    db.Save(item);
                }

                using (var db = OpenDbConnection())
                {
                    var items = db.Select<LicenseCheckTemp>();
                    items.PrintDump();

                    foreach (var licenseCheck in items.OrderBy(x => x.Id))
                    {
                        if (licenseCheck.Body != null && licenseCheck.Body.List.Any())
                        {
                            foreach (var itemHistory in licenseCheck.Body.List)
                            {
                                "{0} : Note {1}".Print(itemHistory.AddedOn, itemHistory.Note);
                            }
                        }
                    }
                }
            }
        }
    }

    public class LicenseCheckTemp
    {
        [AutoIncrement]
        public int Id { get; set; }

        [CustomField("json")]
        public CheckHistory Body { get; set; }
    }

    public class CheckHistory
    {
        public CheckHistory()
        {
            this.List = new List<ItemHistory>();
        }

        public List<ItemHistory> List { get; set; }
    }

    public class ItemHistory
    {
        public string Note { get; set; }

        public DateTime AddedOn { get; set; }

    }
}