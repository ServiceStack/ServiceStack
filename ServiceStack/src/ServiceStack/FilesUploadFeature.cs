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
        if (string.IsNullOrEmpty(basePath))
            throw new ArgumentNullException(nameof(basePath));
        if (basePath[0] != '/')
            throw new ArgumentException($@"{nameof(basePath)} must start with '/'", nameof(basePath));
        BasePath = basePath.TrimEnd('/');
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
        var locationName = pi.FirstAttribute<UploadToAttribute>()?.Location;
        return locationName != null ? GetLocation(locationName) : null;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<PreProcessRequest>(feature => {
            feature.HandleUploadFileAsync = async (req, file, token) => {
                var location = GetLocationFromProperty(req.Dto.GetType(), file.Name) 
                    ?? Locations.First();
                return await UploadFileAsync(location, req, await req.GetSessionAsync(token:token), file, token).ConfigAwait();
            };
        });
    }

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(StoreFileUploadService),   BasePath, BasePath.CombineWith("{Name}"));
        appHost.RegisterService(typeof(GetFileUploadService),     BasePath, BasePath.CombineWith("{Name}/{Path*}"));
        appHost.RegisterService(typeof(ReplaceFileUploadService), BasePath, BasePath.CombineWith("{Name}/{Path*}"));
        appHost.RegisterService(typeof(DeleteFileUploadService),  BasePath, BasePath.CombineWith("{Name}/{Path*}"));

        if (Locations.Length == 0)
        {
            Locations = new[] { new UploadLocation("default", appHost.VirtualFiles) };
        }
        
        appHost.AddToAppMetadata(meta =>
        {
            meta.Plugins.FilesUpload = new FilesUploadInfo
            {
                BasePath = BasePath,
                Locations = Locations.Map(x => new FilesUploadLocation {
                    Name = x.Name,
                    ReadAccessRole = x.ReadAccessRole,
                    WriteAccessRole = x.WriteAccessRole,
                    AllowExtensions = x.AllowExtensions,
                    AllowOperations = x.AllowOperations.ToString(),
                    MaxFileCount = x.MaxFileCount,
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

    public async Task<string> UploadFileAsync(UploadLocation location, IRequest req, IAuthSession session, IHttpFile file, CancellationToken token=default)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.WriteAccessRole, authSecret:req.GetAuthSecret(), token: token);
        var paths = ResolveUploadFilePath(location, req, file);
        var fs = file.InputStream;
        if (fs.CanSeek && fs.Position != 0)
            fs.Position = 0;
        await location.VirtualFiles.WriteFileAsync(paths.VirtualPath, fs, token).ConfigAwait();
        return paths.PublicPath;
    }

    public ResolvedPath ResolveUploadFilePath(UploadLocation location, IRequest req, IHttpFile file)
    {
        var canCreate = location.AllowOperations.HasFlag(FilesUploadOperation.Create);
        var canUpdate = location.AllowOperations.HasFlag(FilesUploadOperation.Update);
        var feature = HostContext.AppHost.AssertPlugin<FilesUploadFeature>();
        var ctx = new FilesUploadContext(feature, location, req, file.FileName);
        var publicPath = location.ResolvePath(ctx);
        if (string.IsNullOrEmpty(publicPath))
            throw new Exception("ResolvePath is Empty");
        if (publicPath[0] != '/')
            publicPath = '/' + publicPath;

        var vfsPath = publicPath.StartsWith(BasePath)
            ? publicPath.Substring(BasePath.Length)
            : publicPath;

        if (!canCreate || !canUpdate)
        {
            var existingFile = location.VirtualFiles.GetFile(vfsPath);
            if (!canUpdate && existingFile != null)
                throw HttpError.Forbidden(Errors.NoUpdateAccess.Localize(req));
            if (!canCreate && existingFile == null)
                throw HttpError.Forbidden(Errors.NoCreateAccess.Localize(req));
        }

        if (location.MaxFileCount != null && req.Files.Length > location.MaxFileCount)
            throw new ArgumentException(Errors.ExceededMaxFileCountFmt.LocalizeFmt(req,location.MaxFileCount), file.Name);

        ValidateFileUpload(location, req, file, vfsPath);

        return new ResolvedPath(publicPath, vfsPath);
    }

    public void ValidateFileUpload(UploadLocation location, IRequest req, IHttpFile file, string vfsPath)
    {
        var param = file.Name;
        if (location.AllowExtensions != null)
        {
            var ext = vfsPath.LastRightPart('.');
            if (!location.AllowExtensions.Contains(ext))
                throw new ArgumentException(Errors.InvalidFileExtensionFmt.LocalizeFmt(req, string.Join(", ",
                    location.AllowExtensions.Select(x => '.' + x).ToList().OrderBy(x => x))), param);
        }

        if (location.MinFileBytes != null && file.ContentLength < location.MinFileBytes)
            throw new ArgumentException(Errors.ExceededMinFileBytesFmt.LocalizeFmt(req, location.MinFileBytes), param);
        if (location.MaxFileBytes != null && file.ContentLength > location.MaxFileBytes)
            throw new ArgumentException(Errors.ExceededMaxFileBytesFmt.LocalizeFmt(req, location.MaxFileBytes), param);
        location.ValidateUpload?.Invoke(req, file);
    }

    public async Task<IVirtualFile?> GetFileAsync(UploadLocation location, IRequest req, IAuthSession session, string vfsPath)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.ReadAccessRole, authSecret:req.GetAuthSecret());
        if (!location.AllowOperations.HasFlag(FilesUploadOperation.Read))
            throw HttpError.NotFound(Errors.NoReadAccess.Localize(req));

        var existingFile = location.VirtualFiles.GetFile(vfsPath);
        location.ValidateDownload?.Invoke(req, existingFile);
        return existingFile;
    }

    public async Task ReplaceFileAsync(UploadLocation location, IRequest req, IAuthSession session, string vfsPath, CancellationToken token=default)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.WriteAccessRole, authSecret:req.GetAuthSecret(), token);
        if (!location.AllowOperations.HasFlag(FilesUploadOperation.Update))
            throw HttpError.Forbidden(Errors.NoUpdateAccess.Localize(req));
        if (req.Files.Length != 1)
            throw HttpError.BadRequest(Errors.BadRequest.Localize(req));
        
        var existingFile = location.VirtualFiles.GetFile(vfsPath);
        if (existingFile != null)
        {
            var file = req.Files[0];
            ValidateFileUpload(location, req, file, vfsPath);
            var fs = file.InputStream;
            if (fs.CanSeek && fs.Position != 0)
                fs.Position = 0;
            await location.VirtualFiles.WriteFileAsync(vfsPath, fs, token).ConfigAwait();
        }
        else throw HttpError.NotFound(Errors.FileNotExists);
    }

    public async Task<bool> DeleteFileAsync(UploadLocation location, IRequest req, IAuthSession session, string vfsPath)
    {
        await RequestUtils.AssertAccessRoleAsync(req, accessRole:location.WriteAccessRole, authSecret:req.GetAuthSecret());
        if (!location.AllowOperations.HasFlag(FilesUploadOperation.Delete))
            throw HttpError.Forbidden(Errors.NoDeleteAccess.Localize(req));
        
        var existingFile = location.VirtualFiles.GetFile(vfsPath);
        if (existingFile != null)
        {
            location.ValidateDelete?.Invoke(req, existingFile);
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
    public string FileNotExists { get; set; } = "File not found";
    public string BadRequest { get; set; } = "Bad Request";
    public string NoDeleteAccess { get; set; } = "Deleting file not permitted";
    public string InvalidFileExtensionFmt { get; set; } = "Supported file extensions: {0}";
    public string ExceededMaxFileCountFmt { get; set; } = "Exceeded maximum files: {0}";
    public string ExceededMinFileBytesFmt { get; set; } = "Min file size: {0} bytes";
    public string ExceededMaxFileBytesFmt { get; set; } = "Max file size: {0} bytes";
}    

[Flags]
public enum FilesUploadOperation
{
    None   = 0,
    Read   = 1 << 0,
    Create = 1 << 1,
    Update = 1 << 2,
    Delete = 1 << 3,
    Write  = Create | Update | Delete,
    All    = Read | Create | Update | Delete,
}

[DefaultRequest(typeof(StoreFileUpload))]
public class StoreFileUploadService : Service
{
    public async Task<object> Any(StoreFileUpload request)
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
        return new StoreFileUploadResponse {
            Results = results,
        };
    }
}

