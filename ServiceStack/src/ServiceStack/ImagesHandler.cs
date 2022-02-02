#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
///  Handler to manage in memory collection of images
/// </summary>
public class ImagesHandler : HttpAsyncTaskHandler
{
    public string Path { get; }
    public Dictionary<string, StaticContent> ImageContents { get; } = new();
        
    public StaticContent Fallback { get; }

    public ImagesHandler(string path, StaticContent fallback)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
    }

    public virtual string? RewriteImageUri(string imageUri)
    {
        var content = StaticContent.CreateFromDataUri(imageUri);
        if (content == null)
            return null;

        var file = imageUri.ToMd5Hash() + '.' + MimeTypes.GetExtension(content.MimeType);
        Save(file, content);
        return Path.CombineWith(file);
    }

    public virtual void Save(string path, StaticContent content) => 
        ImageContents[path] = content;

    public virtual StaticContent? Get(string path) => 
        ImageContents.TryGetValue(path, out var imageUri) ? imageUri : null;

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        var path = httpReq.PathInfo.RightPart(Path).TrimStart('/');
        var content = Get(path) ?? Fallback;
        httpRes.ContentType = content.MimeType;
        await httpRes.OutputStream.WriteAsync(content.Data).ConfigAwait();
    }
}

/// <summary>
///  Handler to manage persistent images
/// </summary>
public class PersistentImagesHandler : ImagesHandler
{
    public IVirtualFiles VirtualFiles { get; }
    public string DirPath { get; }

    public PersistentImagesHandler(string path, StaticContent fallback, IVirtualFiles virtualFiles, string dirPath)
        : base(path, fallback)
    {
        VirtualFiles = virtualFiles;
        DirPath = dirPath;
    }

    public override StaticContent? Get(string path)
    {
        var file = VirtualFiles.GetFile(DirPath.CombineWith(path));
        if (file == null)
            return null;

        return new StaticContent(file.GetBytesContentsAsMemory(), MimeTypes.GetExtension(file.Extension));
    }
        
    public override void Save(string path, StaticContent content)
    {
        VirtualFiles.WriteFile(DirPath.CombineWith(path), content.Data);
    }
}
