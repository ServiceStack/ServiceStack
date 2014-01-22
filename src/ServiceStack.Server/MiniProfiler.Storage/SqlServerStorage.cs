#if true

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
        /// A full install of Sql Server can return multiple result sets in one query, allowing the use of <see cref="SqlMapper.QueryMultiple"/>.
        /// However, Sql Server CE and Sqlite cannot do this, so inheritors for those providers can return false here.
        /// </summary>
        public virtual bool EnableBatchSelects { get { return true; } }

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
             HasUserViewed)
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
             @HasUserViewed
where not exists (select 1 from MiniProfilers where Id = @Id)"; // this syntax works on both mssql and sqlite

            using (var conn = GetOpenConnection())
            {
                var insertCount = conn.ExecuteDapper(sql, new
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
                    HasUserViewed = profiler.HasUserViewed
                });

                if (insertCount > 0)
                    SaveTiming(conn, profiler, profiler.Root);
            }
        }

        /// <summary>
        /// Saves parameter Timing to the dbo.MiniProfilerTimings table.
        /// </summary>
        protected virtual void SaveTiming(DbConnection conn, Profiler profiler, Timing t)
        {
            const string sql =
@"insert into MiniProfilerTimings
            (Id,
             MiniProfilerId,
             ParentTimingId,
             Name,
             Depth,
             StartMilliseconds,
             DurationMilliseconds,
             DurationWithoutChildrenMilliseconds,
             SqlTimingsDurationMilliseconds,
             IsRoot,
             HasChildren,
             IsTrivial,
             HasSqlTimings,
             HasDuplicateSqlTimings,
             ExecutedReaders,
             ExecutedScalars,
             ExecutedNonQueries)
values      (@Id,
             @MiniProfilerId,
             @ParentTimingId,
             @Name,
             @Depth,
             @StartMilliseconds,
             @DurationMilliseconds,
             @DurationWithoutChildrenMilliseconds,
             @SqlTimingsDurationMilliseconds,
             @IsRoot,
             @HasChildren,
             @IsTrivial,
             @HasSqlTimings,
             @HasDuplicateSqlTimings,
             @ExecutedReaders,
             @ExecutedScalars,
             @ExecutedNonQueries)";

            conn.ExecuteDapper(sql, new
            {
                Id = t.Id,
                MiniProfilerId = profiler.Id,
                ParentTimingId = t.IsRoot ? (Guid?)null : t.ParentTiming.Id,
                Name = t.Name.Truncate(200),
                Depth = t.Depth,
                StartMilliseconds = t.StartMilliseconds,
                DurationMilliseconds = t.DurationMilliseconds,
                DurationWithoutChildrenMilliseconds = t.DurationWithoutChildrenMilliseconds,
                SqlTimingsDurationMilliseconds = t.SqlTimingsDurationMilliseconds,
                IsRoot = t.IsRoot,
                HasChildren = t.HasChildren,
                IsTrivial = t.IsTrivial,
                HasSqlTimings = t.HasSqlTimings,
                HasDuplicateSqlTimings = t.HasDuplicateSqlTimings,
                ExecutedReaders = t.ExecutedReaders,
                ExecutedScalars = t.ExecutedScalars,
                ExecutedNonQueries = t.ExecutedNonQueries
            });

            if (t.HasSqlTimings)
            {
                foreach (var st in t.SqlTimings)
                {
                    SaveSqlTiming(conn, profiler, st);
                }
            }

            if (t.HasChildren)
            {
                foreach (var child in t.Children)
                {
                    SaveTiming(conn, profiler, child);
                }
            }
        }

        /// <summary>
        /// Saves parameter SqlTiming to the dbo.MiniProfilerSqlTimings table.
        /// </summary>
        protected virtual void SaveSqlTiming(DbConnection conn, Profiler profiler, SqlTiming st)
        {
            const string sql =
@"insert into MiniProfilerSqlTimings
            (Id,
             MiniProfilerId,
             ParentTimingId,
             ExecuteType,
             StartMilliseconds,
             DurationMilliseconds,
             FirstFetchDurationMilliseconds,
             IsDuplicate,
             StackTraceSnippet,
             CommandString)
values      (@Id,
             @MiniProfilerId,
             @ParentTimingId,
             @ExecuteType,
             @StartMilliseconds,
             @DurationMilliseconds,
             @FirstFetchDurationMilliseconds,
             @IsDuplicate,
             @StackTraceSnippet,
             @CommandString)";

            conn.ExecuteDapper(sql, new
            {
                Id = st.Id,
                MiniProfilerId = profiler.Id,
                ParentTimingId = st.ParentTiming.Id,
                ExecuteType = st.ExecuteType,
                StartMilliseconds = st.StartMilliseconds,
                DurationMilliseconds = st.DurationMilliseconds,
                FirstFetchDurationMilliseconds = st.FirstFetchDurationMilliseconds,
                IsDuplicate = st.IsDuplicate,
                StackTraceSnippet = st.StackTraceSnippet.Truncate(200),
                CommandString = st.CommandString
            });

            if (st.Parameters != null && st.Parameters.Count > 0)
            {
                SaveSqlTimingParameters(conn, profiler, st);
            }
        }

        /// <summary>
        /// Saves any SqlTimingParameters used in the profiled SqlTiming to the dbo.MiniProfilerSqlTimingParameters table.
        /// </summary>
        protected virtual void SaveSqlTimingParameters(DbConnection conn, Profiler profiler, SqlTiming st)
        {
            const string sql =
@"insert into MiniProfilerSqlTimingParameters
            (MiniProfilerId,
             ParentSqlTimingId,
             Name,
             DbType,
             Size,
             Value)
values      (@MiniProfilerId,
             @ParentSqlTimingId,
             @Name,
             @DbType,
             @Size,
             @Value)";

            foreach (var p in st.Parameters)
            {
                conn.ExecuteDapper(sql, new
                {
                    MiniProfilerId = profiler.Id,
                    ParentSqlTimingId = st.Id,
                    Name = p.Name.Truncate(130),
                    DbType = p.DbType.Truncate(50),
                    Size = p.Size,
                    Value = p.Value
                });
            }
        }

        private static readonly Dictionary<Type, string> LoadSqlStatements = new Dictionary<Type, string>
        {
            { typeof(Profiler), "select * from MiniProfilers where Id = @id" },
            { typeof(Timing), "select * from MiniProfilerTimings where MiniProfilerId = @id order by RowId" },
            { typeof(SqlTiming), "select * from MiniProfilerSqlTimings where MiniProfilerId = @id order by RowId" },
            { typeof(SqlTimingParameter), "select * from MiniProfilerSqlTimingParameters where MiniProfilerId = @id" }
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
                var result = EnableBatchSelects ? LoadInBatch(conn, idParameter) : LoadIndividually(conn, idParameter);

                if (result != null)
                {
                    // HACK: stored dates are utc, but are pulled out as local time
                    result.Started = new DateTime(result.Started.Ticks, DateTimeKind.Utc);

                    // loading a profiler means we've viewed it
                    if (!result.HasUserViewed)
                    {
                        conn.ExecuteDapper("update MiniProfilers set HasUserViewed = 1 where Id = @id", idParameter);
                    }
                }

                return result;
            }
        }

        private Profiler LoadInBatch(DbConnection conn, object idParameter)
        {
            Profiler result;

            using (var multi = conn.DapperMultiple(LoadSqlBatch, idParameter))
            {
                result = multi.Read<Profiler>().SingleOrDefault();

                if (result != null)
                {
                    var timings = multi.Read<Timing>().ToList();
                    var sqlTimings = multi.Read<SqlTiming>().ToList();
                    var sqlParameters = multi.Read<SqlTimingParameter>().ToList();
                    MapTimings(result, timings, sqlTimings, sqlParameters);
                }
            }

            return result;
        }

        private Profiler LoadIndividually(DbConnection conn, object idParameter)
        {
            var result = LoadFor<Profiler>(conn, idParameter).SingleOrDefault();

            if (result != null)
            {
                var timings = LoadFor<Timing>(conn, idParameter);
                var sqlTimings = LoadFor<SqlTiming>(conn, idParameter);
                var sqlParameters = LoadFor<SqlTimingParameter>(conn, idParameter);
                MapTimings(result, timings, sqlTimings, sqlParameters);
            }

            return result;
        }

        private List<T> LoadFor<T>(DbConnection conn, object idParameter)
        {
            return conn.Dapper<T>(LoadSqlStatements[typeof(T)], idParameter).ToList();
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
                return conn.Dapper<Guid>(sql, new { user }).ToList();
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
