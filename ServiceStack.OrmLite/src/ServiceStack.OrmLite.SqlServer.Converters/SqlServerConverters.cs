using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Types;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public static class SqlServerConverters
    {
        public static string Msvcr100FileName = "msvcr100.dll";
        public static string SqlServerSpatial110FileName = "SqlServerSpatial110.dll";

        public static IOrmLiteDialectProvider Configure(IOrmLiteDialectProvider dialectProvider, string[] fileNames, string libraryPath = null)
        {
            foreach (var fileName in fileNames)
            {
                LoadAssembly(fileName, libraryPath);
            }

            dialectProvider.RegisterConverter<string>(new SqlServerExtendedStringConverter());
            dialectProvider.RegisterConverter<SqlGeography>(new SqlServerGeographyTypeConverter());
            dialectProvider.RegisterConverter<SqlGeometry>(new SqlServerGeometryTypeConverter());
            dialectProvider.RegisterConverter<SqlHierarchyId>(new SqlServerHierarchyIdTypeConverter());
            return dialectProvider;
        }

        public static IOrmLiteDialectProvider Configure(IOrmLiteDialectProvider dialectProvider, string libraryPath = null) => 
            Configure(dialectProvider, new string[] { Msvcr100FileName, SqlServerSpatial110FileName }, libraryPath);

        public static void LoadAssembly(string assemblyName, string libraryPath = null)
        {
            // default libraryPath to Windows System
            if (string.IsNullOrEmpty(libraryPath))
            {
                // Get the appropriate Windows System Path
                //  32-bit: C:\Windows\System32
                //  64-bit: C:\Windows\SysWOW64
                var systemPathEnum = (!Environment.Is64BitProcess)
                        ? Environment.SpecialFolder.SystemX86
                        : Environment.SpecialFolder.System;

                libraryPath = Environment.GetFolderPath(systemPathEnum);
            }

            var arch = Environment.Is64BitProcess
                ? "x64"
                : "x86";

            var libraryPaths = new[]
            {
                libraryPath,
                "~/SqlServerTypes/{0}/".Fmt(arch).MapAbsolutePath(),
                "~/SqlServerTypes/{0}/".Fmt(arch).MapHostAbsolutePath(),
            };

            foreach (var libraryDir in libraryPaths)
            {
                var assemblyPath = Path.Combine(libraryDir, assemblyName);
                if (!File.Exists(assemblyPath))
                    continue;

                // The versions of the files must match the version associated with Sql Server
                // These files can been installed from the Microsoft SQL Server Feature Pack 
                // 
                // SQL Server 2008: https://www.microsoft.com/en-us/download/details.aspx?id=44277
                // SQL Server 2008 R2: https://www.microsoft.com/en-us/download/details.aspx?id=44272
                // SQL Server 2012 SP2: http://www.microsoft.com/en-us/download/details.aspx?id=43339
                // SQL Server 2014 SP1: https://www.microsoft.com/en-us/download/details.aspx?id=46696

                var ptr = LoadLibrary(assemblyPath);
                if (ptr == IntPtr.Zero)
                    throw new Exception("Error loading {0} (ErrorCode: {1})".Fmt(
                        assemblyPath, Marshal.GetLastWin32Error()));

                return;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetModuleHandleExA(int dwFlags, string moduleName, IntPtr phModule);

        public static void UnloadUnmanagedAssembly(string assemblyName)
        {
            var hMod = IntPtr.Zero;
            if (GetModuleHandleExA(0, assemblyName, hMod))
            {
                while (FreeLibrary(hMod));
            }
        }
    }
}