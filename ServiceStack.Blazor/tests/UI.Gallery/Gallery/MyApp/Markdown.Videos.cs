// BSD License: https://docs.servicestack.net/BSD-LICENSE.txt
// run node postinstall.js to update to latest version
using ServiceStack.IO;

namespace MyApp;

public class MarkdownVideos : MarkdownPagesBase<MarkdownFileInfo>
{
    public override string Id => "videos";
    public MarkdownVideos(ILogger<MarkdownVideos> log, IWebHostEnvironment env) : base(log,env) {}
    public Dictionary<string, List<MarkdownFileInfo>> Groups { get; set; } = new();

    public List<MarkdownFileInfo> GetVideos(string group)
    {
        return Groups.TryGetValue(group, out var docs)
            ? Fresh(docs.Where(IsVisible).OrderBy(x => x.Order).ThenBy(x => x.FileName).ToList())
            : new List<MarkdownFileInfo>();
    }
    
    public void LoadFrom(string fromDirectory)
    {
        Groups.Clear();
        var fs = AssertVirtualFiles();
        var dirs = fs.GetDirectory(fromDirectory).GetDirectories().ToList();
        Log.LogInformation("Found {0} video directories", dirs.Count);

        var pipeline = CreatePipeline();

        foreach (var dir in dirs)
        {
            var group = dir.Name;

            foreach (var file in dir.GetFiles().OrderBy(x => x.Name))
            {
                try
                {
                    var doc = Load(file.VirtualPath, pipeline);
                    if (doc == null)
                        continue;

                    doc.Group = group;
                    var groupVideos = Groups.GetOrAdd(group, v => new List<MarkdownFileInfo>());
                    groupVideos.Add(doc);
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Couldn't load {0}: {1}", file.VirtualPath, e.Message);
                }
            }
        }
    }
    
    public override List<MarkdownFileBase> GetAll()
    {
        var to = new List<MarkdownFileBase>();
        foreach (var entry in Groups)
        {
            to.AddRange(entry.Value.Where(IsVisible).Map(doc => ToMetaDoc(doc, x => x.Content = StripFrontmatter(doc.Content))));
        }
        return to;
    }
}