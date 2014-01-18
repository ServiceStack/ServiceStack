//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ServiceStack
{
    public class WinStorePclExport : PclExport
    {
        public new static WinStorePclExport Provider = new WinStorePclExport();

        public WinStorePclExport()
        {
            this.PlatformName = "WindowsStore";
        }

        public static PclExport Configure()
        {
            Configure(Provider);
            return Provider;
        }

        public override string ReadAllText(string filePath)
        {
            var task = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            task.AsTask().Wait();

            var file = task.GetResults();
            
            var streamTask = file.OpenStreamForReadAsync();
            streamTask.Wait();

            var fileStream = streamTask.Result;

            return new StreamReader(fileStream).ReadToEnd();
        }

        public override bool FileExists(string filePath)
        {
            try
            {
                var task = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                //no exception means file exists
                return true;
            }
            catch (Exception ex)
            {
                //find out through exception 
                return false;
            }
        }

        public override void WriteLine(string line)
        {
            System.Diagnostics.Debug.WriteLine(line);
        }

        public override void WriteLine(string format, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(format, args);
        }

        public override Assembly[] GetAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        private sealed class AppDomain
        {
            public static AppDomain CurrentDomain { get; private set; }
            public static Assembly[] cacheObj = null;
 
            static AppDomain()
            {
                CurrentDomain = new AppDomain();
            }
 
            public Assembly[] GetAssemblies()
            {
                return cacheObj ?? GetAssemblyListAsync().Result.ToArray();
            }
 
            private async System.Threading.Tasks.Task<IEnumerable<Assembly>> GetAssemblyListAsync()
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
 
                var assemblies = new List<Assembly>();
                foreach (Windows.Storage.StorageFile file in await folder.GetFilesAsync())
                {
                    if (file.FileType == ".dll" || file.FileType == ".exe")
                    {
                        try
                        {
                            var filename = file.Name.Substring(0, file.Name.Length - file.FileType.Length);
                            AssemblyName name = new AssemblyName() { Name = filename };
                            Assembly asm = Assembly.Load(name);
                            assemblies.Add(asm);
                        }
                        catch (Exception)
                        {
                            // Invalid WinRT assembly!
                        }
                    }
                }

                cacheObj = assemblies.ToArray();
 
                return cacheObj;
            }
        }

        public override string GetAssemblyCodeBase(Assembly assembly)
        {
            return assembly.GetName().FullName;
        }

        //public override DateTime ToStableUniversalTime(DateTime dateTime)
        //{
        //    // .Net 2.0 - 3.5 has an issue with DateTime.ToUniversalTime, but works ok with TimeZoneInfo.ConvertTimeToUtc.
        //    // .Net 4.0+ does this under the hood anyway.
        //    return TimeZoneInfo.ConvertTimeToUtc(dateTime);
        //}
    }
}

#endif
