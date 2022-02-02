using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Expression
{
    public class GenericEntity
    {
        public int Id { get; set; }

        [Alias("COL_A")]
        public string ColumnA { get; set; }
    }

    [TestFixtureOrmLite]
    public class GenericTableExpressions : OrmLiteProvidersTestBase
    {
        public GenericTableExpressions(DialectContext context) : base(context) {}

        [Test]
        public void Can_change_table_at_runtime()
        {
            const string tableName = "Entity1";
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<GenericEntity>(tableName);

                db.Insert(tableName, new GenericEntity { Id = 1, ColumnA = "A" });

                var rows = db.Select(tableName, db.From<GenericEntity>()
                    .Where(x => x.ColumnA == "A"));

                Assert.That(rows.Count, Is.EqualTo(1));

                db.Update(tableName, new GenericEntity { ColumnA = "B" },
                    where: q => q.ColumnA == "A");

                rows = db.Select(tableName, db.From<GenericEntity>()
                    .Where(x => x.ColumnA == "B"));

                Assert.That(rows.Count, Is.EqualTo(1));
            }
        }
    }

    public static class GenericTableExtensions
    {
        static object ExecWithAlias<T>(string table, Func<object> fn)
        {
            var modelDef = typeof(T).GetModelMetadata();
            lock (modelDef)
            {
                var hold = modelDef.Alias;
                try
                {
                    modelDef.Alias = table;
                    return fn();
                }
                finally
                {
                    modelDef.Alias = hold;
                }
            }
        }

        public static void DropAndCreateTable<T>(this IDbConnection db, string table)
        {
            ExecWithAlias<T>(table, () => { 
                db.DropAndCreateTable<T>();
                return null;
            });
        }

        public static long Insert<T>(this IDbConnection db, string table, T obj, bool selectIdentity = false)
        {
            return (long)ExecWithAlias<T>(table, () => db.Insert(obj, selectIdentity));
        }

        public static List<T> Select<T>(this IDbConnection db, string table, SqlExpression<T> expression)
        {
            return (List<T>)ExecWithAlias<T>(table, () => db.Select(expression));
        }

        public static int Update<T>(this IDbConnection db, string table, T item, Expression<Func<T, bool>> where)
        {
            return (int)ExecWithAlias<T>(table, () => db.Update(item, where));
        }
    }
}