[DefaultRequest(typeof(GetFileUpload))]
public class GetFileUploadService : Service
{
    public async Task<object> Get(GetFileUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        var session = await Request.GetSessionAsync();
        var vfsPath = location.Name.CombineWith(request.Path);
        var file = await feature.GetFileAsync(location, Request, session, vfsPath).ConfigAwait();
        if (file == null)
            throw HttpError.NotFound(feature.Errors.FileNotExists);
        var asAttachment = request.Attachment
            ?? !FileExt.WebFormats.Contains(request.Path.LastRightPart('.'));
        return new HttpResult(file, asAttachment:asAttachment);
    }
}

[DefaultRequest(typeof(ReplaceFileUpload))]
public class ReplaceFileUploadService : Service
{
    public async Task<object> Put(ReplaceFileUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        var session = await Request.GetSessionAsync();
        var vfsPath = location.Name.CombineWith(request.Path);
        await feature.ReplaceFileAsync(location, Request, session, vfsPath).ConfigAwait();
        return new ReplaceFileUploadResponse();
    }
}

[DefaultRequest(typeof(DeleteFileUpload))]
public class DeleteFileUploadService : Service
{
    public async Task<object> Delete(DeleteFileUpload request)
    {
        var feature = AssertPlugin<FilesUploadFeature>();
        var location = feature.AssertLocation(request.Name, Request);
        var session = await Request.GetSessionAsync();
        var vfsPath = location.Name.CombineWith(request.Path);
        var result = await feature.DeleteFileAsync(location, Request, session, vfsPath).ConfigAwait();
        return new DeleteFileUploadResponse {
            Result = result
        };
    }
}

