using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Model;

namespace ServiceStack.Common.Tests.Models
{
    public class SampleOrderLine
        : IHasStringId
    {
        public string Id { get; set; }

        public string OrderUrn
        {
            get
            {
                return CreateUrn(this.UserId, this.OrderId, this.OrderLineId);
            }
        }

        public long OrderId { get; set; }

        public long OrderLineId { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public Guid ProductId { get; set; }

        public string MflowUrn { get; set; }

        public string ProductType { get; set; }

        public string Description { get; set; }

        public string UpcEan { get; set; }

        public string Isrc { get; set; }

        public Guid? RecommendationUserId { get; set; }

        public string RecommendationUserName { get; set; }

        public string SupplierKeyName { get; set; }

        public string CostTierKeyName { get; set; }

        public string PriceTierKeyName { get; set; }

        public decimal VatRate { get; set; }

        public int ProductPriceIncVat { get; set; }

        public int Quantity { get; set; }

        public decimal TransactionValueExVat { get; set; }

        public decimal TransactionValueIncVat { get; set; }

        public decimal RecommendationDiscountRate { get; set; }

        public decimal DistributionDiscountRate { get; set; }

        public decimal RecommendationDiscountAccruedExVat { get; set; }

        public decimal DistributionDiscountAccruedExVat { get; set; }

        public decimal PromoMix { get; set; }

        public decimal DiscountMix { get; set; }

        public decimal CashMix { get; set; }

        public decimal PromoMixValueExVat { get; set; }

        public decimal DiscountMixValueExVat { get; set; }

        public decimal CashMixValueIncVat { get; set; }

        public string ContentUrn
        {
            get { return this.MflowUrn; }
            set { this.MflowUrn = value; }
        }

        public string TrackUrn
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string ArtistUrn
        {
            get;
            set;
        }

        public string ArtistName
        {
            get;
            set;
        }

        public string AlbumUrn
        {
            get;
            set;
        }

        public string AlbumName
        {
            get;
            set;
        }

        public static string CreateUrn(Guid userId, long orderId, long orderLineId)
        {
            return string.Format("urn:orderline:{0}/{1}/{2}",
                                 userId.ToString("N"), orderId, orderLineId);
        }

        public static SampleOrderLine Create(Guid userId)
        {
            return Create(userId, 1, 1);
        }

        public static SampleOrderLine Create(Guid userId, int orderId, int orderLineId)
        {
            return new SampleOrderLine
            {
                Id = CreateUrn(userId, orderId, orderLineId),
                CreatedDate = DateTime.Now,
                OrderId = orderId,
                OrderLineId = orderLineId,
                AlbumName = "AlbumName",
                CashMixValueIncVat = 0.79m / 1.15m,
                TransactionValueExVat = 0.79m,
                ContentUrn = "urn:content:" + Guid.NewGuid().ToString("N"),
            };
        }

    }
}