using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    [Route("/partialfiles/{RelativePath*}")]
    public class PartialFileRequest
    {
        [DataMember]
        public string RelativePath { get; set; }

        [DataMember]
        public string MimeType { get; set; }
    }

    [DataContract]
    [Route("/partialfiles/memory")]
    public class PartialFromMemoryRequest
    {
        
        
    }

    public class PartialContentService : ServiceInterface.Service
    {
        public object Get(PartialFileRequest request)
        {
            if (request.RelativePath.IsNullOrEmpty())
                throw new ArgumentNullException("RelativePath");

            string filePath = ("~/" + request.RelativePath).MapProjectPath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException(request.RelativePath);

            //allow overriding the mime type
            if (string.IsNullOrEmpty(request.MimeType))
                return new PartialContentResult(new FileInfo(filePath));
            return new PartialContentResult(new FileInfo(filePath), request.MimeType);
        }

        public object Get(PartialFromMemoryRequest request)
        {
            var customText = "123456789012345678901234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);

            var httpResult = new PartialContentResult(ms, "audio/mpeg");
            return httpResult;
        }
    }


}