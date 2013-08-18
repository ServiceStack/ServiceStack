using System;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel.Dispatcher;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Route("/fileuploads/{RelativePath*}", HttpMethods.Get)]
	[Route("/fileuploads", HttpMethods.Post)]
	public class FileUpload
	{
		[DataMember]
		public string RelativePath { get; set; }
	}

	[DataContract]
	public class FileUploadResponse
	{
		[DataMember]
		public string FileName { get; set; }

		[DataMember]
		public long ContentLength { get; set; }

		[DataMember]
		public string ContentType { get; set; }

		[DataMember]
		public string Contents { get; set; }
	}

	public class FileUploadService : ServiceInterface.Service
	{
		public object Get(FileUpload request)
		{
			if (request.RelativePath.IsNullOrEmpty())
				throw new ArgumentNullException("RelativePath");

			var filePath = ("~/" + request.RelativePath).MapHostAbsolutePath();
			if (!File.Exists(filePath))
				throw new FilterInvalidBodyAccessException(request.RelativePath);

			var result = new HttpResult(new FileInfo(filePath));
			return result;
		}

		public object Post(FileUpload request)
		{
			if (this.RequestContext.Files.Length == 0)
                throw new ValidationError("UploadError", "No such file exists");

			var file = this.RequestContext.Files[0];
			return new FileUploadResponse
			{
				FileName = file.FileName,
				ContentLength = file.ContentLength,
				ContentType = file.ContentType,
				Contents = new StreamReader(file.InputStream).ReadToEnd(),
			};
		}
	}
}