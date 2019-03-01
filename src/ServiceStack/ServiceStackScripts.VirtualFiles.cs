using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack 
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServiceStackScripts 
    {
        IVirtualFiles VirtualFiles => HostContext.VirtualFiles;
        
        public IEnumerable<IVirtualFile> contentAllFiles() => VirtualFiles.GetAllFiles();
        public IEnumerable<IVirtualFile> contentAllRootFiles() => VirtualFiles.GetRootFiles();
        public IEnumerable<IVirtualDirectory> contentAllRootDirectories() => VirtualFiles.GetRootDirectories();
        public string contentCombinePath(string basePath, string relativePath) => VirtualFiles.CombineVirtualPath(basePath, relativePath);

        public IVirtualDirectory contentDir(string virtualPath) => VirtualFiles.GetDirectory(virtualPath);
        public bool contentDirExists(string virtualPath) => VirtualFiles.DirectoryExists(virtualPath);
        public IVirtualFile contentDirFile(string dirPath, string fileName) => VirtualFiles.GetDirectory(dirPath)?.GetFile(fileName);
        public IEnumerable<IVirtualFile> contentDirFiles(string dirPath) => VirtualFiles.GetDirectory(dirPath)?.GetFiles() ?? new List<IVirtualFile>();
        public IVirtualDirectory contentDirDirectory(string dirPath, string dirName) => VirtualFiles.GetDirectory(dirPath)?.GetDirectory(dirName);
        public IEnumerable<IVirtualDirectory> contentDirDirectories(string dirPath) => VirtualFiles.GetDirectory(dirPath)?.GetDirectories() ?? new List<IVirtualDirectory>();
        public IEnumerable<IVirtualFile> contentDirFilesFind(string dirPath, string globPattern) => VirtualFiles.GetDirectory(dirPath)?.GetAllMatchingFiles(globPattern);

        public IEnumerable<IVirtualFile> contentFilesFind(string globPattern) => VirtualFiles.GetAllMatchingFiles(globPattern);
        public bool contentFileExists(string virtualPath) => VirtualFiles.FileExists(virtualPath);
        public IVirtualFile contentFile(string virtualPath) => VirtualFiles.GetFile(virtualPath);
        public string contentFileWrite(string virtualPath, object contents)
        {
            if (contents is string s)
                VirtualFiles.WriteFile(virtualPath, s);
            else if (contents is byte[] bytes)
                VirtualFiles.WriteFile(virtualPath, bytes);
            else if (contents is Stream stream)
                VirtualFiles.WriteFile(virtualPath, stream);
            else
                return null;

            return virtualPath;
        }

        public string contentFileAppend(string virtualPath, object contents)
        {
            if (contents is string s)
                VirtualFiles.AppendFile(virtualPath, s);
            else if (contents is byte[] bytes)
                VirtualFiles.AppendFile(virtualPath, bytes);
            else if (contents is Stream stream)
                VirtualFiles.AppendFile(virtualPath, stream);
            else
                return null;

            return virtualPath;
        }

        public string contentFileDelete(string virtualPath)
        {
            VirtualFiles.DeleteFile(virtualPath);
            return virtualPath;
        }

        public string dirDelete(string virtualPath)
        {
            VirtualFiles.DeleteFolder(virtualPath);
            return virtualPath;
        }

        public string contentFileReadAll(string virtualPath) => VirtualFiles.GetFile(virtualPath)?.ReadAllText();
        public byte[] contentFileReadAllBytes(string virtualPath) => VirtualFiles.GetFile(virtualPath)?.ReadAllBytes();
        public string contentFileHash(string virtualPath) => VirtualFiles.GetFileHash(virtualPath);
    }
}