public readonly struct ResolvedPath
{
    public string PublicPath { get; }
    public string VirtualPath { get; }

    public ResolvedPath(string publicPath, string virtualPath)
    {
        PublicPath = publicPath;
        VirtualPath = virtualPath;
    }
}

public readonly struct FilesUploadContext
{
    /// <summary>
    /// The current HTTP Request
    /// </summary>
    public IRequest Request { get; }
    /// <summary>
    /// The Request DTO
    /// </summary>
    public object Dto => Request.Dto;
    /// <summary>
    /// The Request DTO explicitly casted to a strong Type  
    /// </summary>
    public T GetDto<T>() => (T)Request.Dto;
    /// <summary>
    /// The Uploaded file name
    /// </summary>
    public string FileName { get; }
    /// <summary>
    /// The Uploaded file extension
    /// </summary>
    public string FileExtension => FileName.LastRightPart('.');
    /// <summary>
    /// Date String Formatted as 'yyyy/MM/dd'
    /// </summary>
    public string DateSegment => DateTime.UtcNow.ToString("yyyy/MM/dd");
    /// <summary>
    /// The FilesUploadFeature Plugin 
    /// </summary>
    public FilesUploadFeature Feature { get; }
    /// <summary>
    /// The UploadLocation used for this upload
    /// </summary>
    public UploadLocation Location { get; }
    /// <summary>
    /// The User Session associated with this Request
    /// </summary>
    public IAuthSession Session => Request.GetSession();
    /// <summary>
    /// The Authenticated User Id
    /// </summary>
    public string UserAuthId => Session.UserAuthId;

    public FilesUploadContext(FilesUploadFeature feature, UploadLocation location, IRequest request, string fileName)
    {
        Feature = feature;
        Location = location;
        Request = request;
        FileName = fileName;
    }

    public string GetLocationPath(string relativePath) => Feature.BasePath.CombineWith(Location.Name, relativePath);
}

