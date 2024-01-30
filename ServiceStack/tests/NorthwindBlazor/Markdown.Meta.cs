using ServiceStack.IO;
using ServiceStack.Text;

namespace MyApp;

public class MarkdownMeta
{
    public List<IMarkdownPages> Features { get; set; } = [];

    public async Task RenderToAsync(string metaDir, string baseUrl)
    {
        FileSystemVirtualFiles.RecreateDirectory(metaDir);
        using var scope = JsConfig.With(new Config { ExcludeTypeInfo = true });
        var featureDocs = new Dictionary<string, List<MarkdownFileBase>>();
        var allYears = new HashSet<int>();
        var index = new Dictionary<string, object>();
        foreach (var feature in Features.Safe())
        {
            var allDocs = feature.GetAll()
                .OrderByDescending(x => x.Date!.Value)
                .ThenBy(x => x.Order)
                .ThenBy(x => x.FileName)
                .ToList();
            allDocs.ForEach(x => {
                if (x.Url?.StartsWith("/") == true)
                    x.Url = baseUrl.CombineWith(x.Url);
                if (x.Image?.StartsWith("/") == true)
                    x.Image = baseUrl.CombineWith(x.Image);
            });
            featureDocs[feature.Id] = allDocs;
            var featureYears = allDocs.Select(x => x.Date!.Value.Year).Distinct().OrderBy(x => x).ToList();
            featureYears.ForEach(x => allYears.Add(x));
            
            index[feature.Id] = featureYears.Map(x => baseUrl.CombineWith($"/meta/{x}/{feature.Id}.json"));
            foreach (var year in featureYears)
            {
                var yearDocs = allDocs
                    .Where(x => x.Date!.Value.Year == year)
                    .ToList();
                var yearDir = metaDir.CombineWith(year).AssertDir();
                var metaPath = yearDir.CombineWith($"{feature.Id}.json");
                await File.WriteAllTextAsync(metaPath, yearDocs.ToJson());
            }
        }
        await File.WriteAllTextAsync(metaDir.CombineWith("index.json"), JSON.stringify(index));

        await File.WriteAllTextAsync(metaDir.CombineWith("all.json"), JSON.stringify(featureDocs));
        foreach (var year in allYears.OrderBy(x => x))
        {
            var yearDocs = new Dictionary<string, List<MarkdownFileBase>>();
            foreach (var entry in featureDocs)
            {
                yearDocs[entry.Key] = entry.Value
                    .Where(x => x.Date!.Value.Year == year)
                    .ToList();
            }
            await File.WriteAllTextAsync(metaDir.CombineWith($"{year}/all.json"), JSON.stringify(yearDocs));
        }
    }
}