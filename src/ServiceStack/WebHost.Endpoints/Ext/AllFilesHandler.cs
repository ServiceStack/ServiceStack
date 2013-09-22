using System;
using System.IO;
using System.Text;
using System.Web;
using ServiceStack.Utils;

namespace ServiceStack.WebHost.Endpoints.Ext
{
	/// <summary>
	/// Summary description for $codebehindclassname$
	/// </summary>
	public class AllFilesHandler 
		: IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			var path = context.Request["Path"];
			if (string.IsNullOrEmpty(path))
			{
				path = context.Request.AppRelativeCurrentExecutionFilePath;
				if (!string.IsNullOrEmpty(path))
				{
					path = path.Substring(0, path.LastIndexOf("/"));
				}
				else
				{
					throw new ArgumentNullException("Path");
				}
			}

			context.Response.ContentType = context.Request["ContentType"] ?? "text/plain";
			var jsText = GetAllTextFiles(path, context.Request["Filter"] ?? "*.*");
			var wrapJs = context.Request["wrapJs"] != null;
			if (wrapJs)
			{
				jsText = WrapJavascriptInNamespace(jsText);
			}

			context.Response.Write(jsText);
		}

		public static string WrapJavascriptInNamespace(string jsText)
		{
			var sb = new StringBuilder();

			sb.Append("(function(){");
			sb.Append(jsText);
			sb.Append("})();");

			return sb.ToString();
		}

		public static bool InHiddenDirectory(string filePath)
		{
			return filePath.Contains(".svn");
		}

		public static string GetAllTextFiles(string path, string filter)
		{
			if (path.Contains(".."))
				throw new UnauthorizedAccessException("Invalid Path");

			var sb = new StringBuilder();

			var absolutePath = path.MapHostAbsolutePath();
			if (!Directory.Exists(absolutePath)) return null;

			foreach (var filePath in Directory.GetFiles(
				absolutePath, filter, SearchOption.AllDirectories))
			{
				if (InHiddenDirectory(filePath)) continue;

				//Can be optimized
				var fileText = File.ReadAllText(filePath);
				sb.AppendLine(fileText);
			}

			return sb.ToString();
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}