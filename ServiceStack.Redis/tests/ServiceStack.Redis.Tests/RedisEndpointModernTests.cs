using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisEndpointModernTests
{
    [Test]
    public void Explicit_URI_semantics_preserve_escaped_credentials_and_database()
    {
        var e = RedisEndpoint.ParseUri(
            "redis://user%3Aname:p%40ss%2F%2B%3F%25@[::1]:6379/3?Client=app%26one&NamespacePrefix=a%2Fb%2Fc");
        Assert.That(e.Host, Is.EqualTo("::1"));
        Assert.That(e.Port, Is.EqualTo(6379));
        Assert.That(e.Username, Is.EqualTo("user:name"));
        Assert.That(e.Password, Is.EqualTo("p@ss/+?%"));
        Assert.That(e.Db, Is.EqualTo(3));
        Assert.That(e.Client, Is.EqualTo("app&one"));
        Assert.That(e.NamespacePrefix, Is.EqualTo("a/b/c"));
        Assert.That(e.Ssl, Is.False);
        Assert.That(e.ToString().ToRedisEndpoint(), Is.EqualTo(e));
        Assert.That(e.GetHostString(), Is.EqualTo("[::1]:6379"));
    }

    [Test]
    public void Rediss_is_opt_in_and_legacy_redis_userinfo_meaning_is_unchanged()
    {
        var legacy = "redis://client:password@localhost".ToRedisEndpoint();
        Assert.That(legacy.Client, Is.EqualTo("client"));
        Assert.That(legacy.Username, Is.Null);
        var modern = "rediss://user:password@localhost/2".ToRedisEndpoint();
        Assert.That(modern.Username, Is.EqualTo("user"));
        Assert.That(modern.Client, Is.Null);
        Assert.That(modern.Ssl, Is.True);
        Assert.That(modern.Port, Is.EqualTo(RedisConfig.DefaultPortSsl));
        Assert.That(modern.Db, Is.EqualTo(2));
        Assert.That("[::1]:1234?Username=acl&Password=secret".ToRedisEndpoint().Host, Is.EqualTo("::1"));
        Assert.That(RedisEndpoint.ParseUri("redis://localhost").Port, Is.EqualTo(RedisConfig.DefaultPort));
    }

    [TestCase("rediss://user:TOPSECRET@localhost/1?ssl=false")]
    [TestCase("rediss://user:TOPSECRET@localhost/1?Db=2")]
    [TestCase("redis://user:TOPSECRET@localhost/-1")]
    [TestCase("redis://user:TOPSECRET@localhost/1/2")]
    [TestCase("redis://user:TOPSECRET@localhost/1/../2")]
    [TestCase("redis://user:TOPSECRET@localhost/0?Client=a&client=b")]
    [TestCase("redis://user:TOPSECRET@localhost/0?Password=other")]
    [TestCase("redis://user:TOPSECRET@localhost/0#fragment")]
    [TestCase("redis://user:TOPSECRET@localhost:0")]
    [TestCase("redis://user:TOPSECRET@localhost/0?Client=%ZZ")]
    public void Invalid_or_conflicting_URIs_do_not_disclose_credentials(string uri)
    {
        var error = Assert.Throws<ArgumentException>(() => RedisEndpoint.ParseUri(uri));
        Assert.That(error.ToString(), Does.Not.Contain("TOPSECRET"));
        Assert.That(error.InnerException, Is.Null);
    }

    [TestCase(0)]
    [TestCase(1500)]
    public void IPv6_literal_and_DNS_connect_and_clone(int timeout)
    {
        var port = Environment.GetEnvironmentVariable("REDIS_IPV6_PORT");
        if (port == null) Assert.Ignore("Set REDIS_IPV6_PORT to an isolated dual-stack Redis server");
        foreach (var host in new[] { "::1", "localhost" })
        {
            using var client = new RedisClient(new RedisEndpoint(host, int.Parse(port)) { ConnectTimeout = timeout });
            Assert.That(client.Ping(), Is.True);
            using var clone = client.CloneClient();
            Assert.That(clone.Ping(), Is.True);
        }

        using var pool = new RedisManagerPool($"[::1]:{port}", new RedisPoolConfig { MaxPoolSize = 1 });
        using var lease = pool.GetClient();
        Assert.That(lease.Ping(), Is.True);
        pool.FailoverTo($"[::1]:{port}");
        using var replacement = pool.GetClient();
        Assert.That(replacement.Ping(), Is.True);
    }

    [Test]
    public void DNS_connect_falls_back_to_IPv4_only_listener()
    {
        var endpoint = TestConfig.SingleHost.ToRedisEndpoint();
        endpoint.Host = "localhost";
        endpoint.ConnectTimeout = 1500;
        using var redis = new RedisClient(endpoint);
        Assert.That(redis.Ping(), Is.True);
    }

    [Test]
    public void TLS_URI_preserves_ACL_identity_clone_and_hostname_validation()
    {
        var port = Environment.GetEnvironmentVariable("REDIS_TLS_PORT");
        var certPath = Environment.GetEnvironmentVariable("REDIS_TLS_CERT");
        var plainPort = Environment.GetEnvironmentVariable("REDIS_IPV6_PORT");
        if (port == null || certPath == null)
            Assert.Ignore("Set REDIS_TLS_PORT and REDIS_TLS_CERT for the isolated TLS fixture");
#if NET10_0_OR_GREATER
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
        using var certificate = new X509Certificate2(certPath);
#endif
        var previous = RedisConfig.CertificateValidationCallback;
        var user = "uri:" + Guid.NewGuid().ToString("N");
        const string password = "test:@/+?%";
        using var admin = new RedisClient(new RedisEndpoint("::1", int.Parse(plainPort)));
        admin.Custom("ACL", "SETUSER", user, "on", ">" + password, "~*", "+@all");
        try
        {
            // Test-only explicit certificate pinning still requires correct hostname/IP validation.
            RedisConfig.CertificateValidationCallback = (_, cert, _, errors) =>
                (errors &
                 (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) == 0
                && cert.GetCertHashString() == certificate.GetCertHashString();
            var uri =
                $"rediss://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(password)}@[::1]:{port}/2?Client=tls-test&RetryTimeout=1";
            using var client = new RedisClient(uri);
            Assert.That(client.Ping(), Is.True);
            using var clone = client.CloneClient();
            Assert.That(clone.Ping(), Is.True);
            Assert.That(clone.Username, Is.EqualTo(user));
            Assert.That(clone.Ssl, Is.True);
            Assert.That(clone.Db, Is.EqualTo(2));
            using var mismatch = new RedisClient($"rediss://127.0.0.1:{port}?RetryTimeout=1");
            Assert.Catch(() => mismatch.Ping(), "Certificate has no IPv4 SAN and must fail hostname validation");
            RedisConfig.CertificateValidationCallback = null;
            using var untrusted = new RedisClient($"rediss://[::1]:{port}?RetryTimeout=1");
            Assert.Catch(() => untrusted.Ping(), "Default validation must reject an untrusted test certificate");
        }
        finally
        {
            RedisConfig.CertificateValidationCallback = previous;
            admin.Custom("ACL", "DELUSER", user);
        }
    }
}