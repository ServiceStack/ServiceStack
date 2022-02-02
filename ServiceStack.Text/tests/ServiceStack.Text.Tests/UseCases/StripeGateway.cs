// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;
using ServiceStack;

namespace ServiceStack.Stripe
{
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
        public GetStripeCharges()
        {
            Include = new[] { "total_count" };
        }

        public int? Limit { get; set; }
        public string StartingAfter { get; set; }
        public string EndingBefore { get; set; }

        public DateTime? Created { get; set; }
        public string Customer { get; set; }

        [IgnoreDataMember]
        public string[] Include { get; set; }

        public string ToUrl(string absoluteUrl)
        {
            return Include == null ? absoluteUrl : absoluteUrl.AddQueryParam("include[]", string.Join(",", Include));
        }
    }

    /* Customers
	 * https://stripe.com/docs/api/curl#customers
	 */
    [Route("/customers")]
    public class CreateStripeCustomer : IPost, IReturn<StripeCustomer>
    {
        public int AccountBalance { get; set; }
        public StripeCard Card { get; set; }
        public string Coupon { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string Plan { get; set; }
        public int? Quantity { get; set; }
        public DateTime? TrialEnd { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public string Currency { get; set; }
        public string BusinessVatId { get; set; }
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
        public string Currency { get; set; }
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
        public string Currency { get; set; }
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
        public GetStripeCustomers()
        {
            Include = new[] { "total_count" };
        }

        public int? Limit { get; set; }
        public string StartingAfter { get; set; }
        public string EndingBefore { get; set; }

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
        public GetStripeCustomerCards()
        {
            Include = new[] { "total_count" };
        }

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

    [Route("/customers/{CustomerId}/subscription")]
    public class CancelStripeSubscription : IDelete, IReturn<StripeSubscription>
    {
        public string CustomerId { get; set; }
        public bool AtPeriodEnd { get; set; }
    }


    [Route("/customers/{CustomerId}/subscriptions/{SubscriptionId}")]
    public class GetStripeSubscription : IGet, IReturn<StripeSubscription>
    {
        public string CustomerId { get; set; }
        public string SubscriptionId { get; set; }
    }


    /* Plans
	 * https://stripe.com/docs/api/curl#plans
	 */
    [Route("/plans")]
    public class CreateStripePlan : IPost, IReturn<StripePlan>
    {
        public string Id { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public StripePlanInterval Interval { get; set; }
        public int? IntervalCount { get; set; }
        public string Name { get; set; }
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
        public string Name { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
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
        public int? PercentOff { get; set; }
        public DateTime? RedeemBy { get; set; }
    }

    [Route("/coupons/{Id}")]
    public class GetStripeCoupon : IGet, IReturn<StripeCoupon>
    {
        public string Id { get; set; }
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
    [Route("/accounts")]
    public class CreateStripeAccount : IPost, IReturn<CreateStripeAccountResponse>
    {
        public string Country { get; set; }
        public bool Managed { get; set; }
        public string Email { get; set; }
        public StripeTosAcceptance TosAcceptance { get; set; }
        public StripeLegalEntity LegalEntity { get; set; }
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
        public bool Managed { get; set; }
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
        private const string APIVersion = "2015-10-16";

        public TimeSpan Timeout { get; set; }

        public string Currency { get; set; }

        private string apiKey;
        private string publishableKey;
        public ICredentials Credentials { get; set; }
        private string UserAgent { get; set; }
        
        public HttpClient Client { get; set; }

        public StripeGateway(string apiKey, string publishableKey = null)
        {
            this.apiKey = apiKey;
            this.publishableKey = publishableKey;
            Credentials = new NetworkCredential(apiKey, "");
            Timeout = TimeSpan.FromSeconds(60);
            UserAgent = "servicestack .net stripe v1";
            Currency = Currencies.UnitedStatesDollar;
            Client = new HttpClient(new HttpClientHandler {
                Credentials = Credentials,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            }, disposeHandler:true);
            JsConfig.InitStatics();
        }
        
        protected virtual void HandleStripeException(WebException ex)
        {
            string errorBody = ex.GetResponseBody();
            var errorStatus = ex.GetStatus() ?? HttpStatusCode.BadRequest;

            if (ex.IsAny400())
            {
                var result = errorBody.FromJson<StripeErrors>();
                throw new StripeException(result.Error)
                {
                    StatusCode = errorStatus
                };
            }
        }

        private HttpRequestMessage PrepareRequest(string relativeUrl, string method, string requestBody, string idempotencyKey)
        {
            var url = BaseUrl.CombineWith(relativeUrl);

            var httpReq = new HttpRequestMessage(new HttpMethod(method), url);

            httpReq.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
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
                jsConfigScope = JsConfig.With(new Config {
                    DateHandler = DateHandler.UnixTime,
                    PropertyConvention = PropertyConvention.Lenient,
                    TextCase = TextCase.SnakeCase,
                });

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
            string relativeUrl = null;

            using (new ConfigScope())
            {
                relativeUrl = request.ToUrl(method);
                var body = sendRequestBody ? QueryStringSerializer.SerializeToString(request) : null;

                var json = Send(relativeUrl, method, body, idempotencyKey);

                var response = json.FromJson<T>();
                return response;
            }
        }

        public async Task<T> SendAsync<T>(IReturn<T> request, string method, bool sendRequestBody = true, string idempotencyKey = null)
        {
            string body = null;
            string relativeUrl = null;

            using (new ConfigScope())
            {
                relativeUrl = request.ToUrl(method);
                body = sendRequestBody ? QueryStringSerializer.SerializeToString(request) : null;
            }

            var json = await SendAsync(relativeUrl, method, body, idempotencyKey);

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

        public Task<T> SendAsync<T>(IReturn<T> request)
        {
            var method = GetMethod(request);
            return SendAsync(request, method, sendRequestBody: method == HttpMethods.Post || method == HttpMethods.Put);
        }

        public T Get<T>(IReturn<T> request)
        {
            return Send(request, HttpMethods.Get, sendRequestBody: false);
        }

        public Task<T> GetAsync<T>(IReturn<T> request)
        {
            return SendAsync(request, HttpMethods.Get, sendRequestBody: false);
        }

        public T Post<T>(IReturn<T> request)
        {
            return Send(request, HttpMethods.Post);
        }

        public Task<T> PostAsync<T>(IReturn<T> request)
        {
            return SendAsync(request, HttpMethods.Post);
        }

        public T Post<T>(IReturn<T> request, string idempotencyKey)
        {
            return Send(request, HttpMethods.Post, true, idempotencyKey);
        }

        public Task<T> PostAsync<T>(IReturn<T> request, string idempotencyKey)
        {
            return SendAsync(request, HttpMethods.Post, true, idempotencyKey);
        }

        public T Put<T>(IReturn<T> request)
        {
            return Send(request, HttpMethods.Put);
        }

        public Task<T> PutAsync<T>(IReturn<T> request)
        {
            return SendAsync(request, HttpMethods.Put);
        }

        public T Delete<T>(IReturn<T> request)
        {
            return Send(request, HttpMethods.Delete, sendRequestBody: false);
        }

        public Task<T> DeleteAsync<T>(IReturn<T> request)
        {
            return SendAsync(request, HttpMethods.Delete, sendRequestBody: false);
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
                    "{0}[{1}]".Fmt(name, entry.Key),
                    entry.Value.Value.ToUnixTime());
            }

            return url;
        }
    }

}

namespace ServiceStack.Stripe.Types
{
    public class StripeErrors
    {
        public StripeError Error { get; set; }
    }

    public class StripeError
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
        public string Param { get; set; }
    }

    public class StripeException : Exception
    {
        public StripeException(StripeError error)
            : base(error.Message)
        {
            Code = error.Code;
            Param = error.Param;
            Type = error.Type;
        }

        public string Code { get; set; }
        public string Param { get; set; }
        public string Type { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class StripeReference
    {
        public string Id { get; set; }
        public bool Deleted { get; set; }
    }

    public class StripeObject
    {
        public StripeType? Object { get; set; }
    }

    public class StripeId : StripeObject
    {
        public string Id { get; set; }
    }

    public enum StripeType
    {
        unknown,
        account,
        card,
        charge,
        coupon,
        customer,
        discount,
        dispute,
        @event,
        invoiceitem,
        invoice,
        line_item,
        plan,
        subscription,
        token,
        transfer,
        list,
    }

    public class StripeInvoice : StripeId
    {
        public DateTime Date { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public StripeCollection<StripeLineItem> Lines { get; set; }
        public int Subtotal { get; set; }
        public int Total { get; set; }
        public string Customer { get; set; }
        public bool Attempted { get; set; }
        public bool Closed { get; set; }
        public bool Paid { get; set; }
        public bool Livemode { get; set; }
        public int AttemptCount { get; set; }
        public int AmountDue { get; set; }
        public string Currency { get; set; }
        public int StartingBalance { get; set; }
        public int? EndingBalance { get; set; }
        public DateTime? NextPaymentAttempt { get; set; }
        public string Charge { get; set; }
        public StripeDiscount Discount { get; set; }
        public int? ApplicationFee { get; set; }
    }

    public class StripeCollection<T> : StripeId
    {
        public string Url { get; set; }
        public int TotalCount { get; set; }
        public bool? HasMore { get; set; }
        public List<T> Data { get; set; }
    }

    public class StripeLineItem : StripeId
    {
        public string Type { get; set; }
        public bool Livemode { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public bool Proration { get; set; }
        public StripePeriod Period { get; set; }
        public int? Quantity { get; set; }
        public StripePlan Plan { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class StripePlan : StripeId
    {
        public bool Livemode { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public string Identifier { get; set; }
        public StripePlanInterval Interval { get; set; }
        public string Name { get; set; }
        public int? TrialPeriodDays { get; set; }
    }

    public class StripePeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public enum StripePlanInterval
    {
        month,
        year
    }

    public class StripeDiscount : StripeId
    {
        public string Customer { get; set; }
        public StripeCoupon Coupon { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    public class StripeCoupon : StripeId
    {
        public int? PercentOff { get; set; }
        public int? AmountOff { get; set; }
        public string Currency { get; set; }
        public bool Livemode { get; set; }
        public StripeCouponDuration Duration { get; set; }
        public DateTime? RedeemBy { get; set; }
        public int? MaxRedemptions { get; set; }
        public int TimesRedeemed { get; set; }
        public int? DurationInMonths { get; set; }
    }

    public enum StripeCouponDuration
    {
        forever,
        once,
        repeating
    }

    public class StripeCustomer : StripeId
    {
        public DateTime? Created { get; set; }
        public bool Livemode { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public bool? Delinquent { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public StripeCollection<StripeSubscription> Subscriptions { get; set; }
        public StripeDiscount Discount { get; set; }
        public int AccountBalance { get; set; }
        public StripeCollection<StripeCard> Sources { get; set; }

        public bool Deleted { get; set; }
        public string DefaultSource { get; set; }
        public string Currency { get; set; }
        public string BusinessVatId { get; set; }
    }

    public class GetAllStripeCustomers
    {
        public int? Count { get; set; }
        public int? Offset { get; set; }
        public StripeDateRange Created { get; set; }
    }

    public class StripeDateRange
    {
        public DateTime? Gt { get; set; }
        public DateTime? Gte { get; set; }
        public DateTime? Lt { get; set; }
        public DateTime? Lte { get; set; }
    }

    public class StripeCard : StripeId
    {
        public string Brand { get; set; }
        public string Number { get; set; }
        public string Last4 { get; set; }
        public string DynamicLast4 { get; set; }
        public int ExpMonth { get; set; }
        public int ExpYear { get; set; }
        public string Cvc { get; set; }
        public string Name { get; set; }

        public string AddressCity { get; set; }
        public string AddressCountry { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressState { get; set; }
        public string AddressZip { get; set; }
        public StripeCvcCheck? CvcCheck { get; set; }
        public string AddressLine1Check { get; set; }
        public string AddressZipCheck { get; set; }

        public string Funding { get; set; }

        public string Fingerprint { get; set; }
        public string Customer { get; set; }
        public string Country { get; set; }
    }

    public enum StripeCvcCheck
    {
        Unknown,
        Pass,
        Fail,
        Unchecked
    }

    public class StripeSubscription : StripeId
    {
        public DateTime? CurrentPeriodEnd { get; set; }
        public StripeSubscriptionStatus Status { get; set; }
        public StripePlan Plan { get; set; }
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? TrialStart { get; set; }
        public bool? CancelAtPeriodEnd { get; set; }
        public DateTime? TrialEnd { get; set; }
        public DateTime? CanceledAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Customer { get; set; }
        public int Quantity { get; set; }
    }

    public enum StripeSubscriptionStatus
    {
        Unknown,
        Trialing,
        Active,
        PastDue,
        Canceled,
        Unpaid
    }

    public class StripeCharge : StripeId
    {
        public bool LiveMode { get; set; }
        public int Amount { get; set; }
        public bool Captured { get; set; }
        public StripeCard Source { get; set; }
        public DateTime Created { get; set; }
        public string Currency { get; set; }
        public bool Paid { get; set; }
        public bool Refunded { get; set; }
        public StripeCollection<StripeRefund> Refunds { get; set; }
        public int AmountRefunded { get; set; }
        public string BalanceTransaction { get; set; }
        public string Customer { get; set; }
        public string Description { get; set; }
        public StripeDispute Dispute { get; set; }
        public string FailureCode { get; set; }
        public string FailureMessage { get; set; }
        public string Invoice { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class CreateStripeCharge : StripeId
    {
        public int Amount { get; set; }
        public string Currency { get; set; }
        public string Customer { get; set; }
        public StripeCard Card { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public bool Capture { get; set; }
        public int? ApplicationFee { get; set; }
    }

    public class GetStripeCharge
    {
        public string Id { get; set; }
    }

    public class UpdateStripeCharge
    {
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class StripeRefund : StripeObject
    {
        public int Amount { get; set; }
        public string Charge { get; set; }
        public DateTime Created { get; set; }
        public string Currency { get; set; }
        public string BalanceTransaction { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public string ReceiptNumber { get; set; }
    }

    public class StripeDispute : StripeObject
    {
        public StripeDisputeStatus Status { get; set; }
        public string Evidence { get; set; }
        public string Charge { get; set; }
        public DateTime? Created { get; set; }
        public string Currency { get; set; }
        public int Amount;
        public bool LiveMode { get; set; }
        public StripeDisputeReason Reason { get; set; }
        public DateTime? EvidenceDueBy { get; set; }
    }

    public class StripeFeeDetail
    {
        public string Type { get; set; }
        public string Currency { get; set; }
        public string Application { get; set; }
        public string Description { get; set; }
        public int Amount { get; set; }
    }

    public enum StripeDisputeStatus
    {
        Won,
        Lost,
        NeedsResponse,
        UnderReview
    }

    public enum StripeDisputeReason
    {
        Duplicate,
        Fraudulent,
        SubscriptionCanceled,
        ProductUnacceptable,
        ProductNotReceived,
        Unrecognized,
        CreditNotProcessed,
        General
    }

    public static class Currencies
    {
        public const string UnitedArabEmiratesDirham = "AED";
        public const string AfghanAfghani = "AFN";
        public const string AlbanianLek = "ALL";
        public const string ArmenianDram = "AMD";
        public const string NetherlandsAntilleanGulden = "ANG";
        public const string AngolanKwanza = "AOA";
        public const string ArgentinePeso = "ARS";
        public const string AustralianDollar = "AUD";
        public const string ArubanFlorin = "AWG";
        public const string AzerbaijaniManat = "AZN";
        public const string BosniaAndHerzegovinaConvertibleMark = "BAM";
        public const string BarbadianDollar = "BBD";
        public const string BangladeshiTaka = "BDT";
        public const string BulgarianLev = "BGN";
        public const string BurundianFran = "BIF";
        public const string BermudianDollar = "BMD";
        public const string BruneiDollar = "BND";
        public const string BolivianBoliviano = "BOB";
        public const string BrazilianReal = "BRL";
        public const string BahamianDollar = "BSD";
        public const string BotswanaPula = "BWP";
        public const string BelizeDollar = "BZD";
        public const string CanadianDollar = "CAD";
        public const string CongoleseFranc = "CDF";
        public const string SwissFranc = "CHF";
        public const string ChileanPeso = "CLP";
        public const string ChineseRenminbiYuan = "CNY";
        public const string ColombianPeso = "COP";
        public const string CostaRicanColón = "CRC";
        public const string CapeVerdeanEscudo = "CVE";
        public const string CzechKoruna = "CZK";
        public const string DjiboutianFranc = "DJF";
        public const string DanishKrone = "DKK";
        public const string DominicanPeso = "DOP";
        public const string AlgerianDinar = "DZD";
        public const string EstonianKroon = "EEK";
        public const string EgyptianPound = "EGP";
        public const string EthiopianBirr = "ETB";
        public const string Euro = "EUR";
        public const string FijianDollar = "FJD";
        public const string FalklandIslandsPound = "FKP";
        public const string BritishPound = "GBP";
        public const string GeorgianLari = "GEL";
        public const string GibraltarPound = "GIP";
        public const string GambianDalasi = "GMD";
        public const string GuineanFranc = "GNF";
        public const string GuatemalanQuetzal = "GTQ";
        public const string GuyaneseDollar = "GYD";
        public const string HongKongDollar = "HKD";
        public const string HonduranLempira = "HNL";
        public const string CroatianKuna = "HRK";
        public const string HaitianGourde = "HTG";
        public const string HungarianForint = "HUF";
        public const string IndonesianRupiah = "IDR";
        public const string IsraeliNewSheqel = "ILS";
        public const string IndianRupee = "INR";
        public const string IcelandicKróna = "ISK";
        public const string JamaicanDollar = "JMD";
        public const string JapaneseYen = "JPY";
        public const string KenyanShilling = "KES";
        public const string KyrgyzstaniSom = "KGS";
        public const string CambodianRiel = "KHR";
        public const string ComorianFranc = "KMF";
        public const string SouthKoreanWon = "KRW";
        public const string CaymanIslandsDollar = "KYD";
        public const string KazakhstaniTenge = "KZT";
        public const string LaoKip = "LAK";
        public const string LebanesePound = "LBP";
        public const string SriLankanRupee = "LKR";
        public const string LiberianDollar = "LRD";
        public const string LesothoLoti = "LSL";
        public const string LithuanianLitas = "LTL";
        public const string LatvianLats = "LVL";
        public const string MoroccanDirham = "MAD";
        public const string MoldovanLeu = "MDL";
        public const string MalagasyAriary = "MGA";
        public const string MacedonianDenar = "MKD";
        public const string MongolianTögrög = "MNT";
        public const string MacanesePataca = "MOP";
        public const string MauritanianOuguiya = "MRO";
        public const string MauritianRupee = "MUR";
        public const string MaldivianRufiyaa = "MVR";
        public const string MalawianKwacha = "MWK";
        public const string MexicanPeso = "MXN";
        public const string MalaysianRinggit = "MYR";
        public const string MozambicanMetical = "MZN";
        public const string NamibianDollar = "NAD";
        public const string NigerianNaira = "NGN";
        public const string NicaraguanCórdoba = "NIO";
        public const string NorwegianKrone = "NOK";
        public const string NepaleseRupee = "NPR";
        public const string NewZealandDollar = "NZD";
        public const string PanamanianBalboa = "PAB";
        public const string PeruvianNuevoSol = "PEN";
        public const string PapuaNewGuineanKina = "PGK";
        public const string PhilippinePeso = "PHP";
        public const string PakistaniRupee = "PKR";
        public const string PolishZłoty = "PLN";
        public const string ParaguayanGuaraní = "PYG";
        public const string QatariRiyal = "QAR";
        public const string RomanianLeu = "RON";
        public const string SerbianDinar = "RSD";
        public const string RussianRuble = "RUB";
        public const string RwandanFranc = "RWF";
        public const string SaudiRiyal = "SAR";
        public const string SolomonIslandsDollar = "SBD";
        public const string SeychelloisRupee = "SCR";
        public const string SwedishKrona = "SEK";
        public const string SingaporeDollar = "SGD";
        public const string SaintHelenianPound = "SHP";
        public const string SierraLeoneanLeone = "SLL";
        public const string SomaliShilling = "SOS";
        public const string SurinameseDollar = "SRD";
        public const string SãoToméandPríncipeDobra = "STD";
        public const string SalvadoranColón = "SVC";
        public const string SwaziLilangeni = "SZL";
        public const string ThaiBaht = "THB";
        public const string TajikistaniSomoni = "TJS";
        public const string TonganPaʻanga = "TOP";
        public const string TurkishLira = "TRY";
        public const string TrinidadandTobagoDollar = "TTD";
        public const string NewTaiwanDollar = "TWD";
        public const string TanzanianShilling = "TZS";
        public const string UkrainianHryvnia = "UAH";
        public const string UgandanShilling = "UGX";
        public const string UnitedStatesDollar = "USD";
        public const string UruguayanPeso = "UYU";
        public const string UzbekistaniSom = "UZS";
        public const string VenezuelanBolívar = "VEF";
        public const string VietnameseĐồng = "VND";
        public const string VanuatuVatu = "VUV";
        public const string SamoanTala = "WST";
        public const string CentralAfricanCfaFranc = "XAF";
        public const string EastCaribbeanDollar = "XCD";
        public const string WestAfricanCfaFranc = "XOF";
        public const string CfpFranc = "XPF";
        public const string YemeniRial = "YER";
        public const string SouthAfricanRand = "ZAR";
    }
}
