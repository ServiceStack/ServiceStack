using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Async;

namespace ServiceStack.OrmLite.Tests
{
    public class EnsureTests : OrmLiteTestBase
    {
        void InitRockstars(IDbConnection db)
        {
            db.DropAndCreateTable<Rockstar>();
            db.InsertAll(AutoQueryTests.SeedRockstars);
            // OrmLiteUtils.PrintSql();
        }
        
        [Test]
        public void Can_pre_Ensure_sql_filter()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(1));
            }

            var q = db.From<Rockstar>();
            q.Ensure("Id = {0}", 1);

            assertEnsure(q);

            q.Where(x => x.Age == 27);

            assertEnsure(q);

            q.Or(x => x.LivingStatus == LivingStatus.Dead);

            assertEnsure(q);
        }
        
        [Test]
        public void Can_pre_Ensure_typed_filter()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(1));
            }

            var q = db.From<Rockstar>();
            q.Ensure(x => x.Id == 1);

            assertEnsure(q);

            q.Where(x => x.Age == 27);

            assertEnsure(q);

            q.Or(x => x.LivingStatus == LivingStatus.Dead);

            assertEnsure(q);
        }

        [Test]
        public void Can_post_Ensure()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(1));
            }

            var q = db.From<Rockstar>();

            q.Where(x => x.Age == 27)
                .Or(x => x.LivingStatus == LivingStatus.Dead);

            q.Ensure(x => x.Id == 1);
            assertEnsure(q);
        }

        [Test]
        public void Can_post_Ensure_joined_table()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);
            db.DropAndCreateTable<RockstarAlbum>();
            db.InsertAll(AutoQueryTests.SeedAlbums);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(3));
            }

            var q = db
                .From<Rockstar>()
                .Join<RockstarAlbum>((r,a) => r.Id == a.RockstarId);

            q.Where(x => x.Age == 27)
                .Or(x => x.LivingStatus == LivingStatus.Dead);

            q.Ensure<RockstarAlbum>(x => x.Name == "Nevermind");
            assertEnsure(q);
        }

        [Test]
        public void Can_post_Ensure_multi_tables()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);
            db.DropAndCreateTable<RockstarAlbum>();
            db.InsertAll(AutoQueryTests.SeedAlbums);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(3));
            }

            var q = db
                .From<Rockstar>()
                .Join<RockstarAlbum>((r,a) => r.Id == a.RockstarId);

            q.Where(x => x.Age == 27)
                .Or(x => x.LivingStatus == LivingStatus.Dead);

            q.Ensure<Rockstar,RockstarAlbum>((r,a) => a.Name == "Nevermind" && r.Id == a.RockstarId);
            assertEnsure(q);
        }

        [Test]
        public void Can_pre_Ensure_multi_tables()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);
            db.DropAndCreateTable<RockstarAlbum>();
            db.InsertAll(AutoQueryTests.SeedAlbums);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(3));
            }

            var q = db
                .From<Rockstar>()
                .Join<RockstarAlbum>((r,a) => r.Id == a.RockstarId);

            q.Ensure<Rockstar,RockstarAlbum>((r,a) => a.Name == "Nevermind" && r.Id == a.RockstarId);

            q.Where(x => x.Age == 27)
                .Or(x => x.LivingStatus == LivingStatus.Dead);

            assertEnsure(q);
        }

        [Test]
        public void Can_pre_multi_Ensure_and_tables()
        {
            using var db = OpenDbConnection();
            InitRockstars(db);
            db.DropAndCreateTable<RockstarAlbum>();
            db.InsertAll(AutoQueryTests.SeedAlbums);

            void assertEnsure(SqlExpression<Rockstar> q)
            {
                var rows = db.Select(q);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].Id, Is.EqualTo(3));
            }

            var q = db
                .From<Rockstar>()
                .Join<RockstarAlbum>((r,a) => r.Id == a.RockstarId);

            q.Ensure<Rockstar,RockstarAlbum>((r,a) => a.Name == "Nevermind" && r.Id == a.RockstarId);

            q.Where(x => x.Age == 27)
                .Or(x => x.LivingStatus == LivingStatus.Dead);

            q.Ensure(x => x.Id == 3);

            assertEnsure(q);
        }
        
        [Test]
        public void Ensure_does_use_aliases()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<ModelWithAlias>();

            db.Insert(new ModelWithAlias {
                IntField = 1,
            });

            var q = db.From<ModelWithAlias>()
                .Ensure(x => x.IntField > 0);

            var results = db.Select(q);
            Assert.That(results.Count, Is.EqualTo(1));
        }
    }
}