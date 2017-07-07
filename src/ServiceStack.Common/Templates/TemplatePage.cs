using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;

#endif

namespace ServiceStack.Templates
{
    public class TemplatePage
    {
        public IVirtualFile File { get; set; }
        public StringSegment ServerHtml { get; set; }
        public StringSegment PageHtml { get; set; }
        public Dictionary<string, string> PageVars { get; set; }
        public string Layout { get; set; }
        public TemplatePage LayoutPage { get; set; }
        public List<PageFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsCompletePage { get; set; }
        public bool HasInit { get; private set; }

        public TemplatePagesContext Context { get; }
        private readonly object semaphore = new object();

        public TemplatePage(TemplatePagesContext feature, IVirtualFile file)
        {
            this.Context = feature ?? throw new ArgumentNullException(nameof(feature));
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public async Task<TemplatePage> Init()
        {
            return HasInit
                ? this
                : await Load();
        }

        public async Task<TemplatePage> Load()
        {
            string serverHtml;
            using (var stream = File.OpenRead())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                serverHtml = await reader.ReadToEndAsync();
            }

            lock (semaphore)
            {
                LastModified = File.LastModified;

                ServerHtml = serverHtml.ToStringSegment();
                PageVars = new Dictionary<string, string>();

                var pos = 0;
                while (char.IsWhiteSpace(ServerHtml.GetChar(pos)))
                    pos++;

                ServerHtml.TryReadLine(out StringSegment line, ref pos);
                if (line.StartsWith("<!--"))
                {
                    while (ServerHtml.TryReadLine(out line, ref pos))
                    {
                        if (line.Trim().Length == 0)
                            continue;


                        if (line.StartsWith("-->"))
                            break;

                        var kvp = line.SplitOnFirst(':');
                        PageVars[kvp[0].Trim().ToString()] = kvp.Length > 1 ? kvp[1].Trim().ToString() : "";
                    }
                }
                else
                {
                    pos = 0;
                }

                PageHtml = ServerHtml.Subsegment(pos).TrimStart();
                PageFragments = TemplatePageUtils.ParseTemplatePage(PageHtml);
                IsCompletePage = Context.IsCompletePage(PageHtml);

                HasInit = true;

                if (!IsCompletePage)
                {
                    if (PageVars.TryGetValue(TemplatePages.Layout, out string layout))
                        Layout = layout;

                    LayoutPage = Context.TemplatePages.ResolveLayoutPage(this);
                }
            }

            if (LayoutPage != null)
            {
                if (!LayoutPage.HasInit)
                {
                    await LayoutPage.Load();
                }
                else if (Context.CheckModifiedPages || Context.DebugMode)
                {
                    LayoutPage.File.Refresh();
                    if (LayoutPage.File.LastModified > LayoutPage.LastModified)
                        await LayoutPage.Load();
                }
            }

            return this;
        }

        public object GetValue(PageVariableFragment var)
        {
            return PageVars.TryGetValue(var.NameString, out string value)
                ? value
                : LayoutPage?.GetValue(var);
        }

        public string GetEncodedValue(PageVariableFragment var)
        {
            return Context.EncodeValue(GetValue(var));
        }
    }
}