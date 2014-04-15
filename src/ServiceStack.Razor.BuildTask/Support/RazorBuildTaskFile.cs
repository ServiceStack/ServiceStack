using System;
using System.IO;

using ServiceStack.IO;

namespace ServiceStack.Razor.BuildTask.Support
{
    // Dummy class to satisfy linked files from SS.Razor project
    public class RazorBuildTaskFile : IVirtualFile
    {
        public RazorBuildTaskFile(string relativePath, RazorBuildPathProvider pathProvider)
        {
            this.VirtualPath = relativePath.Replace(pathProvider.RealPathSeparator, pathProvider.VirtualPathSeparator);
            this.RealPath = pathProvider.RootPath + pathProvider.VirtualPathSeparator + relativePath;
            this.RealPath = this.RealPath.Replace(pathProvider.VirtualPathSeparator, pathProvider.RealPathSeparator);

            this.VirtualPathProvider = pathProvider;
        }
        
        public IVirtualDirectory Directory { get; private set; }

        public string Name { get; private set; }

        public string VirtualPath { get; private set; }

        public string RealPath { get; private set; }

        public bool IsDirectory { get; private set; }

        public DateTime LastModified { get; private set; }

        public string GetFileHash()
        {
            throw new NotImplementedException();
        }

        public Stream OpenRead()
        {
            return File.OpenRead(this.RealPath);
        }

        public StreamReader OpenText()
        {
            throw new NotImplementedException();
        }

        public string ReadAllText()
        {
            throw new NotImplementedException();
        }

        public IVirtualPathProvider VirtualPathProvider { get; private set; }

        public string Extension { get; private set; }

        public long Length { get; private set; }
    }
}
