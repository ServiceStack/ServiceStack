//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ServiceStack.Text
{
    public static class Env
    {
        static Env()
        {
            if (PclExport.Instance == null)
                throw new ArgumentException("PclExport.Instance needs to be initialized");

#if NETCORE
            IsNetStandard = true;
            try
            {
                IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                IsOSX  = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                IsNetCore3 = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core 3");
                
                var fxDesc = RuntimeInformation.FrameworkDescription;
                IsMono = fxDesc.Contains("Mono");
                IsNetCore = fxDesc.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception) {} //throws PlatformNotSupportedException in AWS lambda
            IsUnix = IsOSX || IsLinux;
            HasMultiplePlatformTargets = true;
            IsUWP = IsRunningAsUwp();
#elif NETFX
            IsNetFramework = true;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    IsWindows = true;
                break;
            }
            
            var platform = (int)Environment.OSVersion.Platform;
            IsUnix = platform is 4 or 6 or 128;

            if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
                IsOSX = true;
            var osType = File.Exists(@"/proc/sys/kernel/ostype") 
                ? File.ReadAllText(@"/proc/sys/kernel/ostype") 
                : null;
            IsLinux = osType?.IndexOf("Linux", StringComparison.OrdinalIgnoreCase) >= 0;
            try
            {
                IsMono = AssemblyUtils.FindType("Mono.Runtime") != null;
            }
            catch (Exception) {}

            SupportsDynamic = true;
#endif

#if NETCORE
            IsNetStandard = false;
            IsNetCore = true;
            SupportsDynamic = true;
            IsNetCore21 = true;
#endif
#if NET6_0
            IsNet6 = true;
#endif
#if NETSTANDARD2_0
            IsNetStandard20 = true;
#endif

            if (!IsUWP)
            {
                try
                {
                    IsAndroid = AssemblyUtils.FindType("Android.Manifest") != null;
                    if (IsOSX && IsMono)
                    {
                        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
                        //iOS detection no longer trustworthy so assuming iOS based on some current heuristics. TODO: improve iOS detection
                        IsIOS = runtimeDir.StartsWith("/private/var") ||
                                runtimeDir.Contains("/CoreSimulator/Devices/"); 
                    }
                }
                catch (Exception) {}
            }
            
            SupportsExpressions = true;
            SupportsEmit = !(IsUWP || IsIOS);

            if (!SupportsEmit)
            {
                ReflectionOptimizer.Instance = ExpressionReflectionOptimizer.Provider;
            }

            VersionString = ServiceStackVersion.ToString(CultureInfo.InvariantCulture);

            __releaseDate = new DateTime(2001,01,01);
            
            UpdateServerUserAgent();
        }

        internal static void UpdateServerUserAgent()
        {
            ServerUserAgent = "ServiceStack/"
                + VersionString + " "
                + PclExport.Instance.PlatformName
                + (IsLinux ? "/Linux" : IsOSX ? "/macOS" : IsUnix ? "/Unix" : IsWindows ? "/Windows" : "/UnknownOS")
                    + (IsIOS ? "/iOS" : IsAndroid ? "/Android" : IsUWP ? "/UWP" : "")
                + (IsNet6 ? "/net6" : IsNetStandard20 ? "/std2.0" : IsNetFramework ? "/netfx" : "") + (IsMono ? "/Mono" : "")
                + $"/{LicenseUtils.Info}";
        }

        public static string VersionString { get; set; }

        public static decimal ServiceStackVersion = 6.51m;

        public static bool IsLinux { get; set; }

        public static bool IsOSX { get; set; }

        public static bool IsUnix { get; set; }

        public static bool IsWindows { get; set; }

        public static bool IsMono { get; set; }

        public static bool IsIOS { get; set; }

        public static bool IsAndroid { get; set; }

        public static bool IsNetNative { get; set; }

        public static bool IsUWP { get; private set; }

        public static bool IsNetStandard { get; set; }

        public static bool IsNetCore21 { get; set; }
        public static bool IsNet6 { get; set; }
        public static bool IsNetStandard20 { get; set; }

        public static bool IsNetFramework { get; set; }

        public static bool IsNetCore { get; set; }
        
        public static bool IsNetCore3 { get; set; }

        public static bool SupportsExpressions { get; private set; }

        public static bool SupportsEmit { get; private set; }

        public static bool SupportsDynamic { get; private set; }

        private static bool strictMode;
        public static bool StrictMode
        {
            get => strictMode;
            set => Config.Instance.ThrowOnError = strictMode = value;
        }

        public static string ServerUserAgent { get; set; }

        public static bool HasMultiplePlatformTargets { get; set; }

        private static readonly DateTime __releaseDate;
        public static DateTime GetReleaseDate()
        {
            return __releaseDate;
        }
        
        private static string referenceAssemblyPath;

        public static string ReferenceAssemblyPath
        {
            get
            {
                if (!IsMono && referenceAssemblyPath == null)
                {
                    var programFilesPath = PclExport.Instance.GetEnvironmentVariable("ProgramFiles(x86)") ?? @"C:\Program Files (x86)";
                    var netFxReferenceBasePath = programFilesPath + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\";
                    if ((netFxReferenceBasePath + @"v4.5.2\").DirectoryExists())
                        referenceAssemblyPath = netFxReferenceBasePath + @"v4.5.2\";
                    else if ((netFxReferenceBasePath + @"v4.5.1\").DirectoryExists())
                        referenceAssemblyPath = netFxReferenceBasePath + @"v4.5.1\";
                    else if ((netFxReferenceBasePath + @"v4.5\").DirectoryExists())
                        referenceAssemblyPath = netFxReferenceBasePath + @"v4.5\";
                    else if ((netFxReferenceBasePath + @"v4.0\").DirectoryExists())
                        referenceAssemblyPath = netFxReferenceBasePath + @"v4.0\";
                    else
                    {
                        var v4Dirs = PclExport.Instance.GetDirectoryNames(netFxReferenceBasePath, "v4*");
                        if (v4Dirs.Length == 0)
                        {
                            var winPath = PclExport.Instance.GetEnvironmentVariable("SYSTEMROOT") ?? @"C:\Windows";
                            var gacPath = winPath + @"\Microsoft.NET\Framework\";
                            v4Dirs = PclExport.Instance.GetDirectoryNames(gacPath, "v4*");                            
                        }
                        if (v4Dirs.Length > 0)
                        {
                            referenceAssemblyPath = v4Dirs[v4Dirs.Length - 1] + @"\"; //latest v4
                        }
                        else
                        {
                            throw new FileNotFoundException(
                                "Could not infer .NET Reference Assemblies path, e.g '{0}'.\n".Fmt(netFxReferenceBasePath + @"v4.0\") +
                                "Provide path manually 'Env.ReferenceAssemblyPath'.");
                        }
                    }
                }
                return referenceAssemblyPath;
            }
            set => referenceAssemblyPath = value;
        }

#if NETCORE
        private static bool IsRunningAsUwp()
        {
            try
            {
                IsNetNative = RuntimeInformation.FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);
                return IsInAppContainer || IsNetNative;
            }
            catch (Exception) {}
            return false;
        }
        
        private static bool IsWindows7OrLower
        {
            get
            {
                int versionMajor = Environment.OSVersion.Version.Major;
                int versionMinor = Environment.OSVersion.Version.Minor;
                double version = versionMajor + (double)versionMinor / 10;
                return version <= 6.1;
            }
        } 
        
        // From: https://github.com/dotnet/corefx/blob/master/src/CoreFx.Private.TestUtilities/src/System/PlatformDetection.Windows.cs
        private static int s_isInAppContainer = -1;
        private static bool IsInAppContainer
        {
            // This actually checks whether code is running in a modern app. 
            // Currently this is the only situation where we run in app container.
            // If we want to distinguish the two cases in future,
            // EnvironmentHelpers.IsAppContainerProcess in desktop code shows how to check for the AC token.
            get
            {
                if (s_isInAppContainer != -1)
                    return s_isInAppContainer == 1;

                if (!IsWindows || IsWindows7OrLower)
                {
                    s_isInAppContainer = 0;
                    return false;
                }

                byte[] buffer = TypeConstants.EmptyByteArray;
                uint bufferSize = 0;
                try
                {
                    int result = GetCurrentApplicationUserModelId(ref bufferSize, buffer);
                    switch (result)
                    {
                        case 15703: // APPMODEL_ERROR_NO_APPLICATION
                            s_isInAppContainer = 0;
                            break;
                        case 0:     // ERROR_SUCCESS
                        case 122:   // ERROR_INSUFFICIENT_BUFFER
                                    // Success is actually insufficient buffer as we're really only looking for
                                    // not NO_APPLICATION and we're not actually giving a buffer here. The
                                    // API will always return NO_APPLICATION if we're not running under a
                                    // WinRT process, no matter what size the buffer is.
                            s_isInAppContainer = 1;
                            break;
                        default:
                            throw new InvalidOperationException($"Failed to get AppId, result was {result}.");
                    }
                }
                catch (Exception e)
                {
                    // We could catch this here, being friendly with older portable surface area should we
                    // desire to use this method elsewhere.
                    if (e.GetType().FullName.Equals("System.EntryPointNotFoundException", StringComparison.Ordinal))
                    {
                        // API doesn't exist, likely pre Win8
                        s_isInAppContainer = 0;
                    }
                    else
                    {
                        throw;
                    }
                }

                return s_isInAppContainer == 1;
            }
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern int GetCurrentApplicationUserModelId(ref uint applicationUserModelIdLength, byte[] applicationUserModelId);
 #endif

        public const bool ContinueOnCapturedContext = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable ConfigAwait(this Task task) => 
            task.ConfigureAwait(ContinueOnCapturedContext);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable<T> ConfigAwait<T>(this Task<T> task) =>
            task.ConfigureAwait(ContinueOnCapturedContext);

        /// <summary>
        /// Only .ConfigAwait(false) in .NET Core as loses HttpContext.Current in NETFX/ASP.NET
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable ConfigAwaitNetCore(this Task task) =>
#if NETCORE
            task.ConfigureAwait(false);
#else
            task.ConfigureAwait(true);
#endif

        /// <summary>
        /// Only .ConfigAwait(false) in .NET Core as loses HttpContext.Current in NETFX/ASP.NET
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable<T> ConfigAwaitNetCore<T>(this Task<T> task) =>
#if NETCORE
            task.ConfigureAwait(false);
#else
            task.ConfigureAwait(true);
#endif

#if NETCORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredValueTaskAwaitable ConfigAwait(this ValueTask task) => 
            task.ConfigureAwait(ContinueOnCapturedContext);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredValueTaskAwaitable<T> ConfigAwait<T>(this ValueTask<T> task) => 
            task.ConfigureAwait(ContinueOnCapturedContext);
#endif

    }
}