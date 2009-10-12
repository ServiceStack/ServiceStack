using System.IO;
using RemoteInfo.ServiceModel.Operations;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace RemoteInfo.ServiceInterface
{
	/// <summary>
	///  Contains the implementation of the GetTextFile Web Service.
	///  
	///  @Returns some file properties and contents of the text file at: GetTextFile.AtPath
	///  
	///  Can also be called using the REST Urls below (default urls for XSP provided):
	/// 		- xml:  http://localhost:8080/Public/Xml/SyncReply/GetTextFile?AtPath=/Server/RemoteInfo.ServiceInterface/GetTextFileHandler.cs
	///  	- json: http://localhost:8080/Public/Json/SyncReply/GetTextFile?AtPath=/Server/RemoteInfo.ServiceInterface/GetTextFileHandler.cs
	/// </summary>
	[Port(typeof(GetTextFile))]
	public class GetTextFileHandler
		: IService
	{
		private readonly RemoteInfoConfig config;

		public GetTextFileHandler(RemoteInfoConfig config)
		{
			this.config = config;
		}

		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetTextFile>();

			var textFilePath = Path.Combine(this.config.RootDirectory,
				GetDirectoryInfoHandler.GetSafePath(request.AtPath ?? string.Empty));

			var fileInfo = new FileInfo(textFilePath);

			//For simplicity, just return an empty DTO
			if (!fileInfo.Exists || !config.TextFileExtensions.Contains(fileInfo.Extension))
			{
				return new GetTextFileResponse();
			}

			return new GetTextFileResponse {
				Contents = File.ReadAllText(fileInfo.FullName),
                CreatedDate = fileInfo.CreationTime,
                LastModifiedDate = fileInfo.LastWriteTime
			};
		}
	}

}
