using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class PreProcessRequest : IPlugin, IHasStringId
{
    public string Id => "prerequest";

    /// <summary>
    /// Handle async file uploads 
    /// </summary>
    public Func<IRequest, IHttpFile, CancellationToken, Task<string>> HandleUploadFileAsync { get; set; }

    public void Register(IAppHost appHost)
    {
        if (HandleUploadFileAsync != null)
        {
            appHost.GlobalRequestFiltersAsync.Add(HandleFileUploadsAsync);
            appHost.GatewayRequestFiltersAsync.Add((req,dto) => HandleFileUploadsAsync(req, req.Response, dto));
        }
    }
    
    public async Task HandleFileUploadsAsync(IRequest req, IResponse res, object dto)
    {
        if (req.Files?.Length > 0)
        {
            var requestType = dto.GetType();
            // ignore unless a Request DTO Property contains [UploadTo] Attribute
            if (!HostContext.Metadata.GetOperation(requestType).RequestPropertyAttributes.Contains(typeof(UploadToAttribute)))
                return;

            var uploadFileAsync = HandleUploadFileAsync
                ?? throw new NotSupportedException("AppHost.HandleUploadFileAsync needs to be configured to allow file uploads in " + requestType.Name);

            var dtoProps = TypeProperties.Get(requestType);
            var uploadedPathsMap = new Dictionary<string,List<(string,IHttpFile)>>();
            foreach (var file in req.Files)
            {
                var uploadedPath = await uploadFileAsync(req, file, default).ConfigAwait();
                if (string.IsNullOrEmpty(uploadedPath))
                    continue;
                if (!uploadedPathsMap.TryGetValue(file.Name, out var uploadedPaths))
                    uploadedPaths = uploadedPathsMap[file.Name] = new List<(string,IHttpFile)>();
                uploadedPaths.Add((uploadedPath,file));
            }

            var dtoValues = new Dictionary<string, object>();
            foreach (var entry in uploadedPathsMap)
            {
                var prop = dtoProps.GetPublicProperty(entry.Key);
                if (prop == null)
                    continue;

                var paths = entry.Value.Map(x => x.Item1);
                if (prop.PropertyType == typeof(string))
                {
                    dtoValues[prop.Name] = paths[0];
                }
                else if (prop.PropertyType.HasInterface(typeof(IEnumerable<string>)))
                {
                    dtoValues[prop.Name] = paths.ConvertTo(prop.PropertyType);
                }
                else
                {
                    var elType = prop.PropertyType.GetCollectionType();
                    if (elType != null && elType != typeof(object))
                    {
                        var to = new List<object>();
                        foreach (var fileEntry in entry.Value)
                        {
                            var el = CreateFromHttpFileInfo(filePath:fileEntry.Item1, file:fileEntry.Item2, elType);
                            to.Add(el);
                        }
                        dtoValues[prop.Name] = to.ConvertTo(prop.PropertyType);
                    }
                    else if (prop.PropertyType != typeof(object) && prop.PropertyType.IsClass)
                    {
                        var fileEntry = entry.Value[0];
                        var to = CreateFromHttpFileInfo(filePath:fileEntry.Item1, file:fileEntry.Item2, prop.PropertyType);
                        dtoValues[prop.Name] = to;
                    }
                    else throw new NotSupportedException("Cannot populated uploaded Request.Files metadata to " + prop.PropertyType.Name);
                }
            }
            dtoValues.PopulateInstance(dto);
        }
    }
    
    static object CreateFromHttpFileInfo(string filePath, IHttpFile file, Type intoType)
    {
        var obj = new Dictionary<string, object>
        {
            [Keywords.FilePath] = filePath,
            [nameof(file.Name)] = file.Name,
            [nameof(file.FileName)] = file.FileName,
            [nameof(file.ContentLength)] = file.ContentLength,
            [nameof(file.ContentType)] = file.ContentType ?? MimeTypes.GetMimeType(file.FileName),
        };
        var o = obj.FromObjectDictionary(intoType);
        return o;
    }

    
}