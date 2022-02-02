using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ServiceStack.Common.Support;

namespace ServiceStack.Text
{
    /// <summary>
    /// Utils to load types
    /// </summary>
    public static class AssemblyUtils
    {
        private const string FileUri = "file:///";
        private const char UriSeperator = '/';

        private static Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        /// <summary>
        /// Find the type from the name supplied
        /// </summary>
        /// <param name="typeName">[typeName] or [typeName, assemblyName]</param>
        /// <returns></returns>
        public static Type FindType(string typeName)
        {
            if (TypeCache.TryGetValue(typeName, out var type)) return type;

            type = Type.GetType(typeName);
            if (type == null)
            {
                var typeDef = new AssemblyTypeDefinition(typeName);
                type = !string.IsNullOrEmpty(typeDef.AssemblyName) 
                    ? FindType(typeDef.TypeName, typeDef.AssemblyName) 
                    : FindTypeFromLoadedAssemblies(typeDef.TypeName);
            }

            Dictionary<string, Type> snapshot, newCache;
            do
            {
                snapshot = TypeCache;
                newCache = new Dictionary<string, Type>(TypeCache) { [typeName] = type };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref TypeCache, newCache, snapshot), snapshot));

            return type;
        }

        /// <summary>
        /// The top-most interface of the given type, if any.
        /// </summary>
        public static Type MainInterface<T>()
        {
            var t = typeof(T);
            if (t.BaseType == typeof(object))
            {
                // on Windows, this can be just "t.GetInterfaces()" but Mono doesn't return in order.
                var interfaceType = t.GetInterfaces().FirstOrDefault(i => !t.GetInterfaces().Any(i2 => i2.GetInterfaces().Contains(i)));
                if (interfaceType != null) return interfaceType;
            }
            return t; // not safe to use interface, as it might be a superclass one.
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

            return PclExport.Instance.FindType(typeName, assemblyName);
        }

        public static Type FindTypeFromLoadedAssemblies(string typeName)
        {
            var assemblies = PclExport.Instance.GetAllAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        public static Assembly LoadAssembly(string assemblyPath)
        {
            return PclExport.Instance.LoadAssembly(assemblyPath);
        }

        public static string GetAssemblyBinPath(Assembly assembly)
        {
            var codeBase = PclExport.Instance.GetAssemblyCodeBase(assembly);
            var binPathPos = codeBase.LastIndexOf(UriSeperator);
            var assemblyPath = codeBase.Substring(0, binPathPos + 1);
            if (assemblyPath.StartsWith(FileUri, StringComparison.OrdinalIgnoreCase))
            {
                assemblyPath = assemblyPath.Remove(0, FileUri.Length);
            }
            return assemblyPath;
        }

        static readonly Regex versionRegEx = new Regex(", Version=[^\\]]+", PclExport.Instance.RegexOptions);
        public static string ToTypeString(this Type type)
        {
            return versionRegEx.Replace(type.AssemblyQualifiedName, "");
        }

        public static string WriteType(Type type)
        {
            return type.ToTypeString();
        }
    }
}