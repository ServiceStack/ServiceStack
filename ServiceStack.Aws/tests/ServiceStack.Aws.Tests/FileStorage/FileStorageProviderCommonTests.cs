using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Aws.FileStorage;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Tests.FileStorage
{
    public abstract class FileStorageProviderCommonTests
    {
        protected const string TestSubDirectory = "servicestack-fileprovider-tests";

        protected Func<IFileStorageProvider> providerFactory;
        protected string baseFolderName;

        protected void Initialize()
        {
            var provider = providerFactory();
            provider.DeleteFolder(baseFolderName, recursive: true);
            provider.CreateFolder(baseFolderName);
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            var provider = providerFactory();
            provider.DeleteFolder(baseFolderName, recursive: true);
        }

        protected FileSystemObject GetTestFso(string subDirectory = null)
        {
            var fileName = String.Concat(Guid.NewGuid().ToString("N"), ".txt");

            var baseFolder = String.IsNullOrEmpty(subDirectory)
                                 ? baseFolderName
                                 : Path.Combine(baseFolderName, subDirectory);

            return new FileSystemObject(baseFolder, fileName);
        }

        protected byte[] GetTestFileBytes(string fileName)
        {
            var contents = String.Concat(fileName, " || ", String.Join(String.Empty,Enumerable.Repeat("a", 100)), " || ", fileName);
            return Encoding.ASCII.GetBytes(contents);
        }

        private void CreateAndFillPathForTesting(string testSubFolder, IFileStorageProvider provider)
        {
            var testFolder = Path.Combine(baseFolderName, testSubFolder);

            provider.DeleteFolder(testFolder, recursive: true);

            // Put some files in each of the paths
            var subFolderList = new List<string>
                                {
                                    Path.Combine(testSubFolder, "nested-folder2a", "nested-folder3"),
                                    Path.Combine(testSubFolder, "nested-folder2a"),
                                    Path.Combine(testSubFolder, "nested-folder2b"),
                                    testSubFolder
                                };

            foreach (var subFolder in subFolderList)
            {
                for (var x = 1; x <= 3; x++)
                {
                    var file = GetTestFso(subFolder);
                    provider.Store(GetTestFileBytes(file.FullName), file);
                }
            }
        }

        [Test]
        public void ListWithSubfolderReturnsAppropriateNumberOfFiles()
        {
            const string testSubFolder = "nested-list-folder1";

            var testFolder = Path.Combine(baseFolderName, testSubFolder);

            var provider = providerFactory();

            CreateAndFillPathForTesting(testSubFolder, provider);

            // Middle folder, non-recursive and recursive
            var checkFolder = Path.Combine(testFolder, "nested-folder2a");
            var list2A = provider.ListFolder(checkFolder, recursive: false).ToList();
            Assert.AreEqual(3, list2A.Count);

            list2A = provider.ListFolder(checkFolder, recursive: true).ToList();
            Assert.AreEqual(6, list2A.Count);

            // Base folder
            var list1 = provider.ListFolder(testFolder).ToList();
            Assert.AreEqual(3, list1.Count);

            list1 = provider.ListFolder(testFolder, recursive: true).ToList();
            Assert.AreEqual(12, list1.Count);

            provider.DeleteFolder(testFolder, recursive: true);
        }
        
        [Test]
        public void ListReturnsFullNameAndPathByDefault()
        {
            const string testSubFolder = "list-fullname-default";

            var testFolder = Path.Combine(baseFolderName, testSubFolder);
            
            var provider = providerFactory();

            CreateAndFillPathForTesting(testSubFolder, provider);

            var files = provider.ListFolder(testFolder, recursive: true);

            var normalizedTestFolder = provider.NormalizePath(testFolder);

            Assert.IsTrue(files.All(f => f.StartsWith(normalizedTestFolder, StringComparison.Ordinal)));
        }

        [Test]
        public void ListReturnsFileNameOnlyWhenRequested()
        {
            const string testSubFolder = "list-fullname-nameonly";

            var testFolder = Path.Combine(baseFolderName, testSubFolder);

            var provider = providerFactory();

            CreateAndFillPathForTesting(testSubFolder, provider);

            var files = provider.ListFolder(testFolder, recursive: true, fileNamesOnly: true);

            Assert.IsTrue(files.All(f => !f.ContainsAny("\\", "/")));
        }

        [Test]
        public void CanDeleteNonEmptyNestedFolder()
        {
            const string testSubFolder = "nested-delete-folder1";

            var testFolder = Path.Combine(baseFolderName, testSubFolder);

            var provider = providerFactory();

            CreateAndFillPathForTesting(testSubFolder, provider);
            
            var checkFolder = Path.Combine(testFolder, "nested-folder2a");
            provider.DeleteFolder(checkFolder, recursive: true);

            var contents = provider.ListFolder(testFolder, recursive: true).ToList();

            Assert.AreEqual(6, contents.Count);

            provider.DeleteFolder(testFolder, recursive: true);
        }

        [Test]
        public void CanDownloadToLocalFile()
        {
            var file = GetTestFso();
            var provider = providerFactory();

            // Put a test file at the provider for download
            var testFileContents = GetTestFileBytes(file.FileName);
            StoreGet(provider, file, sourceBytes: testFileContents);
            
            var downloadFile = GetTestFso();

            provider.Download(file, downloadFile);

            var downloadedContents = File.ReadAllBytes(downloadFile.FullName);

            var testHash = testFileContents.ToSha256HashBytes().ToBase64String();
            var downloadHash = downloadedContents.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(testHash, downloadHash);
        }

        [Test]
        public void CanCopyToInMemoryProvider()
        {
            var sourceFile = GetTestFso();
            var memFile = GetTestFso();

            var sourceProvider = providerFactory();
            var memProvider = InMemoryFileStorageProvider.Instance;

            var sourceFileContents = GetTestFileBytes(sourceFile.FileName);
            StoreGet(sourceProvider, sourceFile, sourceBytes: sourceFileContents);

            sourceProvider.Copy(sourceFile, memFile, memProvider);

            Assert.IsTrue(sourceProvider.Exists(sourceFile));
            Assert.IsTrue(memProvider.Exists(memFile));

            var memContents = memProvider.Get(memFile);

            var sourceHash = sourceFileContents.ToSha256HashBytes().ToBase64String();
            var memHash = memContents.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(sourceHash, memHash);
        }

        [Test]
        public void CanMoveToInMemoryProvider()
        {
            var sourceFile = GetTestFso();
            var memFile = GetTestFso();

            var sourceProvider = providerFactory();
            var memProvider = InMemoryFileStorageProvider.Instance;

            var sourceFileContents = GetTestFileBytes(sourceFile.FileName);
            StoreGet(sourceProvider, sourceFile, sourceBytes: sourceFileContents);

            sourceProvider.Move(sourceFile, memFile, memProvider);

            Assert.IsFalse(sourceProvider.Exists(sourceFile));
            Assert.IsTrue(memProvider.Exists(memFile));

            var memContents = memProvider.Get(memFile);

            var sourceHash = sourceFileContents.ToSha256HashBytes().ToBase64String();
            var memHash = memContents.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(sourceHash, memHash);
        }

        [Test]
        public void GetReturnsNullForObjectThatDoesNotExist()
        {
            var file = GetTestFso();
            var provider = providerFactory();
            var contents = provider.Get(file);

            Assert.IsNull(contents);
        }

        [Test]
        public void CanCopyWithinSameProviderByDefault()
        {
            var file = GetTestFso();
            var provider = providerFactory();

            var testFileContents = GetTestFileBytes(file.FileName);
            StoreGet(provider, file, sourceBytes: testFileContents);
            
            var copiedFile = GetTestFso();

            provider.Copy(file, copiedFile);

            Assert.IsTrue(provider.Exists(copiedFile));
            Assert.IsTrue(provider.Exists(file));

            var copiedContents = provider.Get(copiedFile);

            var testHash = testFileContents.ToSha256HashBytes().ToBase64String();
            var copyHash = copiedContents.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(testHash, copyHash);
        }

        [Test]
        public void CanMoveWithinSameProviderByDefault()
        {
            var file = GetTestFso();
            var provider = providerFactory();

            var testFileContents = GetTestFileBytes(file.FileName);
            StoreGet(provider, file, sourceBytes: testFileContents);

            var movedFile = GetTestFso();

            provider.Move(file, movedFile);

            Assert.IsTrue(provider.Exists(movedFile));
            Assert.IsFalse(provider.Exists(file));

            var movedContents = provider.Get(movedFile);

            var testHash = testFileContents.ToSha256HashBytes().ToBase64String();
            var moveHash = movedContents.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(testHash, moveHash);
        }

        [Test]
        public void CanPostAndGetWithBytes()
        {
            var file = GetTestFso();
            CanPostAndGet(file, sourceBytes: GetTestFileBytes(file.FileName));
        }

        [Test]
        public void CanPostWhenFolderDoesNotExistWithBytes()
        {
            var file = GetTestFso("can-post-when-folder-does-not-exist-bytes");
            CanPostAndGet(file, deleteFolderBeforeStore: true, sourceBytes: GetTestFileBytes(file.FileName));
        }

        [Test]
        public void CanPostAndGetWithSourceFile()
        {
            var sourceFile = GetTestFso();
            var targetFile = GetTestFso();

            // Store the source file on the local file system
            FileSystemStorageProvider.Instance.Store(GetTestFileBytes(sourceFile.FileName), sourceFile);

            CanPostAndGet(targetFile, sourceFileData: sourceFile, deleteAfterGet: false);

            // Source and target files have different names, verify
            var provider = providerFactory();

            var postedFileExists = provider.Exists(targetFile);
            Assert.IsTrue(postedFileExists);

            // Cleanup
            provider.Delete(targetFile);

            // Shouldn't be there no mo...
            var targetExists = provider.Exists(targetFile);
            Assert.IsFalse(targetExists);
        }

        [Test]
        public void CanPostAndGetWhenFolderDoesNotExistWithSourceFile()
        {
            var sourceFile = GetTestFso("can-post-when-foder-doesnot-exist-sourcefile");
            var targetFile = GetTestFso("can-post-when-foder-doesnot-exist-targetfile");

            // Store the source file on the local file system
            FileSystemStorageProvider.Instance.Store(GetTestFileBytes(sourceFile.FileName), sourceFile);

            CanPostAndGet(targetFile, deleteFolderBeforeStore: true, sourceFileData: sourceFile, deleteAfterGet: false);

            // Source and target files have different names, verify
            var provider = providerFactory();

            var postedFileExists = provider.Exists(targetFile);
            Assert.IsTrue(postedFileExists);

            // Cleanup
            provider.Delete(targetFile);

            // Shouldn't be there no mo...
            var targetExists = provider.Exists(targetFile);
            Assert.IsFalse(targetExists);
        }

        [Test]
        public void CanCreateAndDeleteFolder()
        {
            var provider = providerFactory();

            var folderName = Path.Combine(baseFolderName, "can-create-and-delete-subfolder");

            // Delete should not fail if not there
            provider.DeleteFolder(folderName, recursive: true);
            
            // Create the folder
            provider.CreateFolder(folderName);

            // Should delete just fine now
            provider.DeleteFolder(folderName, recursive: true);

            Assert.IsFalse(provider.FolderExists(folderName));
        }

        private void CanPostAndGet(FileSystemObject targetFile,
                                   bool deleteFolderBeforeStore = false,
                                   FileSystemObject sourceFileData = null,
                                   byte[] sourceBytes = null,
                                   bool deleteAfterGet = true)
        {
            var provider = providerFactory();

            if (deleteFolderBeforeStore)
            {
                provider.DeleteFolder(targetFile.FolderName, recursive: true);
            }

            StoreGet(provider, targetFile, sourceFileData, sourceBytes);

            if (!deleteAfterGet)
            {
                return;
            } 
            
            // Delete it
            provider.Delete(targetFile);

            // Shouldn't be there no mo...
            var targetExists = provider.Exists(targetFile);
            Assert.IsFalse(targetExists);

            if (deleteFolderBeforeStore)
            {
                provider.DeleteFolder(targetFile.FolderName, recursive: true);
            }
        }

        private void StoreGet(IFileStorageProvider provider,
                              FileSystemObject file,
                              FileSystemObject localFileSystemSourceFile = null,
                              byte[] sourceBytes = null)
        {
            // Pssshhhh Ahhhhh tssss Push it
            if (sourceBytes != null)
            {
                provider.Store(sourceBytes, file);
            }
            else if (localFileSystemSourceFile != null)
            {
                provider.Store(localFileSystemSourceFile, file);
                sourceBytes = File.ReadAllBytes(localFileSystemSourceFile.FullName);
            }
            else
            {
                throw new Exception("Must include either bytes or a local file reference to StoreGetDelete");
            }

            // Get it
            var getFile = provider.Get(file);

            var getFileHash = getFile.ToSha256HashBytes().ToBase64String();
            var fileHash = sourceBytes.ToSha256HashBytes().ToBase64String();

            Assert.AreEqual(fileHash, getFileHash);
        }

    }
}
