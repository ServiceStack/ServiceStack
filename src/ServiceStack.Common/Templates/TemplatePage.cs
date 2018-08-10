using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class TemplatePage
    {
        public IVirtualFile File { get; }
        public ReadOnlyMemory<char> FileContents { get; private set; }
        public ReadOnlyMemory<char> BodyContents { get; private set; }
        public Dictionary<string, object> Args { get; protected set; }
        public TemplatePage LayoutPage { get; set; }
        public List<PageFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime LastModifiedCheck { get; private set; }
        public bool HasInit { get; private set; }
        public bool IsLayout { get; private set; }

        public TemplateContext Context { get; }
        public PageFormat Format { get; }
        private readonly object semaphore = new object();

        public bool IsTempFile => File.Directory.VirtualPath == TemplateConstants.TempFilePath;
        public string VirtualPath => IsTempFile ? "{temp file}" : File.VirtualPath;

        public TemplatePage(TemplateContext context, IVirtualFile file, PageFormat format=null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            File = file ?? throw new ArgumentNullException(nameof(file));
            
            Format = format ?? Context.GetFormat(File.Extension);
            if (Format == null)
                throw new ArgumentException($"File with extension '{File.Extension}' is not a registered PageFormat in Context.PageFormats", nameof(file));
        }

        public virtual async Task<TemplatePage> Init()
        {
            if (HasInit)
            {
                var skipCheck = !Context.DebugMode &&
                    (Context.CheckForModifiedPagesAfter != null
                        ? DateTime.UtcNow - LastModifiedCheck < Context.CheckForModifiedPagesAfter.Value
                        : !Context.CheckForModifiedPages);
                
                if (skipCheck)
                    return this;

                File.Refresh();
                LastModifiedCheck = DateTime.UtcNow;
                if (File.LastModified == LastModified)
                    return this;
            }
            
            return await Load();
        }

        public async Task<TemplatePage> Load()
        {
            string contents;
            using (var stream = File.OpenRead())
            {
                contents = await stream.ReadToEndAsync();
            }

            var lastModified = File.LastModified;
            var fileContents = contents.AsMemory();
            var pageVars = new Dictionary<string, object>();

            var pos = 0;
            var bodyContents = fileContents;
            fileContents.AdvancePastWhitespace().TryReadLine(out ReadOnlyMemory<char> line, ref pos);
            if (line.StartsWith(Format.ArgsPrefix))
            {
                while (fileContents.TryReadLine(out line, ref pos))
                {
                    if (line.Trim().Length == 0)
                        continue;


                    if (line.StartsWith(Format.ArgsSuffix))
                        break;

                    line.SplitOnFirst(':', out var first, out var last);
                    pageVars[first.Trim().ToString()] = !last.IsEmpty ? last.Trim().ToString() : "";
                }
                
                //When page has variables body starts from first non whitespace after variable's end  
                var argsSuffixPos = line.LastIndexOf(Format.ArgsSuffix);
                if (argsSuffixPos >= 0)
                {
                    //Start back from the end of the ArgsSuffix
                    pos -= line.Length - argsSuffixPos - Format.ArgsSuffix.Length;
                }
                bodyContents = fileContents.SafeSlice(pos).AdvancePastWhitespace();
            }

            var pageFragments = pageVars.TryGetValue("ignore", out object ignore) 
                    && ("page".Equals(ignore.ToString()) || "template".Equals(ignore.ToString()))
                ? new List<PageFragment> { new PageStringFragment(bodyContents) } 
                : TemplatePageUtils.ParseTemplatePage(bodyContents);

            foreach (var fragment in pageFragments)
            {
                if (fragment is PageVariableFragment var && var.Binding == TemplateConstants.Page)
                {
                    IsLayout = true;
                    break;
                }
            }
            
            lock (semaphore)
            {
                LastModified = lastModified;
                LastModifiedCheck = DateTime.UtcNow;                
                FileContents = fileContents;
                Args = pageVars;
                BodyContents = bodyContents;
                PageFragments = pageFragments;

                HasInit = true;
                LayoutPage = Format.ResolveLayout(this);
            }

            if (LayoutPage != null)
            {
                if (!LayoutPage.HasInit)
                {
                    await LayoutPage.Load();
                }
                else
                {
                    if (Context.DebugMode || Context.CheckForModifiedPagesAfter != null &&
                        DateTime.UtcNow - LayoutPage.LastModifiedCheck >= Context.CheckForModifiedPagesAfter.Value)
                    {
                        LayoutPage.File.Refresh();
                        LayoutPage.LastModifiedCheck = DateTime.UtcNow;
                        if (LayoutPage.File.LastModified != LayoutPage.LastModified)
                            await LayoutPage.Load();
                    }
                }
            }

            return this;
        }
    }

    public class TemplatePartialPage : TemplatePage
    {
        private static readonly MemoryVirtualFiles TempFiles = new MemoryVirtualFiles();
        private static readonly InMemoryVirtualDirectory TempDir = new InMemoryVirtualDirectory(TempFiles, TemplateConstants.TempFilePath);

        static IVirtualFile CreateFile(string name, string format) =>
            new InMemoryVirtualFile(TempFiles, TempDir)
            {
                FilePath = name + "." + format, 
                TextContents = "",
            };

        public TemplatePartialPage(TemplateContext context, string name, IEnumerable<PageFragment> body, string format, Dictionary<string,object> args=null)
            : base(context, CreateFile(name, format), context.GetFormat(format))
        {
            PageFragments = body.ToList();
            Args = args ?? new Dictionary<string, object>();
        }

        public override Task<TemplatePage> Init() => ((TemplatePage)this).InTask();
    }
}