using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Aws.FileStorage
{
    public abstract class BaseFileStorageProvider : IFileStorageProvider
    {
        private string replaceThisDirectorySeparatorCharacter;

        protected BaseFileStorageProvider() { }

        public virtual char DirectorySeparatorCharacter => Path.DirectorySeparatorChar;

        private string ReplaceThisDirectorySeparatorCharacter => replaceThisDirectorySeparatorCharacter ??
            (replaceThisDirectorySeparatorCharacter = DirectorySeparatorCharacter.Equals('\\')
                ? "/"
                : "\\");

        public string NormalizePath(string path)
        {
            return path.Replace(ReplaceThisDirectorySeparatorCharacter, DirectorySeparatorCharacter.ToString());
        }

        protected FileSystemObject GetFileSystemObject(string fileName)
        {
            return new FileSystemObject(fileName, DirectorySeparatorCharacter);
        }

        public void Download(string thisFileName, string localFileSystemTargetFileName)
        {
            var sourceFso = GetFileSystemObject(thisFileName);
            var targetFso = GetFileSystemObject(localFileSystemTargetFileName);
            Download(sourceFso, targetFso);
        }

        public byte[] Get(string thisFileName)
        {
            var fso = GetFileSystemObject(thisFileName);
            return Get(fso);
        }

        public void Store(byte[] bytes, string targetFileName)
        {
            var fso = GetFileSystemObject(targetFileName);
            Store(bytes, fso);
        }

        public void Store(string localFileSystemSourceFileName, string targetFileName)
        {
            var localFileSystemFso = GetFileSystemObject(localFileSystemSourceFileName);
            var fso = GetFileSystemObject(targetFileName);
            Store(localFileSystemFso, fso);
        }

        public void Delete(string fileName)
        {
            var fso = GetFileSystemObject(fileName);
            Delete(fso);
        }

        public void Delete(IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Delete(fileName);
            }
        }

        public void Copy(string thisFileName, string copyToFileName, IFileStorageProvider targetProvider = null)
        {
            var sourceFso = GetFileSystemObject(thisFileName);
            var targetFso = GetFileSystemObject(copyToFileName);
            Copy(sourceFso, targetFso, targetProvider);
        }

        public void Move(string thisFileName, string moveToFileName, IFileStorageProvider targetProvider = null)
        {
            var sourceFso = GetFileSystemObject(thisFileName);
            var targetFso = GetFileSystemObject(moveToFileName);
            Move(sourceFso, targetFso, targetProvider);
        }

        public bool Exists(string fileName)
        {
            var fso = GetFileSystemObject(fileName);
            return Exists(fso);
        }

        public abstract bool FolderExists(string path);
        public abstract void Download(FileSystemObject thisFso, FileSystemObject localFileSystemFso);
        public abstract byte[] Get(FileSystemObject fso);
        public abstract Stream GetStream(FileSystemObject fso);
        public abstract void Store(byte[] bytes, FileSystemObject fso);
        public abstract void Store(Stream stream, FileSystemObject fso);
        public abstract void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso);
        public abstract void Delete(FileSystemObject fso);
        public abstract void Delete(IEnumerable<FileSystemObject> fsos);
        public abstract void Copy(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);
        public abstract void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);
        public abstract bool Exists(FileSystemObject fso);
        public abstract IEnumerable<string> ListFolder(string folderName, bool recursive = false, bool fileNamesOnly = false);
        public abstract void DeleteFolder(string folderName, bool recursive);
        public abstract void CreateFolder(string folderName);
    }
}
