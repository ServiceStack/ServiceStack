using System;
using NpgsqlTypes;
using NUnit.Framework;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    public class PgSqlTests
    {
        [Test]
        public void Can_create_NpgsqlParameter()
        {
            Assert.That(PgSql.Param("p", 1).NpgsqlDbType, Is.EqualTo(NpgsqlDbType.Integer));
            Assert.That(PgSql.Param("p", "s").NpgsqlDbType, Is.EqualTo(NpgsqlDbType.Text));
            Assert.That(PgSql.Param("p", 'c').NpgsqlDbType, Is.EqualTo(NpgsqlDbType.Char));
            Assert.That(PgSql.Param("p", new [] { 1 }).NpgsqlDbType, 
                Is.EqualTo(NpgsqlDbType.Integer | NpgsqlDbType.Array));
        }

        [Test]
        public void Does_PgSqlArray()
        {
            Assert.That(PgSql.Array((string[])null), Is.EqualTo("ARRAY[]"));
            Assert.That(PgSql.Array(Array.Empty<string>()), Is.EqualTo("ARRAY[]"));
            Assert.That(PgSql.Array(Array.Empty<int>()), Is.EqualTo("ARRAY[]"));
            Assert.That(PgSql.Array(1,2,3), Is.EqualTo("ARRAY[1,2,3]"));
            Assert.That(PgSql.Array("A","B","C"), Is.EqualTo("ARRAY['A','B','C']"));
            Assert.That(PgSql.Array("A'B","C\"D"), Is.EqualTo("ARRAY['A''B','C\"D']"));
            
            Assert.That(PgSql.Array(Array.Empty<string>(), nullIfEmpty:true), Is.EqualTo("null"));
            Assert.That(PgSql.Array(new[]{"A","B","C"}, nullIfEmpty:true), Is.EqualTo("ARRAY['A','B','C']"));
            
            Assert.That(PgSql.Array(new int?[]{ 1,null,3 }), Is.EqualTo("ARRAY[1,NULL,3]"));
            Assert.That(PgSql.Array("A",null,"C"), Is.EqualTo("ARRAY['A',NULL,'C']"));
        }
    }
}