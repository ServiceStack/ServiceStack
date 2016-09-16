using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class FileSystemVirtualPathProvider : AbstractVirtualPathProviderBase, IVirtualFiles, IWriteableVirtualPathProvider
    {
        protected DirectoryInfo RootDirInfo;
        protected FileSystemVirtualDirectory RootDir;

        public override IVirtualDirectory RootDirectory => RootDir;
        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

        public FileSystemVirtualPathProvider(IAppHost appHost, string rootDirectoryPath)
            : this(appHost, new DirectoryInfo(rootDirectoryPath))
        { }

        public FileSystemVirtualPathProvider(IAppHost appHost, DirectoryInfo rootDirInfo)
            : base(appHost)
        {
            if (rootDirInfo == null)
                throw new ArgumentNullException(nameof(rootDirInfo));

            this.RootDirInfo = rootDirInfo;
            Initialize();
        }

        public FileSystemVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            Initialize();
        }

        protected sealed override void Initialize()
        {
            if (RootDirInfo == null)
                RootDirInfo = new DirectoryInfo(AppHost.Config.WebHostPhysicalPath);

            if (RootDirInfo == null || !RootDirInfo.Exists)
                throw new Exception($"RootDir '{RootDirInfo.FullName}' for virtual path does not exist");

            RootDir = new FileSystemVirtualDirectory(this, null, RootDirInfo);
        }


        public string EnsureDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            return dirPath;
        }

        public void WriteFile(string filePath, string textContents)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            File.WriteAllText(realFilePath, textContents);
        }

        public void WriteFile(string filePath, Stream stream)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            File.WriteAllBytes(realFilePath, stream.ReadFully());
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void AppendFile(string filePath, string textContents)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            File.AppendAllText(realFilePath, textContents);
        }

        public void AppendFile(string filePath, Stream stream)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            EnsureDirectory(Path.GetDirectoryName(realFilePath));
            using (var fs = new FileStream(realFilePath, FileMode.Append))
            {
                stream.WriteTo(fs);
            }
        }

        public void DeleteFile(string filePath)
        {
            var realFilePath = RootDir.RealPath.CombineWith(filePath);
            try
            {
                File.Delete(realFilePath);
            }
            catch (Exception /*ignore*/) {}
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            filePaths.Each(DeleteFile);
        }

        public void DeleteFolder(string dirPath)
        {
            var realPath = RootDir.RealPath.CombineWith(dirPath);
            if (Directory.Exists(realPath))
                Directory.Delete(realPath, recursive: true);
        }
    }
}
