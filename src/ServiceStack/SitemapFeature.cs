using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Host.Handlers;
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

    public class SitemapFeature : IPlugin
    {
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

        public class SitemapIndexHandler : HttpAsyncTaskHandler
        {
            private readonly SitemapFeature feature;

            public SitemapIndexHandler(SitemapFeature feature)
            {
                this.feature = feature;
            }

            public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
            {
                if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                    return;

                httpRes.ContentType = MimeTypes.Xml;

                var xml = new StringBuilder();
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

                xml.Append("<sitemapindex");
                foreach (var ns in feature.UrlSetNamespaces)
                {
                    xml.Append(" {0}=\"{1}\"".Fmt(ns.Key, ns.Value));
                }
                xml.AppendLine(">");

                if (feature.CustomXml != null)
                {
                    if (feature.CustomXml.SitemapIndexHeaderXml != null)
                        xml.AppendLine(feature.CustomXml.SitemapIndexHeaderXml);
                }

                foreach (var sitemap in feature.SitemapIndex.Safe())
                {
                    xml.AppendLine("<sitemap>");

                    if (sitemap.Location != null || sitemap.AtPath != null)
                        xml.AppendLine("  <loc>{0}</loc>".Fmt((sitemap.Location ?? sitemap.AtPath.ToAbsoluteUri()).EncodeXml()));
                    if (sitemap.LastModified != null)
                        xml.AppendLine("  <lastmod>{0}</lastmod>".Fmt(sitemap.LastModified.Value.ToString("yyyy-MM-dd")));

                    if (sitemap.CustomXml != null)
                        xml.AppendLine(sitemap.CustomXml);

                    xml.AppendLine("</sitemap>");
                }

                if (feature.CustomXml != null)
                {
                    if (feature.CustomXml.SitemapIndexFooterXml != null)
                        xml.AppendLine(feature.CustomXml.SitemapIndexFooterXml);
                }

                xml.AppendLine("</sitemapindex>");
                httpRes.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(xml.ToString()));
            }
        }

        public class SitemapUrlSetHandler : HttpAsyncTaskHandler
        {
            private readonly SitemapFeature feature;

            private List<SitemapUrl> urlSet;

            public SitemapUrlSetHandler(SitemapFeature feature, List<SitemapUrl> urlSet)
            {
                this.feature = feature;
                this.urlSet = urlSet;
            }

            public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
            {
                if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                    return;

                httpRes.ContentType = MimeTypes.Xml;

                var xml = new StringBuilder();
                xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

                xml.Append("<urlset");
                foreach (var ns in feature.UrlSetNamespaces)
                {
                    xml.Append(" {0}=\"{1}\"".Fmt(ns.Key, ns.Value));
                }
                xml.AppendLine(">");

                if (feature.CustomXml != null)
                {
                    if (feature.CustomXml.UrlSetHeaderXml != null)
                        xml.AppendLine(feature.CustomXml.UrlSetHeaderXml);
                }

                foreach (var url in urlSet.Safe())
                {
                    xml.AppendLine("<url>");

                    if (url.Location != null)
                        xml.AppendLine("  <loc>{0}</loc>".Fmt(url.Location.EncodeXml()));
                    if (url.LastModified != null)
                        xml.AppendLine("  <lastmod>{0}</lastmod>".Fmt(url.LastModified.Value.ToString("yyyy-MM-dd")));
                    if (url.ChangeFrequency != null)
                        xml.AppendLine("  <changefreq>{0}</changefreq>".Fmt(url.ChangeFrequency.Value.ToString().ToLower()));
                    if (url.Priority != null)
                        xml.AppendLine("  <priority>{0}</priority>".Fmt(url.Priority));

                    if (url.CustomXml != null)
                        xml.AppendLine(url.CustomXml);

                    xml.AppendLine("</url>");
                }

                if (feature.CustomXml != null)
                {
                    if (feature.CustomXml.UrlSetFooterXml != null)
                        xml.AppendLine(feature.CustomXml.UrlSetFooterXml);
                }

                xml.AppendLine("</urlset>");

                httpRes.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(xml.ToString()));
            }
        }
    }
}