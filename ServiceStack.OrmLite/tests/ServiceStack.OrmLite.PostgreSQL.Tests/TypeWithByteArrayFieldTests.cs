using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class TypeWithByteArrayFieldTests : OrmLiteProvidersTestBase
    {
        public TypeWithByteArrayFieldTests(DialectContext context) : base(context) {}

        TypeWithByteArrayField getSampleObject()
        {
            var testByteArray = new byte[256];
            for(int i = 0; i < 256; i++) { testByteArray[i] = (byte)i; }
            
            return new TypeWithByteArrayField { Id = 1, Content = testByteArray };
        }

        [Test]
        public void CanInsertAndSelectByteArray()
        {
            var orig = getSampleObject();

            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithByteArrayField>(true);

                db.Save(orig);

                var target = db.SingleById<TypeWithByteArrayField>(orig.Id);

                Assert.AreEqual(orig.Id, target.Id);
                Assert.AreEqual(orig.Content, target.Content);
            }
        }

        [Test]
        [NonParallelizable]
        public void CanInsertAndSelectByteArray__manual_insert__manual_select()
        {
            var orig = getSampleObject();

            using(var db = OpenDbConnection()) {
                //insert and select manually - ok
                db.CreateTable<TypeWithByteArrayField>(true);
                _insertManually(orig, db);

                _selectAndVerifyManually(orig, db);
            }
        }

        [Test]
        public void CanInsertAndSelectByteArray__InsertParam_insert__manual_select()
        {
            var orig = getSampleObject();

            using(var db = OpenDbConnection()) {
                //insert using InsertParam, and select manually - ok
                db.CreateTable<TypeWithByteArrayField>(true);
                db.Insert(orig);

                _selectAndVerifyManually(orig, db);
            }
        }

        [Test]
        public void CanInsertAndSelectByteArray__InsertParam_insert__GetById_select()
        {
            var orig = getSampleObject();

            using(var db = OpenDbConnection()) {
                //InsertParam + GetByID - fails
                db.CreateTable<TypeWithByteArrayField>(true);
                db.Insert(orig);

                var target = db.SingleById<TypeWithByteArrayField>(orig.Id);

                Assert.AreEqual(orig.Id, target.Id);
                Assert.AreEqual(orig.Content, target.Content);
            }
        }

        [Test]
        public void CanInsertAndSelectByteArray__Insert_insert__GetById_select()
        {
            var orig = getSampleObject();

            using(var db = OpenDbConnection()) {
                //InsertParam + GetByID - fails
                db.CreateTable<TypeWithByteArrayField>(true);
                db.Insert(orig);

                var target = db.SingleById<TypeWithByteArrayField>(orig.Id);

                Assert.AreEqual(orig.Id, target.Id);
                Assert.AreEqual(orig.Content, target.Content);
            }
        }

        [Test]
        public void CanInsertAndSelectByteArray__Insert_insert__manual_select()
        {
            var orig = getSampleObject();

            using(var db = OpenDbConnection()) {
                //InsertParam + GetByID - fails
                db.CreateTable<TypeWithByteArrayField>(true);
                db.Insert(orig);

                _selectAndVerifyManually(orig, db);
            }
        }

        private static void _selectAndVerifyManually(TypeWithByteArrayField orig, System.Data.IDbConnection db)
        {
            using(var cmd = db.CreateCommand()) {
                cmd.CommandText = @"select ""content"" from ""type_with_byte_array_field"" where ""id"" = 1 --manual select";
                using(var reader = cmd.ExecuteReader()) {
                    reader.Read();
                    var ba = reader["content"] as byte[];
                    Assert.AreEqual(orig.Content.Length, ba.Length);
                    Assert.AreEqual(orig.Content, ba);
                }
            }
        }

        private static void _insertManually(TypeWithByteArrayField orig, System.Data.IDbConnection db)
        {
            using(var cmd = db.CreateCommand()) {
                cmd.CommandText = @"INSERT INTO ""type_with_byte_array_field"" (""id"",""content"") VALUES (@Id, @Content) --manual parameterized insert";

                var p_id = cmd.CreateParameter();
                p_id.ParameterName = "@Id";
                p_id.Value = orig.Id;

                cmd.Parameters.Add(p_id);

                var p_content = cmd.CreateParameter();
                p_content.ParameterName = "@Content";
                p_content.Value = orig.Content;

                cmd.Parameters.Add(p_content);

                cmd.ExecuteNonQuery();
            }
        }
    }

    class TypeWithByteArrayField
    {
        public int Id { get; set; }
        public byte[] Content { get; set; }
    }
}