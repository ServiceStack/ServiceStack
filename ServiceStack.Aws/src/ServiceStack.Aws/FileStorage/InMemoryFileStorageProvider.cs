using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceStack.Aws.FileStorage
{
    internal class MemoryFileMeta
    {
        public byte[] Bytes;
        public FileSystemObject Fso;
    }

    public class InMemoryFileStorageProvider : BaseFileStorageProvider
    {
        private readonly ConcurrentDictionary<string, MemoryFileMeta> fileMap;
        private static readonly FileSystemStorageProvider localFs = FileSystemStorageProvider.Instance;

        static InMemoryFileStorageProvider() { }
        
        public InMemoryFileStorageProvider()
        {
            fileMap = new ConcurrentDictionary<string, MemoryFileMeta>();
        }

        public static InMemoryFileStorageProvider Instance { get; } = new InMemoryFileStorageProvider();

        public override void Download(FileSystemObject thisFso, FileSystemObject downloadToFso)
        {
            var bytes = Get(thisFso);

            if (bytes == null)
            {
                throw new FileNotFoundException("File does not exist in memory provider", thisFso.FullName);
            }

            localFs.Store(bytes, downloadToFso);
        }
        
        public override Stream GetStream(FileSystemObject fso)
        {
            var map = TryGetMap(fso.FullName);

            return map == null
                ? null
                : new MemoryStream(map.Bytes);
        }

        public override byte[] Get(FileSystemObject fso)
        {
            var map = TryGetMap(fso.FullName);
            return map?.Bytes;
        }

        public override void Store(byte[] bytes, FileSystemObject fso)
        {
            SaveToMap(bytes, fso);
        }

        public override void Store(Stream stream, FileSystemObject fso)
        {
            SaveToMap(stream.ToBytes(), fso);
        }

        public override void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso)
        {
            SaveToMap(localFs.Get(localFileSystemFso), targetFso);
        }
        
        public override void Delete(FileSystemObject fso)
        {
            fileMap.TryRemove(fso.FullName, out _);
        }

        public override void Delete(IEnumerable<FileSystemObject> fsos)
        {
            foreach (var fso in fsos)
            {
                Delete(fso);
            }
        }
        
        public override void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {
            var bytes = Get(thisFso);

            if (bytes == null)
                throw new FileNotFoundException("File does not exist in memory provider", thisFso.FullName);

            if (TreatAsInMemoryProvider(targetProvider))
            {
                if (thisFso.Equals(targetFso))
                {
                    return;
                }

                Store(bytes, targetFso);
                return;
            }
            
            targetProvider.Store(bytes, targetFso);
        }

        public override void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {
            var sourceBytes = Get(sourceFso);

            if (sourceBytes == null)
                throw new FileNotFoundException("File does not exist in memory provider", sourceFso.FullName);

            if (TreatAsInMemoryProvider(targetProvider))
            {
                if (sourceFso.Equals(targetFso))
                {
                    return;
                }

                Store(sourceBytes, targetFso);
                Delete(sourceFso);
                
                return;
            }

            // Moving across providers (from in-memory to some other file provider), so store then delete source
            targetProvider.Store(sourceBytes, targetFso);
            Delete(sourceFso);
        }

        public override bool Exists(FileSystemObject fso)
        {
            return fileMap.ContainsKey(fso.FullName);
        }

        public override bool FolderExists(string path)
        {
            return false;
        }

        public override IEnumerable<string> ListFolder(string folderName, bool recursive = false, bool fileNamesOnly = false)
        {
            return fileMap.Where(f => recursive
                    ? f.Value.Fso.FolderName.StartsWith(folderName, StringComparison.Ordinal)
                    : f.Value.Fso.FolderName.Equals(folderName, StringComparison.Ordinal))
                .Select(f => fileNamesOnly
                    ? f.Value.Fso.FileNameAndExtension
                    : f.Value.Fso.FullName);
        }

        public override void DeleteFolder(string path, bool recursive)
        {
            var keysToDelete = fileMap.Where(i => i.Value.Fso.FolderName.StartsWith(path, StringComparison.Ordinal))
                .Select(kvp => kvp.Key);

            Delete(keysToDelete);
        }
        
        public override void CreateFolder(string path)
        {
            // No need to do anything with creating folders here...in memory is mapped to a dict that needs no folder structure
        }

        private bool TreatAsInMemoryProvider(IFileStorageProvider targetProvider)
        {
            if (targetProvider == null)
                return true;

            return targetProvider is InMemoryFileStorageProvider provider;
        }

        private MemoryFileMeta TryGetMap(string key)
        {
            return fileMap.TryGetValue(key, out var item)
                ? item
                : null;
        }

        private void SaveToMap(byte[] bytes, FileSystemObject target)
        {
            var s = new MemoryFileMeta
            {
                Bytes = bytes,
                Fso = target.Clone()
            };

            fileMap[target.FullName] = s;
        }
    }
}
