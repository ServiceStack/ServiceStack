using System;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class LoadSelectIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class PlayerEquipment
    {
        public string Id => $"{PlayerId}/{ItemId}";

        public int PlayerId { get; set; }

        [References(typeof(ItemData))]
        public int ItemId { get; set; }

        public int Quantity { get; set; }

        public bool IsEquipped { get; set; }

        [Reference]
        public ItemData ItemData { get; set; }
    }

    public class ItemData
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Data { get; set; }
    }

    [Test]
    public void Can_LoadSelect_PlayerEquipment()
    {
        using var db = OpenDbConnection();
        db.DropTable<PlayerEquipment>();
        db.DropTable<ItemData>();

        db.CreateTable<ItemData>();
        db.CreateTable<PlayerEquipment>();

        var item1 = new ItemData { Data = "ITEM1" };
        db.Save(item1);

        db.Save(new PlayerEquipment
        {
            PlayerId = 1,
            ItemId = item1.Id,
            Quantity = 1,
            IsEquipped = true,
        });

        var item2 = new ItemData { Data = "ITEM2" };
        db.Save(item2);

        db.Save(new PlayerEquipment
        {
            PlayerId = 1,
            ItemId = item2.Id,
            Quantity = 1,
            IsEquipped = false,
        });

        var playerId = 1;
        var results = db.LoadSelect<PlayerEquipment>(q => q.PlayerId == playerId);

        results.PrintDump();
    }

    [Alias("EventCategory")]
    public class EventCategoryTbl : IHasSoftDelete, IHasTimeStamp
    {
        [PrimaryKey]
        public Guid EventCategoryId { get; set; }


        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Link to the file record that contains any image related to this category
        /// </summary>
        [References(typeof(FileTbl))]
        public Guid LinkedImageId { get; set; }

        [Reference]
        public FileTbl LinkedImage { get; set; }

        public bool IsDeleted { get; set; }

        [RowVersion]
        public ulong RowVersion { get; set; }
    }

    [Alias("File")]
    public class FileTbl : IHasSoftDelete, IHasTimeStamp
    {
        [PrimaryKey]
        public Guid FileId { get; set; }

        public string Name { get; set; }

        public string Extension { get; set; }

        public long FileSizeBytes { get; set; }

        public bool IsDeleted { get; set; }

        [RowVersion]
        public ulong RowVersion { get; set; }
    }

    public interface IHasTimeStamp
    {
        [RowVersion]
        ulong RowVersion { get; set; }
    }

    public interface IHasSoftDelete
    {
        bool IsDeleted { get; set; }
    }

    private static void CreateTables(IDbConnection db)
    {
        db.DropTable<EventCategoryTbl>();
        db.DropTable<FileTbl>();

        db.CreateTable<FileTbl>();
        db.CreateTable<EventCategoryTbl>();
    }

    [Test]
    public void Can_execute_LoadSelect_when_child_references_implement_IHasSoftDelete()
    {
        // Automatically filter out all soft deleted records, for ALL table types.
        OrmLiteConfig.SqlExpressionSelectFilter = q =>
        {
            if (q.ModelDef.ModelType.HasInterface(typeof(IHasSoftDelete)))
            {
                q.Where<IHasSoftDelete>(x => x.IsDeleted != true);
            }
        };

        using (var db = OpenDbConnection())
        {
            CreateTables(db);

            var results = db.LoadSelect<EventCategoryTbl>();
        }

        OrmLiteConfig.SqlExpressionSelectFilter = null;
    }

    [Test]
    public void Can_execute_SoftDelete_with_GroupBy()
    {
        OrmLiteConfig.SqlExpressionSelectFilter = q =>
        {
            if (q.ModelDef.ModelType.HasInterface(typeof(IHasSoftDelete)))
            {
                q.Where<IHasSoftDelete>(x => x.IsDeleted != true);
            }
        };

        using (var db = OpenDbConnection())
        {
            CreateTables(db);

            var name = "name";
            var q = db.From<FileTbl>()
                .Where(x => x.Name == name && x.FileSizeBytes > 1000)
                .GroupBy(x => x.Extension)
                .Select(x => new { x.Extension, Total = Sql.As(Sql.Count("*"), "Total") });

            var results = db.Dictionary<string, long>(q);
        }

        OrmLiteConfig.SqlExpressionSelectFilter = null;
    }
        
    public class Person
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public int ContactId { get; set; }
            
        [Reference]
        public Contact Contact { get; set; }
    }
        
    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Mobile { get; set; }
    }

    [Test]
    public async Task Can_order_by_parent_table_in_LoadSelectAsync()
    {
        OrmLiteConfig.BeforeExecFilter = cmd => cmd.GetDebugString().Print();

        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Person>();
        db.DropAndCreateTable<Contact>();
                
        string[] include = null; 
        var personQuery = db.From<Person>();
        personQuery.OrderByFields("Id");

        var results = await db.LoadSelectAsync(personQuery, include);
    }
}