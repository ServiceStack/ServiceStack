﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Logging;

namespace ServiceStack.VirtualPath
{
    public class FileSystemVirtualDirectory : AbstractVirtualDirectoryBase
    {
        private static ILog Log = LogManager.GetLogger(typeof(FileSystemVirtualDirectory));

        protected DirectoryInfo BackingDirInfo;

        public override IEnumerable<IVirtualFile> Files
        {
            get { return this.Where(n => n.IsDirectory == false).Cast<IVirtualFile>(); }
        }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return this.Where(n => n.IsDirectory).Cast<IVirtualDirectory>(); }
        }

        public override string Name
        {
            get { return BackingDirInfo.Name; }
        }

        public override DateTime LastModified
        {
            get { return BackingDirInfo.LastWriteTime; }
        }

        public override string RealPath
        {
            get { return BackingDirInfo.FullName; }
        }

        public FileSystemVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory, DirectoryInfo dInfo)
            : base(owningProvider, parentDirectory)
        {
            if (dInfo == null)
                throw new ArgumentNullException("dInfo");

            this.BackingDirInfo = dInfo;
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            var directoryNodes = GetDirectories()
                .Select(dInfo => new FileSystemVirtualDirectory(VirtualPathProvider, this, dInfo))
                .Where(x => !x.ShouldSkipPath());

            var fileNodes = GetFiles()
                .Select(fInfo => new FileSystemVirtualFile(VirtualPathProvider, this, fInfo));

            return directoryNodes.Cast<IVirtualNode>()
                .Union<IVirtualNode>(fileNodes.Cast<IVirtualNode>())
                .GetEnumerator();
        }

        private FileInfo[] GetFiles()
        {
            try
            {
                return BackingDirInfo.GetFiles();
            }
            catch (Exception ex)
            {
                //Possible exception from scanning symbolic links
                Log.Warn("Unable to GetFiles for {0}".Fmt(RealPath), ex);
                return new FileInfo[0];
            }
        }

        private DirectoryInfo[] GetDirectories()
        {
            try
            {
                return BackingDirInfo.GetDirectories();
            }
            catch (Exception ex)
            {
                //Possible exception from scanning symbolic links
                Log.Warn("Unable to GetDirectories for {0}".Fmt(RealPath), ex);
                return new DirectoryInfo[0];
            }
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fName)
        {
            var fInfo = EnumerateFiles(fName).FirstOrDefault();

            return fInfo != null
                ? new FileSystemVirtualFile(VirtualPathProvider, this, fInfo)
                : null;
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        { 
            try
            {
                var matchingFilesInBackingDir = EnumerateFiles(globPattern)
                    .Select(fInfo => (IVirtualFile)new FileSystemVirtualFile(VirtualPathProvider, this, fInfo));

                return matchingFilesInBackingDir;
            }
            catch (Exception ex)
            {
                //Possible exception from scanning symbolic links
                Log.Warn("Unable to scan for {0} in {1}".Fmt(globPattern, RealPath), ex);
                return new IVirtualFile[0];
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string dName)
        {
            var dInfo = EnumerateDirectories(dName)
                .FirstOrDefault();

            return dInfo != null
                ? new FileSystemVirtualDirectory(VirtualPathProvider, this, dInfo)
                : null;
        }

        public IEnumerable<FileInfo> EnumerateFiles(string pattern)
        {
            return BackingDirInfo.GetFiles(pattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string dirName)
        {
            return BackingDirInfo.GetDirectories(dirName, SearchOption.TopDirectoryOnly);
        }
    }
}
