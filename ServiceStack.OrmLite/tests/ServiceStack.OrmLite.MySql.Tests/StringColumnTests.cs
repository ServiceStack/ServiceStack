using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite.MySql.DataAnnotations;

namespace ServiceStack.OrmLite.MySql.Tests
{
    [TestFixture]
    public class StringColumnTests
        : OrmLiteTestBase
    {
        [Test]
        public void Can_create_primary_key_varchar_with_string_length_255()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TypeWithStringId_255>(true);
        }

        [Test]
        public void Can_create_primary_key_varchar_without_setting_string_length()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TypeWithStringId>(true);
        }

        [Test]
        public void Can_create_unique_key_on_varchar_without_setting_string_length()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TypeWithUniqeKeyOnVarchar>(true);
        }

        [Test]
        public void Can_store_and_retrieve_string_with_8000_characters_from_varchar_field()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TypeWithStringId>(true);

            var obj = new TypeWithStringId {
                Id = "a",
                Value = CreateString(8000)
            };

            Assert.AreEqual(8000, obj.Value.Length);

            db.Save(obj);
            var target = db.SingleById<TypeWithStringId>(obj.Id);

            Assert.AreEqual(obj.Value, target.Value);
            Assert.AreEqual(8000, obj.Value.Length);
        }

        [Test]
        public void Can_store_and_retrieve_string_with_8000_characters_from_text_field()
        {
            using var db = OpenDbConnection();
            db.CreateTable<TypeWithTextField>(true);

            var obj = new TypeWithTextField() {
                Value = CreateString(8000)
            };

            Assert.AreEqual(8000, obj.Value.Length);

            db.Save(obj);
            obj.Id = (int)db.LastInsertId();

            var target = db.SingleById<TypeWithTextField>(obj.Id);

            Assert.AreEqual(obj.Value, target.Value);
            Assert.AreEqual(8000, obj.Value.Length);
        }

        class TypeWithUniqeKeyOnVarchar
        {
            public int Id { get; set; }

            [Index(true)]
            public string Value { get; set; }
        }

        class TypeWithUniqeKeyOnVarchar_256
        {
            public int Id { get; set; }

            [StringLength(256)]
            [Index(true)]
            public string Value { get; set; }
        }

        class TypeWithStringId : IHasStringId
        {
            public string Id { get; set; }
            [StringLength(8000)]
            public string Value { get; set; }
        }

        class TypeWithUniqueIndexOnTextField
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Index(true)]
            [Text]
            public string Value { get; set; }
        }

        class TypeWithTextField
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Text]
            public string Value { get; set; }
        }

        class TypeWithStringId_255 : IHasStringId
        {
            [StringLength(255)]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        class TypeWithStringId_256 : IHasStringId
        {
            [StringLength(256)]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        private static string CreateString(int length)
        {
            const string loremIpsum =
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";

            var retVal = "";

            for (int i = 0; i < length / loremIpsum.Length; i++)
                retVal += loremIpsum;

            return retVal + loremIpsum.Substring(0, length - retVal.Length);
        }
    }
}
