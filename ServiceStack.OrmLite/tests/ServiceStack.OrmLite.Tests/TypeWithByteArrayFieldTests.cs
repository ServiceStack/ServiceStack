using System.Data;
using System.IO;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class TypeWithByteArrayFieldTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void CanInsertAndSelectByteArray()
    {
        var orig = new TypeWithByteArrayField { Id = 1, Content = new byte[] { 0, 17, 0, 17, 0, 7 } };

        using (var db = OpenDbConnection())
        {
            db.CreateTable<TypeWithByteArrayField>(true);

            db.Save(orig);

            var target = db.SingleById<TypeWithByteArrayField>(orig.Id);

            Assert.AreEqual(orig.Id, target.Id);
            Assert.AreEqual(orig.Content, target.Content);
        }
    }

    [Test, Explicit]
    public void Can_add_attachment()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Attachment>();
            db.GetLastSql().Print();

            var bytes = "https://www.google.com/images/srpr/logo11w.png".GetBytesFromUrl();

            var file = new Attachment {
                Data = bytes,
                Description = "Google Logo",
                Type = "png",
                FileName = "logo11w.png"
            };

            db.Insert(file);

            var fromDb = db.Single<Attachment>(q => q.FileName == "logo11w.png");

            Assert.AreEqual(file.Data, fromDb.Data);

            File.WriteAllBytes(fromDb.FileName, fromDb.Data);
        }
    }

    [Test, Explicit]
    public void Can_upload_attachment_via_sp()
    {
        if (Dialect != Dialect.SqlServer)
            return;

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Attachment>();

            try
            {
                db.ExecuteSql("DROP PROCEDURE dbo.[SP_upload_file]");
            }
            catch (System.Exception) {}

            db.ExecuteSql(@"
CREATE PROCEDURE dbo.[SP_upload_file](          
      @filename varchar(50),
      @filetype varchar(50),
      @filecontent varbinary(MAX))
AS
begin
INSERT INTO [Attachment]([FileName], [Type], [Data], [Description])
VALUES ({0}filename, {0}filetype, {0}filecontent, {0}filename) 
end".Fmt(DialectProvider.ParamString));
            var bytes = "https://www.google.com/images/srpr/logo11w.png".GetBytesFromUrl();

            db.ExecuteNonQuery("EXEC SP_upload_file @filename, @filetype, @filecontent", 
                new {
                    filename = "logo11w.png",
                    filetype = "png",
                    filecontent = bytes,
                });

            var fromDb = db.Single<Attachment>(q => q.FileName == "logo11w.png");

            Assert.AreEqual(bytes, fromDb.Data);
        }
    }

    [Test, Explicit]
    public void Can_upload_attachment_via_sp_with_ADONET()
    {
        if (Dialect != Dialect.SqlServer)
            return;

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Attachment>();

            try
            {
                db.ExecuteSql("DROP PROCEDURE dbo.[SP_upload_file]");
            }
            catch (System.Exception) { }

            db.ExecuteSql(@"
CREATE PROCEDURE dbo.[SP_upload_file](          
      @filename varchar(50),
      @filetype varchar(50),
      @filecontent varbinary(MAX))
AS
begin
INSERT INTO [Attachment]([FileName], [Type], [Data], [Description])
VALUES ({0}filename, {0}filetype, {0}filecontent, {0}filename) 
end".Fmt(DialectProvider.ParamString));
            var bytes = "https://www.google.com/images/srpr/logo11w.png".GetBytesFromUrl();

            using (var dbCmd = db.CreateCommand())
            {
                var p = dbCmd.CreateParameter();
                p.ParameterName = "filename";
                p.DbType = DbType.String;
                p.Value = "logo11w.png";
                dbCmd.Parameters.Add(p);

                p = dbCmd.CreateParameter();
                p.ParameterName = "filetype";
                p.DbType = DbType.String;
                p.Value = "png";
                dbCmd.Parameters.Add(p);

                p = dbCmd.CreateParameter();
                p.ParameterName = "filecontent";
                p.DbType = DbType.Binary;
                p.Value = bytes;
                dbCmd.Parameters.Add(p);

                dbCmd.CommandText = "EXEC SP_upload_file @filename, @filetype, @filecontent";
                dbCmd.ExecuteNonQuery();
            }

            var fromDb = db.Single<Attachment>(q => q.FileName == "logo11w.png");

            Assert.AreEqual(bytes, fromDb.Data);
        }
    }
}

class TypeWithByteArrayField
{
    public int Id { get; set; }
    public byte[] Content { get; set; }
}

public class Attachment
{
    public string Description { get; set; }
    public string FileName { get; set; }
    public string Type { get; set; }
    public byte[] Data { get; set; }
}