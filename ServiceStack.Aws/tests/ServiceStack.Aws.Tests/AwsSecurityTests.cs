using System;
using Amazon.S3;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.IO;

namespace ServiceStack.Aws.Tests;

[TestFixture]
public class AwsSecurityTests
{
    [Test]
    public void S3VirtualFiles_SanitizePath_prevents_directory_traversal()
    {
        var vfs = new S3VirtualFiles(null, "test-bucket");

        Assert.That(vfs.SanitizePath("foo/bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(vfs.SanitizePath("/foo/bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(vfs.SanitizePath(@"foo\bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(vfs.SanitizePath("/a/b/../../c.txt"), Is.EqualTo("c.txt"));
        Assert.That(vfs.SanitizePath("../../secret.txt"), Is.EqualTo("secret.txt"));
        Assert.That(vfs.SanitizePath("a/../../secret.txt"), Is.EqualTo("secret.txt"));
    }

    [Test]
    public void DynamoDbAuthRepository_rejects_usernames_containing_at_symbol()
    {
        var authRepo = new DynamoDbAuthRepository(null);

        var newUserWithAt = new UserAuth
        {
            UserName = "user@domain.com",
            Email = "user@domain.com"
        };

        var ex = Assert.Throws<ArgumentException>(() => authRepo.CreateUserAuth(newUserWithAt, "Password123!"));
        Assert.That(ex.Message, Does.Contain("invalid characters").IgnoreCase);
    }
}
