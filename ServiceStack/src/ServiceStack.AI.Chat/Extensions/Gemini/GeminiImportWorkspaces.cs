using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>Own a detached private copy of named crawls, with atomic directory replacement.</summary>
public static class GeminiImportWorkspaces
{
    static string Name(string value) {
        var safe=Regex.Replace(value.Trim(),"[^A-Za-z0-9._-]+","-").Trim('.','-');
        if(safe.Length==0 || safe is "." or "..")throw new ArgumentException("Import name is required");return safe;
    }
    static bool Legacy(string ownedRoot,string manifest,JsonObject config) => Path.GetDirectoryName(Path.GetDirectoryName(manifest))==ownedRoot && Regex.IsMatch(Path.GetFileName(Path.GetDirectoryName(manifest)),@".+-[0-9a-f]{10}$") && config.GetString("importedFrom")!=null;
    public static string Target(string ownedRoot,string manifest)
    {
        manifest=GeminiIngest.ResolvePath(manifest);ownedRoot=GeminiIngest.ResolvePath(ownedRoot);var directory=Path.GetDirectoryName(manifest)!;var config=GeminiImportManifest.Read(manifest);var crawl=config.GetObject("crawl");
        if(string.IsNullOrEmpty(crawl.GetString("url")))return directory;
        if(GeminiIngest.WithinRoots(directory,[ownedRoot]) && !Legacy(ownedRoot,manifest,config))return directory;
        var name=crawl.GetString("name")??(Uri.TryCreate(crawl.GetString("url"),UriKind.Absolute,out var uri)?uri.Authority.ToLowerInvariant().Replace(':','-'):throw new ArgumentException("Invalid crawl URL"));
        if(Legacy(ownedRoot,manifest,config) && name==Path.GetFileName(directory))name=name[..^11];
        var target=Path.Combine(ownedRoot,Name(name));
        if(!ProjectsExtension.IsWithin(ProjectsExplorer.PhysicalPath(target),ownedRoot))throw new ArgumentException("Import path escapes the user's private folder");return target;
    }
    public static IEnumerable<string> LegacyManifests(string ownedRoot,string target)
    {
        if(!Directory.Exists(ownedRoot))return [];
        return Directory.EnumerateDirectories(ownedRoot).Where(x=>new DirectoryInfo(x).LinkTarget==null).Select(x=>Path.Combine(x,GeminiImportManifest.Filename))
            .Where(File.Exists).Where(x=>Legacy(GeminiIngest.ResolvePath(ownedRoot),x,GeminiImportManifest.Read(x)) && Target(ownedRoot,x)==target).ToArray();
    }
    public static async Task<string> ImportAsync(string ownedRoot,string manifest,CancellationToken token=default)
    {
        manifest=GeminiIngest.ResolvePath(manifest);ownedRoot=GeminiIngest.ResolvePath(ownedRoot);var source=Path.GetDirectoryName(manifest)!;var config=GeminiImportManifest.Read(manifest);
        var target=Target(ownedRoot,manifest);if(target==source)return manifest;
        if(ProjectsExtension.IsWithin(target,source))throw new ArgumentException("A crawl workspace cannot contain the private imports directory");
        var settings=config.GetObject("source")?.Clone()??new JsonObject();var input=settings.GetObject("config")?.Clone()??new JsonObject();var path=GeminiIngest.ResolvePath(Path.Combine(source,input.GetString("path")??"."));
        if(!GeminiIngest.WithinRoots(path,[source]))throw new ArgumentException("The crawl input folder must be inside its manifest workspace");
        Directory.CreateDirectory(ownedRoot);var staging=Path.Combine(ownedRoot,".import-"+Guid.NewGuid().ToString("N"));var copy=Path.Combine(staging,"workspace");var backup=Path.Combine(staging,"previous");Directory.CreateDirectory(staging);
        try {
            await Task.Run(()=>Copy(source,copy,token),token).ConfigureAwait(false);
            config["importedFrom"]=Legacy(ownedRoot,manifest,config)?config.GetString("importedFrom"):manifest;
            config.GetObject("crawl")!["name"]=Path.GetFileName(target);settings["name"]??="Import "+Path.GetFileName(source);input["path"]=Path.GetRelativePath(source,path).Replace('\\','/');settings["config"]=input;config["source"]=settings;
            await GeminiImportManifest.WriteAsync(Path.Combine(copy,GeminiImportManifest.Filename),config).ConfigureAwait(false);token.ThrowIfCancellationRequested();
            if(Directory.Exists(target))Directory.Move(target,backup);
            try{Directory.Move(copy,target);}catch{
                if(Directory.Exists(backup)) {
                    try { Directory.Move(backup,target); }
                    catch(Exception e) { throw new IOException("Import replacement failed; the previous workspace is preserved at "+backup,e); }
                }
                throw;
            }
            if(Directory.Exists(backup))Directory.Delete(backup,true);
            return Path.Combine(target,GeminiImportManifest.Filename);
        } finally {if(Directory.Exists(staging) && !Directory.Exists(backup))Directory.Delete(staging,true);}
    }
    static void Copy(string source,string destination,CancellationToken token,int depth=0)
    {
        if(depth>100)throw new ArgumentException("Import directory nesting is too deep");Directory.CreateDirectory(destination);
        foreach(var info in new DirectoryInfo(source).EnumerateFileSystemInfos()) {
            token.ThrowIfCancellationRequested();
            if(info.LinkTarget!=null || (info.Attributes&FileAttributes.ReparsePoint)!=0 || info.Name==GeminiImportManifest.Filename+".tmp")continue;
            var target=Path.Combine(destination,info.Name);
            if(info is DirectoryInfo)Copy(info.FullName,target,token,depth+1);else File.Copy(info.FullName,target);
        }
    }
}