public class UploadLocation
{
    public UploadLocation(string name, 
        IVirtualFiles virtualFiles, Func<FilesUploadContext,string>? resolvePath = null, 
        string readAccessRole = RoleNames.AllowAnyUser, string writeAccessRole = RoleNames.AllowAnyUser, 
        string[]? allowExtensions = null, FilesUploadOperation allowOperations = FilesUploadOperation.All, 
        int? maxFileCount = null, long? minFileBytes = null, long? maxFileBytes = null,
        Action<IRequest,IHttpFile>? validateUpload = null, Action<IRequest,IVirtualFile>? validateDownload = null,
        Action<IRequest,IVirtualFile>? validateDelete = null,
        Func<IRequest,IVirtualFile,object>? fileResult = null)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.VirtualFiles = virtualFiles ?? throw new ArgumentNullException(nameof(virtualFiles));
        this.ResolvePath = resolvePath ?? (ctx => ctx.GetLocationPath($"{DateTime.UtcNow:yyyy/MM/dd}/{ctx.FileName}"));
        this.ReadAccessRole = readAccessRole ?? throw new ArgumentNullException(nameof(readAccessRole));
        this.WriteAccessRole = writeAccessRole ?? throw new ArgumentNullException(nameof(writeAccessRole));
        this.AllowExtensions = allowExtensions?.ToSet(StringComparer.OrdinalIgnoreCase);
        this.AllowOperations = allowOperations;
        this.MaxFileCount = maxFileCount;
        this.MinFileBytes = minFileBytes;
        this.MaxFileBytes = maxFileBytes;
        this.ValidateUpload = validateUpload;
        this.ValidateDownload = validateDownload;
        this.ValidateDelete = validateDelete;
        this.FileResult = fileResult;
    }

    public string Name { get; set; }
    public IVirtualFiles VirtualFiles { get; set; }
    public string ReadAccessRole { get; set; }
    public string WriteAccessRole { get; set; }
    public HashSet<string>? AllowExtensions { get; set; }
    public FilesUploadOperation AllowOperations { get; set; }
    public int? MaxFileCount { get; set; }
    public long? MinFileBytes { get; set; }
    public long? MaxFileBytes { get; set; }
    public Func<FilesUploadContext,string> ResolvePath { get; set; }
    public Action<IRequest,IHttpFile>? ValidateUpload { get; set; }
    public Action<IRequest,IVirtualFile>? ValidateDownload { get; set; }
    public Action<IRequest,IVirtualFile>? ValidateDelete { get; set; }
    Func<IRequest,IVirtualFile,object>? FileResult { get; set; }
}

public static class FileExt
{
    public static string[] WebImages { get; set; } = { "png", "jpg", "jpeg", "gif", "svg", "webp" };
    public static string[] BinaryImages { get; set; } = { "png", "jpg", "jpeg", "gif", "bmp", "tif", "tiff", "webp", "ai", "psd", "ps" };
    public static string[] Images { get; set; } = WebImages.CombineDistinct(BinaryImages);
    public static string[] WebVideos { get; set; } = { "avi", "m4v", "mov", "mp4", "mpg", "mpeg", "wmv", "webm" };
    public static string[] WebAudios { get; set; } = { "mp3", "mpa", "ogg", "wav", "wma", "mid", "webm" };
    public static string[] BinaryDocuments { get; set; } = { "doc", "docx", "pdf", "rtf" };
    public static string[] TextDocuments { get; set; } = { "tex", "txt", "md", "rst" };
    public static string[] Spreadsheets { get; set; } = { "xls", "xlsm", "xlsx", "ods", "csv", "txv" };
    public static string[] Presentations { get; set; } = { "key", "odp", "pps", "ppt", "pptx" };
    public static string[] AllDocuments { get; set; } = BinaryDocuments.CombineDistinct(TextDocuments, Presentations, Spreadsheets);
    public static string[] WebFormats { get; set; } = WebImages.CombineDistinct(WebVideos, WebAudios);
}
