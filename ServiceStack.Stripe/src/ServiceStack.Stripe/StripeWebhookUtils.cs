// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace ServiceStack.Stripe;

public static class StripeWebhookUtils
{
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromSeconds(300); // 5 minutes

    /// <summary>
    /// Verifies the Stripe-Signature header against the raw payload and webhook signing secret.
    /// Returns true if valid, false otherwise.
    /// </summary>
    public static bool VerifySignature(string payload, string sigHeader, string secret, TimeSpan? tolerance = null, long? utcNowSeconds = null)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(sigHeader) || string.IsNullOrEmpty(secret))
            return false;

        var (timestamp, signatures) = ParseSignatureHeader(sigHeader);
        if (timestamp == 0 || signatures.Count == 0)
            return false;

        var maxTolerance = tolerance ?? DefaultTolerance;
        if (maxTolerance > TimeSpan.Zero)
        {
            var nowSec = utcNowSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var diff = Math.Abs(nowSec - timestamp);
            if (diff > maxTolerance.TotalSeconds)
                return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var expectedSig = ComputeHmacSha256(secret, signedPayload);

        foreach (var sig in signatures)
        {
            if (CryptUtils.FixedTimeEquals(expectedSig, sig))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies the webhook signature and deserializes the payload into a StripeEvent.
    /// Throws StripeException if invalid.
    /// </summary>
    public static StripeEvent ConstructEvent(string payload, string sigHeader, string secret, TimeSpan? tolerance = null)
    {
        if (!VerifySignature(payload, sigHeader, secret, tolerance))
            throw new StripeException(new StripeError {
                Type = "webhook_error",
                Message = "The webhook signature could not be verified or the timestamp was outside the allowed tolerance."
            });

        return payload.FromJson<StripeEvent>();
    }

    internal static (long Timestamp, List<string> Signatures) ParseSignatureHeader(string sigHeader)
    {
        long timestamp = 0;
        var signatures = new List<string>();

        var parts = sigHeader.Split(',');
        foreach (var part in parts)
        {
            var kv = part.Trim().Split(new[] { '=' }, 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim();
            var val = kv[1].Trim();

            if (key == "t" && long.TryParse(val, out var t))
            {
                timestamp = t;
            }
            else if (key == "v1")
            {
                signatures.Add(val);
            }
        }

        return (timestamp, signatures);
    }

    public static string ComputeHmacSha256(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
