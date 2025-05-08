using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.DataAnnotations;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO;

public class ResourceVirtualFiles 
    : AbstractVirtualPathProviderBase
{
    protected ResourceVirtualDirectory RootDir;
    public Assembly BackingAssembly { get; }
    public string RootNamespace { get; }

    public override IVirtualDirectory RootDirectory => RootDir;
    public override string VirtualPathSeparator => "/";
    public override string RealPathSeparator => ".";
        
    public DateTime LastModified { get; set; } 

    public ResourceVirtualFiles(Type baseTypeInAssembly)
        : this(baseTypeInAssembly.Assembly, GetNamespace(baseTypeInAssembly)) { }

    public ResourceVirtualFiles(Assembly backingAssembly, string rootNamespace=null)
    {
        this.BackingAssembly = backingAssembly ?? throw new ArgumentNullException(nameof(backingAssembly));
        this.RootNamespace = rootNamespace ?? backingAssembly.GetName().Name;
        this.LastModified = GetAssemblyLastModified(BackingAssembly);

        Initialize();
    }
        
    private static DateTime GetAssemblyLastModified(Assembly asm)
    {
        try
        {
            return new FileInfo(asm.Location).LastWriteTime;
        }
        catch (Exception) { /* ignored */ }
        return DateTime.UtcNow;
    }
        
    //https://docs.microsoft.com/en-us/dotnet/api/system.resources.tools.stronglytypedresourcebuilder.verifyresourcename?redirectedfrom=MSDN&view=netframework-4.8#remarks
    static readonly char [] NamespaceSpecialChars = { ' ', '\u00A0', ',', ';', '|', '~', '@', '#', '%', '^', '&', 
        '*', '+', '-', /*'/', '\\',*/ '<', '>', '?', '[', ']', '(', ')', '{', 
        '}', '\"', '\'', '!'};

    private static string CleanChars(string name)
    {
        var newChars = new char[name.Length];
        var nameChars = name.AsSpan();
        for (var i = 0; i < nameChars.Length ;i++) 
        {
            newChars[i] = nameChars[i];
            foreach (var c in NamespaceSpecialChars)
            {
                if (nameChars[i] == c)
                {
                    newChars[i] = '_';
                    break;
                }
            }
        }
        return new string (newChars);
    }
        
    public static HashSet<string> PartialFileNames { get; set; } = new HashSet<string>
    {
        "min.js",
        "min.css",
    };

    public string CleanPath(string filePath)
    {
        var sanitizedPath = base.SanitizePath(filePath);
        if (sanitizedPath == null)
            return null;
        var lastDirPos = sanitizedPath.LastIndexOf('/');
        if (lastDirPos >= 0)
        {
            var dirPath = sanitizedPath.Substring(0, lastDirPos);
            var fileName = sanitizedPath.Substring(lastDirPos + 1);
            if (PartialFileNames.Contains(fileName))
            {
                var partialName = dirPath.LastRightPart('/');
                dirPath = dirPath.LastLeftPart('/');
                fileName = partialName + '.' + fileName;
            }
                
            var cleanDir = CleanChars(dirPath); //only dirs are replaced 
            var cleanPath = cleanDir + '/' + fileName;
            return cleanPath;
        }
        return sanitizedPath;
    }

    public override IVirtualFile GetFile(string virtualPath)
    {
        var virtualFile = RootDirectory.GetFile(CleanPath(virtualPath));
        virtualFile?.Refresh();
        return virtualFile;
    }

    private static string GetNamespace(Type type)
    {
        var attr = type.FirstAttribute<SchemaAttribute>();
        return attr != null ? attr.Name : type.Namespace;
    }

    protected sealed override void Initialize()
    {
        var asm = BackingAssembly;
        RootDir = new ResourceVirtualDirectory(this, null, asm, LastModified, RootNamespace);
    }

    public override string CombineVirtualPath(string basePath, string relativePath)
    {
        return string.Concat(basePath, VirtualPathSeparator, relativePath);
    }
}