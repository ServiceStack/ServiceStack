using System;
using System.Collections.Generic;
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
            appHost.GlobalRequestFiltersAsync.Add(HandleFileUploadsAsync);
    }
    
    public async Task HandleFileUploadsAsync(IRequest req, IResponse res, object dto)
    {
        if (req.Files?.Length > 0)
        {
            var uploadFileAsync = HandleUploadFileAsync
                ?? throw new NotSupportedException("AppHost.HandleUploadFileAsync needs to be configured to allow file uploads in " + dto.GetType().Name);

            var uploadedPathsMap = new Dictionary<string,List<string>>();
            foreach (var file in req.Files)
            {
                var uploadedPath = await uploadFileAsync(req, file, default).ConfigAwait();
                if (string.IsNullOrEmpty(uploadedPath))
                    continue;
                if (!uploadedPathsMap.TryGetValue(file.Name, out var uploadedPaths))
                    uploadedPaths = uploadedPathsMap[file.Name] = new List<string>();
                uploadedPaths.Add(uploadedPath);
            }

            var dtoValues = new Dictionary<string, object>();
            var dtoProps = TypeProperties.Get(dto.GetType());
            foreach (var entry in uploadedPathsMap)
            {
                var prop = dtoProps.GetPublicProperty(entry.Key);
                if (prop == null)
                    continue;
                
                if (prop.PropertyType == typeof(string))
                {
                    dtoValues[prop.Name] = entry.Value[0];
                }
                else
                {
                    dtoValues[prop.Name] = entry.Value.ConvertTo(prop.PropertyType);
                }
            }
            dtoValues.PopulateInstance(dto);
        }
    }
}