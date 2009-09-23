using System.IO;
using RemoteInfo.ServiceModel.Operations;
using RemoteInfo.ServiceModel.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace RemoteInfo.ServiceInterface
{
	[Port(typeof(GetDirectoryInfo))]
	public class GetDirectoryInfoHandler
		: IService
	{
		private readonly RemoteInfoConfig config;

		public GetDirectoryInfoHandler(RemoteInfoConfig config)
		{
			this.config = config;
		}

		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetDirectoryInfo>();

			var showDirPath = Path.Combine(this.config.RootDirectory, GetSafePath(request.ForPath ?? string.Empty));

			var response = new GetDirectoryInfoResponse();

			foreach (var dirPath in Directory.GetDirectories(showDirPath))
			{
				var dirInfo = new DirectoryInfo(dirPath);

				response.Directories.Add(new DirectoryResult {
					Name = dirInfo.Name,
					FileCount = dirInfo.GetFileSystemInfos().Length
				});
			}

			foreach (var filePath in Directory.GetFiles(showDirPath))
			{
				var fileInfo = new FileInfo(filePath);

				response.Files.Add(new FileResult {
					Name = fileInfo.Name,
					Extension = fileInfo.Extension,
					FileSizeBytes = fileInfo.Length,
					IsTextFile = config.TextFileExtensions.Contains(fileInfo.Extension),
				});
			}

			return response;
		}

		public static string GetSafePath(string filePath)
		{
			foreach (var invalidChar in Path.GetInvalidPathChars())
			{
				filePath = filePath.Replace(invalidChar.ToString(), string.Empty);
			}
			return filePath.Replace("../", string.Empty).Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
		}
	}

}
