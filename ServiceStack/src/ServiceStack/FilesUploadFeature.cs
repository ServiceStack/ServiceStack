#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class FilesUploadFeature : IPlugin, IHasStringId, IPreInitPlugin
{
    public string Id => "filesupload";
    public UploadLocation[] Locations { get; set; }
    public string BasePath { get; set; }
    public FilesUploadErrorMessages Errors { get; set; } = new();

    public Func<IRequest, IVirtualFile, object> FileResult { get; set; } = DefaultFileResult;

    public static object DefaultFileResult(IRequest req, IVirtualFile file) =>
        new HttpResult(file, asAttachment: FileExt.Images.Contains(file.Extension));

    public FilesUploadFeature(string basePath, params UploadLocation[] locations)
    {
        BasePath = basePath;
        Locations = locations;
    }
    public FilesUploadFeature(params UploadLocation[] locations) : this("/uploads", locations) {}

    public UploadLocation? GetLocation(string name) =>
        Locations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public UploadLocation? GetLocationFromProperty(Type requestType, string propName)
    {
        var props = TypeProperties.Get(requestType);
        var pi = props.GetPublicProperty(propName);
        if (pi == null) return null;
        var locationName = pi.FirstAttribute<InputAttribute>()?.Target;
        return locationName != null ? GetLocation(locationName) : null;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<PreProcessRequest>(feature =>
        {
            // preprocess.HandleUploadFile = (req, file) => {
            //     var location = GetLocationFromProperty(req.Dto.GetType(), file.Name)
            //                    ?? Locations.First();
            //     return UploadFile(location, req, req.GetSession(), file);
            // };
            feature.HandleUploadFileAsync = async (req, file, token) => {
                var location = GetLocationFromProperty(req.Dto.GetType(), file.Name) 
                    ?? Locations.First();
                return await UploadFileAsync(location, req, await req.GetSessionAsync(token:token), file, token).ConfigAwait();
            };
        });
    }

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(PostFilesUploadService),   BasePath, BasePath.CombineWith("{Name}"));
        appHost.RegisterService(typeof(GetFilesUploadService),    BasePath, BasePath.CombineWith("{Name}/{File*}"));
        appHost.RegisterService(typeof(DeleteFilesUploadService), BasePath.CombineWith("{Name}/{File*}"));

        if (Locations.Length == 0)
        {
            Locations = new[] {
                new UploadLocation("default", appHost.VirtualFiles, resolvePath:(req,fileName) => BasePath.CombineWith(fileName))
            };
        }
        
        appHost.AddToAppMetadata(meta =>
        {
            meta.Plugins.FilesUpload = new FilesUploadInfo
            {
                BasePath = BasePath,
                Locations = Locations.Map(x => new FilesUploadLocation {
                    Name = x.Name,
                    AccessRole = x.AccessRole,
                    AllowExtensions = x.AllowExtensions,
                    AllowOperations = x.AllowOperations.ToString(),
                    MinFileBytes = x.MinFileBytes,
                    MaxFileBytes = x.MaxFileBytes,
                })
            };
        });
    }

    public UploadLocation AssertLocation(string name, IRequest? req=null)
    {
        var location = GetLocation(name)
            ?? throw HttpError.NotFound(Errors.UnknownLocationFmt.LocalizeFmt(req,name));
        return location;
    }

    public string UploadFile(UploadLocation location, IRequest req, IAuthSession session, IHttpFile file)
    {
        RequestUtils.AssertAccessRole(req, accessRole:location.AccessRole, authSecret:req.GetAuthSecret());
        var vfsPath = ResolveUploadFilePath(location, req, file);
        location.VirtualFiles.WriteFile(vfsPath, file.InputStream);
        return vfsPath;
    }

    public async Task<string> UploadFileAsync(UploadLocation location, IRequest req, IAuthSession session, IHttpFile file, CancellationToken token=default)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.AccessRole, authSecret:req.GetAuthSecret(), token: token);
        var vfsPath = ResolveUploadFilePath(location, req, file);
        await location.VirtualFiles.WriteFileAsync(vfsPath, file.InputStream, token).ConfigAwait();
        return vfsPath;
    }

    public string ResolveUploadFilePath(UploadLocation location, IRequest req, IHttpFile file)
    {
        var canCreate = location.AllowOperations.HasFlag(FilesUploadOperation.Create);
        var canUpdate = location.AllowOperations.HasFlag(FilesUploadOperation.Update);
        var vfsPath = location.ResolvePath(req, file.FileName);

        if (!canCreate || !canUpdate)
        {
            var existingFile = location.VirtualFiles.GetFile(vfsPath);
            if (!canUpdate && existingFile != null)
                throw HttpError.Forbidden(Errors.NoUpdateAccess.Localize(req));
            if (!canCreate && existingFile == null)
                throw HttpError.Forbidden(Errors.NoCreateAccess.Localize(req));
        }

        if (location.AllowExtensions != null)
        {
            var ext = vfsPath.LastRightPart('.');
            if (!location.AllowExtensions.Contains(ext))
                throw HttpError.Forbidden(Errors.InvalidFileExtensionFmt.LocalizeFmt(string.Join(", ",
                    location.AllowExtensions.Select(x => '.' + x).ToList().OrderBy(x => x))));
        }

        if (location.MinFileBytes != null && file.ContentLength < location.MinFileBytes)
            throw HttpError.Forbidden($"Min file size: {location.MinFileBytes} bytes");
        if (location.MaxFileBytes != null && file.ContentLength > location.MaxFileBytes)
            throw HttpError.Forbidden($"Max file size: {location.MinFileBytes} bytes");
        location.Validate?.Invoke(req, file);
        
        return vfsPath;
    }

    public async Task<IVirtualFile> GetFileAsync(UploadLocation location, IRequest req, IAuthSession session, string file)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.AccessRole, authSecret:req.GetAuthSecret());
        var vfsPath = location.ResolvePath(req, file);
        if (!location.AllowOperations.HasFlag(FilesUploadOperation.Read))
            throw HttpError.NotFound(Errors.NoReadAccess.Localize(req));

        var existingFile = location.VirtualFiles.GetFile(vfsPath);
        return existingFile;
    }

    public async Task<bool> DeleteFileAsync(UploadLocation location, IRequest req, IAuthSession session, string file)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.AccessRole, authSecret:req.GetAuthSecret());
        var vfsPath = location.ResolvePath(req, file);
        if (!location.AllowOperations.HasFlag(FilesUploadOperation.Delete))
            throw HttpError.Forbidden(Errors.NoDeleteAccess.Localize(req));
        
        var existingFile = location.VirtualFiles.GetFile(vfsPath);
        if (existingFile != null)
        {
            location.VirtualFiles.DeleteFile(vfsPath);
            return true;
        }
        return false;
    }
}

