using System;

namespace ServiceStack.Server.Tests
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION")
                                            ?? "Server=localhost;Database=test;User Id=test;Password=test;";
    }
}