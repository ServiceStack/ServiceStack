using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;

namespace RemoteInfo.ServiceInterface
{
	public class RemoteInfoConfig
	{
		private readonly IResourceManager resources;

		public RemoteInfoConfig(IResourceManager resources)
		{
			this.resources = resources;
		}

		public string RootDirectory
		{
			get
			{
				return resources.GetString("RootDirectory").MapAbsolutePath()
					.Replace('\\', Path.DirectorySeparatorChar);
			}
		}

		public IList<string> TextFileExtensions
		{
			get
			{
				return resources.GetList("TextFileExtensions");
			}
		}

	}

}