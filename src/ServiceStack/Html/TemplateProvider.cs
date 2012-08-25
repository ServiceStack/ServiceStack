using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.Html
{

	public class TemplateProvider
	{
		string defaultTemplateName;

		public TemplateProvider(string defaultTemplateName)
		{
			this.defaultTemplateName = defaultTemplateName;
		}

		readonly Dictionary<string, IVirtualFile> templatePathsFound = new Dictionary<string, IVirtualFile>(StringComparer.InvariantCultureIgnoreCase);
		readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
		
		public string GetTemplatePath(IVirtualDirectory fileDir)
		{
			try
			{
				if (templatePathsNotFound.Contains(fileDir.VirtualPath)) return null;
				
				var templateDir = fileDir;
				IVirtualFile templateFile;
				while (templateDir != null && templateDir.GetFile(defaultTemplateName) == null)
				{
					if (templatePathsFound.TryGetValue(templateDir.VirtualPath, out templateFile))
						return templateFile.RealPath;
					
					templateDir = templateDir.ParentDirectory;
				}
				
				if (templateDir != null)
				{
					templateFile = templateDir.GetFile(defaultTemplateName);
					templatePathsFound[templateDir.VirtualPath] = templateFile;
					return templateFile.VirtualPath;
				}
				
				templatePathsNotFound.Add(fileDir.VirtualPath);
				return null;
				
			}
			catch (Exception ex)
			{
				ex.Message.Print();
				throw;
			}
		}
	}

}

