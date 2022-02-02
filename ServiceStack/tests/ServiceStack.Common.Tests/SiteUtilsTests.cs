using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    public class SiteUtilsTests
    {
        [Test]
        [TestCase("northwind.netcore.io:", "https://northwind.netcore.io/")]
        [TestCase("techstacks.io", "https://techstacks.io")]
        [TestCase("http:techstacks.io", "http://techstacks.io")]
        [TestCase("http:techstacks.io:1000", "http://techstacks.io:1000")]
        [TestCase("http:techstacks.io:1000:site1:site2", "http://techstacks.io:1000/site1/site2")]
        [TestCase("http:techstacks.io:1000:site1%7Csite2", "http://techstacks.io:1000/site1|site2")]
        [TestCase("techstacks.io:site1%7Csite2", "https://techstacks.io/site1|site2")]
        [TestCase("http://techstacks.io:1000/site1/site2", "http://techstacks.io:1000/site1/site2")]
        [TestCase("https://techstacks.io:1000/site1/site2", "https://techstacks.io:1000/site1/site2")]
        [TestCase("techstacks.io:1000:site1:site2(a:1,b:\"c,d\",e:f)", "https://techstacks.io:1000/site1/site2(a:1,b:\"c,d\",e:f)")]
        [TestCase("apps.servicestack.net/gists/techstacks.io/swift/FindTechnologies(VendorName:Google,Take:5,OrderByDesc:ViewCount,Fields:\"Name,ProductUrl,Tier,VendorName\")?use=xcode&name=MyApp", 
            "https://apps.servicestack.net/gists/techstacks.io/swift/FindTechnologies(VendorName:Google,Take:5,OrderByDesc:ViewCount,Fields:\"Name,ProductUrl,Tier,VendorName\")?use=xcode&name=MyApp")]
        public void Can_resolve_url_from_slug(string slug, string expected)
        {
            var url = SiteUtils.UrlFromSlug(slug);
            Assert.That(url, Is.EqualTo(expected));
        }
 
        [Test]
        [TestCase("techstacks.io", "https://techstacks.io")]
        [TestCase("http:techstacks.io", "http://techstacks.io")]
        [TestCase("http:techstacks.io:1000", "http://techstacks.io:1000")]
        [TestCase("http:techstacks.io:1000:site1:site2", "http://techstacks.io:1000/site1/site2")]
        [TestCase("http:techstacks.io:1000:site1|site2", "http://techstacks.io:1000/site1|site2")]
        [TestCase("techstacks.io:site1%7Csite2", "https://techstacks.io/site1%7Csite2")]
        [TestCase("http:techstacks.io:1000:site1:site2", "http://techstacks.io:1000/site1/site2")]
        [TestCase("techstacks.io:1000:site1:site2", "https://techstacks.io:1000/site1/site2")]
        [TestCase("techstacks.io:1000:site1:site2(a:1,b:\"c,d\",e:f)", "https://techstacks.io:1000/site1/site2(a:1,b:\"c,d\",e:f)")]
        [TestCase("apps.servicestack.net:gists:techstacks.io:swift:FindTechnologies(VendorName:Google,Take:5,OrderByDesc:ViewCount,Fields:\"Name,ProductUrl,Tier,VendorName\")?use=xcode&name=MyApp", 
            "https://apps.servicestack.net/gists/techstacks.io/swift/FindTechnologies(VendorName:Google,Take:5,OrderByDesc:ViewCount,Fields:\"Name,ProductUrl,Tier,VendorName\")?use=xcode&name=MyApp")]
        public void Can_convert_url_to_slug(string expected, string url)
        {
            var slug = SiteUtils.UrlToSlug(url);
            Assert.That(slug, Is.EqualTo(expected));
        }
    }
}