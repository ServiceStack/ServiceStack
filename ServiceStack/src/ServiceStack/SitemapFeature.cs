using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Sitemap
    {
        public Sitemap()
        {
            this.UrlSet = new List<SitemapUrl>();
        }

        public string AtPath { get; set; }

        public string Location { get; set; }

        public DateTime? LastModified { get; set; }

        public List<SitemapUrl> UrlSet { get; set; }

        public string CustomXml { get; set; }
    }

    public class SitemapUrl
    {
        public string Location { get; set; }
        public DateTime? LastModified { get; set; }
        public SitemapFrequency? ChangeFrequency { get; set; }
        public decimal? Priority { get; set; }
        public string CustomXml { get; set; }
    }

    public enum SitemapFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never
    }

    public class SitemapCustomXml
    {
        public string SitemapIndexHeaderXml { get; set; }
        public string SitemapIndexFooterXml { get; set; }
        public string UrlSetHeaderXml { get; set; }
        public string UrlSetFooterXml { get; set; }
    }

    public class SitemapFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Sitemap;
        public Dictionary<string, string> SitemapIndexNamespaces { get; set; }
        public Dictionary<string, string> UrlSetNamespaces { get; set; }

        public string AtPath { get; set; }

        public List<Sitemap> SitemapIndex { get; set; }
        public List<SitemapUrl> UrlSet { get; set; }

        public SitemapCustomXml CustomXml { get; set; }

        public SitemapFeature()
        {
            AtPath = "/sitemap.xml";
            SitemapIndex = new List<Sitemap>();
            UrlSet = new List<SitemapUrl>();

            UrlSetNamespaces = new Dictionary<string, string>
            {
                {"xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"},
            };

            SitemapIndexNamespaces = new Dictionary<string, string>
            {
                {"xmlns:xsi", "http://www.sitemaps.org/schemas/sitemap/0.9"},
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.RawHttpHandlers.Add(req =>
            {
                if (SitemapIndex.Count > 0)
                {
                    if (req.PathInfo == AtPath)
                        return new SitemapIndexHandler(this);

                    foreach (var sitemap in SitemapIndex)
                    {
                        if (req.PathInfo == sitemap.AtPath)
                            return new SitemapUrlSetHandler(this, sitemap.UrlSet);
                    }
                }
                else if (UrlSet.Count > 0)
                {
                    if (req.PathInfo == AtPath)
                        return new SitemapUrlSetHandler(this, UrlSet);
                }

                return null;
            });
        }

        public string GetSitemapIndex()
        {
            var xml = StringBuilderCache.Allocate();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            xml.Append("<sitemapindex");
            foreach (var ns in UrlSetNamespaces)
            {
                xml.Append($" {ns.Key}=\"{ns.Value}\"");
            }

            xml.AppendLine(">");

            if (CustomXml?.SitemapIndexHeaderXml != null)
                xml.AppendLine(CustomXml.SitemapIndexHeaderXml);

            foreach (var sitemap in SitemapIndex.Safe())
            {
                xml.AppendLine("<sitemap>");

                if (sitemap.Location != null || sitemap.AtPath != null)
                    xml.AppendLine($"  <loc>{(sitemap.Location ?? sitemap.AtPath.ToAbsoluteUri()).EncodeXml()}</loc>");
                if (sitemap.LastModified != null)
                    xml.AppendLine($"  <lastmod>{sitemap.LastModified.Value:yyyy-MM-dd}</lastmod>");

                if (sitemap.CustomXml != null)
                    xml.AppendLine(sitemap.CustomXml);

                xml.AppendLine("</sitemap>");
            }

            if (CustomXml?.SitemapIndexFooterXml != null)
                xml.AppendLine(CustomXml.SitemapIndexFooterXml);

            xml.AppendLine("</sitemapindex>");
            return StringBuilderCache.ReturnAndFree(xml);
        }

        public string GetSitemapUrlSet(List<SitemapUrl> urlSet)
        {
            var xml = StringBuilderCache.Allocate();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            xml.Append("<urlset");
            foreach (var ns in UrlSetNamespaces)
            {
                xml.Append(" {0}=\"{1}\"".Fmt(ns.Key, ns.Value));
            }

            xml.AppendLine(">");

            if (CustomXml?.UrlSetHeaderXml != null)
                xml.AppendLine(CustomXml.UrlSetHeaderXml);

            foreach (var url in urlSet.Safe())
            {
                xml.AppendLine("<url>");

                if (url.Location != null)
                    xml.AppendLine($"  <loc>{url.Location.EncodeXml()}</loc>");
                if (url.LastModified != null)
                    xml.AppendLine($"  <lastmod>{url.LastModified.Value:yyyy-MM-dd}</lastmod>");
                if (url.ChangeFrequency != null)
                    xml.AppendLine($"  <changefreq>{url.ChangeFrequency.Value.ToString().ToLower()}</changefreq>");
                if (url.Priority != null)
                    xml.AppendLine($"  <priority>{url.Priority.Value.ToString(CultureInfo.InvariantCulture)}</priority>");

                if (url.CustomXml != null)
                    xml.AppendLine(url.CustomXml);

                xml.AppendLine("</url>");
            }

            if (CustomXml?.UrlSetFooterXml != null)
                xml.AppendLine(CustomXml.UrlSetFooterXml);

            xml.AppendLine("</urlset>");

            return StringBuilderCache.ReturnAndFree(xml);
        }

        public class SitemapIndexHandler : HttpAsyncTaskHandler
        {
            private readonly SitemapFeature feature;

            public SitemapIndexHandler(SitemapFeature feature)
            {
                this.feature = feature;
            }

            public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
            {
                if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                    return;

                httpRes.ContentType = MimeTypes.Xml;

                var text = feature.GetSitemapIndex();
                await httpRes.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r => r.WriteAsync(text));
            }

        }

        public class SitemapUrlSetHandler : HttpAsyncTaskHandler
        {
            private readonly SitemapFeature feature;

            private readonly List<SitemapUrl> urlSet;

            public SitemapUrlSetHandler(SitemapFeature feature, List<SitemapUrl> urlSet)
            {
                this.feature = feature;
                this.urlSet = urlSet;
            }

            public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
            {
                if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                    return;

                httpRes.ContentType = MimeTypes.Xml;

                var text = feature.GetSitemapUrlSet(urlSet);
                await httpRes.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r => r.WriteAsync(text));
            }
        }
    }
}