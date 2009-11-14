
using System;
using System.IO;
using RemoteInfo.ServiceModel.Operations;
using RemoteInfo.ServiceModel.Types;
using ServiceStack.ServiceHost;

namespace RemoteInfo.ServiceInterface
{
	public class GetTextFileHandler
		: IService<GetTextFile>
	{
		
		public RemoteInfoConfig Config { get; set; }

		
		public object Execute(GetTextFile request)
		{
			var textFilePath = Path.Combine(this.Config.RootDirectory, 
				GetDirectoryInfoHandler.GetSafePath(request.AtPath ?? string.Empty));
			
			var fileInfo = new FileInfo(textFilePath);
			
			//For simplicity just return an empty DTO
			if (!fileInfo.Exists || !this.Config.TextFileExtensions.Contains(fileInfo.Extension))
			{
				return new GetTextFileResponse();
			}
			
			return new GetTextFileResponse
			{
				Contents = File.ReadAllText(fileInfo.FullName),
				CreatedDate = fileInfo.CreationTime,
				LastModifiedDate = fileInfo.LastWriteTime,
			};
		}
		
	}
}
