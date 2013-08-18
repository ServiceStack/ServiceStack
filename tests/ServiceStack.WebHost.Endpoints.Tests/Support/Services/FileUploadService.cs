using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[DataContract]
	[Route("/fileuploads/{RelativePath*}")]
	[Route("/fileuploads", HttpMethods.Post)]
	public class FileUpload
	{
		[DataMember]
		public string RelativePath { get; set; }

        [DataMember]
        public string CustomerName { get; set; }

        [DataMember]
        public int CustomerId { get; set; }
	}

	[DataContract]
	public class FileUploadResponse : IHasResponseStatus
	{
		[DataMember]
		public string FileName { get; set; }

		[DataMember]
		public long ContentLength { get; set; }

		[DataMember]
		public string ContentType { get; set; }

		[DataMember]
		public string Contents { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }

        [DataMember]
        public string CustomerName { get; set; }

        [DataMember]
        public int CustomerId { get; set; }
	}

	public class FileUploadService : ServiceInterface.Service
	{
		public object Get(FileUpload request)
		{
			if (request.RelativePath.IsNullOrEmpty())
				throw new ArgumentNullException("RelativePath");

			var filePath = ("~/" + request.RelativePath).MapProjectPath();
			if (!File.Exists(filePath))
				throw new FileNotFoundException(request.RelativePath);

			var result = new HttpResult(new FileInfo(filePath));
			return result;
		}

		public object Post(FileUpload request)
		{
			if (this.RequestContext.Files.Length == 0)
				throw new FileNotFoundException("UploadError", "No such file exists");

			if (request.RelativePath == "ThrowError")
				throw new NotSupportedException("ThrowError");

			var file = this.RequestContext.Files[0];
			return new FileUploadResponse
			{
				FileName = file.FileName,
				ContentLength = file.ContentLength,
				ContentType = file.ContentType,
				Contents = new StreamReader(file.InputStream).ReadToEnd(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName
			};
		}
	}
}