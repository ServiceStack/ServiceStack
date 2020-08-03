using System;
using System.ComponentModel.DataAnnotations;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    [Alias("AliasAnnotations")]
    [Schema("Annotations.dbo")]
    [NamedConnection("AnnotationsDb")]
    [Tag("web"),Tag("mobile"),Tag("desktop")]
    public class HelloAnnotations : IReturn<HelloAnnotations>
    {
        [Display(AutoGenerateField = false, AutoGenerateFilter = true, ShortName = "Id", Order = 1)]
        [AutoIncrement]
        public int Id { get; set; }

        [Display(AutoGenerateField = false, AutoGenerateFilter = true, ShortName = "ItemNumber", Order = 1)]
        public string ItemNumber { get; set; }

        [Display(AutoGenerateField = false, AutoGenerateFilter = true, ShortName = "WarehouseCode", Order = 2)]
        public string WarehouseCode { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Qty", Order = 3)]
        public int? QtyOnHand { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Lot Serial", Order = 4)]
        public string LotSerial { get; set; }

        [Display(AutoGenerateField = false, AutoGenerateFilter = true, ShortName = "LocationCode", Order = 5)]
        public string LocationCode { get; set; }

        [Display(AutoGenerateField = false, AutoGenerateFilter = true, ShortName = "Device ID", Order = 6)]
        public string DeviceId { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Counted", Order = 7)]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "MM/dd/yyyy HH:mm:ss")]
        public DateTime CountDate { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Counted By", Order = 8)]
        public string DeviceUser { get; set; }

        [Display(AutoGenerateField = false, AutoGenerateFilter = false, ShortName = "BatchKey", Order = 9)]
        public int? BatchKey { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = false, ShortName = "Item", Order = -1)]
        [Association(name: "Item.ItemID", thisKey: "ItemKey", otherKey: "ItemKey")]
        public int ItemKey { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Expiration Date", Order = 4)]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "MM/yyyy")]
        public DateTime? ExpirationDate { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Updated", Order = 14)]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "MM/dd/yyyy HH:mm:ss")]
        public DateTime? UpdateDate { get; set; }

        [Display(AutoGenerateField = true, AutoGenerateFilter = true, ShortName = "Updated By", Order = 15)]
        public string UpdatedBy { get; set; }
    }
}