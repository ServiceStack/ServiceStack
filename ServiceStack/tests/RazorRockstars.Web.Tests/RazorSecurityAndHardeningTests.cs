using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Html.AntiXsrf;
using ServiceStack.IO;
using ServiceStack.Razor;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Managers;

namespace RazorRockstars.Web.Tests
{
    [TestFixture]
    public class RazorSecurityAndHardeningTests
    {
        private class TestViewPage : ViewPage
        {
            public override void Execute()
            {
            }
        }

        [Test]
        public void GetErrorHtml_Escapes_XSS_Vectors_In_ErrorCode_Message_And_StackTrace()
        {
            var page = new TestViewPage();
            var responseStatus = new ResponseStatus
            {
                ErrorCode = "<script>alert('xss-code')</script>",
                Message = "\"><img src=x onerror=alert('xss-msg')>",
                StackTrace = "at App.Run() <script>alert('xss-stack')</script>"
            };

            var html = page.GetErrorHtml(responseStatus);

            Assert.That(html, Is.Not.Null);
            // Verify raw dangerous script/img tags are not present
            Assert.That(html, Does.Not.Contain("<script>alert('xss-code')</script>"));
            Assert.That(html, Does.Not.Contain("\"><img src=x onerror=alert('xss-msg')>"));
            Assert.That(html, Does.Not.Contain("<script>alert('xss-stack')</script>"));

            // Verify encoded equivalents are present
            Assert.That(html, Does.Contain("&lt;script&gt;alert(&#39;xss-code&#39;)&lt;/script&gt;"));
            Assert.That(html, Does.Contain("&quot;&gt;&lt;img src=x onerror=alert(&#39;xss-msg&#39;)&gt;"));
            Assert.That(html, Does.Contain("at App.Run() &lt;script&gt;alert(&#39;xss-stack&#39;)&lt;/script&gt;"));
        }

        [Test]
        public void GetErrorHtml_Returns_Null_When_ResponseStatus_Is_Null()
        {
            var page = new TestViewPage();
            var html = page.GetErrorHtml(null);
            Assert.That(html, Is.Null);
        }

        [Test]
        public void CryptoUtil_AreByteArraysEqual_ConstantTimeComparison()
        {
            var a = new byte[] { 1, 2, 3, 4, 5 };
            var b = new byte[] { 1, 2, 3, 4, 5 };
            var c = new byte[] { 1, 2, 3, 4, 6 };
            var d = new byte[] { 1, 2, 3, 4 };

            Assert.That(CryptoUtil.AreByteArraysEqual(a, b), Is.True);
            Assert.That(CryptoUtil.AreByteArraysEqual(a, c), Is.False);
            Assert.That(CryptoUtil.AreByteArraysEqual(a, d), Is.False);
            Assert.That(CryptoUtil.AreByteArraysEqual(a, null), Is.False);
            Assert.That(CryptoUtil.AreByteArraysEqual(null, b), Is.False);
            Assert.That(CryptoUtil.AreByteArraysEqual(null, null), Is.False);
        }

        [Test]
        public void AntiForgeryTokenSerializer_Throws_On_Null_Token_Or_SecurityToken()
        {
            var serializer = new AntiForgeryTokenSerializer(null);

            Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));

            var tokenWithoutSecurityToken = new AntiForgeryToken { SecurityToken = null };
            Assert.Throws<ArgumentException>(() => serializer.Serialize(tokenWithoutSecurityToken));
        }

        [Test]
        public void DynamicRequestObject_And_DynamicDictionary_NullSafety()
        {
            var dynReq = new DynamicRequestObject(null, new { Foo = "Bar" });
            dynamic dyn = dynReq;
            Assert.DoesNotThrow(() =>
            {
                var val = dyn.NonExistentParam;
            });

            var dict = new DynamicDictionary(null);
            Assert.That(dict.TryGetItem("unknown", out var item), Is.False);
            Assert.That(item, Is.Null);
        }

        [Test]
        public void CompilerServices_GetLoadedAssemblies_ThreadSafety()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 50; j++)
                    {
                        var list = CompilerServices.GetLoadedAssemblies();
                        Assert.That(list, Is.Not.Null);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void RazorViewManager_Concurrent_Access_To_Pages_And_ViewNamesMap()
        {
            var viewConfig = new RazorFormat();
            var manager = new RazorViewManager(viewConfig, new MemoryVirtualFiles());

            var tasks = new List<Task>();
            for (int i = 0; i < 8; i++)
            {
                var threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var key = $"page_{threadId}_{j}";
                        manager.Pages[key] = new RazorPage();
                        var retrieved = manager.GetPage(key);
                        Assert.That(retrieved, Is.Not.Null);
                        manager.Pages.TryRemove(key, out _);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public void FileSystemWatcherLiveReload_Implements_IDisposable()
        {
            var viewConfig = new RazorFormat();
            var manager = new RazorViewManager(viewConfig, new MemoryVirtualFiles());
            var liveReload = new FileSystemWatcherLiveReload(manager);

            Assert.That(liveReload, Is.InstanceOf<IDisposable>());
            Assert.DoesNotThrow(() => liveReload.Dispose());
        }

        [Test]
        public void RazorFormat_Dispose_CleansUp_LiveReload()
        {
            var razorFormat = new RazorFormat();
            Assert.That(razorFormat, Is.InstanceOf<IDisposable>());
            Assert.DoesNotThrow(() => razorFormat.Dispose());
        }
    }
}
