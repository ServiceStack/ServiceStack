#if !NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data.Common;
using ServiceStack.OrmLite.Dapper;

namespace ServiceStack.MiniProfiler.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="Profiler"/> to a MSSQL database.
    /// </summary>
    public class SqlServerStorage : DatabaseStorageBase
    {
        /// <summary>
        /// Returns a new <see cref="SqlServerStorage"/>.
        /// </summary>
        public SqlServerStorage(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Stores <param name="profiler"/> to dbo.MiniProfilers under its <see cref="Profiler.Id"/>; 
        /// stores all child Timings and SqlTimings to their respective tables.
        /// </summary>
        public override void Save(Profiler profiler)
        {
            const string sql =
@"insert into MiniProfilers
            (Id,
             Name,
             Started,
             MachineName,
             [User],
             Level,
             RootTimingId,
             DurationMilliseconds,
             DurationMillisecondsInSql,
             HasSqlTimings,
             HasDuplicateSqlTimings,
             HasTrivialTimings,
             HasAllTrivialTimings,
             TrivialDurationThresholdMilliseconds,
             HasUserViewed,
             Json)
select       @Id,
             @Name,
             @Started,
             @MachineName,
             @User,
             @Level,
             @RootTimingId,
             @DurationMilliseconds,
             @DurationMillisecondsInSql,
             @HasSqlTimings,
             @HasDuplicateSqlTimings,
             @HasTrivialTimings,
             @HasAllTrivialTimings,
             @TrivialDurationThresholdMilliseconds,
             @HasUserViewed,
             @Json
where not exists (select 1 from MiniProfilers where Id = @Id)"; // this syntax works on both mssql and sqlite

            using (var conn = GetOpenConnection())
            {
                var insertCount = conn.Execute(sql, new
                {
                    Id = profiler.Id,
                    Name = profiler.Name.Truncate(200),
                    Started = profiler.Started,
                    MachineName = profiler.MachineName.Truncate(100),
                    User = profiler.User.Truncate(100),
                    Level = profiler.Level,
                    RootTimingId = profiler.Root.Id,
                    DurationMilliseconds = profiler.DurationMilliseconds,
                    DurationMillisecondsInSql = profiler.DurationMillisecondsInSql,
                    HasSqlTimings = profiler.HasSqlTimings,
                    HasDuplicateSqlTimings = profiler.HasDuplicateSqlTimings,
                    HasTrivialTimings = profiler.HasTrivialTimings,
                    HasAllTrivialTimings = profiler.HasAllTrivialTimings,
                    TrivialDurationThresholdMilliseconds = profiler.TrivialDurationThresholdMilliseconds,
                    HasUserViewed = profiler.HasUserViewed,
                    Json = profiler.Root.ToJson()
                });
            }
        }

        private static readonly Dictionary<Type, string> LoadSqlStatements = new Dictionary<Type, string>
        {
            { typeof(Profiler), "select * from MiniProfilers where Id = @id" }
        };

        private static readonly string LoadSqlBatch = string.Join("\n", LoadSqlStatements.Select(pair => pair.Value).ToArray());

        /// <summary>
        /// Loads the MiniProfiler identifed by 'id' from the database.
        /// </summary>
        public override Profiler Load(Guid id)
        {
            using (var conn = GetOpenConnection())
            {
                var idParameter = new { id };
                var result = LoadIndividually(conn, idParameter);

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);

                    // loading a profiler means we've viewed it
                    if (!result.HasUserViewed)
                    {
                        conn.Execute("update MiniProfilers set HasUserViewed = 1 where Id = @id", idParameter);
                    }
                }

                return result;
            }
        }

        private Profiler LoadIndividually(DbConnection conn, object idParameter)
        {
            var result = LoadFor<Profiler>(conn, idParameter).SingleOrDefault();

            if (result != null)
            {
                if (!String.IsNullOrWhiteSpace(result.Json))
                {
                    result.Root = result.Json.FromJson<Timing>();
                }
            }

            return result;
        }

        private List<T> LoadFor<T>(DbConnection conn, object idParameter)
        {
            return conn.Query<T>(LoadSqlStatements[typeof(T)], idParameter).ToList();
        }


        /// <summary>
        /// Returns a list of <see cref="Profiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="Profiler.Settings.UserProvider"/>.</param>
        public override List<Guid> GetUnviewedIds(string user)
        {
            const string sql =
@"select Id
from   MiniProfilers
where  [User] = @user
and    HasUserViewed = 0
order  by Started";

            using (var conn = GetOpenConnection())
            {
                return conn.Query<Guid>(sql, new { user }).ToList();
            }
        }

        /// <summary>
        /// Returns a connection to Sql Server.
        /// </summary>
        protected override DbConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Creates needed tables. Run this once on your database.
        /// </summary>
        /// <remarks>
        /// Works in sql server and sqlite (with documented removals).
        /// </remarks>
        public const string TableCreationScript =
@"create table MiniProfilers
  (
     RowId                                integer not null identity constraint PK_MiniProfilers primary key clustered, -- Need a clustered primary key for SQL Azure
     Id                                   uniqueidentifier not null, -- don't cluster on a guid
     Name                                 nvarchar(200) not null,
     Started                              datetime not null,
     MachineName                          nvarchar(100) null,
     [User]                               nvarchar(100) null,
     Level                                tinyint null,
     RootTimingId                         uniqueidentifier null,
     DurationMilliseconds                 decimal(7, 1) not null,
     DurationMillisecondsInSql            decimal(7, 1) null,
     HasSqlTimings                        bit not null,
     HasDuplicateSqlTimings               bit not null,
     HasTrivialTimings                    bit not null,
     HasAllTrivialTimings                 bit not null,
     TrivialDurationThresholdMilliseconds decimal(5, 1) null,
     HasUserViewed                        bit not null,
     Json                                 nvarchar(max)
  );
  -- displaying results selects everything based on the main MiniProfilers.Id column
  create unique nonclustered index IX_MiniProfilers_Id on MiniProfilers (Id);
                
  -- speeds up a query that is called on every .Stop()
  create nonclustered index IX_MiniProfilers_User_HasUserViewed_Includes on MiniProfilers ([User], HasUserViewed) include (Id, [Started]);
";
    }

    public static class MiniProfilerExt
    {
        public static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }
    }
}


#endif
