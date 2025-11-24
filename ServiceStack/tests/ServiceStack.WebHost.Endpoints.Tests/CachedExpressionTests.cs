using System;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class CachedExpressionTests
{
    [Test]
    public void Can_compile_cached_expressions()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        using var db = dbFactory.Open();
        db.CreateTable<RequestLog>();
        
        var now = DateTime.UtcNow;
        var take = 100;
        
        var request = new RequestLogs
        {
            Ids = [1,2,3]
        };
        
        var q = db.From<RequestLog>();
        if (request.BeforeSecs.HasValue)
            q = q.Where(x => (now - x.DateTime) <= TimeSpan.FromSeconds(request.BeforeSecs.Value));
        if (request.AfterSecs.HasValue)
            q = q.Where(x => (now - x.DateTime) > TimeSpan.FromSeconds(request.AfterSecs.Value));
        if (!request.OperationName.IsNullOrEmpty())
            q = q.Where(x => x.OperationName == request.OperationName);
        if (!request.IpAddress.IsNullOrEmpty())
            q = q.Where(x => x.IpAddress == request.IpAddress);
        if (!request.ForwardedFor.IsNullOrEmpty())
            q = q.Where(x => x.ForwardedFor == request.ForwardedFor);
        if (!request.UserAuthId.IsNullOrEmpty())
            q = q.Where(x => x.UserAuthId == request.UserAuthId);
        if (!request.SessionId.IsNullOrEmpty())
            q = q.Where(x => x.SessionId == request.SessionId);
        if (!request.Referer.IsNullOrEmpty())
            q = q.Where(x => x.Referer == request.Referer);
        if (!request.PathInfo.IsNullOrEmpty())
            q = q.Where(x => x.PathInfo == request.PathInfo);
        if (!request.BearerToken.IsNullOrEmpty())
            q = q.Where("Headers LIKE {0}", $"%Bearer {request.BearerToken.SqlVerifyFragment()}%");
        if (!request.Ids.IsEmpty())
            q = q.Where(x => request.Ids.Contains(x.Id));
        if (request.BeforeId.HasValue)
            q = q.Where(x => x.Id <= request.BeforeId);
        if (request.AfterId.HasValue)
            q = q.Where(x => x.Id > request.AfterId);
        if (request.WithErrors.HasValue)
            q = request.WithErrors.Value
                ? q.Where(x => x.Error != null || x.StatusCode >= 400)
                : q.Where(x => x.Error == null);
        if (request.DurationLongerThan.HasValue)
            q = q.Where(x => x.RequestDuration > request.DurationLongerThan.Value);
        if (request.DurationLessThan.HasValue)
            q = q.Where(x => x.RequestDuration < request.DurationLessThan.Value);
        q = string.IsNullOrEmpty(request.OrderBy)
            ? q.OrderByDescending(x => x.Id)
            : q.OrderBy(request.OrderBy);
        q = request.Skip > 0
            ? q.Limit(request.Skip, take)
            : q.Limit(take);
        
        Assert.That(q.WhereExpression, Is.EqualTo("WHERE \"Id\" IN (@0,@1,@2)"));
    }
}