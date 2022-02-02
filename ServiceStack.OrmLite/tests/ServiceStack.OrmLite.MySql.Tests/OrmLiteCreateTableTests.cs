using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.MySql.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests
{
    public class MaxStringTest
    {
        public int Id { get; set; }

        [StringLength(int.MaxValue)]
        public string MaxText { get; set; }

        [Text]
        public string Text { get; set; }

        [CustomField("MEDIUMTEXT")]
        public string MediumText { get; set; }
    }

    [TestFixture]
    public class OrmLiteCreateTableTests
        : OrmLiteTestBase
    {
        [Test]
        public void Can_create_table_with_MaxString_and_Custom_MediumText()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<MaxStringTest>();

            //var sql = db.GetLastSql();
            //Assert.That(sql, Is.StringContaining("`MaxText` LONGTEXT NULL"));
            //Assert.That(sql, Is.StringContaining("`MediumText` MEDIUMTEXT NULL"));
        }

        [Test]
        public void Can_create_ModelWithIdOnly_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdOnly>(true);
        }

        [Test]
        public void Can_create_ModelWithOnlyStringFields_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithOnlyStringFields>(true);
        }

        [Test]
        public void Can_create_ModelWithLongIdAndStringFields_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithLongIdAndStringFields>(true);
        }

        [Test]
        public void Can_create_ModelWithFieldsOfDifferentTypes_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
        }

        [Test]
        public void Can_preserve_ModelWithIdOnly_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdOnly>(true);

            db.Insert(new ModelWithIdOnly(1));
            db.Insert(new ModelWithIdOnly(2));

            db.CreateTable<ModelWithIdOnly>(false);

            var rows = db.Select<ModelWithIdOnly>();

            Assert.That(rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Can_preserve_ModelWithIdAndName_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdAndName>(true);

            db.Insert(new ModelWithIdAndName(1));
            db.Insert(new ModelWithIdAndName(2));

            db.CreateTable<ModelWithIdAndName>(false);

            var rows = db.Select<ModelWithIdAndName>();

            Assert.That(rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void Can_overwrite_ModelWithIdOnly_table()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdOnly>(true);

            db.Insert(new ModelWithIdOnly(1));
            db.Insert(new ModelWithIdOnly(2));

            db.CreateTable<ModelWithIdOnly>(true);

            var rows = db.Select<ModelWithIdOnly>();

            Assert.That(rows, Has.Count.EqualTo(0));
        }

        [Test]
        public void Can_create_multiple_tables()
        {
            using var db = OpenDbConnection();
            db.CreateTables(true, typeof(ModelWithIdOnly), typeof(ModelWithIdAndName));

            db.Insert(new ModelWithIdOnly(1));
            db.Insert(new ModelWithIdOnly(2));

            db.Insert(new ModelWithIdAndName(1));
            db.Insert(new ModelWithIdAndName(2));

            var rows1 = db.Select<ModelWithIdOnly>();
            var rows2 = db.Select<ModelWithIdOnly>();

            Assert.That(rows1, Has.Count.EqualTo(2));
            Assert.That(rows2, Has.Count.EqualTo(2));
        }

        [Test]
        public void Can_create_ModelWithIdAndName_table_with_specified_DefaultStringLength()
        {
            OrmLiteConfig.DialectProvider.GetStringConverter().StringLength = 255;
            var createTableSql = OrmLiteConfig.DialectProvider.ToCreateTableStatement(typeof(ModelWithIdAndName));

            Console.WriteLine("createTableSql: " + createTableSql);
            Assert.That(createTableSql.Contains("VARCHAR(255)"), Is.True);
        }

        public class Signal
        {
            [AutoIncrement]
            public int Id { get; set; }
            public short Code { get; set; }
        }

        [Test]
        public void Does_DoesColumnExist()
        {
            using var db = OpenDbConnection();
            db.DropTable<Signal>();
            var exists = db.ColumnExists<Signal>(x => x.Code);
            Assert.That(exists, Is.False);

            db.CreateTable<Signal>();
            exists = db.ColumnExists<Signal>(x => x.Code);
            Assert.That(exists);
        }

        [Test]
        public void Can_create_table_with_custom_field_order()
        {
            using (var db = OpenDbConnection())
            {
                var modelDefinition = typeof(ModelWithCustomFiledOrder).GetModelMetadata();
                db.DropAndCreateTable<ModelWithCustomFiledOrder>();
                var defs=db.SqlList<(string field,string type,string @null,string key,string @default,string extra)>("desc " + modelDefinition.Name);
                Assert.AreEqual(nameof(ModelWithCustomFiledOrder.Filed1),defs[0].field);
                Assert.AreEqual(nameof(ModelWithCustomFiledOrder.Filed3),defs[1].field);
                Assert.AreEqual(nameof(ModelWithCustomFiledOrder.Filed2), defs[2].field);
                Assert.AreEqual(nameof(ModelWithCustomFiledOrder.Id),defs[3].field);
            }
        }

        [Test]
        public void Can_create_table_without_custom_field_order()
        {
            using (var db = OpenDbConnection())
            {
                var modelDefinition = typeof(ModelWithoutCustomFiledOrder).GetModelMetadata();
                db.DropAndCreateTable<ModelWithoutCustomFiledOrder>();
                var defs = db.SqlList<(string field, string type, string @null, string key, string @default, string extra)>("desc " + modelDefinition.Name);
                Assert.AreEqual(nameof(ModelWithoutCustomFiledOrder.Id), defs[0].field);
                Assert.AreEqual(nameof(ModelWithoutCustomFiledOrder.Filed1), defs[1].field);
                Assert.AreEqual(nameof(ModelWithoutCustomFiledOrder.Filed2), defs[2].field);
                Assert.AreEqual(nameof(ModelWithoutCustomFiledOrder.Filed3), defs[3].field);
            }
        }

        [Test]
        public void model_definition_without_custom_order()
        {
            var modelDefinition = typeof(ModelWithoutCustomFiledOrder).GetModelMetadata();
            Assert.AreEqual(4, modelDefinition.FieldDefinitions.Count);
            Assert.AreEqual(0, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Id)).Order);
            Assert.AreEqual(1, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed1)).Order);
            Assert.AreEqual(2, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed2)).Order);
            Assert.AreEqual(3, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed3)).Order);
        }

        [Test]
        public void model_definition_with_custom_order()
        {
            var modelDefinition = typeof(ModelWithCustomFiledOrder).GetModelMetadata();
            Assert.AreEqual(4, modelDefinition.FieldDefinitions.Count);
            Assert.AreEqual(100, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Id)).Order);
            Assert.AreEqual(5, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed1)).Order);
            Assert.AreEqual(13, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed2)).Order);
            Assert.AreEqual(8, modelDefinition.FieldDefinitions.Find(fd => fd.Name == nameof(ModelWithoutCustomFiledOrder.Filed3)).Order);
        }

        public class ModelWithoutCustomFiledOrder
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public int Filed1 { get; set; }
            public string Filed2 { get; set; }
            public float Filed3 { get; set; }
        }

        public class ModelWithCustomFiledOrder
        {
            [PrimaryKey]
            [AutoIncrement]
            [CustomField(Order = 100)]
            public int Id { get; set; }

            [CustomField(Order = 5)]
            public int Filed1 { get; set; }
            [CustomField(Order = 13)]
            public string Filed2 { get; set; }
            [CustomField(Order = 8)]
            public float Filed3 { get; set; }
        }
    }
}