using System;
using ServiceStack.Model;

using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models{
	
	public class SampleOrderLine : IHasStringId, IHasId<string>
	{
		public string AlbumName
		{
			get;
			set;
		}
	
		public string AlbumUrn
		{
			get;
			set;
		}
	
		public string ArtistName
		{
			get;
			set;
		}
	
		public string ArtistUrn
		{
			get;
			set;
		}
	
		public decimal CashMix
		{
			get;
			set;
		}
	
		public decimal CashMixValueIncVat
		{
			get;
			set;
		}
	
		public string ContentUrn
		{
			get
			{
				return this.MflowUrn;
			}
			set
			{
				this.MflowUrn = value;
			}
		}
	
		public string CostTierKeyName
		{
			get;
			set;
		}
	
		public DateTime CreatedDate
		{
			get;
			set;
		}
	
		public string Description
		{
			get;
			set;
		}
	
		public decimal DiscountMix
		{
			get;
			set;
		}
	
		public decimal DiscountMixValueExVat
		{
			get;
			set;
		}
	
		[Alias("DistributionDiscountAccruedEV")]
		public decimal DistributionDiscountAccruedExVat
		{
			get;
			set;
		}
	
		public decimal DistributionDiscountRate
		{
			get;
			set;
		}
	
		[StringLength(256)]
		public string Id
		{
			get;
			set;
		}
	
		public string Isrc
		{
			get;
			set;
		}
	
		public string MflowUrn
		{
			get;
			set;
		}
	
		public long OrderId
		{
			get;
			set;
		}
	
		public long OrderLineId
		{
			get;
			set;
		}
	
		public string OrderUrn
		{
			get
			{
				return SampleOrderLine.CreateUrn(this.UserId, this.OrderId, this.OrderLineId);
			}
		}
	
		public string PriceTierKeyName
		{
			get;
			set;
		}
	
		public Guid ProductId
		{
			get;
			set;
		}
	
		public int ProductPriceIncVat
		{
			get;
			set;
		}
	
		public string ProductType
		{
			get;
			set;
		}
	
		public decimal PromoMix
		{
			get;
			set;
		}
	
		public decimal PromoMixValueExVat
		{
			get;
			set;
		}
	
		public int Quantity
		{
			get;
			set;
		}
	
		[Alias("RecommendationDiscountAccruedEV")]
		public decimal RecommendationDiscountAccruedExVat
		{
			get;
			set;
		}
	
		public decimal RecommendationDiscountRate
		{
			get;
			set;
		}
	
		public Guid? RecommendationUserId
		{
			get;
			set;
		}
	
		public string RecommendationUserName
		{
			get;
			set;
		}
	
		public string SupplierKeyName
		{
			get;
			set;
		}
	
		public string Title
		{
			get;
			set;
		}
	
		public string TrackUrn
		{
			get;
			set;
		}
	
		public decimal TransactionValueExVat
		{
			get;
			set;
		}
	
		public decimal TransactionValueIncVat
		{
			get;
			set;
		}
	
		public string UpcEan
		{
			get;
			set;
		}
	
		public Guid UserId
		{
			get;
			set;
		}
	
		public string UserName
		{
			get;
			set;
		}
	
		public decimal VatRate
		{
			get;
			set;
		}
	
		public SampleOrderLine()
		{
		}
	
		public static SampleOrderLine Create(Guid userId)
		{
			return SampleOrderLine.Create(userId, 1, 1);
		}
	
		public static SampleOrderLine Create(Guid userId, int orderId, int orderLineId)
		{
			SampleOrderLine sampleOrderLine = new SampleOrderLine();
			sampleOrderLine.Id = SampleOrderLine.CreateUrn(userId, (long)orderId, (long)orderLineId);
			sampleOrderLine.CreatedDate = DateTime.Now;
			sampleOrderLine.OrderId = (long)orderId;
			sampleOrderLine.OrderLineId = (long)orderLineId;
			sampleOrderLine.AlbumName = "AlbumName";
			sampleOrderLine.CashMixValueIncVat = new decimal(1528914989, -1561970359, 372399876, false, 28);
			sampleOrderLine.TransactionValueExVat = new decimal(79, 0, 0, false, 2);
			sampleOrderLine.ContentUrn="urn:content:"+ Guid.NewGuid().ToString("N");
			return sampleOrderLine;
		}
	
		public static string CreateUrn(Guid userId, long orderId, long orderLineId)
		{
			return string.Format("urn:orderline:{0}/{1}/{2}", userId.ToString("N"), orderId, orderLineId);
		}
	}
}