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
		public static string MapAbsolutePath(string relativePath, string appendPartialPathModifier)
		{
			if (relativePath.StartsWith("~"))
			{
				string assemblyDirectoryPath = Path.GetDirectoryName(new Uri(typeof(PathUtils).Assembly.EscapedCodeBase).LocalPath);

				// Escape the assembly bin directory to the hostname directory
				string hostDirectoryPath = assemblyDirectoryPath + appendPartialPathModifier;

				return Path.GetFullPath(relativePath.Replace("~", hostDirectoryPath));
			}

			return relativePath;
		}

		public static string MapTestAbsolutePath(this string relativePath)
		{
			var mapPath = MapAbsolutePath(relativePath, @"\..\..");
			return mapPath;
		}

		public static string MapHostAbsolutePath(this string relativePath)
		{
			var mapPath = MapAbsolutePath(relativePath, @"\..");
			return mapPath;
		}
	}


}