public class FilesUploadErrorMessages
{
    public string UnknownLocationFmt { get; set; } = "Unknown upload location: {0}";
    public string NoUpdateAccess { get; set; } = "Uploading existing files not permitted";
    public string NoCreateAccess { get; set; } = "Uploading new files not permitted";
    public string NoReadAccess { get; set; } = "File not found";
    public string NoDeleteAccess { get; set; } = "Deleting file not permitted";
    public string InvalidFileExtensionFmt { get; set; } = "Supported file extensions: {0}";
}    

[Flags]
public enum FilesUploadOperation
{
    None   = 0,
    Read   = 1 << 0,
    Create = 1 << 1,
    Update = 1 << 2,
    Delete = 1 << 3,
    Write = Create | Update | Delete,
    All =   Read | Create | Update | Delete,
}

public class PostFilesUpload : IReturn<PostFilesUploadResponse>, IHasBearerToken, IPost
{
    public string Name { get; set; }
    public string File { get; set; }
    public string? BearerToken { get; set; }
}

public class PostFilesUploadResponse
{
    public List<string> Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public class GetFilesUpload : IReturn<byte[]>, IHasBearerToken, IGet
{
    public string Name { get; set; }
    public string File { get; set; }
    public string? BearerToken { get; set; }
}

public class DeleteFilesUpload : IReturn<DeleteFilesUploadResponse>, IHasBearerToken, IDelete
{
    public string Name { get; set; }
    public string File { get; set; }
    public string? BearerToken { get; set; }
}
public class DeleteFilesUploadResponse
{
    public bool Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(PostFilesUpload))]
