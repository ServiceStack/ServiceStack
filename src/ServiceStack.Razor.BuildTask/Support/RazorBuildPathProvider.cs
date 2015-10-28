using System;
using System.Collections.Generic;

namespace ServiceStack.Razor.BuildTask.Support
{
    using System.IO;

    using ServiceStack.IO;
    
    // Dummy class to satisfy linked files from SS.Razor project
    public class RazorBuildPathProvider : IVirtualPathProvider
    {
        public RazorBuildPathProvider(string rootPath)
        {
            RootPath = rootPath.Replace(this.RealPathSeparator, this.VirtualPathSeparator);
        }
        
        public string CombineVirtualPath(string basePath, string relativePath)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string virtualPath)
        {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string virtualPath)
        {
            throw new NotImplementedException();
        }

        public IVirtualFile GetFile(string virtualPath)
        {
            throw new NotImplementedException();
        }

        public string GetFileHash(string virtualPath)
        {
            throw new NotImplementedException();
        }

        public string GetFileHash(IVirtualFile virtualFile)
        {
            throw new NotImplementedException();
        }

        public IVirtualDirectory GetDirectory(string virtualPath)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = 2147483647)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IVirtualFile> GetAllFiles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IVirtualFile> GetRootFiles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            throw new NotImplementedException();
        }

        public bool IsSharedFile(IVirtualFile virtualFile)
        {
            throw new NotImplementedException();
        }

        public bool IsViewFile(IVirtualFile virtualFile)
        {
            throw new NotImplementedException();
        }

        public IVirtualDirectory RootDirectory { get; private set; }

        public string VirtualPathSeparator
        {
            get
            {
                return "/";
            }
        }

        public string RealPathSeparator
        {
            get
            {
                return Convert.ToString(Path.DirectorySeparatorChar);
            }
        }

        public string RootPath { get; private set; }
    }
}
