using System.Collections.Generic;
using System.IO;

namespace ServiceStack.Aws.FileStorage
{
    public interface IFileStorageProvider
    {
        char DirectorySeparatorCharacter { get; }
        string NormalizePath(string path);

        void Download(string thisFileName, string localFileSystemTargetFileName);
        void Download(FileSystemObject thisFso, FileSystemObject localFileSystemFso);

        byte[] Get(string thisFileName);
        byte[] Get(FileSystemObject fso);
        Stream GetStream(FileSystemObject fso);

        void Store(byte[] bytes, string targetFileName);
        void Store(byte[] bytes, FileSystemObject fso);
        void Store(Stream stream, FileSystemObject fso);
        void Store(string localFileSystemSourceFileName, string targetFileName);
        void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso);

        void Delete(string fileName);
        void Delete(FileSystemObject fso);
        void Delete(IEnumerable<string> fileNames);
        void Delete(IEnumerable<FileSystemObject> fsos);

        void Copy(string thisFileName, string copyToFileName, IFileStorageProvider targetProvider = null);
        void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);

        void Move(string thisFileName, string moveToFileName, IFileStorageProvider targetProvider = null);
        void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);

        bool Exists(string fileName);
        bool Exists(FileSystemObject fso);

        bool FolderExists(string path);
        IEnumerable<string> ListFolder(string folderName, bool recursive = false, bool fileNamesOnly = false);
        void DeleteFolder(string folderName, bool recursive);
        void CreateFolder(string folderName);
    }
}