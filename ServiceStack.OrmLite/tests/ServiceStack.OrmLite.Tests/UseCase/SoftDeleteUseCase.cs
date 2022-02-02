using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }

    public class ModelWithSoftDelete : ISoftDelete
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class ModelWithSoftDeleteJoin : ISoftDelete
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }

    public static class SqlExpressionExtensions
    {
        public static SqlExpression<T> OnlyActive<T>(this SqlExpression<T> q)
            where T : ISoftDelete
        {
            return q.Where(x => x.IsDeleted != true);
        }
    }
    
    [TestFixtureOrmLite]
    public class SoftDeleteUseCase : OrmLiteProvidersTestBase
    {
        public SoftDeleteUseCase(DialectContext context) : base(context) {}

        [Test]
        public void Can_add_generic_soft_delete_filter_to_SqlExpression()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithSoftDelete>();

                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });

                var results = db.Select(db.From<ModelWithSoftDelete>().OnlyActive());

                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].Name, Is.EqualTo("foo"));

                var result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "foo").OnlyActive());
                Assert.That(result.Name, Is.EqualTo("foo"));
                result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "bar").OnlyActive());
                Assert.That(result, Is.Null);
            }
        }

        [Test]
        public void Can_add_generic_soft_delete_filter_to_SqlExpression_using_SelectFilter()
        {
            using (var db = OpenDbConnection())
            {
                SqlExpression<ModelWithSoftDelete>.SelectFilter = q => q.Where(x => x.IsDeleted != true);

                db.DropAndCreateTable<ModelWithSoftDelete>();

                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });

                var results = db.Select(db.From<ModelWithSoftDelete>());

                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].Name, Is.EqualTo("foo"));

                results = db.Select<ModelWithSoftDelete>(x => x.Id > 0);
                Assert.That(results.Count, Is.EqualTo(1));

                var result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "foo"));
                Assert.That(result.Name, Is.EqualTo("foo"));
                result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "bar"));
                Assert.That(result, Is.Null);

                result = db.Single<ModelWithSoftDelete>(x => x.Name == "bar");
                Assert.That(result, Is.Null);

                SqlExpression<ModelWithSoftDelete>.SelectFilter = null;
            }
        }

        [Test]
        public void Can_get_RowCount_with_generic_soft_delete_filter_using_SelectFilter()
        {
            using (var db = OpenDbConnection())
            {
                SqlExpression<ModelWithSoftDelete>.SelectFilter = q => q.Where(x => x.IsDeleted != true);

                db.DropAndCreateTable<ModelWithSoftDelete>();

                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });

                Assert.DoesNotThrow(() =>
                {
                    var count = db.RowCount(db.From<ModelWithSoftDelete>());
                });

                SqlExpression<ModelWithSoftDelete>.SelectFilter = null;
            }
        }

        [Test]
        public void Can_add_generic_soft_delete_filter_to_SqlExpression_using_SqlExpressionSelectFilter()
        {
            using (var db = OpenDbConnection())
            {
                OrmLiteConfig.SqlExpressionSelectFilter = q =>
                {
                    if (q.ModelDef.ModelType.HasInterface(typeof(ISoftDelete)))
                    {
                        q.Where<ISoftDelete>(x => x.IsDeleted != true);
                    }
                };

                db.DropAndCreateTable<ModelWithSoftDelete>();
                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });

                db.DropTable<Table3>();
                db.DropTable<Table2>();
                db.DropTable<Table1>();
                db.CreateTable<Table1>();
                db.Insert(new Table1 { Id = 1, String = "foo" });
                db.Insert(new Table1 { Id = 2, String = "bar" });

                var results = db.Select(db.From<ModelWithSoftDelete>());

                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0].Name, Is.EqualTo("foo"));

                results = db.Select<ModelWithSoftDelete>(x => x.Id > 0);
                Assert.That(results.Count, Is.EqualTo(1));

                var result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "foo"));
                Assert.That(result.Name, Is.EqualTo("foo"));
                result = db.Single(db.From<ModelWithSoftDelete>().Where(x => x.Name == "bar"));
                Assert.That(result, Is.Null);

                result = db.Single<ModelWithSoftDelete>(x => x.Name == "bar");
                Assert.That(result, Is.Null);

                result = db.Single(db.From<ModelWithSoftDelete>()
                    .Join<Table1>((m, t) => m.Name == t.String)
                    .Where(x => x.Name == "foo"));
                Assert.That(result.Name, Is.EqualTo("foo"));

                result = db.Single(db.From<ModelWithSoftDelete>()
                    .Join<Table1>((m, t) => m.Name == t.String)
                    .Where(x => x.Name == "bar"));
                Assert.That(result, Is.Null);

                OrmLiteConfig.SqlExpressionSelectFilter = null;
            }
        }

        [Test]
        public void Can_get_RowCount_with_generic_soft_delete_filter_using_SqlExpressionSelectFilter()
        {
            using (var db = OpenDbConnection())
            {
                OrmLiteConfig.SqlExpressionSelectFilter = q =>
                {
                    if (q.ModelDef.ModelType.HasInterface(typeof(ISoftDelete)))
                    {
                        q.Where<ISoftDelete>(x => x.IsDeleted != true);
                    }
                };

                db.DropAndCreateTable<ModelWithSoftDelete>();

                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });

                Assert.DoesNotThrow(() =>
                {
                    var count = db.RowCount(db.From<ModelWithSoftDelete>());
                });

                OrmLiteConfig.SqlExpressionSelectFilter = null;
            }
        }

        [Test]
        public void Can_use_interface_condition_on_table_with_join()
        {
            using (var db = OpenDbConnection())
            {
                OrmLiteConfig.SqlExpressionSelectFilter = q =>
                {
                    if (q.ModelDef.ModelType.HasInterface(typeof(ISoftDelete)))
                    {
                        q.Where<ISoftDelete>(x => x.IsDeleted != true);
                    }
                };

                db.DropAndCreateTable<ModelWithSoftDelete>();
                db.Insert(new ModelWithSoftDelete { Name = "foo" });
                db.Insert(new ModelWithSoftDelete { Name = "bar", IsDeleted = true });
                db.DropAndCreateTable<ModelWithSoftDeleteJoin>();
                db.Insert(new ModelWithSoftDeleteJoin { Name = "foo" });
                db.Insert(new ModelWithSoftDeleteJoin { Name = "bar", IsDeleted = true });

                var result = db.Single(db.From<ModelWithSoftDelete>()
                    .Join<ModelWithSoftDeleteJoin>((m, j) => m.Name == j.Name)
                    .Where(x => x.Name == "foo"));
                Assert.That(result.Name, Is.EqualTo("foo"));

                result = db.Single(db.From<ModelWithSoftDelete>()
                    .Join<ModelWithSoftDeleteJoin>((m, j) => m.Name == j.Name)
                    .Where(x => x.Name == "bar"));
                Assert.That(result, Is.Null);

                OrmLiteConfig.SqlExpressionSelectFilter = null;
            }
        }
    }
}