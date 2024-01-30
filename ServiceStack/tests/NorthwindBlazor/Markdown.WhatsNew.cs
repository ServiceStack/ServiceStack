// run node postinstall.js to update to latest version
using System.Globalization;
using ServiceStack.IO;

namespace MyApp;

public class MarkdownWhatsNew(ILogger<MarkdownWhatsNew> log, IWebHostEnvironment env, IVirtualFiles fs)
    : MarkdownPagesBase<MarkdownFileInfo>(log, env, fs)
{
    public override string Id => "whatsnew";
    public Dictionary<string, List<MarkdownFileInfo>> Features { get; set; } = new();

    public List<MarkdownFileInfo> GetFeatures(string release)
    {
        return Features.TryGetValue(release, out var docs)
            ? Fresh(docs.Where(IsVisible).OrderBy(x => x.Order).ThenBy(x => x.FileName).ToList())
            : [];
    }
    
    public void LoadFrom(string fromDirectory)
    {
        Features.Clear();
        var dirs = VirtualFiles.GetDirectory(fromDirectory).GetDirectories().ToList();
        log.LogInformation("Found {Count} whatsnew directories", dirs.Count);

        var pipeline = CreatePipeline();

        foreach (var dir in dirs)
        {
            var datePart = dir.Name.LeftPart('_');
            if (!DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out var date))
            {
                log.LogWarning("Could not parse date '{DatePart}', ignoring...", datePart);
                continue;
            }

            var releaseVersion = dir.Name.RightPart('_');
            var releaseDate = date;

            foreach (var file in dir.GetFiles().OrderBy(x => x.Name))
            {
                try
                {
                    var doc = Load(file.VirtualPath, pipeline);
                    if (doc == null)
                        continue;
                    
                    doc.Date = releaseDate;
                    doc.Group = releaseVersion;

                    var releaseFeatures = Features.GetOrAdd(dir.Name, v => new List<MarkdownFileInfo>());
                    releaseFeatures.Add(doc);
                }
                catch (Exception e)
                {
                    log.LogError(e, "Couldn't load {VirtualPath}: {Message}", file.VirtualPath, e.Message);
                }
            }
        }
    }
    
    public override List<MarkdownFileBase> GetAll()
    {
        var to = new List<MarkdownFileBase>();
        foreach (var entry in Features)
        {
            to.AddRange(entry.Value.Where(IsVisible).Map(doc => ToMetaDoc(doc, x => x.Content = StripFrontmatter(doc.Content))));
        }
        return to;
    }
}