using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using ServiceStack.MiniProfiler;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.RazorEngine
{
    public class ViewPage
    {
        public RazorFormat RazorFormat { get; set; }

        public string Layout { get; set; }

        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Contents { get; set; }

        public RazorPageType PageType { get; set; }
        public string TemplatePath { get; set; }
        public string DirectiveTemplatePath { get; set; }
        public DateTime? LastModified { get; set; }
        public List<IExpirable> Dependents { get; private set; }

        public const string ModelName = "Model";

        public ViewPage()
        {
            this.Dependents = new List<IExpirable>();
        }

        public ViewPage(RazorFormat razorFormat, string fullPath, string name, string contents)
            : this(razorFormat, fullPath, name, contents, RazorPageType.ViewPage) {}

        public ViewPage(RazorFormat razorFormat, string fullPath, string name, string contents, RazorPageType pageType)
            : this()
        {
            RazorFormat = razorFormat;
            FilePath = fullPath;
            Name = name;
            Contents = contents;
            PageType = pageType;
        }

        public DateTime? GetLastModified()
        {
            //if (!hasCompletedFirstRun) return null;
            var lastModified = this.LastModified;
            foreach (var expirable in this.Dependents)
            {
                if (!expirable.LastModified.HasValue) continue;
                if (!lastModified.HasValue || expirable.LastModified > lastModified)
                {
                    lastModified = expirable.LastModified;
                }
            }
            return lastModified;
        }

        public string GetTemplatePath()
        {
            return this.DirectiveTemplatePath ?? this.TemplatePath;
        }

        public string PageName
        {
            get
            {
                return this.PageType == RazorPageType.Template
                    || this.PageType == RazorPageType.ContentPage
                    ? this.FilePath
                    : this.Name;
            }
        }

        public void Prepare()
        {
            Razor.Compile(this.Contents, PageName);
        }

        private int timesRun;

        private Exception initException;
        readonly object readWriteLock = new object();
        private bool isBusy;
        public void Reload()
        {
            var contents = File.ReadAllText(this.FilePath);
            Reload(contents);
        }

        public void Reload(string contents)
        {
            var fi = new FileInfo(this.FilePath);
            var lastModified = fi.LastWriteTime;
            lock (readWriteLock)
            {
                try
                {
                    isBusy = true;

                    this.Contents = contents;
                    foreach (var markdownReplaceToken in RazorFormat.MarkdownReplaceTokens)
                    {
                        this.Contents = this.Contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
                    }

                    this.LastModified = lastModified;
                    initException = null;
                    timesRun = 0;
                    Prepare();
                }
                catch (Exception ex)
                {
                    initException = ex; 
                }
                isBusy = false;
                Monitor.PulseAll(readWriteLock);
            }
        }

        public string RenderToHtml()
        {
            return RenderToString((object)null);
        }

        public string RenderToHtml<T>(T model)
        {
            return RenderToString(model);
        }

        public string RenderToString<T>(T model)
        {
            var template = RazorFormat.ExecuteTemplate(model, this.PageName, this.TemplatePath);
            return template.Result;
        }

        public IRazorTemplate GetRazorTemplate()
        {
            return Razor.DefaultTemplateService.GetTemplate(this.PageName);
        }

        //From https://github.com/NancyFx/Nancy/blob/master/src/Nancy.ViewEngines.Razor/NancyRazorViewBase.cs

        public virtual void Write(object value)
        {
            WriteLiteral(HtmlEncode(value));
        }

        public virtual void WriteLiteral(object value)
        {
            //contents.Append(value);
        }

        public virtual void WriteTo(TextWriter writer, object value)
        {
            writer.Write(HtmlEncode(value));
        }

        public virtual void WriteLiteralTo(TextWriter writer, object value)
        {
            writer.Write(value);
        }

        public virtual void WriteTo(TextWriter writer, HelperResult value)
        {
            if (value != null)
            {
                value.WriteTo(writer);
            }
        }

        public virtual void WriteLiteralTo(TextWriter writer, HelperResult value)
        {
            //if (value != null)
            //{
            //    value.WriteTo(writer);
            //}
        }

        public virtual void DefineSection(string sectionName, Action action)
        {
            //this.Sections.Add(sectionName, action);
        }

        public virtual object RenderBody()
        {
            //this.contents.Append(this.childBody);

            return null;
        }

        public virtual object RenderSection(string sectionName)
        {
            return this.RenderSection(sectionName, true);
        }

        public virtual object RenderSection(string sectionName, bool required)
        {
            //string sectionContent;

            //var exists = this.childSections.TryGetValue(sectionName, out sectionContent);
            //if (!exists && required)
            //{
            //    throw new InvalidOperationException("Section name " + sectionName + " not found and is required.");
            //}

            //this.contents.Append(sectionContent ?? String.Empty);

            return null;
        }
        
        public virtual bool IsSectionDefined(string sectionName)
        {
            //return this.childSections.ContainsKey(sectionName);
            return false;
        }

        private static string HtmlEncode(object value)
        {
            if (value == null)
            {
                return null;
            }

            var str = value as IHtmlString;

            return str != null ? str.ToHtmlString() : HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture));
        }
    }
}