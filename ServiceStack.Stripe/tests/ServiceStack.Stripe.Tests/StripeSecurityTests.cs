using System;
using System.Reflection;
using System.Net.Http;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;

namespace Stripe.Tests;

[TestFixture]
public class StripeSecurityTests
{
    [Test]
    public void StripeGateway_attaches_Stripe_Account_header()
    {
        using var gateway = new StripeGateway("sk_test_123", stripeAccount: "acct_connected456");
        Assert.That(gateway.StripeAccount, Is.EqualTo("acct_connected456"));

        var mi = typeof(StripeGateway).GetMethod("PrepareRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        var httpReq = (HttpRequestMessage)mi.Invoke(gateway, new object[] { "charges", "GET", null, null });

        Assert.That(httpReq.Headers.Contains("Stripe-Account"), Is.True);
        Assert.That(httpReq.Headers.GetValues("Stripe-Account"), Is.EquivalentTo(new[] { "acct_connected456" }));
        Assert.That(httpReq.Headers.GetValues("Stripe-Version"), Is.EquivalentTo(new[] { StripeGateway.DefaultAPIVersion }));
    }

    [Test]
    public void StripeGateway_propagates_Timeout_to_HttpClient()
    {
        using var gateway = new StripeGateway("sk_test_123");
        gateway.Timeout = TimeSpan.FromSeconds(45);
        Assert.That(gateway.Client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(45)));
    }

    [Test]
    public void StripeGateway_disposes_HttpClient()
    {
        var gateway = new StripeGateway("sk_test_123");
        var client = gateway.Client;
        gateway.Dispose();

        Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.GetAsync("https://api.stripe.com"));
    }

    [Test]
    public void ToUrl_formats_include_parameters_as_separate_query_keys()
    {
        var customersReq = new GetStripeCustomers {
            Include = new[] { "total_count", "sources" }
        };
        var url = customersReq.ToUrl("https://api.stripe.com/v1/customers");
        Assert.That(url, Is.EqualTo("https://api.stripe.com/v1/customers?include%5b%5d=total%5fcount&include%5b%5d=sources"));

        var cardsReq = new GetStripeCustomerCards {
            Include = new[] { "total_count" }
        };
        var cardUrl = cardsReq.ToUrl("https://api.stripe.com/v1/customers/cus_123/sources");
        Assert.That(cardUrl, Is.EqualTo("https://api.stripe.com/v1/customers/cus_123/sources?include%5b%5d=total%5fcount"));
    }

    [Test]
    public void StripeWebhookUtils_verifies_valid_signature()
    {
        var payload = "{\"id\": \"evt_test_webhook\", \"object\": \"event\"}";
        var secret = "whsec_test_secret_key";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        var signature = StripeWebhookUtils.ComputeHmacSha256(secret, signedPayload);
        var sigHeader = $"t={timestamp},v1={signature}";

        var isValid = StripeWebhookUtils.VerifySignature(payload, sigHeader, secret);
        Assert.That(isValid, Is.True);

        var stripeEvent = StripeWebhookUtils.ConstructEvent(payload, sigHeader, secret);
        Assert.That(stripeEvent, Is.Not.Null);
        Assert.That(stripeEvent.Id, Is.EqualTo("evt_test_webhook"));
    }

    [Test]
    public void StripeWebhookUtils_rejects_tampered_payload_or_bad_secret()
    {
        var payload = "{\"id\": \"evt_test_webhook\", \"object\": \"event\"}";
        var secret = "whsec_test_secret_key";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = StripeWebhookUtils.ComputeHmacSha256(secret, $"{timestamp}.{payload}");
        var sigHeader = $"t={timestamp},v1={signature}";

        // Tampered payload
        Assert.That(StripeWebhookUtils.VerifySignature("{\"id\": \"evt_tampered\"}", sigHeader, secret), Is.False);

        // Wrong secret
        Assert.That(StripeWebhookUtils.VerifySignature(payload, sigHeader, "whsec_wrong_secret"), Is.False);

        // ConstructEvent throws StripeException on bad signature
        Assert.Throws<StripeException>(() => StripeWebhookUtils.ConstructEvent(payload, sigHeader, "whsec_wrong_secret"));
    }

    [Test]
    public void StripeWebhookUtils_rejects_expired_timestamp()
    {
        var payload = "{\"id\": \"evt_test_webhook\", \"object\": \"event\"}";
        var secret = "whsec_test_secret_key";
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var signature = StripeWebhookUtils.ComputeHmacSha256(secret, $"{oldTimestamp}.{payload}");
        var sigHeader = $"t={oldTimestamp},v1={signature}";

        var isValid = StripeWebhookUtils.VerifySignature(payload, sigHeader, secret, tolerance: TimeSpan.FromMinutes(5));
        Assert.That(isValid, Is.False);
    }
}
