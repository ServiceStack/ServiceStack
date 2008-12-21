using System;
using System.IO;
using System.Reflection;

namespace ServiceStack.Messaging.ActiveMq.Support.Utils
{
    /// <summary>
    /// Utils to load types
    /// </summary>
    public class AssemblyUtils
    {
        private const string FILE_URI = "file:///";
        private const string DLL_EXT = "dll";
        private const string EXE_EXT = "dll";
        private const char URI_SEPERATOR = '/';

        /// <summary>
        /// Find the type from the name supplied
        /// </summary>
        /// <param name="typeName">[typeName] or [typeName, assemblyName]</param>
        /// <returns></returns>
        public static Type FindType(string typeName)
        {
            TypeDefinition typeDef = new TypeDefinition(typeName);
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
            Type type = FindTypeFromLoadedAssemblies(typeName);
            if (type != null)
            {
                return type;
            }
            string binPath = GetAssemblyBinPath(Assembly.GetExecutingAssembly());
            Assembly assembly = null;
            string assemblyDllPath = binPath + string.Format("{0}.{1}", assemblyName, DLL_EXT);
            if (File.Exists(assemblyDllPath))
            {
                assembly = LoadAssembly(assemblyDllPath);
            }
            string assemblyExePath = binPath + string.Format("{0}.{1}", assemblyName, EXE_EXT);
            if (File.Exists(assemblyExePath))
            {
                assembly = LoadAssembly(assemblyExePath);
            }
            if (assembly != null)
            {
                return assembly.GetType(typeName);
            }
            return null;
        }

        public static Type FindTypeFromLoadedAssemblies(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
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
            int binPathPos = assembly.CodeBase.LastIndexOf(URI_SEPERATOR);
            string assemblyPath = assembly.CodeBase.Substring(0, binPathPos + 1);
            if (assemblyPath.StartsWith(FILE_URI))
            {
                assemblyPath = assemblyPath.Remove(0, FILE_URI.Length);
            }
            return assemblyPath;
        }
    }
}
