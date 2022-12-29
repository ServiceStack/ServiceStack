using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests.Issues
{
    [Alias("color")]
    public class ColorModel
    {
        public string Color { get; set; }
        public string Value { get; set; }
    }

    public class ColorJsonModel
    {
        public int Id { get; set; }
        public string ColorJson { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class OrmLiteModelArrayTests : OrmLiteProvidersTestBase
    {
        public OrmLiteModelArrayTests(DialectContext context) : base(context) {}

        [Test]
        public void test_model_with_array_to_json()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ColorModel>();

                db.Insert(new ColorModel { Color = "red", Value = "#f00" });
                db.Insert(new ColorModel { Color = "green", Value = "#0f0" });
                db.Insert(new ColorModel { Color = "blue", Value = "#00f" });
                db.Insert(new ColorModel { Color = "cyan", Value = "#0ff" });
                db.Insert(new ColorModel { Color = "magenta", Value = "#f0f" });
                db.Insert(new ColorModel { Color = "yellow", Value = "#ff0" });
                db.Insert(new ColorModel { Color = "black", Value = "#000" });

                const string sql = @"SELECT 1::integer AS id
                                        , json_agg(color.*) AS color_json
                                    FROM color;";

                var results = db.Select<ColorJsonModel>(sql);

                //results.PrintDump();

                Assert.That(results.Count, Is.EqualTo(1));

                foreach (var result in results)
                {
                    Assert.That(result.Id, Is.EqualTo(1));
                    Assert.That(result.ColorJson, Is.Not.Null);
                }

            }
        }

        [Test]
        public void test_model_with_array_and_json()
        {
            //OrmLiteConfig.DeoptimizeReader = true;

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ColorModel>();

                db.Insert(new ColorModel { Color = "red", Value = "#f00" });
                db.Insert(new ColorModel { Color = "green", Value = "#0f0" });
                db.Insert(new ColorModel { Color = "blue", Value = "#00f" });
                db.Insert(new ColorModel { Color = "cyan", Value = "#0ff" });
                db.Insert(new ColorModel { Color = "magenta", Value = "#f0f" });
                db.Insert(new ColorModel { Color = "yellow", Value = "#ff0" });
                db.Insert(new ColorModel { Color = "black", Value = "#000" });

                // SQL contains array and json aggs.
                // We usually have ARRAY fields defined in the db, but when
                // retrieved we json-ize them. In otherwords the array exists in the tables/views.
                // We use SELECT.* which would contain the ARRAY field.
                // Array fields are not used in any of our models and should not cause the other
                // fields in the model to not be populated.
                const string sql = @"SELECT 1::integer AS id
                                            , json_agg(color.*) AS color_json
                                            , array_agg(color.*) AS color_array
                                    FROM color;";

                var results = db.Select<ColorJsonModel>(sql);

                Assert.That(results.Count, Is.EqualTo(1));

                foreach (var result in results)
                {
                    result.ColorJson.Print();
                    Assert.That(result.Id, Is.EqualTo(1));
                    Assert.That(result.ColorJson, Is.Not.Null);
                }
            }
        }

        [Alias("my_table")]
        public class MyModel
        {
            public int MyModelId { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string NewField { get; set; }
        }

        public class MyNewModel
        {
            public int MyModelId { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string NewField { get; set; }
            public int RenamedId { get; set; }
        }

        [Test]
        public void test_model_with_simple_array_and_duplicate_fields()
        {
            using (var db = OpenDbConnection())
            {

                db.DropAndCreateTable<MyModel>();

                db.Insert(new MyModel { MyModelId = 100, Name = "Test Name", NewField = "New Field", Type = "My Type" });
                db.Insert(new MyModel { MyModelId = 200, Name = "Tester Name 2", NewField = "New Field 2", Type = "My Type 2" });

                const string sql = @"
                SELECT *, t2.my_model_id AS renamed_id
                FROM (
                    SELECT *
                    FROM my_table
                    CROSS JOIN (SELECT ARRAY[1,2,3,4] AS int_array) AS c
                    ) AS t1
                INNER JOIN my_table AS t2 ON t1.my_model_id = t2.my_model_id;";

                var results = db.Select<MyNewModel>(sql);

                Assert.That(results.Count, Is.GreaterThan(1));

                foreach (var result in results)
                {
                    Console.WriteLine("{0} - {1} - {2}".Fmt(result.MyModelId, result.Name, result.RenamedId));
                    Assert.That(result.MyModelId, Is.Not.EqualTo(0));
                    Assert.That(result.RenamedId, Is.Not.EqualTo(0));
                    Assert.That(result.Name, Is.Not.Empty);
                }

            }
        }

        [Test]
        public void test_model_with_complex_array_and_duplicate_fields()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ColorModel>();

                db.Insert(new ColorModel { Color = "red", Value = "#f00" });
                db.Insert(new ColorModel { Color = "green", Value = "#0f0" });
                db.Insert(new ColorModel { Color = "blue", Value = "#00f" });
                db.Insert(new ColorModel { Color = "cyan", Value = "#0ff" });
                db.Insert(new ColorModel { Color = "magenta", Value = "#f0f" });
                db.Insert(new ColorModel { Color = "yellow", Value = "#ff0" });
                db.Insert(new ColorModel { Color = "black", Value = "#000" });

                db.DropAndCreateTable<MyModel>();

                db.Insert(new MyModel { MyModelId = 100, Name = "Test Name", NewField = "New Field", Type = "My Type" });
                db.Insert(new MyModel { MyModelId = 200, Name = "Tester Name 2", NewField = "New Field 2", Type = "My Type 2" });

                const string sql = @"
                SELECT *, t2.my_model_id AS renamed_id
                FROM (
                    SELECT *
                    FROM my_table
                    CROSS JOIN (SELECT array_agg(color.*) AS color_array FROM color) AS c
                    ) AS t1
                INNER JOIN my_table AS t2 ON t1.my_model_id = t2.my_model_id;";

                var results = db.Select<MyNewModel>(sql);

                Assert.That(results.Count, Is.GreaterThan(1));

                foreach (var result in results)
                {
                    Console.WriteLine("{0} - {1} - {2}".Fmt(result.MyModelId, result.Name, result.RenamedId));
                    Assert.That(result.MyModelId, Is.Not.EqualTo(0));
                    Assert.That(result.RenamedId, Is.Not.EqualTo(0));
                    Assert.That(result.Name, Is.Not.Empty);
                }
            }
        }

    }

}