using System;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class Config
{
    public const string BaseUri = "http://localhost:20000";
    public const string BaseUriHost = "http://localhost:20000/";
    public static readonly string ServiceStackBaseUri = Environment.GetEnvironmentVariable("CI_BASEURI") ?? BaseUri;
    public static readonly string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        
    public static readonly string HostNameBaseUrl = "http://DESKTOP-BCS76J0:20000/"; //Allow fiddler
    public static readonly string AnyHostBaseUrl = "http://*:20000/"; //Allow capturing by fiddler

    public static readonly string ListeningOn = ServiceStackBaseUri + "/";
    public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
    public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION") ?? "Server=localhost;Database=test;User Id=test;Password=test;";
    public static readonly string PostgreSqlConnString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
    public static readonly string DynamoDbServiceURL = Environment.GetEnvironmentVariable("CI_DYNAMODB") ?? "http://localhost:8000";

    public const string AspNetBaseUri = "http://localhost:50000/";
    public const string AspNetServiceStackBaseUri = AspNetBaseUri + "api";
}