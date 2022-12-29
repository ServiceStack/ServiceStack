using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class OrmLiteUpdateTests : OrmLiteProvidersTestBase
    {
        public OrmLiteUpdateTests(DialectContext context) : base(context) {}

        private ModelWithFieldsOfDifferentTypes CreateModelWithFieldsOfDifferentTypes(IDbConnection db)
        {
            db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

            var row = ModelWithFieldsOfDifferentTypes.Create(1);
            return row;
        }

        [Test]
        public void Can_update_ModelWithFieldsOfDifferentTypes_table()
        {
            using var db = OpenDbConnection();
            var row = CreateModelWithFieldsOfDifferentTypes(db);

            row.Id = (int)db.Insert(row, selectIdentity: true);

            row.Name = "UpdatedName";

            db.Update(row);

            var dbRow = db.SingleById<ModelWithFieldsOfDifferentTypes>(row.Id);

            ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
        }

        [Test]
        public void Can_update_ModelWithFieldsOfDifferentTypes_table_with_commandFilter()
        {
            using var db = OpenDbConnection();
            var row = CreateModelWithFieldsOfDifferentTypes(db);

            row.Id = (int)db.Insert(row, selectIdentity: true);

            row.Name = "UpdatedName";

            db.Update(row, cmd => cmd.CommandText.Print());

            var dbRow = db.SingleById<ModelWithFieldsOfDifferentTypes>(row.Id);

            ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
        }

        [Test]
        public void Can_update_ModelWithFieldsOfDifferentTypes_table_with_filter()
        {
            using var db = OpenDbConnection();
            var row = CreateModelWithFieldsOfDifferentTypes(db);

            row.Id = (int)db.Insert(row, selectIdentity: true);

            row.Name = "UpdatedName";

            db.Update(row, x => x.Long <= row.Long);

            var dbRow = db.SingleById<ModelWithFieldsOfDifferentTypes>(row.Id);

            ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
        }

        [Test]
        public void Can_update_with_anonymousType_and_expr_filter()
        {
            using var db = OpenDbConnection();
            var row = CreateModelWithFieldsOfDifferentTypes(db);

            row.Id = (int)db.Insert(row, selectIdentity: true);
            row.DateTime = DateTime.Now;
            row.Name = "UpdatedName";

            db.Update<ModelWithFieldsOfDifferentTypes>(new { row.Name, row.DateTime },
                x => x.Long >= row.Long && x.Long <= row.Long);

            var dbRow = db.SingleById<ModelWithFieldsOfDifferentTypes>(row.Id);
            Console.WriteLine(dbRow.Dump());
            ModelWithFieldsOfDifferentTypes.AssertIsEqual(dbRow, row);
        }

        [Test]
        public void Can_Update_Into_Table_With_Id_Only()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdOnly>(true);
            var row1 = new ModelWithIdOnly(1);
            db.Insert(row1);

            db.Update(row1);
        }

        [Test]
        public void Can_Update_Many_Into_Table_With_Id_Only()
        {
            using var db = OpenDbConnection();
            db.CreateTable<ModelWithIdOnly>(true);
            var row1 = new ModelWithIdOnly(1);
            var row2 = new ModelWithIdOnly(2);
            db.Insert(row1, row2);

            db.Update(row1, row2);

            var list = new List<ModelWithIdOnly> { row1, row2 };
            db.UpdateAll(list);
        }

        [Test]
        public void Can_UpdateOnly_multiple_columns()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();

            db.Insert(new Person { FirstName = "FirstName", Age = 100, LastName = "Original" });

            var existingPerson = db.Select<Person>().First();

            existingPerson.LastName = "Updated";
            existingPerson.FirstName = "JJ";
            existingPerson.Age = 12;

            db.UpdateOnlyFields(existingPerson,
                onlyFields: p => new { p.FirstName, p.Age });

            var person = db.Select<Person>().First();

            Assert.That(person.FirstName, Is.EqualTo("JJ"));
            Assert.That(person.Age, Is.EqualTo(12));
            Assert.That(person.LastName, Is.EqualTo("Original"));
        }

        [Test]
        public void Supports_different_ways_to_UpdateOnly()
        {
            void Reset(IDbConnection db)
            {
                db.DeleteAll<Person>();
                db.Insert(new Person {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});
            }

            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();                               

            Reset(db);
            db.UpdateOnly(() => new Person { FirstName = "UpdatedFirst", Age = 27 });
            var row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            db.UpdateOnlyFields(new Person { FirstName = "UpdatedFirst", Age = 27 }, p => p.FirstName);
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 100)));

            Reset(db);
            db.UpdateOnlyFields(new Person { FirstName = "UpdatedFirst", Age = 27 }, p => new { p.FirstName, p.Age });
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            db.UpdateOnlyFields(new Person { FirstName = "UpdatedFirst", Age = 27 }, new[] { "FirstName", "Age" });
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            db.UpdateOnly(() => new Person { FirstName = "UpdatedFirst", Age = 27 }, db.From<Person>().Where(x => x.Age == 100));
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            var q = db.From<Person>().Where(x => x.Age == 100);
            db.UpdateOnly(() => new Person { FirstName = "UpdatedFirst", Age = 27 }, q.WhereExpression, q.Params);
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));
        }

        [Test]
        public async Task Supports_different_ways_to_UpdateOnly_Async()
        {
            void Reset(IDbConnection db)
            {
                db.DeleteAll<Person>();
                db.Insert(new Person {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});
            }

            using var db = await OpenDbConnectionAsync();
            db.DropAndCreateTable<Person>();                               

            Reset(db);
            await db.UpdateOnlyAsync(() => new Person { FirstName = "UpdatedFirst", Age = 27 });
            var row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "UpdatedFirst", Age = 27 }, p => p.FirstName);
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 100)));

            Reset(db);
            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "UpdatedFirst", Age = 27 }, p => new { p.FirstName, p.Age });
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "UpdatedFirst", Age = 27 }, new[] { "FirstName", "Age" });
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            await db.UpdateOnlyAsync(() => new Person { FirstName = "UpdatedFirst", Age = 27 }, db.From<Person>().Where(x => x.Age == 100));
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

            Reset(db);
            var q = db.From<Person>().Where(x => x.Age == 100);
            await db.UpdateOnlyAsync(() => new Person { FirstName = "UpdatedFirst", Age = 27 }, q.WhereExpression, q.Params);
            row = db.Select<Person>().First();
            Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));
        }

        [Test]
        public void Can_Update_Only_Blobs()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<SomeBlobs>();

            db.Insert(new SomeBlobs { FirstName = "Bro", LastName = "Last" });
            db.Insert(new SomeBlobs { FirstName = "Sis", LastName = "Last" });

            var existing = db.Select<SomeBlobs>(p => p.FirstName == "Bro").First();

            const string blob1String = "This is going into Blob1";
            var blob1Array = blob1String.ToArray();
            var blob1Bytes = blob1Array.Length * 2;
            existing.Blob1 = new byte[blob1Bytes];
            Buffer.BlockCopy(blob1Array, 0, existing.Blob1, 0, blob1Bytes);

            const string blob2String = "And this is going into Blob2";
            var blob2Array = blob2String.ToArray();
            var blob2Bytes = blob2Array.Length * 2;
            existing.Blob2 = new byte[blob2Bytes];
            Buffer.BlockCopy(blob2Array, 0, existing.Blob2, 0, blob2Bytes);

            db.UpdateOnlyFields(existing, p => new { p.Blob1, p.Blob2, p.FirstName }, r => r.LastName == "Last" && r.FirstName == "Bro");

            var verify = db.Select<SomeBlobs>(p => p.FirstName == "Bro").First();

            var verifyBlob1 = new char[verify.Blob1.Length / 2];
            Buffer.BlockCopy(verify.Blob1, 0, verifyBlob1, 0, verify.Blob1.Length);

            Assert.That(existing.Blob1, Is.EquivalentTo(verify.Blob1));
            Assert.That(existing.Blob2, Is.EquivalentTo(verify.Blob2));
        }

        public class PocoWithBool
        {
            public int Id { get; set; }
            public bool Bool { get; set; }
        }

        public class PocoWithNullableBool
        {
            public int Id { get; set; }
            public bool? Bool { get; set; }
        }

        [Test]
        public void Can_UpdateOnly_bool_columns()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PocoWithBool>();

            db.Insert(new PocoWithBool { Id = 1, Bool = false });
            var row = db.SingleById<PocoWithBool>(1);
            Assert.That(row.Bool, Is.False);

            db.UpdateNonDefaults(new PocoWithBool { Bool = true }, x => x.Id == 1);
            row = db.SingleById<PocoWithBool>(1);
            Assert.That(row.Bool, Is.True);

            Assert.Throws<ArgumentException>(() =>
                db.UpdateNonDefaults(new PocoWithBool { Bool = false }, x => x.Id == 1));

            db.UpdateOnlyFields(new PocoWithBool { Bool = false },
                onlyFields: x => x.Bool,
                @where: x => x.Id == 1);
            row = db.SingleById<PocoWithBool>(1);
            Assert.That(row.Bool, Is.False);

            db.UpdateOnly(() => new PocoWithBool { Bool = true },
                @where: x => x.Id == 1);
            row = db.SingleById<PocoWithBool>(1);
            Assert.That(row.Bool, Is.True);

            db.UpdateOnly(() => new PocoWithBool { Bool = false },
                db.From<PocoWithBool>().Where(x => x.Id == 1));
            row = db.SingleById<PocoWithBool>(1);
            Assert.That(row.Bool, Is.False);
        }

        [Test]
        public void Can_UpdateOnly_nullable_bool_columns()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PocoWithNullableBool>();

            db.Insert(new PocoWithNullableBool { Id = 1, Bool = true });
            var row = db.SingleById<PocoWithNullableBool>(1);
            Assert.That(row.Bool, Is.True);

            db.UpdateNonDefaults(new PocoWithNullableBool { Bool = false }, x => x.Id == 1);
            row = db.SingleById<PocoWithNullableBool>(1);
            Assert.That(row.Bool, Is.False);

            db.UpdateOnly(() => new PocoWithNullableBool { Bool = true }, x => x.Id == 1);
            row = db.SingleById<PocoWithNullableBool>(1);
            Assert.That(row.Bool, Is.True);

            db.UpdateOnly(() => new PocoWithNullableBool { Bool = false },
                db.From<PocoWithNullableBool>().Where(x => x.Id == 1));
            row = db.SingleById<PocoWithNullableBool>(1);
            Assert.That(row.Bool, Is.False);
        }

        public class PocoWithNullableInt
        {
            public int Id { get; set; }
            public int? Int { get; set; }
        }

        [Test]
        public void Can_UpdateOnly_nullable_int_columns()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PocoWithNullableInt>();

            db.Insert(new PocoWithNullableInt { Id = 1, Int = 1 });
            var row = db.SingleById<PocoWithNullableInt>(1);
            Assert.That(row.Int, Is.EqualTo(1));

            db.UpdateNonDefaults(new PocoWithNullableInt { Int = 0 }, x => x.Id == 1);
            row = db.SingleById<PocoWithNullableInt>(1);
            Assert.That(row.Int, Is.EqualTo(0));

            db.UpdateOnly(() => new PocoWithNullableInt { Int = 1 }, x => x.Id == 1);
            Assert.That(db.SingleById<PocoWithNullableInt>(1).Int, Is.EqualTo(1));
            db.UpdateOnly(() => new PocoWithNullableInt { Int = 0 }, x => x.Id == 1);
            Assert.That(db.SingleById<PocoWithNullableInt>(1).Int, Is.EqualTo(0));
        }

        [Test]
        public void Can_UpdateAdd_nullable_int_columns()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PocoWithNullableInt>();

            db.Insert(new PocoWithNullableInt { Id = 1, Int = 0 });
            var row = db.SingleById<PocoWithNullableInt>(1);
            Assert.That(row.Int, Is.EqualTo(0));

            db.UpdateAdd(() => new PocoWithNullableInt { Int = 1 }, x => x.Id == 1);
            Assert.That(db.SingleById<PocoWithNullableInt>(1).Int, Is.EqualTo(1));
            db.UpdateAdd(() => new PocoWithNullableInt { Int = -1 }, x => x.Id == 1);
            Assert.That(db.SingleById<PocoWithNullableInt>(1).Int, Is.EqualTo(0));
        }

        [Test]
        public void Does_Save_nullable_bool()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Shutdown>();

            db.Insert(new Shutdown { IsShutdownGraceful = null });
            var rows = db.Select<Shutdown>();

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].IsShutdownGraceful, Is.Null);
        }

        [Test]
        public void Can_updated_with_ExecuteSql_and_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PocoUpdate>();

            db.Insert(new PocoUpdate { Id = 1, Name = "A" });
            db.Insert(new PocoUpdate { Id = 2, Name = "B" });

            var paramString = DialectProvider.ParamString;
            var table = db.GetDialectProvider().GetTableName(nameof(PocoUpdate));
            var sql = $"UPDATE {table} SET name = {paramString}name WHERE id = {paramString}id";
            var result = db.ExecuteSql(sql, new { id = 2, name = "UPDATED" });
            Assert.That(result, Is.EqualTo(1));

            var row = db.SingleById<PocoUpdate>(2);
            Assert.That(row.Name, Is.EqualTo("UPDATED"));

            sql = $"UPDATE {table} SET name = {paramString}name WHERE id = {paramString}id";
            result = db.ExecuteSql(sql, new Dictionary<string, object> { {"id", 2}, {"name", "RE-UPDATED" } });
            Assert.That(result, Is.EqualTo(1));

            row = db.SingleById<PocoUpdate>(2);
            Assert.That(row.Name, Is.EqualTo("RE-UPDATED"));
        }
        
        private class PocoUpdate
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void Does_Update_using_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.Update(new Person { Id = 1, FirstName = "JJ", Age = 27 }, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("id=@id"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
        }

        [Test]
        public void Does_Update_anonymous_using_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.Update<Person>(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
        }

        [Test]
        public void Does_UpdateNonDefaults_using_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.UpdateNonDefaults(new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
        }

        [Test]
        public void Does_UpdateOnly_using_Object_Dictionary()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);
                
            var fields = new Dictionary<string, object> {
                [nameof(Person.FirstName)] = "JJ",
                [nameof(Person.LastName)] = null,
            };

            db.UpdateOnly<Person>(fields, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.LastName, Is.Null);
        }

        [Test]
        public void Does_UpdateOnly_using_Object_Dictionary_containing_Id()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);
                
            var fields = new Dictionary<string, object> {
                [nameof(Person.Id)] = 1,
                [nameof(Person.FirstName)] = "JJ",
                [nameof(Person.LastName)] = null,
            };

            db.UpdateOnly<Person>(fields);

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (id = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.LastName, Is.Null);
        }

        [Test]
        public async Task Does_UpdateOnly_using_Object_Dictionary_containing_Id_Async()
        {
            using var db = await OpenDbConnectionAsync();
            db.DropAndCreateTable<Person>();
            await db.InsertAllAsync(Person.Rockstars);
                
            var fields = new Dictionary<string, object> {
                [nameof(Person.Id)] = 1,
                [nameof(Person.FirstName)] = "JJ",
                [nameof(Person.LastName)] = null,
            };

            await db.UpdateOnlyAsync<Person>(fields);

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (id = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.LastName, Is.Null);
        }

        [Test]
        public void Does_UpdateOnly_using_Object_Dictionary_and_id()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);
                
            var fields = new Dictionary<string, object> {
                [nameof(Person.FirstName)] = "JJ",
                [nameof(Person.LastName)] = null,
            };

            db.UpdateOnly<Person>(fields, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.LastName, Is.Null);
        }

        [Test]
        public async Task Does_UpdateOnly_using_Object_Dictionary_Async()
        {
            using var db = await OpenDbConnectionAsync();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);
                
            var fields = new Dictionary<string, object> {
                [nameof(Person.FirstName)] = "JJ",
                [nameof(Person.LastName)] = null,
            };

            await db.UpdateOnlyAsync<Person>(fields, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.LastName, Is.Null);
        }

        [Test]
        public void Does_UpdateOnly_using_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.UpdateOnlyFields(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
        }

        [Test]
        public void Does_UpdateOnly_using_AssignmentExpression()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.UpdateOnly(() => new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (lastname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));

            db.UpdateOnly(() => new Person { FirstName = "HH" },
                db.From<Person>().Where(p => p.LastName == "Hendrix"));
            row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("HH"));
        }

        [Test]
        public void Does_UpdateAdd_using_AssignmentExpression()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            var count = db.UpdateAdd(() => new Person { FirstName = "JJ", Age = 1 }, @where: p => p.LastName == "Hendrix");
            Assert.That(count, Is.EqualTo(1));

            var hendrix = Person.Rockstars.First(x => x.LastName == "Hendrix");
            var kurt = Person.Rockstars.First(x => x.LastName == "Cobain");

            var row = db.Single<Person>(p => p.LastName == "Hendrix");
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
            Assert.That(row.Age, Is.EqualTo(hendrix.Age + 1));

            count = db.UpdateAdd(() => new Person { FirstName = "KC", Age = hendrix.Age + 1 }, @where: p => p.LastName == "Cobain");
            Assert.That(count, Is.EqualTo(1));

            row = db.Single<Person>(p => p.LastName == "Cobain");
            Assert.That(row.FirstName, Is.EqualTo("KC"));
            Assert.That(row.Age, Is.EqualTo(kurt.Age + hendrix.Age + 1));
        }

        [Test]
        public void Does_UpdateOnly_with_SqlExpression_using_db_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.InsertAll(Person.Rockstars);

            db.UpdateOnlyFields(new Person { FirstName = "JJ" }, db.From<Person>().Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));

            var sql = db.GetLastSql().NormalizeSql();
            Assert.That(sql, Does.Contain("where (firstname = @0)"));
            Assert.That(sql, Does.Contain("firstname=@firstname"));

            var row = db.SingleById<Person>(1);
            Assert.That(row.FirstName, Is.EqualTo("JJ"));
        }


        [Test]
        public void Can_UpdateOnly_aliased_fields()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PersonWithAliasedAge>();

            db.UpdateOnly(() => new PersonWithAliasedAge { Name = "Bob", Age = 30 });
        }

        [Test]
        public void Can_UpdateOnly_fields_using_EnumAsInt()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<PersonUsingEnumAsInt>();

            db.InsertOnly(() => new PersonUsingEnumAsInt { Name = "Gene", Gender = Gender.Female });
            db.UpdateOnly(() => new PersonUsingEnumAsInt { Name = "Gene", Gender = Gender.Male });

            var saved = db.Single<PersonUsingEnumAsInt>(p => p.Name == "Gene");
            Assert.That(saved.Name, Is.EqualTo("Gene"));
            Assert.That(saved.Gender, Is.EqualTo(Gender.Male));
        }

        [Test]
        public void Can_UpdateOnly_fields_case_insensitive()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            var hendrix = new Person(1, "Jimi", "Hendrix", 27);
            db.Insert(hendrix);

            hendrix.FirstName = "JJ";
            hendrix.LastName = "Ignored";

            var q = db.From<Person>().Update(new[] { "FIRSTNAME" });

            db.UpdateOnlyFields(hendrix, q);

            var updatedRow = db.SingleById<Person>(hendrix.Id);

            Assert.That(updatedRow.FirstName, Is.EqualTo("JJ"));
            Assert.That(updatedRow.LastName, Is.EqualTo("Hendrix"));
            Assert.That(updatedRow.Age, Is.EqualTo(27));
        }

        [Test]
        public void Does_use_constant_size_string_params()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();

            var converter = db.GetDialectProvider().GetStringConverter();

            void AssertDbStringParamSizes(IDbCommand cmd)
            {
                foreach (IDbDataParameter p in cmd.Parameters)
                {
                    if (p.Value is string s)
                    {
                        //MySql sets DB Param to string length
                        Assert.That(p.Size, Is.EqualTo(converter.StringLength).Or.EqualTo(s.Length));
                    }
                }
            }
                
            var hendrix = new Person(1, "Jimi", "Hendrix", 27);
            db.Insert(hendrix, commandFilter: AssertDbStringParamSizes);

            hendrix.FirstName = "Updated";

            db.Update(hendrix, commandFilter: AssertDbStringParamSizes);

            var row = db.SingleById<Person>(hendrix.Id);
            Assert.That(row.FirstName, Is.EqualTo("Updated"));
        }

        [Test]
        public void Can_update_DefaultValue()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<DefaultValue>();
            AssertDefaultValueFieldTypeDefaultValues();

            var orig = new DefaultValue {
                Id = 1,
                Bool = true,
                NBool = false,
                Int = 2,
                NInt = 0,
                String = "A",
            };
            db.Insert(orig);
            var row = db.SingleById<DefaultValue>(1);
            AssertDefaultValues(row, orig);

            db.UpdateOnly<DefaultValue>(new Dictionary<string, object> {
                [nameof(DefaultValue.Id)] = 1,
                [nameof(DefaultValue.Bool)] = false,
                [nameof(DefaultValue.NBool)] = null,
                [nameof(DefaultValue.Int)] = 0,
                [nameof(DefaultValue.NInt)] = null,
                [nameof(DefaultValue.String)] = null,
            });
            
            row = db.SingleById<DefaultValue>(1);
            AssertDefaultValues(row, new DefaultValue {
                Id = 1,
                Bool = false,
                NBool = null,
                Int = 0,
                NInt = null,
                String = null,
            });

            db.UpdateOnly<DefaultValue>(new Dictionary<string, object> {
                [nameof(DefaultValue.Id)] = 1,
                [nameof(DefaultValue.Bool)] = true,
                [nameof(DefaultValue.NBool)] = false,
                [nameof(DefaultValue.Int)] = 1,
                [nameof(DefaultValue.NInt)] = 0,
                [nameof(DefaultValue.String)] = "",
            });
            
            row = db.SingleById<DefaultValue>(1);
            AssertDefaultValues(row, new DefaultValue {
                Id = 1,
                Bool = true,
                NBool = false,
                Int = 1,
                NInt = 0,
                String = "",
            });
        }

        [Test]
        public async Task Can_update_DefaultValue_Async()
        {
            using var db = await OpenDbConnectionAsync();
            db.DropAndCreateTable<DefaultValue>();
            AssertDefaultValueFieldTypeDefaultValues();

            var orig = new DefaultValue {
                Id = 1,
                Bool = true,
                NBool = false,
                Int = 2,
                NInt = 0,
                String = "A",
            };
            await db.InsertAsync(orig);
            var row = await db.SingleByIdAsync<DefaultValue>(1);
            AssertDefaultValues(row, orig);

            db.UpdateOnly<DefaultValue>(new Dictionary<string, object> {
                [nameof(DefaultValue.Id)] = 1,
                [nameof(DefaultValue.Bool)] = false,
                [nameof(DefaultValue.NBool)] = null,
                [nameof(DefaultValue.Int)] = 0,
                [nameof(DefaultValue.NInt)] = null,
                [nameof(DefaultValue.String)] = null,
            });
            
            row = await db.SingleByIdAsync<DefaultValue>(1);
            AssertDefaultValues(row, new DefaultValue {
                Id = 1,
                Bool = false,
                NBool = null,
                Int = 0,
                NInt = null,
                String = null,
            });

            await db.UpdateOnlyAsync<DefaultValue>(new Dictionary<string, object> {
                [nameof(DefaultValue.Id)] = 1,
                [nameof(DefaultValue.Bool)] = true,
                [nameof(DefaultValue.NBool)] = false,
                [nameof(DefaultValue.Int)] = 1,
                [nameof(DefaultValue.NInt)] = 0,
                [nameof(DefaultValue.String)] = "",
            });
            
            row = db.SingleById<DefaultValue>(1);
            AssertDefaultValues(row, new DefaultValue {
                Id = 1,
                Bool = true,
                NBool = false,
                Int = 1,
                NInt = 0,
                String = "",
            });
        }

        private static void AssertDefaultValueFieldTypeDefaultValues()
        {
            var modelDef = typeof(DefaultValue).GetModelMetadata();
            Assert.That(modelDef.GetFieldDefinition(nameof(DefaultValue.Int)).FieldTypeDefaultValue,
                Is.EqualTo(default(int)));
            Assert.That(modelDef.GetFieldDefinition(nameof(DefaultValue.NInt)).FieldTypeDefaultValue,
                Is.EqualTo(default(int?)));
            Assert.That(modelDef.GetFieldDefinition(nameof(DefaultValue.Bool)).FieldTypeDefaultValue,
                Is.EqualTo(default(bool)));
            Assert.That(modelDef.GetFieldDefinition(nameof(DefaultValue.NBool)).FieldTypeDefaultValue,
                Is.EqualTo(default(bool?)));
            Assert.That(modelDef.GetFieldDefinition(nameof(DefaultValue.String)).FieldTypeDefaultValue,
                Is.EqualTo(default(string)));
        }

        private void AssertDefaultValues(DefaultValue row, DefaultValue orig)
        {
            Assert.That(row.Id, Is.EqualTo(orig.Id));
            Assert.That(row.Bool, Is.EqualTo(orig.Bool));
            Assert.That(row.NBool, Is.EqualTo(orig.NBool));
            Assert.That(row.Int, Is.EqualTo(orig.Int));
            Assert.That(row.NInt, Is.EqualTo(orig.NInt));
            Assert.That(row.String, Is.EqualTo(orig.String));
        }
        
    }

    [CompositeIndex("FirstName", "LastName")]
    public class SomeBlobs
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[] Blob1 { get; set; }
        public byte[] Blob2 { get; set; }
    }

    public class Shutdown
    {
        public int Id { get; set; }
        public bool? IsShutdownGraceful { get; set; }
    }
    
    public class DefaultValue
    {
        public int Id { get; set; }
        public int Int { get; set; }
        public int? NInt { get; set; }
        public bool Bool { get; set; }
        public bool? NBool { get; set; }
        public string String { get; set; }
    }

}
