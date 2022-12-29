using System;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.TableOptions
{
    [TestFixture]
    public class FileTableAttributeTests : SqlServer2012TableOptionsOrmLiteTestBase
    {
        [Explicit("Requires FileGroups enabled in DB")]
        [Test]
        public void Can_select_from_FileTable()
        {
            Db.DropAndCreateTable<FileTable>();

            var ft = new FileTable
            {
                Name = "content.txt",
                Path = SqlHierarchyId.Parse("/1/"),
                FileContent = "contents".ToUtf8Bytes(),
                FileType = MimeTypes.PlainText
            };
            Db.Insert(ft);

            var fileTable = Db.Single<FileTable>(x => x.Name == "content.txt");

            Assert.IsNotNull(fileTable);
            Assert.IsTrue(ft.FileContent.EquivalentTo(fileTable.FileContent));
        }
    }

    
    public class FileWithCustomSelect : FileTable
    {
        [CustomSelect("Contents.GetFileNamespacePath() + (CASE WHEN is_directory = 1 THEN '\' ELSE '' END)")]
        public string FullPath { get; set; }
    }

    [SqlServerFileTable(directory: "FILESTREAM_DIR")]
    public class FileTable
    {
        [PrimaryKey]
        [CustomField("uniqueidentifier ROWGUIDCOL NOT NULL")]
        [Default("newuid()")]
        [Alias("stream_id")]
        public Guid Id { get; set; }

        [CustomField("varbinary(max) FILESTREAM")]
        [Alias("file_stream")]
        //[DataAnnotations.Ignore]
        public byte[] FileContent { get; set; }

        [Alias("name")]
        [StringLength(255)]
        public string Name { get; set; }

        [Alias("path_locator")]
        public SqlHierarchyId Path { get; set; }

        //[ForeignKey(typeof(FileStream))]
        [Alias("parent_path_locator")]
        [Compute]
        public SqlHierarchyId? ParentPath { get; set; }

        [Alias("file_type")]
        [Compute]
        [StringLength(255)]
        public string FileType { get; set; }

        [Alias("cached_file_size")]
        [Compute]
        public long? FileSize { get; set; }

        [Alias("creation_time")]
        public DateTimeOffset CreationDateTime { get; set; }

        [Alias("last_write_time")]
        public DateTimeOffset LastWriteDateTime { get; set; }

        [Alias("last_access_time")]
        public DateTimeOffset? LastAccessDateTime { get; set; }

        [Alias("is_directory")]
        public bool IsDirectory { get; set; }

        [Alias("is_offline")]
        public bool IsOffline { get; set; }

        [Alias("is_hidden")]
        public bool IsHidden { get; set; }

        [Alias("is_readonly")]
        public bool IsReadOnly { get; set; }

        [Alias("is_archive")]
        public bool IsArchive { get; set; }

        [Alias("is_system")]
        public bool IsSystem { get; set; }

        [Alias("is_temporary")]
        public bool IsTemporary { get; set; }
    }
}