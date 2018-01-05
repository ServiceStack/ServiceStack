﻿using System;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class Config
    {
        public static readonly string ServiceStackBaseUri = Environment.GetEnvironmentVariable("CI_BASEURI") ?? "http://localhost:20000";
        public static readonly string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public static readonly string ListeningOn = ServiceStackBaseUri + "/";
        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("CI_SQLSERVER") ?? "Server=localhost;Database=test;User Id=test;Password=test;";

        public const string AspNetBaseUri = "http://localhost:50000/";
        public const string AspNetServiceStackBaseUri = AspNetBaseUri + "api";
    }
}