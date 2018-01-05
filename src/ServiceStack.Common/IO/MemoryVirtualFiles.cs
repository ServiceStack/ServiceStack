﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class MemoryVirtualFiles 
        : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public MemoryVirtualFiles()
        {
            this.files = new List<InMemoryVirtualFile>();
            this.rootDirectory = new InMemoryVirtualDirectory(this, null);
        }

        public const char DirSep = '/';

        public List<InMemoryVirtualFile> files;

        public InMemoryVirtualDirectory rootDirectory;

        public override IVirtualDirectory RootDirectory => rootDirectory;

        public override string VirtualPathSeparator => "/";

        public override string RealPathSeparator => "/";

        protected override void Initialize() {}

        public override IVirtualFile GetFile(string virtualPath)
        {
            var filePath = SanitizePath(virtualPath);
            return files.FirstOrDefault(x => x.FilePath == filePath);
        }

        public override IVirtualDirectory GetDirectory(string virtualPath) => GetDirectory(virtualPath, forceDir: false);

        public IVirtualDirectory GetDirectory(string virtualPath, bool forceDir)
        {
            var dirPath = SanitizePath(virtualPath);
            if (string.IsNullOrEmpty(dirPath))
                return rootDirectory;
            
            var dir = new InMemoryVirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
            return forceDir || dir.Files.Any()
                ? dir
                : null;
        }

        public IVirtualDirectory GetParentDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
                return null;

            var lastDirPos = dirPath.LastIndexOf('/');
            if (lastDirPos >= 0)
            {
                var parentDir = dirPath.Substring(0, lastDirPos);
                if (!string.IsNullOrEmpty(parentDir))
                    return GetDirectory(parentDir, forceDir:true);
            }

            return this.rootDirectory;
        }

        public override bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(virtualPath) != null;
        }

        private IVirtualDirectory CreateDirectory(string dirPath)
        {
            return new InMemoryVirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
        }

        public void WriteFile(string filePath, string textContents)
        {
            filePath = SanitizePath(filePath);
            DeleteFile(filePath);
            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                TextContents = textContents,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void WriteFile(string filePath, Stream stream)
        {
            filePath = SanitizePath(filePath);
            DeleteFile(filePath);
            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                ByteContents = stream.ReadFully(),
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void AppendFile(string filePath, string textContents)
        {
            filePath = SanitizePath(filePath);

            var existingFile = GetFile(filePath);
            var text = existingFile != null
                ? existingFile.ReadAllText() + textContents
                : textContents;

            DeleteFile(filePath);

            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                TextContents = text,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void AppendFile(string filePath, Stream stream)
        {
            filePath = SanitizePath(filePath);

            var existingFile = GetFile(filePath);
            var bytes = existingFile != null
                ? existingFile.ReadAllBytes().Combine(stream.ReadFully())
                : stream.ReadFully();

            DeleteFile(filePath);

            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                ByteContents = bytes,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void DeleteFile(string filePath)
        {
            filePath = SanitizePath(filePath);
            this.files.RemoveAll(x => x.FilePath == filePath);
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            filePaths.Each(DeleteFile);
        }

        public void DeleteFolder(string dirPath)
        {
            var subFiles = files.Where(x => x.DirPath.StartsWith(dirPath));
            DeleteFiles(subFiles.Map(x => x.VirtualPath));            
        }

        public IEnumerable<InMemoryVirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = files
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetImmediateSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            return dirPaths.Map(x => new InMemoryVirtualDirectory(this, x, GetParentDirectory(x)));
        }

        public IEnumerable<InMemoryVirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return files.Where(x => x.DirPath == fromDirPath);
        }

        public override IEnumerable<IVirtualFile> GetAllFiles()
        {
            return files;
        }

        public string GetDirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var lastDirPos = filePath.LastIndexOf(DirSep);
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public string GetImmediateSubDirPath(string fromDirPath, string subDirPath)
        {
            if (string.IsNullOrEmpty(subDirPath))
                return null;

            if (fromDirPath == null)
            {
                return subDirPath.CountOccurrencesOf(DirSep) == 0 
                    ? subDirPath
                    : null;
            }

            if (!subDirPath.StartsWith(fromDirPath))
                return null;

            return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1 
                ? subDirPath
                : null;
        }
    }

    public class InMemoryVirtualDirectory : AbstractVirtualDirectoryBase
    {
        private readonly MemoryVirtualFiles pathProvider;

        public InMemoryVirtualDirectory(MemoryVirtualFiles pathProvider, string dirPath, IVirtualDirectory parentDir=null) 
            : base(pathProvider, parentDir)
        {
            this.pathProvider = pathProvider;
            this.DirPath = dirPath;
        }
        
        public DateTime DirLastModified { get; set; }
        public override DateTime LastModified => DirLastModified;

        public override IEnumerable<IVirtualFile> Files => pathProvider.GetImmediateFiles(DirPath);

        public override IEnumerable<IVirtualDirectory> Directories => pathProvider.GetImmediateDirectories(DirPath);

        public string DirPath { get; set; }

        public override string VirtualPath => DirPath;

        public override string Name => DirPath?.LastRightPart(MemoryVirtualFiles.DirSep);

        public override IVirtualFile GetFile(string virtualPath)
        {
            return pathProvider.GetFile(DirPath.CombineWith(virtualPath));
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            return GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var matchingFilesInBackingDir = EnumerateFiles(globPattern);
            return matchingFilesInBackingDir;
        }

        public IEnumerable<InMemoryVirtualFile> EnumerateFiles(string pattern)
        {
            foreach (var file in pathProvider.GetImmediateFiles(DirPath).Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            var subDir = DirPath.CombineWith(directoryName);
            return new InMemoryVirtualDirectory(pathProvider, subDir, this);
        }

        public void AddFile(string filePath, string contents)
        {
            pathProvider.WriteFile(DirPath.CombineWith(filePath), contents);
        }

        public void AddFile(string filePath, Stream stream)
        {
            pathProvider.WriteFile(DirPath.CombineWith(filePath), stream);
        }
    }

    public class InMemoryVirtualFile : AbstractVirtualFileBase
    {
        public InMemoryVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory) 
            : base(owningProvider, directory)
        {
            this.FileLastModified = DateTime.MinValue;            
        }

        public string DirPath => base.Directory.VirtualPath;

        public string FilePath { get; set; }

        public override string Name => FilePath.LastRightPart(MemoryVirtualFiles.DirSep);

        public override string VirtualPath => FilePath;

        public DateTime FileLastModified { get; set; }
        public override DateTime LastModified => FileLastModified;

        public override long Length => TextContents?.Length ?? (ByteContents?.Length ?? 0);

        public string TextContents { get; set; }

        public byte[] ByteContents { get; set; }

        public override Stream OpenRead()
        {
            return MemoryStreamFactory.GetStream(ByteContents ?? (TextContents ?? "").ToUtf8Bytes());
        }

        public override void Refresh()
        {
            if (base.VirtualPathProvider.GetFile(VirtualPath) is InMemoryVirtualFile file)
            {
                this.FilePath = file.FilePath;
                this.FileLastModified = file.FileLastModified;
                this.TextContents = file.TextContents;
                this.ByteContents = file.ByteContents;
            }
        }
    }
}