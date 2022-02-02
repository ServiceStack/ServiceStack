using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceStack.Aws.FileStorage
{
    public class FileSystemStorageProvider : BaseFileStorageProvider
    {
        static FileSystemStorageProvider() { }
        
        public static FileSystemStorageProvider Instance { get; } = new FileSystemStorageProvider();

        public override void Download(FileSystemObject thisFso, FileSystemObject downloadToFso)
        {   // Download on FileSystem is just a copy operation
            Copy(thisFso, downloadToFso);
        }

        public override Stream GetStream(FileSystemObject fso)
        {
            return Exists(fso.FullName)
                ? new FileStream(fso.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)
                : null;
        }

        public override byte[] Get(FileSystemObject fso)
        {
            return Exists(fso.FullName)
                ? File.ReadAllBytes(fso.FullName)
                : null;
        }

        public override void Store(byte[] bytes, FileSystemObject fso)
        {
            using (var byteStream = new MemoryStream(bytes, false))
            {
                Store(byteStream, fso);
            }
        }

        public override void Store(Stream stream, FileSystemObject fso)
        {
            CreateFolder(fso.FolderName);

            using (var fs = new FileStream(fso.FullName, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fs);
            }
        }

        public override void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso)
        {   // Store from local file system object to another file is just a copy on the file system
            CopyInFileSystem(localFileSystemFso, targetFso);
        }

        public override void Delete(FileSystemObject fso)
        {   // File system doesn't throw exception on missing file during delete, no need for a guard
            File.Delete(fso.FullName);
        }

        public override void Delete(IEnumerable<FileSystemObject> fsos)
        {
            foreach (var fso in fsos)
            {
                Delete(fso);
            }
        }

        private bool TreatAsLocalFileSystemProvider(IFileStorageProvider targetProvider)
        {
            if (targetProvider == null)
                return true;

            var provider = targetProvider as FileSystemStorageProvider;
            return provider != null;
        }

        public override void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {   // If targetProvider is null, copying within file system
            if (TreatAsLocalFileSystemProvider(targetProvider))
            {
                CopyInFileSystem(thisFso, targetFso);
                return;
            }
            
            // Copying across providers (from local file system to some other file provider)
            targetProvider.Store(thisFso, targetFso);
        }

        private void CopyInFileSystem(FileSystemObject sourceFso, FileSystemObject targetFso)
        {
            if (sourceFso.Equals(targetFso))
                return;

            CreateFolder(targetFso.FolderName);
            File.Copy(sourceFso.FullName, targetFso.FullName, overwrite: true);
        }
        
        public override void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {   // If targetProvider is null, copying within file system
            if (TreatAsLocalFileSystemProvider(targetProvider))
            {
                MoveInFileSystem(sourceFso, targetFso);
                return;
            }
            
            // Moving across providers (from local file system to some other file provider), so store then delete source
            targetProvider.Store(sourceFso, targetFso);
            Delete(sourceFso);
        }

        private void MoveInFileSystem(FileSystemObject sourceFso, FileSystemObject targetFso)
        {
            if (sourceFso.Equals(targetFso))
                return;

            CreateFolder(targetFso.FolderName);
            File.Move(sourceFso.FullName, targetFso.FullName);
        }

        public override bool Exists(FileSystemObject fso)
        {
            return File.Exists(fso.FullName);
        }

        public override bool FolderExists(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        public override IEnumerable<string> ListFolder(string folderName, bool recursive = false, bool fileNamesOnly = false)
        {
            var searchOption = recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.EnumerateFiles(folderName, "*", searchOption);

            return fileNamesOnly
                ? files.Select(f => new FileSystemObject(f).FileNameAndExtension)
                : files;
        }

        public override void DeleteFolder(string path, bool recursive)
        {
            if (FolderExists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        public override void CreateFolder(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
