// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace ServiceStack.Stripe;

/* Charges
 * https://stripe.com/docs/api/curl#charges
 */
[Route("/charges")]
public class ChargeStripeCustomer : IPost, IReturn<StripeCharge>
{
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string Customer { get; set; }
    public string Card { get; set; }
    public string Description { get; set; }
    public bool? Capture { get; set; }
    public int? ApplicationFee { get; set; }
}

[Route("/charges/{ChargeId}")]
public class GetStripeCharge : IGet, IReturn<StripeCharge>
{
    public string ChargeId { get; set; }
}

[Route("/charges/{ChargeId}")]
public class UpdateStripeCharge : IPost, IReturn<StripeCharge>
{
    [IgnoreDataMember]
    public string ChargeId { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

[Route("/charges/{ChargeId}/refund")]
public class RefundStripeCharge : IPost, IReturn<StripeCharge>
{
    [IgnoreDataMember]
    public string ChargeId { get; set; }
    public int? Amount { get; set; }
    public bool? RefundApplicationFee { get; set; }
}

[Route("/charges/{ChargeId}/capture")]
public class CaptureStripeCharge : IPost, IReturn<StripeCharge>
{
    [IgnoreDataMember]
    public string ChargeId { get; set; }
    public int? Amount { get; set; }
    public bool? ApplicationFee { get; set; }
}

[Route("/charges")]
public class GetStripeCharges : IGet, IReturn<StripeCollection<StripeCharge>>, IUrlFilter
{
    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public string EndingBefore { get; set; }

    public DateTime? Created { get; set; }
    public string Customer { get; set; }

    [IgnoreDataMember]
    public string[] Include { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        if (Include?.Length > 0)
        {
            foreach (var include in Include)
            {
                absoluteUrl = absoluteUrl.AddQueryParam("include[]", include);
            }
        }

        return absoluteUrl;
    }
}

/* Customers
 * https://stripe.com/docs/api/curl#customers
 */
[Route("/customers")]
public class CreateStripeCustomer : IPost, IReturn<StripeCustomer>
{
    public int AccountBalance { get; set; }
    public string BusinessVatId { get; set; }
    public string Coupon { get; set; }
    public string DefaultSource { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public StripeShipping Shipping { get; set; }
    public StripeCard Source { get; set; }
}

[Route("/customers")]
public class CreateStripeCustomerWithToken : IPost, IReturn<StripeCustomer>
{
    public int AccountBalance { get; set; }
    public string Card { get; set; }
    public string Coupon { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public string Plan { get; set; }
    public int? Quantity { get; set; }
    public DateTime? TrialEnd { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string BusinessVatId { get; set; }
}

[Route("/customers/{Id}")]
public class GetStripeCustomer : IGet, IReturn<StripeCustomer>
{
    public string Id { get; set; }
}

[Route("/customers/{Id}")]
public class UpdateStripeCustomer : IPost, IReturn<StripeCustomer>
{
    [IgnoreDataMember]
    public string Id { get; set; }
    public int AccountBalance { get; set; }
    public StripeCard Card { get; set; }
    public string Coupon { get; set; }
    public string DefaultSource { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public string Source { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string BusinessVatId { get; set; }
}

[Route("/customers/{Id}")]
public class DeleteStripeCustomer : IDelete, IReturn<StripeReference>
{
    public string Id { get; set; }
}

[Route("/customers")]
public class GetStripeCustomers : IGet, IReturn<StripeCollection<StripeCustomer>>, IUrlFilter
{
    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public string EndingBefore { get; set; }
    public string Email { get; set; }

    public DateTime? Created { get; set; }

    [IgnoreDataMember]
    public string[] Include { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        return Include == null ? absoluteUrl : absoluteUrl.AddQueryParam("include[]", string.Join(",", Include));
    }
}

/* Cards
 * https://stripe.com/docs/api/curl#cards
 */
[Route("/customers/{CustomerId}/cards")]
public class CreateStripeCard : IPost, IReturn<StripeCard>
{
    [IgnoreDataMember]
    public string CustomerId { get; set; }

    public StripeCard Card { get; set; }
}
    
[Route("/customers/{CustomerId}/sources")]
public class CreateStripeCardWithToken : IPost, IReturn<StripeCard>
{
    [IgnoreDataMember]
    public string CustomerId { get; set; }

    public string Source { get; set; }
}

[Route("/customers/{CustomerId}/cards/{CardId}")]
public class GetStripeCard : IGet, IReturn<StripeCard>
{
    public string CustomerId { get; set; }
    public string CardId { get; set; }
}

[Route("/customers/{CustomerId}/cards/{CardId}")]
public class UpdateStripeCard : IPost, IReturn<StripeCard>
{
    [IgnoreDataMember]
    public string CustomerId { get; set; }
    [IgnoreDataMember]
    public string CardId { get; set; }

    public string AddressCity { get; set; }
    public string AddressCountry { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string AddressState { get; set; }
    public string AddressZip { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public string Name { get; set; }
}

[Route("/customers/{CustomerId}/sources/{CardId}")]
public class DeleteStripeCustomerCard : IDelete, IReturn<StripeReference>
{
    public string CustomerId { get; set; }
    public string CardId { get; set; }
}

[Route("/customers/{CustomerId}/sources")]
public class GetStripeCustomerCards : IGet, IReturn<StripeCollection<StripeCard>>, IUrlFilter
{
    public string CustomerId { get; set; }

    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public string EndingBefore { get; set; }

    [IgnoreDataMember]
    public string[] Include { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        return Include == null ? absoluteUrl : absoluteUrl.AddQueryParam("include[]", string.Join(",", Include));
    }
}

/* Subscriptions
 * https://stripe.com/docs/api/curl#subscriptions
 */
[Route("/subscriptions/{SubscriptionId}")]
public class GetStripeSubscription : IGet, IReturn<StripeSubscription>
{
    public string SubscriptionId { get; set; }
}

[Route("/subscriptions/{SubscriptionId}")]
public class CancelStripeSubscription : IDelete, IReturn<StripeSubscription>
{
    public string SubscriptionId { get; set; }
    public bool? InvoiceNow { get; set; }
    public bool? Prorate { get; set; }
    public bool? CancelAtPeriodEnd { get; set; }
}

/* Customer Subscriptions (Old)
 */
[Route("/customers/{CustomerId}/subscription")]
public class SubscribeStripeCustomer : IPost, IReturn<StripeSubscription>
{
    [IgnoreDataMember]
    public string CustomerId { get; set; }
    public string Plan { get; set; }
    public string Coupon { get; set; }
    public bool? Prorate { get; set; }
    public DateTime? TrialEnd { get; set; }
    public string Card { get; set; }
    public int? Quantity { get; set; }
    public int? ApplicationFeePercent { get; set; }
}
    
[Obsolete("Use GetStripeSubscription")]
[Route("/customers/{CustomerId}/subscriptions/{SubscriptionId}")]
public class GetStripeCustomerSubscription : IGet, IReturn<StripeSubscription>
{
    public string CustomerId { get; set; }
    public string SubscriptionId { get; set; }
}
    
[Obsolete("Use CancelStripeSubscription")]
[Route("/customers/{CustomerId}/subscription")]
public class CancelStripeCustomerSubscriptions : IDelete, IReturn<StripeSubscription>
{
    public string CustomerId { get; set; }
    public bool AtPeriodEnd { get; set; }
}
    
    

/* Products
 * https://stripe.com/docs/api#products
 */

[Route("/products/{Id}")]
public class GetStripeProduct : IGet, IReturn<StripeProduct>
{
    public string Id { get; set; }
}

[Route("/products")]
public class CreateStripeProduct : IPost, IReturn<StripeProduct>, IStripeProduct, IUrlFilter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public StripeProductType Type { get; set; }
    public bool Active { get; set; }
    [IgnoreDataMember]
    public string[] Attributes { get; set; }
    public string Caption { get; set; }
    [IgnoreDataMember]
    public string[] DeactivateOn { get; set; }
    public string Description { get; set; }
    [IgnoreDataMember]
    public string[] Images { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public StripePackageDimensions PackageDimensions { get; set; }
    public bool Shippable { get; set; }
    public string StatementDescriptor { get; set; }
    public string Url { get; set; }

    public string ToUrl(string absoluteUrl) => this.UpdateUrl(absoluteUrl);
}

[Route("/products/{Id}")]
public class UpdateStripeProduct : IPost, IReturn<StripeProduct>, IStripeProduct, IUrlFilter
{
    [IgnoreDataMember]
    public string Id { get; set; }
    [IgnoreDataMember]
    public string[] Attributes { get; set; }
    public string Caption { get; set; }
    [IgnoreDataMember]
    public string[] DeactivateOn { get; set; }
    public string Description { get; set; }
    [IgnoreDataMember]
    public string[] Images { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string Name { get; set; }
    public StripePackageDimensions PackageDimensions { get; set; }
    public bool Shippable { get; set; }
    public string StatementDescriptor { get; set; }
    public string Url { get; set; }

    public string ToUrl(string absoluteUrl) => this.UpdateUrl(absoluteUrl);
}

public interface IStripeProduct
{
    string Id { get; set; }
    [IgnoreDataMember]
    string[] Attributes { get; set; }
    string Caption { get; set; }
    [IgnoreDataMember]
    string[] DeactivateOn { get; set; }
    string Description { get; set; }
    [IgnoreDataMember]
    string[] Images { get; set; }
    Dictionary<string, string> Metadata { get; set; }
    string Name { get; set; }
    StripePackageDimensions PackageDimensions { get; set; }
    bool Shippable { get; set; }
    string StatementDescriptor { get; set; }
    string Url { get; set; }
}

internal static class ProductExtensions
{
    internal static string UpdateUrl(this IStripeProduct product, string url)
    {
        if (product.Attributes?.Length > 0)
        {
            foreach (var attr in product.Attributes)
            {
                url = url.AddQueryParam("attributes[]", attr);
            }
        }

        if (product.DeactivateOn?.Length > 0)
        {
            foreach (var item in product.DeactivateOn)
            {
                url = url.AddQueryParam("deactivate_on[]", item);
            }
        }

        if (product.Images?.Length > 0)
        {
            foreach (var image in product.Images)
            {
                url = url.AddQueryParam("images[]", image);
            }
        }

        return url;
    }
}

[Route("/products/{Id}")]
public class DeleteStripeProduct : IDelete, IReturn<StripeReference>
{
    public string Id { get; set; }
}

[Route("/products")]
public class GetStripeProducts : IGet, IReturn<StripeCollection<StripeProduct>>, IUrlFilter
{
    public DateTime? Created { get; set; }
    public string EndingBefore { get; set; }
    [IgnoreDataMember]
    public string[] Ids { get; set; }
    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public bool Shippable { get; set; }
    public StripeProductType? Type { get; set; }
    public string Url { get; set; }

    [IgnoreDataMember]
    public StripeDateOptions CreatedOptions { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        absoluteUrl = Created != null || CreatedOptions == null
            ? absoluteUrl
            : absoluteUrl.AppendOptions("date", CreatedOptions);

        if (Ids?.Length > 0)
        {
            foreach (var id in Ids)
            {
                absoluteUrl = absoluteUrl.AddQueryParam("ids[]", id);
            }
        }

        return absoluteUrl;
    }
}


/* Plans
 * https://stripe.com/docs/api/curl#plans
 */
[Route("/plans")]
public class CreateStripePlan : IPost, IReturn<StripePlan>
{
    public string Id { get; set; }
    public string Currency { get; set; }
    public StripePlanInterval Interval { get; set; }
    public StripePlanProduct Product { get; set; }
    public int Amount { get; set; }
    public int? IntervalCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string Nickname { get; set; }

    /// <summary>
    /// Still supported but not specified in arguments in latest API Version: https://stripe.com/docs/api#create_plan
    /// </summary>
    public int? TrialPeriodDays { get; set; } 
}

[Route("/plans/{Id}")]
public class GetStripePlan : IGet, IReturn<StripePlan>
{
    public string Id { get; set; }
}

[Route("/plans/{Id}")]
public class UpdateStripePlan : IPost, IReturn<StripePlan>
{
    [IgnoreDataMember]
    public string Id { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string Nickname { get; set; }
    public string Product { get; set; }

    /// <summary>
    /// Still supported but not specified in arguments in latest API Version: https://stripe.com/docs/api#create_plan
    /// </summary>
    public int? TrialPeriodDays { get; set; }
}

[Route("/plans/{Id}")]
public class DeleteStripePlan : IDelete, IReturn<StripeReference>
{
    public string Id { get; set; }
}

[Route("/plans")]
public class GetStripePlans : IGet, IReturn<StripeCollection<StripePlan>>
{
    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public string EndingBefore { get; set; }
}

/* Coupons
 * https://stripe.com/docs/api/curl#coupons
 */
[Route("/coupons")]
public class CreateStripeCoupon : IPost, IReturn<StripeCoupon>
{
    public string Id { get; set; }
    public StripeCouponDuration Duration { get; set; }
    public int? AmountOff { get; set; }
    public string Currency { get; set; }
    public int? DurationInMonths { get; set; }
    public int? MaxRedemptions { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public int? PercentOff { get; set; }
    public DateTime? RedeemBy { get; set; }
}

[Route("/coupons/{Id}")]
public class GetStripeCoupon : IGet, IReturn<StripeCoupon>
{
    public string Id { get; set; }
}

[Route("/coupons/{Id}")]
public class UpdateStripeCoupon : IPost, IReturn<StripeCoupon>
{
    [IgnoreDataMember]
    public string Id { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

[Route("/coupons/{Id}")]
public class DeleteStripeCoupon : IDelete, IReturn<StripeReference>
{
    public string Id { get; set; }
}

[Route("/coupons")]
public class GetStripeCoupons : IGet, IReturn<StripeCollection<StripeCoupon>>
{
    public int? Limit { get; set; }
    public string StartingAfter { get; set; }
    public string EndingBefore { get; set; }
}

/* Discounts
 * https://stripe.com/docs/api/curl#discounts
 */
[Route("/customers/{CustomerId}/discount")]
public class DeleteStripeDiscount : IDelete, IReturn<StripeReference>
{
    public string CustomerId { get; set; }
}

/* Invoices
 * https://stripe.com/docs/api/curl#invoices
 */

[Route("/invoices/{Id}")]
public class GetStripeInvoice : IGet, IReturn<StripeInvoice>
{
    public string Id { get; set; }
}

[Route("/invoices")]
public class CreateStripeInvoice : IPost, IReturn<StripeInvoice>
{
    public string Customer { get; set; }
    public int? ApplicationFee { get; set; }
}

[Route("/invoices/{Id}/pay")]
public class PayStripeInvoice : IPost, IReturn<StripeInvoice>
{
    [IgnoreDataMember]
    public string Id { get; set; }
}

[Route("/invoices")]
public class GetStripeInvoices : IGet, IReturn<StripeCollection<StripeInvoice>>, IUrlFilter
{
    public string Customer { get; set; }
    public DateTime? Date { get; set; }
    public int? Count { get; set; }
    public int? Offset { get; set; }

    [IgnoreDataMember]
    public StripeDateOptions DateOptions { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        return Date != null || DateOptions == null 
            ? absoluteUrl 
            : absoluteUrl.AppendOptions("date", DateOptions);
    }
}

[Route("/invoices/upcoming")]
public class GetUpcomingStripeInvoice : IGet, IReturn<StripeInvoice>
{
    public string Customer { get; set; }
}

/* Tokens */
[Route("/tokens")]
public class CreateStripeToken : IPost, IReturn<StripeToken>
{
    public StripeCard Card { get; set; }

    public string Customer { get; set; }
}

public class StripeToken : StripeId
{
    public bool Livemode { get; set; }
    public DateTime Created { get; set; }
    public bool Used { get; set; }
    public string Type { get; set; }
    public StripeCard Card { get; set; }
}

/*
    Accounts
*/

public enum StripeAccountType
{
    custom,
    standard,
    express,
}

// https://github.com/stripe/stripe-node/blob/master/types/2020-03-02/Accounts.d.ts
public enum StripeCapability
{
    au_becs_debit_payments,
    bacs_debit_payments,
    card_issuing,
    card_payments,
    cartes_bancaires_payments,
    fpx_payments,
    jcb_payments,
    legacy_payments,
    tax_reporting_us_1099_misc,
    tax_reporting_us_1099_k,
    transfers,
}

[Route("/accounts")]
public class CreateStripeAccount : IPost, IReturn<CreateStripeAccountResponse>, IUrlFilter
{
    public string Country { get; set; }
    public string Email { get; set; }
    public StripeAccountType Type { get; set; }
    public StripeTosAcceptance TosAcceptance { get; set; }
    public StripeLegalEntity LegalEntity { get; set; }
    [IgnoreDataMember]
    public StripeCapability[] RequestedCapabilities { get; set; }

    public string ToUrl(string absoluteUrl)
    {
        if (RequestedCapabilities?.Length > 0)
        {
            foreach (var capability in RequestedCapabilities)
            {
                absoluteUrl = absoluteUrl.AddQueryParam($"capabilities[{capability}][requested]", true);
            }
        }
        return absoluteUrl;
    }
}

public class CreateStripeAccountResponse : StripeAccount
{
    public Dictionary<string, string> Keys { get; set; }
}

public class StripeTosAcceptance
{
    public DateTime Date { get; set; }
    public string Ip { get; set; }
    public string UserAgent { get; set; }
}

public class StripeLegalEntity
{
    public StripeOwner[] AdditionalOwners { get; set; }
    public StripeAddress Address { get; set; }
    public string BusinessName { get; set; }
    public bool? BusinessTaxIdProvided { get; set; }
    public StripeDate Dob { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public StripeAddress PersonalAddress { get; set; }
    public bool? PersonalIdNumberProvided { get; set; }
    public bool? SsnLast4Provided { get; set; }
    public string Type { get; set; }
    public StripeVerificationBusiness Verification { get; set; }
}

public class StripeAccount : StripeId
{
    public string BusinessName { get; set; }
    public string BusinessPrimaryColor { get; set; }
    public string BusinessUrl { get; set; }
    public bool ChargesEnabled { get; set; }
    public string Country { get; set; }
    public string[] CurrenciesSupported { get; set; }
    public bool DebitNegativeBalances { get; set; }
    public StripeDeclineCharge DeclineChargeOn { get; set; }
    public string DefaultCurrency { get; set; }
    public bool DetailsSubmitted { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public StripeLegalEntity LegalEntity { get; set; }
    public string ProductDescription { get; set; }
    public string StatementDescriptor { get; set; }
    public string SupportEmail { get; set; }
    public string SupportPhone { get; set; }
    public string SupportUrl { get; set; }
    public string Timezone { get; set; }
    public StripeTosAcceptance TosAcceptance { get; set; }
    public StripeVerificationAccount Verification { get; set; }
}

public class StripeDeclineCharge
{
    public bool AvsFailure { get; set; }
    public bool CvcFailure { get; set; }
}

public class StripeOwner
{
    public StripeAddress Address { get; set; }
    public StripeDate Dob { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class StripeAddress
{
    public string City { get; set; }
    public string Country { get; set; }
    public string Line1 { get; set; }
    public string Line2 { get; set; }
    public string PostalCode { get; set; }
    public string State { get; set; }
}

public class StripeDate
{
    public StripeDate() { }
    public StripeDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

public class StripeVerificationBusiness
{
    public string Details { get; set; }
    public string DetailsCode { get; set; }
    public string Document { get; set; }
    public string Status { get; set; }
}

public class StripeTransferSchedule
{
    public int DelayDays { get; set; }
    public string Interval { get; set; }
    public int MonthlyAnchor { get; set; }
    public string WeeklyAnchor { get; set; }
    public bool TransfersEnabled { get; set; }
}

public class StripeVerificationAccount
{
    public string DisabledReason { get; set; }
    public DateTime? DueBy { get; set; }
    public string[] FieldsNeeded { get; set; }
}


public class StripeGateway : IRestGateway
{
    private const string BaseUrl = "https://api.stripe.com/v1";
    private const string APIVersion = "2018-02-28";

    public TimeSpan Timeout { get; set; }

    public string Currency { get; set; }

    private string apiKey;
    private string publishableKey;
    private string stripeAccount;
    public ICredentials Credentials { get; set; }
    private string UserAgent { get; set; }
        
    public HttpClient Client { get; set; }

    public StripeGateway(string apiKey, string publishableKey = null, string stripeAccount = null)
    {
        this.apiKey = apiKey;
        this.publishableKey = publishableKey;
        this.stripeAccount = stripeAccount;
        Credentials = new NetworkCredential(apiKey, "");
        Timeout = TimeSpan.FromSeconds(60);
        UserAgent = "ServiceStack.Stripe";
        Currency = Currencies.UnitedStatesDollar;
        Client = new HttpClient(new HttpClientHandler {
            Credentials = Credentials,
            PreAuthenticate = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        }, disposeHandler:true);
        JsConfig.InitStatics();
    }

    private HttpRequestMessage PrepareRequest(string relativeUrl, string method, string requestBody, string idempotencyKey)
    {
        var url = BaseUrl.CombineWith(relativeUrl);

        var httpReq = new HttpRequestMessage(new HttpMethod(method), url);

        httpReq.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, Env.VersionString));
        httpReq.Headers.Add(HttpHeaders.Accept, MimeTypes.Json);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            httpReq.Headers.Add("Idempotency-Key", idempotencyKey);

        httpReq.Headers.Add("Stripe-Version", APIVersion);

        if (requestBody != null)
        {
            httpReq.Content = new StringContent(requestBody, Encoding.UTF8);
            if (method is HttpMethods.Post or HttpMethods.Put)
                httpReq.Content!.Headers.ContentType = new MediaTypeHeaderValue(MimeTypes.FormUrlEncoded);
        }

        return httpReq;
    }

    private Exception CreateException(HttpResponseMessage httpRes)
    {
#if NET6_0_OR_GREATER            
            return new HttpRequestException(httpRes.ReasonPhrase, null, httpRes.StatusCode); 
#else
        return new HttpRequestException(httpRes.ReasonPhrase); 
#endif
    }

    protected virtual string Send(string relativeUrl, string method, string requestBody, string idempotencyKey)
    {
        var httpReq = PrepareRequest(relativeUrl, method, requestBody, idempotencyKey);

#if NET6_0_OR_GREATER            
            var httpRes = Client.Send(httpReq);
            var responseBody = httpRes.Content.ReadAsStream().ReadToEnd(Encoding.UTF8);
#else
        var httpRes = Client.SendAsync(httpReq).GetAwaiter().GetResult();
        var responseBody = httpRes.Content.ReadAsStreamAsync().GetAwaiter().GetResult().ReadToEnd(Encoding.UTF8);
#endif
            
        if (httpRes.IsSuccessStatusCode)
            return responseBody;

        if (httpRes.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
        {
            var result = responseBody.FromJson<StripeErrors>();
            throw new StripeException(result.Error)
            {
                StatusCode = httpRes.StatusCode
            };
        }

        httpRes.EnsureSuccessStatusCode();
        throw CreateException(httpRes); // should never reach here 
    }

    protected virtual async Task<string> SendAsync(string relativeUrl, string method, string requestBody, string idempotencyKey, CancellationToken token=default)
    {
        var httpReq = PrepareRequest(relativeUrl, method, requestBody, idempotencyKey);

        var httpRes = await Client.SendAsync(httpReq, token).ConfigAwait();
        var responseBody = await (await httpRes.Content.ReadAsStreamAsync()).ReadToEndAsync(Encoding.UTF8).ConfigAwait();
        if (httpRes.IsSuccessStatusCode)
            return responseBody;

        if (httpRes.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
        {
            var result = responseBody.FromJson<StripeErrors>();
            throw new StripeException(result.Error)
            {
                StatusCode = httpRes.StatusCode
            };
        }

        httpRes.EnsureSuccessStatusCode();
        throw CreateException(httpRes); // should never reach here 
    }

    public class ConfigScope : IDisposable
    {
        private readonly WriteComplexTypeDelegate holdQsStrategy;
        private readonly JsConfigScope jsConfigScope;

        public ConfigScope()
        {
            var config = Config.Defaults;
            config.DateHandler = DateHandler.UnixTime;
            config.PropertyConvention = PropertyConvention.Lenient;
            config.TextCase = TextCase.SnakeCase;
            config.TreatEnumAsInteger = false;

            jsConfigScope = JsConfig.With(config);

            holdQsStrategy = QueryStringSerializer.ComplexTypeStrategy;
            QueryStringSerializer.ComplexTypeStrategy = QueryStringStrategy.FormUrlEncoded;
        }

        public void Dispose()
        {
            QueryStringSerializer.ComplexTypeStrategy = holdQsStrategy;
            jsConfigScope.Dispose();
        }
    }

    public T Send<T>(IReturn<T> request, string method, bool sendRequestBody = true, string idempotencyKey = null)
    {
        using (new ConfigScope())
        {
            var relativeUrl = request.ToUrl(method);
            var body = sendRequestBody ? QueryStringSerializer.SerializeToString(request) : null;

            var json = Send(relativeUrl, method, body, idempotencyKey);

            var response = json.FromJson<T>();
            return response;
        }
    }

    public async Task<T> SendAsync<T>(IReturn<T> request, string method, bool sendRequestBody = true, string idempotencyKey = null, CancellationToken token=default)
    {
        string relativeUrl;
        string body;

        using (new ConfigScope())
        {
            relativeUrl = request.ToUrl(method);
            body = sendRequestBody ? QueryStringSerializer.SerializeToString(request) : null;
        }

        var json = await SendAsync(relativeUrl, method, body, idempotencyKey, token);

        using (new ConfigScope())
        {
            var response = json.FromJson<T>();
            return response;
        }
    }

    private static string GetMethod<T>(IReturn<T> request)
    {
        var method = request is IPost ?
            HttpMethods.Post
            : request is IPut ?
                HttpMethods.Put
                : request is IDelete ?
                    HttpMethods.Delete
                    : HttpMethods.Get;
        return method;
    }

    public T Send<T>(IReturn<T> request)
    {
        var method = GetMethod(request);
        return Send(request, method, sendRequestBody: method == HttpMethods.Post || method == HttpMethods.Put);
    }

    public Task<T> SendAsync<T>(IReturn<T> request, CancellationToken token=default)
    {
        var method = GetMethod(request);
        return SendAsync(request, method, sendRequestBody: method == HttpMethods.Post || method == HttpMethods.Put, token: token);
    }

    public T Get<T>(IReturn<T> request)
    {
        return Send(request, HttpMethods.Get, sendRequestBody: false);
    }

    public Task<T> GetAsync<T>(IReturn<T> request, CancellationToken token=default)
    {
        return SendAsync(request, HttpMethods.Get, sendRequestBody: false, token: token);
    }

    public T Post<T>(IReturn<T> request)
    {
        return Send(request, HttpMethods.Post);
    }

    public Task<T> PostAsync<T>(IReturn<T> request, CancellationToken token=default)
    {
        return SendAsync(request, HttpMethods.Post, token: token);
    }

    public T Post<T>(IReturn<T> request, string idempotencyKey)
    {
        return Send(request, HttpMethods.Post, true, idempotencyKey);
    }

    public Task<T> PostAsync<T>(IReturn<T> request, string idempotencyKey, CancellationToken token=default)
    {
        return SendAsync(request, HttpMethods.Post, true, idempotencyKey, token);
    }

    public T Put<T>(IReturn<T> request)
    {
        return Send(request, HttpMethods.Put);
    }

    public Task<T> PutAsync<T>(IReturn<T> request, CancellationToken token=default)
    {
        return SendAsync(request, HttpMethods.Put, token: token);
    }

    public T Delete<T>(IReturn<T> request)
    {
        return Send(request, HttpMethods.Delete, sendRequestBody: false);
    }

    public Task<T> DeleteAsync<T>(IReturn<T> request, CancellationToken token=default)
    {
        return SendAsync(request, HttpMethods.Delete, sendRequestBody: false, token: token);
    }
}

public class StripeDateOptions
{
    public DateTime? After { get; set; }
    public DateTime? OnOrAfter { get; set; }
    public DateTime? Before { get; set; }
    public DateTime? OnOrBefore { get; set; }
}

internal static class UrlExtensions
{
    public static string AppendOptions(this string url, string name, StripeDateOptions options)
    {
        var sb = new StringBuilder();
        var map = new Dictionary<string, DateTime?>
        {
            { "gt", options.After },
            { "gte", options.OnOrAfter },
            { "lt", options.Before },
            { "lte", options.OnOrBefore },
        };

        foreach (var entry in map)
        {
            if (entry.Value == null)
                continue;

            url = url.AddQueryParam(
                $"{name}[{entry.Key}]",
                entry.Value.Value.ToUnixTime());
        }

        return url;
    }
}