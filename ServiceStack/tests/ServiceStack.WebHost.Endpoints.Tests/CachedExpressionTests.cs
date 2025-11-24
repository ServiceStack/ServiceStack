using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class CachedExpressionTests
{
    void AssertExpr(Action<SqlExpression<RequestLog>> fn, string expected)
    {
        var q = new OrmLite.Sqlite.SqliteExpression<RequestLog>(SqliteDialect.Provider);
        Console.WriteLine($"Running: {expected}");
        fn(q);
        Assert.That(q.WhereExpression, Is.EqualTo(expected));
    }

    [Test]
    public void Does_reuse_cached_expressions()
    {
        Can_compile_cached_expressions();
        Can_compile_cached_expressions();
    }
    
    [Test]
    public void Can_compile_cached_expressions()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        using var db = dbFactory.Open();
        
        var now = DateTime.UtcNow;
        var take = 100;
        
        var request = new RequestLogs
        {
            BeforeSecs = 1,
            AfterSecs = 1,
            OperationName = nameof(RequestLogs),
            IpAddress = "127.0.0.1",
            ForwardedFor = "127.0.0.1",
            UserAuthId = "1",
            SessionId = "1",
            Referer = "https://example.org",
            PathInfo = "/requestlogs",
            BeforeId = 1,
            AfterId = 1,
            DurationLongerThan = TimeSpan.FromSeconds(1),
            DurationLessThan = TimeSpan.FromSeconds(1),
            Ids = [1,2,3]
        };
        
        AssertExpr(q => 
            q.Where(x => (now - x.DateTime) <= TimeSpan.FromSeconds(request.BeforeSecs.Value)), 
            "WHERE ((@0 - \"DateTime\") <= @1)");
        AssertExpr(q => 
                q.Where(x => (now - x.DateTime) > TimeSpan.FromSeconds(request.AfterSecs.Value)), 
            "WHERE ((@0 - \"DateTime\") > @1)");
        AssertExpr(q => 
                q.Where(x => x.OperationName == request.OperationName), 
            "WHERE (\"OperationName\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.IpAddress == request.IpAddress), 
            "WHERE (\"IpAddress\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.ForwardedFor == request.ForwardedFor), 
            "WHERE (\"ForwardedFor\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.UserAuthId == request.ForwardedFor), 
            "WHERE (\"UserAuthId\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.SessionId == request.SessionId), 
            "WHERE (\"SessionId\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.Referer == request.Referer), 
            "WHERE (\"Referer\" = @0)");
        AssertExpr(q => 
                q.Where(x => x.PathInfo == request.PathInfo), 
            "WHERE (\"PathInfo\" = @0)");
        AssertExpr(q => 
                q.Where(q.Column<RequestLog>(x => x.Headers) + " LIKE {0}", $"%Bearer {request.BearerToken.SqlVerifyFragment()}%"), 
            "WHERE \"Headers\" LIKE @0");
        AssertExpr(q => q.Where(x => request.Ids.Contains(x.Id)), 
            "WHERE \"Id\" IN (@0,@1,@2)"); 
        AssertExpr(q => 
                q.Where(x => x.Id <= request.BeforeId), 
            "WHERE (\"Id\" <= @0)");
        AssertExpr(q => 
                q.Where(x => x.Id > request.AfterId), 
            "WHERE (\"Id\" > @0)");
        AssertExpr(q => 
                q.Where(x => x.Error != null || x.StatusCode >= 400), 
            "WHERE ((\"Error\" is not null) OR (\"StatusCode\" >= @0))");
        AssertExpr(q => 
                q.Where(x => x.RequestDuration > request.DurationLongerThan.Value), 
            "WHERE (\"RequestDuration\" > @0)");
        AssertExpr(q => 
                q.Where(x => x.RequestDuration < request.DurationLessThan.Value), 
            "WHERE (\"RequestDuration\" < @0)");
    }
}