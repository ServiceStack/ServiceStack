using System.IO;
using NUnit.Framework;
using ServiceStack.Aws.FileStorage;

namespace ServiceStack.Aws.Tests.FileStorage
{
    [TestFixture]
    public class FileSystemObjectTests
    {
        [Test]
        public void ParsesValidWindowsName()
        {
            var test = new FileSystemObject("c:\\temp\\subdir1\\subdir2\\testfilename.txt");

            Assert.AreEqual(test.FileName, "testfilename");
            Assert.AreEqual(test.FileExtension, "txt");
            Assert.AreEqual(test.FileNameAndExtension, "testfilename.txt");
            Assert.AreEqual(test.FolderName, "c:\\temp\\subdir1\\subdir2");
            Assert.AreEqual(test.FullName, "c:\\temp\\subdir1\\subdir2\\testfilename.txt");
        }

        [Test]
        public void ParsesValidUncName()
        {
            var test = new FileSystemObject("\\root\\subdir1\\subdir2\\testfile.zip");

            Assert.AreEqual(test.FileName, "testfile");
            Assert.AreEqual(test.FileExtension, "zip");
            Assert.AreEqual(test.FileNameAndExtension, "testfile.zip");
            Assert.AreEqual(test.FolderName, "\\root\\subdir1\\subdir2");
            Assert.AreEqual(test.FullName, "\\root\\subdir1\\subdir2\\testfile.zip");
        }

        [Test]
        public void ParseValidS3Name()
        {
            var test = new FileSystemObject("/test-bucket-name/archive/subdir/testfile_hi.zip");
            
            Assert.AreEqual(test.FileName, "testfile_hi");
            Assert.AreEqual(test.FileExtension, "zip");
            Assert.AreEqual(test.FileNameAndExtension, "testfile_hi.zip");
            Assert.AreEqual(test.FolderName, "/test-bucket-name/archive/subdir");
            Assert.AreEqual(test.FullName, "/test-bucket-name/archive/subdir/testfile_hi.zip");
        }

        [Test]
        public void ParseValidMixedFileSystemNameUnix()
        {
            var testFileAndPath = Path.Combine("/test-bucket-name/archive", "subdir", "testfile_hi.zip");
            var test = new FileSystemObject(testFileAndPath);
            
            Assert.AreEqual(test.FileName, "testfile_hi");
            Assert.AreEqual(test.FileExtension, "zip");
            Assert.AreEqual(test.FileNameAndExtension, "testfile_hi.zip");
            Assert.AreEqual(test.FolderName, "/test-bucket-name/archive/subdir");
            Assert.AreEqual(test.FullName, "/test-bucket-name/archive/subdir/testfile_hi.zip");
        }

        [Test]
        public void ParseValidMixedFileSystemNameWindows()
        {
            var testFileAndPath = Path.Combine("\\test-bucket-name/archive", "subdir", "testfile_hi.zip");
            var test = new FileSystemObject(testFileAndPath);

            Assert.AreEqual(test.FileName, "testfile_hi");
            Assert.AreEqual(test.FileExtension, "zip");
            Assert.AreEqual(test.FileNameAndExtension, "testfile_hi.zip");
            Assert.AreEqual(test.FolderName, "\\test-bucket-name\\archive\\subdir");
            Assert.AreEqual(test.FullName, "\\test-bucket-name\\archive\\subdir\\testfile_hi.zip");
        }

        [Test]
        public void ParsesFileEndingWithDirectorySeparatorCorrectly()
        {
            var test = new FileSystemObject("/test-bucket-name/archive/subdir/testfile_hi.zip/");

            Assert.AreEqual(test.FileName, "testfile_hi");
            Assert.AreEqual(test.FileExtension, "zip");
            Assert.AreEqual(test.FileNameAndExtension, "testfile_hi.zip");
            Assert.AreEqual(test.FolderName, "/test-bucket-name/archive/subdir");
            Assert.AreEqual(test.FullName, "/test-bucket-name/archive/subdir/testfile_hi.zip");
        }

        [Test]
        public void SameFullNamesEqualEachOther()
        {
            var test = new FileSystemObject("/test-bucket-name/archive/subdir/testfile_hi.zip/");
            var test2 = new FileSystemObject("/test-bucket-name/archive\\subdir/testfile_hi.zip");

            Assert.AreEqual(test, test2);
            Assert.AreEqual(test.ToString(), test2.ToString());
        }

        [Test]
        public void ClonedObjectsEqualEachOther()
        {
            var test = new FileSystemObject("/test-bucket-name/archive/subdir/testfile_hi.zip/");
            var test2 = test.Clone();

            Assert.AreEqual(test, test2);
            Assert.AreEqual(test.ToString(), test2.ToString());
        }
    }
}