public class PostFilesUploadService : Service
{
    public async Task<object> Post(PostFilesUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        if (Request.Files.Length == 0)
            throw HttpError.BadRequest("No files uploaded");

        var session = await Request.GetSessionAsync();
        var results = new List<string>();
        foreach (var file in Request.Files)
        {
            var path = await feature.UploadFileAsync(location, Request, session, file).ConfigAwait();
            results.Add(path);
        }
        return new PostFilesUploadResponse {
            Results = results,
        };
    }
}

[DefaultRequest(typeof(GetFilesUpload))]
public class GetFilesUploadService : Service
{
    public async Task<object> Get(GetFilesUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        var session = await Request.GetSessionAsync();
        var file = await feature.GetFileAsync(location, Request, session, request.File).ConfigAwait();
        return new HttpResult(file, asAttachment:true);
    }
}

[DefaultRequest(typeof(DeleteFilesUpload))]
public class DeleteFilesUploadService : Service
{
    public async Task<object> Any(DeleteFilesUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        var session = await Request.GetSessionAsync();
        var result = await feature.DeleteFileAsync(location, Request, session, request.File).ConfigAwait();
        return new DeleteFilesUploadResponse {
            Result = result
        };
    }
}

public class UploadLocation
{
    public UploadLocation(string name, 
        IVirtualFiles virtualFiles, Func<IRequest,string,string>? resolvePath = null, 
        string accessRole = RoleNames.AllowAnyUser, 
        string[]? allowExtensions = null, FilesUploadOperation allowOperations = FilesUploadOperation.All, 
        long? minFileBytes = null, long? maxFileBytes = null,
        Action<IRequest,IHttpFile>? validate = null,
        Func<IRequest,IVirtualFile,object>? fileResult = null)
    {
        this.Name = name;
        this.VirtualFiles = virtualFiles;
        this.ResolvePath = resolvePath ?? ((IRequest req, string fileName) => $"/{Name}/{fileName}");
        this.AccessRole = accessRole;
        this.AllowExtensions = allowExtensions?.ToSet();
        this.AllowOperations = allowOperations;
        this.MinFileBytes = minFileBytes;
        this.MaxFileBytes = maxFileBytes;
        this.Validate = validate;
        this.FileResult = fileResult;
    }

    public string Name { get; set; }
    public IVirtualFiles VirtualFiles { get; set; }
    public string AccessRole { get; set; }
    public HashSet<string>? AllowExtensions { get; set; }
    public FilesUploadOperation AllowOperations { get; set; }
    public long? MinFileBytes { get; set; }
    public long? MaxFileBytes { get; set; }
    public Func<IRequest,string,string> ResolvePath { get; set; }
    public Action<IRequest,IHttpFile>? Validate { get; set; }
    Func<IRequest,IVirtualFile,object>? FileResult { get; set; }
}

public static class FileExt
{
    public static string[] StandardImages { get; set; } = { "png", "jpg", "jpeg", "gif", };
    public static string[] WebImages { get; set; } = { "png", "jpg", "jpeg", "gif", "svg", "webp" };
    public static string[] BinaryImages { get; set; } = { "png", "jpg", "jpeg", "gif", "bmp", "tif", "tiff", "webp" };
    public static string[] Images { get; set; } = { "png", "jpg", "jpeg", "gif", "bmp", "tif", "tiff", "webp", "svg", "ico" };
}
