# Security Changes & Remediation Reference (`ServiceStack.Stripe`)

This document details security vulnerabilities identified and remediated in `ServiceStack.Stripe`. It describes each finding, the changes introduced, and how to use the new security features.

---

## 1. Missing `Stripe-Account` Header in `PrepareRequest` (Stripe Connect Multi-Tenancy)
- **Severity**: High (Multi-Tenant Authorization Bypass / Financial Misrouting)
- **Description**: `StripeGateway` accepted `stripeAccount` in its constructor for Stripe Connect platform operations, but `PrepareRequest` failed to attach the `Stripe-Account` header to outgoing HTTP requests. Requests on behalf of connected merchant accounts were silently executed against the platform's primary Stripe account instead.
- **Change**: Updated `PrepareRequest` to include `Stripe-Account: {stripeAccount}` whenever `StripeAccount` is configured. Added a public `StripeAccount` property to allow reading and configuring connected account IDs dynamically.
- **Usage**:
  ```csharp
  var gateway = new StripeGateway(apiKey, stripeAccount: "acct_connected123");
  // Outgoing requests automatically include Stripe-Account: acct_connected123
  ```

---

## 2. Race Condition on Process-Wide State in `ConfigScope`
- **Severity**: Medium (Serialization Race Condition)
- **Description**: `ConfigScope` modified `QueryStringSerializer.ComplexTypeStrategy` (a process-wide global static property in `ServiceStack.Text`). Under concurrent requests, threads could mutate and restore this global delegate concurrently, corrupting query string serialization for other threads.
- **Change**: Updated `QueryStringSerializer.ComplexTypeStrategy` in `ServiceStack.Text` to use `[ThreadStatic]` backing fields. Scoped strategy modifications now strictly affect the current executing thread without cross-thread race conditions.

---

## 3. Stripe Webhook Signature Verification (`StripeWebhookUtils`)
- **Severity**: Medium (Webhook Spoofing / Forged Events / Replay Attacks)
- **Description**: `ServiceStack.Stripe` previously lacked a webhook signature validator, leaving applications vulnerable to accepting forged Stripe webhook events if they did not implement manual HMAC verification.
- **Change**: Added `StripeWebhookUtils` providing:
  - `VerifySignature(payload, sigHeader, secret, tolerance)`: Validates HMAC-SHA256 signatures and guards against replay attacks using timestamp tolerance (default: 300s / 5 min) with constant-time equality comparisons (`CryptUtils.FixedTimeEquals`).
  - `ConstructEvent(payload, sigHeader, secret, tolerance)`: Cryptographically verifies the signature and parses the verified event into a strongly-typed `StripeEvent`.
- **Usage**:
  ```csharp
  // In your Webhook HTTP handler:
  var json = await request.GetRawBodyAsync();
  var sigHeader = request.Headers["Stripe-Signature"];
  var webhookSecret = "whsec_...";

  try
  {
      StripeEvent stripeEvent = StripeWebhookUtils.ConstructEvent(json, sigHeader, webhookSecret);
      if (stripeEvent.Type == "charge.succeeded")
      {
          // Handle verified charge
      }
  }
  catch (StripeException ex)
  {
      // Invalid signature or expired timestamp
      return HttpResult.Status400("Invalid signature");
  }
  ```

---

## 4. Configured `Timeout` Not Applied to `HttpClient`
- **Severity**: Low (Slow-Loris / Socket Hang)
- **Description**: Setting `gateway.Timeout` had no effect on the underlying `HttpClient`, which retained its default 100-second timeout.
- **Change**: Updated the `Timeout` property to dynamically apply the timeout duration directly to `Client.Timeout`.

---

## 5. Missing `IDisposable` Implementation on `StripeGateway`
- **Severity**: Low (Socket / Connection Pool Leakage)
- **Description**: `StripeGateway` allocated an internal `HttpClientHandler` and `HttpClient` without implementing `IDisposable`.
- **Change**: `StripeGateway` now implements `IDisposable` to deterministically dispose the underlying `HttpClient` and connection handler.
- **Usage**:
  ```csharp
  using var gateway = new StripeGateway(apiKey);
  ```

---

## 6. Inconsistent Query Parameter Formatting in `GetStripeCustomers.ToUrl` and `GetStripeCustomerCards.ToUrl`
- **Severity**: Low (API Request Malformation)
- **Description**: `GetStripeCustomers` and `GetStripeCustomerCards` joined array parameters with commas (`include[]=a,b`) instead of repeating the parameter key (`include[]=a&include[]=b`), causing Stripe's API to reject the parameter.
- **Change**: Standardized `ToUrl` to iterate array elements and append separate `include[]` parameters matching `GetStripeCharges.ToUrl`.
