using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class JoinAliasIntIssue : OrmLiteProvidersTestBase
{
    public JoinAliasIntIssue(DialectContext context) : base(context) {}

    class Team
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? TeamLeaderId { get; set; }
    }

    class Teamuser
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int TeamId { get; set; }
    }

    [Test]
    public void Can_create_query_with_int_JoinAlias()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Teamuser>();
            db.DropAndCreateTable<Team>();

            db.InsertAll(new[] {
                new Team
                {
                    Id = 1,
                    Name = "Team 1"
                },
            });

            db.InsertAll([
                new Teamuser
                {
                    Id = 1,
                    Name = "User 1",
                    TeamId = 1
                },
                new Teamuser
                {
                    Id = 2,
                    Name = "User 2",
                    TeamId = 1
                }
            ]);

            db.UpdateOnlyFields(new Team { TeamLeaderId = 1 }, 
                onlyFields: x => x.TeamLeaderId, 
                where: x => x.Id == 1);

            var q = db.From<Team>();
            q.Join<Teamuser>((t, u) => t.Id == u.TeamId, db.JoinAlias("Teamuser"));
            q.Join<Teamuser>((t, u) => t.TeamLeaderId == u.Id, db.JoinAlias("Leader"));
            q.Where<Team, Teamuser>((t, u) => t.Id == Sql.JoinAlias(u.TeamId, "Leader"));
            q.Where<Teamuser>(u => Sql.JoinAlias(u.Id, "Leader") == 1);
            q.Where<Team, Teamuser>((t, u) => Sql.JoinAlias(t.Id, q.DialectProvider.GetQuotedTableName(ModelDefinition<Team>.Definition)) == Sql.JoinAlias(u.TeamId, "Leader")); // Workaround, but only works for fields, not constants
            q.Where<Team, Teamuser>((user, leader) => Sql.JoinAlias(user.Id, "Teamuser") < Sql.JoinAlias(leader.Id, "Leader"));
            q.Select<Team, Teamuser, Teamuser>((t, u, l) => new
            {
                TeamName = Sql.As(t.Name, "TeamName"),
                UserName = Sql.As(u.Name, "UserName"),
                LeaderName = Sql.As(l.Name, "LeaderName")
            });

            var results = db.Select<dynamic>(q);
        }
    }

    [Test]
    [IgnoreDialect(Dialect.MySql,"Needs review - MONOREPO")]
    public void Can_create_query_with_int_TableAlias()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Teamuser>();
            db.DropAndCreateTable<Team>();

            db.InsertAll(new[] {
                new Team
                {
                    Id = 1,
                    Name = "Team 1"
                },
            });

            db.InsertAll(new[]
            {
                new Teamuser
                {
                    Id = 1,
                    Name = "User 1",
                    TeamId = 1
                },
                new Teamuser
                {
                    Id = 2,
                    Name = "User 2",
                    TeamId = 1
                },
            });

            db.UpdateOnlyFields(new Team { TeamLeaderId = 1 }, 
                onlyFields: x => x.TeamLeaderId, 
                where: x => x.Id == 1);

            var q = db.From<Team>();
            q.Join<Teamuser>((t, u) => t.Id == u.TeamId, db.TableAlias("tu"));
            q.Join<Teamuser>((t, u) => t.TeamLeaderId == u.Id, db.TableAlias("leader"));
            q.Where<Team, Teamuser>((t, u) => t.Id == Sql.TableAlias(u.TeamId, "Leader"));
            q.Where<Teamuser>(u => Sql.TableAlias(u.Id, "leader") == 1);
            q.Where<Team, Teamuser>((t, u) => Sql.TableAlias(t.Id, q.DialectProvider.GetQuotedTableName(ModelDefinition<Team>.Definition)) == Sql.TableAlias(u.TeamId, "leader")); // Workaround, but only works for fields, not constants
            q.Where<Team, Teamuser>((user, leader) => Sql.TableAlias(user.Id, "tu") < Sql.TableAlias(leader.Id, "leader"));
            q.Select<Team, Teamuser, Teamuser>((t, u, l) => new
            {
                TeamName = t.Name,
                UserName = Sql.TableAlias(u.Name, "tu"),
                LeaderName = Sql.TableAlias(l.Name, "leader"),
            });

            var results = db.Select<dynamic>(q);
        }
    }

}