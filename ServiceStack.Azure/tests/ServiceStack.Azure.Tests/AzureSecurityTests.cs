using System;
using NUnit.Framework;
using ServiceStack.Azure.Messaging;
using ServiceStack.Azure.Storage;
using ServiceStack.Text;

namespace ServiceStack.Azure.Tests;

[TestFixture]
public class AzureSecurityTests
{
    [Test]
    public void AzureBlobVirtualFiles_SanitizePath_prevents_directory_traversal()
    {
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath("foo/bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath("/foo/bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath(@"foo\bar.txt"), Is.EqualTo("foo/bar.txt"));
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath("/a/b/../../c.txt"), Is.EqualTo("c.txt"));
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath("../../secret.txt"), Is.EqualTo("secret.txt"));
        Assert.That(AzureBlobVirtualFilesHelpers.SanitizePath("a/../../secret.txt"), Is.EqualTo("secret.txt"));
    }

    [Test]
    public void AzureTableCacheClient_has_regex_timeout()
    {
        Assert.That(AzureTableCacheClient.RegexTimeout, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void ServiceBusMqServer_does_not_mutate_global_AllowRuntimeType()
    {
        // Save initial AllowRuntimeType
        var initialAllowRuntimeType = JsConfig.AllowRuntimeType;
        try
        {
            // Set to a custom predicate that returns false for unknown types
            JsConfig.AllowRuntimeType = type => false;

            // Instantiate factory/server
            var factory = new ServiceBusMqMessageFactory(null!, "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummy");

            // Global AllowRuntimeType should NOT have been overridden to (_ => true)
            Assert.That(JsConfig.AllowRuntimeType(typeof(AzureSecurityTests)), Is.False);
        }
        finally
        {
            JsConfig.AllowRuntimeType = initialAllowRuntimeType;
        }
    }
}
