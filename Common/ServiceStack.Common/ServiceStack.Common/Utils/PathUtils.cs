using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceStack.Common.Utils
{

	public static class PathUtils
	{
		public static string MapHostAbsolutePath(this string relativePath)
		{
			if (relativePath.StartsWith("~"))
			{
				var binPath = Assembly.GetExecutingAssembly().CodeBase;
				var hostPath = Path.GetDirectoryName(Path.Combine("..", binPath));
				relativePath.Replace("~", hostPath);
			}

			return relativePath;
		}

	}

}
