using System;
using System.IO;
using System.Reflection;
using ServiceStack.Common.Support;

namespace ServiceStack.Common.Utils
{
	/// <summary>
	/// Utils to load types
	/// </summary>
	public class AssemblyUtils
	{
		private const string FileUri = "file:///";
		private const string DllExt = "dll";
		private const string ExeExt = "dll";
		private const char UriSeperator = '/';

		/// <summary>
		/// Find the type from the name supplied
		/// </summary>
		/// <param name="typeName">[typeName] or [typeName, assemblyName]</param>
		/// <returns></returns>
		public static Type FindType(string typeName)
		{
			var typeDef = new AssemblyTypeDefinition(typeName);
			if (!string.IsNullOrEmpty(typeDef.AssemblyName))
			{
				return FindType(typeDef.TypeName, typeDef.AssemblyName);
			}
			else
			{
				return FindTypeFromLoadedAssemblies(typeDef.TypeName);
			}
		}

		/// <summary>
		/// Find type if it exists
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="assemblyName"></param>
		/// <returns>The type if it exists</returns>
		public static Type FindType(string typeName, string assemblyName)
		{
			var type = FindTypeFromLoadedAssemblies(typeName);
			if (type != null)
			{
				return type;
			}
			var binPath = GetAssemblyBinPath(Assembly.GetExecutingAssembly());
			Assembly assembly = null;
			var assemblyDllPath = binPath + string.Format("{0}.{1}", assemblyName, DllExt);
			if (File.Exists(assemblyDllPath))
			{
				assembly = LoadAssembly(assemblyDllPath);
			}
			var assemblyExePath = binPath + string.Format("{0}.{1}", assemblyName, ExeExt);
			if (File.Exists(assemblyExePath))
			{
				assembly = LoadAssembly(assemblyExePath);
			}
			return assembly != null ? assembly.GetType(typeName) : null;
		}

		public static Type FindTypeFromLoadedAssemblies(string typeName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}

		private static Assembly LoadAssembly(string assemblyPath)
		{
			return Assembly.LoadFrom(assemblyPath);
		}

		public static string GetAssemblyBinPath(Assembly assembly)
		{
			var binPathPos = assembly.CodeBase.LastIndexOf(UriSeperator);
			var assemblyPath = assembly.CodeBase.Substring(0, binPathPos + 1);
			if (assemblyPath.StartsWith(FileUri))
			{
				assemblyPath = assemblyPath.Remove(0, FileUri.Length);
			}
			return assemblyPath;
		}
	}
}