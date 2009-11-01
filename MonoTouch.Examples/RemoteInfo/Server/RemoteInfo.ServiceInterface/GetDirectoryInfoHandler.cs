using System.IO;
using RemoteInfo.ServiceModel.Operations;
using RemoteInfo.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace RemoteInfo.ServiceInterface
{
	
	/// <summary>
	///  Contains the implementation of the GetDirectoryInfo Web Service.
	///  
	///  @Returns the Directory Contents of the GetDirectoryInfo.ForPath
	///  
	///  Can also be called using the REST Urls below (default urls for XSP provided):
	/// 		- xml:  http://localhost:8080/Public/Xml/SyncReply/GetDirectoryInfo?ForPath=/Server/RemoteInfo.ServiceInterface
	///  	- json: http://localhost:8080/Public/Json/SyncReply/GetDirectoryInfo?ForPath=/Server/RemoteInfo.ServiceInterface
	/// </summary>
	public class GetDirectoryInfoHandler
		: IService<GetDirectoryInfo>
	{
		private readonly RemoteInfoConfig config;

		public GetDirectoryInfoHandler(RemoteInfoConfig config)
		{
			this.config = config;
		}

		public object Execute(GetDirectoryInfo request)
		{
			var showDirPath = Path.Combine(this.config.RootDirectory, GetSafePath(request.ForPath ?? string.Empty));

			var response = new GetDirectoryInfoResponse();

			foreach (var dirPath in Directory.GetDirectories(showDirPath))
			{
				var dirInfo = new DirectoryInfo(dirPath);

				if (this.config.ExcludeDirectories.Contains(dirInfo.Name)) continue;

				response.Directories.Add(new DirectoryResult {
					Name = dirInfo.Name,
					FileCount = dirInfo.GetFiles().Length
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
			//Strip invalid chars
			foreach (var invalidChar in Path.GetInvalidPathChars())
			{
				filePath = filePath.Replace(invalidChar.ToString(), string.Empty);
			}

			return filePath
				.TrimStart('.','/','\\')						//Remove illegal chars at the start
				.Replace("../", string.Empty)				//Remove access to top-level directories anywhere else 
				.Replace('\\', '/')							//Switch all to use the same seperator
				.Replace('/', Path.DirectorySeparatorChar); //Switch all to use the OS seperator
		}
	}

}
