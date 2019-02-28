using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class SharpPage
    {
        public IVirtualFile File { get; }
        public ReadOnlyMemory<char> FileContents { get; private set; }
        public ReadOnlyMemory<char> BodyContents { get; private set; }
        public Dictionary<string, object> Args { get; protected set; }
        public SharpPage LayoutPage { get; set; }
        public List<PageFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime LastModifiedCheck { get; private set; }
        public bool HasInit { get; private set; }
        public bool IsLayout { get; private set; }

        public ScriptContext Context { get; }
        public PageFormat Format { get; }
        private readonly object semaphore = new object();

        public bool IsTempFile => File.Directory.VirtualPath == ScriptConstants.TempFilePath;
        public string VirtualPath => IsTempFile ? "{temp file}" : File.VirtualPath;

        public SharpPage(ScriptContext context, IVirtualFile file, PageFormat format=null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            File = file ?? throw new ArgumentNullException(nameof(file));
            
            Format = format ?? Context.GetFormat(File.Extension);
            if (Format == null)
                throw new ArgumentException($"File with extension '{File.Extension}' is not a registered PageFormat in Context.PageFormats", nameof(file));
        }

        public virtual async Task<SharpPage> Init()
        {
            if (HasInit)
            {
                var skipCheck = !Context.DebugMode &&
                    (Context.CheckForModifiedPagesAfter != null
                        ? DateTime.UtcNow - LastModifiedCheck < Context.CheckForModifiedPagesAfter.Value
                        : !Context.CheckForModifiedPages) &&
                    (Context.InvalidateCachesBefore == null || LastModifiedCheck > Context.InvalidateCachesBefore.Value);
                
                if (skipCheck)
                    return this;

                File.Refresh();
                LastModifiedCheck = DateTime.UtcNow;
                if (File.LastModified == LastModified)
                    return this;
            }
            
            return await Load();
        }

        public async Task<SharpPage> Load()
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

                    var colonPos = line.IndexOf(':');
                    var spacePos = line.IndexOf(' ');
                    var bracePos = line.IndexOf('{');
                    var sep = colonPos >= 0 ? ':' : ' ';

                    if (bracePos > 0 && spacePos > 0 && colonPos > spacePos)
                        sep = ' ';
                    
                    line.SplitOnFirst(sep, out var first, out var last);

                    pageVars[first.Trim().ToString()] = !last.IsEmpty ? last.Trim().ToString() : "";
                }
                
                //When page has variables body starts from first non whitespace after variables end  
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
                : SharpPageUtils.ParseTemplatePage(bodyContents);

            foreach (var fragment in pageFragments)
            {
                if (fragment is PageVariableFragment var && var.Binding == ScriptConstants.Page)
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

    public class SharpPartialPage : SharpPage
    {
        private static readonly MemoryVirtualFiles TempFiles = new MemoryVirtualFiles();
        private static readonly InMemoryVirtualDirectory TempDir = new InMemoryVirtualDirectory(TempFiles, ScriptConstants.TempFilePath);

        static IVirtualFile CreateFile(string name, string format) =>
            new InMemoryVirtualFile(TempFiles, TempDir)
            {
                FilePath = name + "." + format, 
                TextContents = "",
            };

        public SharpPartialPage(ScriptContext context, string name, IEnumerable<PageFragment> body, string format, Dictionary<string,object> args=null)
            : base(context, CreateFile(name, format), context.GetFormat(format))
        {
            PageFragments = body.ToList();
            Args = args ?? new Dictionary<string, object>();
        }

        public override Task<SharpPage> Init() => ((SharpPage)this).InTask();
    }
}