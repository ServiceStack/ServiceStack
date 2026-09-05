using System;
using System.Data.SqlClient;

namespace ServiceStack.Server.Tests
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION")
            ?? "Server=localhost;Database=master;User Id=sa;Password=p@55wOrd;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;";

        static Config()
        {
            try
            {
                using var conn = new SqlConnection("Server=localhost;Database=master;User Id=sa;Password=p@55wOrd;TrustServerCertificate=True;Connect Timeout=5;");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'test')
BEGIN
    CREATE DATABASE [test];
END;
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'test')
BEGIN
    CREATE LOGIN [test] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
END;
ELSE
BEGIN
    ALTER LOGIN [test] ENABLE;
    ALTER LOGIN [test] WITH PASSWORD = 'p@55wOrd', CHECK_POLICY = OFF;
END;
ALTER SERVER ROLE sysadmin ADD MEMBER [test];
";
                cmd.ExecuteNonQuery();

                // Run database-scoped setup separately. SQL Server resolves USE targets
                // when compiling a batch, before CREATE DATABASE above can take effect.
                cmd.CommandText = @"
USE [test];
IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE type = 'FX')
BEGIN
    ALTER DATABASE [test] ADD FILEGROUP [test_mod] CONTAINS MEMORY_OPTIMIZED_DATA;
    ALTER DATABASE [test] ADD FILE (name='test_mod_file', filename='/var/opt/mssql/data/test_mod_file') TO FILEGROUP [test_mod];
END;
ALTER DATABASE [test] SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config.SQL_BOOTSTRAP] Error: {ex}");
            }
        }
    }
}
