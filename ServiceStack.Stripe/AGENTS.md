# CONTEXT: ServiceStack.Stripe

## Purpose

`ServiceStack.Stripe` is a strongly-typed, message-based .NET client gateway (`StripeGateway`) for the **Stripe REST API**. It provides end-to-end typed Request and Response DTOs for processing payments, subscriptions, recurring billing, invoices, customers, and payment intents.

Key capabilities include:
- **Typed Message Gateway (`StripeGateway`)**: Full CRUD coverage of Stripe resources using typed C# request DTOs (`ChargeStripeCustomer`, `CreateStripeCustomer`, `CreateStripeSubscription`, etc.).
- **Stripe Connect Multi-Tenancy**: Built-in support for executing operations on behalf of connected merchant accounts via the `Stripe-Account` header.
- **Cryptographic Webhook Verification (`StripeWebhookUtils`)**: HMAC-SHA256 signature verification and replay-attack defense for incoming Stripe webhook events.
- **Form-UrlEncoded REST Serialization**: Custom serializer translating complex .NET object graphs into Stripe's bracketed query and form parameter conventions (`items[0][plan]=...`).

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Client     ServiceStack.Text
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                          ServiceStack.Stripe
                                    │
                                    ▼
       E-Commerce, Subscriptions, SaaS Billing & Webhook Processing
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Client`, `ServiceStack.Text`.
- **Depended on by**: ServiceStack applications handling payments, merchant onboarding, recurring billing, and Stripe webhook ingestion.
- **Role**: Provides the official payment gateway integration for the ServiceStack suite.

---

## Key Functionality

### Primary Types
| Class / Interface | Role |
|---|---|
| `StripeGateway` | Core client implementing `IStripeGateway`. Handles authentication, HTTP requests, retries, and deserialization. |
| `StripeWebhookUtils` | Validates HMAC-SHA256 signatures (`VerifySignature`) and parses verified events (`ConstructEvent`). |
| `StripeEvent` | Strongly-typed model representing verified Stripe webhook payloads. |
| DTOs (`CreateStripeCharge`, etc.) | Comprehensive message contracts for Stripe API endpoints. |

---

## Architecture & Design Patterns

### Message-Based HTTP Gateway
Operations inherit ServiceStack's `IReturn<T>` message pattern. Calling `gateway.Post(new ChargeStripeCustomer { ... })` maps to `POST /v1/charges` with form-urlencoded payloads and typed response mapping.

### Multi-Tenancy via Stripe Connect
`StripeGateway` accepts a `stripeAccount` property (e.g. `acct_123`) which automatically attaches the `Stripe-Account` header to outgoing HTTP requests, routing operations to connected accounts.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always observe these constraints:

### 1. Stripe Connect Header (`Stripe-Account`)
- **Risk**: Failing to propagate `StripeAccount` causes operations meant for connected merchant accounts to execute against the platform's primary Stripe account.
- **Rule**: In `StripeGateway.PrepareRequest`, attach `Stripe-Account: {stripeAccount}` whenever `StripeAccount` is configured.

### 2. Webhook Signature Verification & Constant-Time Comparison
- Incoming webhooks must be verified using `StripeWebhookUtils.ConstructEvent` or `VerifySignature`.
- Signature comparison must use constant-time verification (`CryptUtils.FixedTimeEquals`) to prevent side-channel timing attacks.
- Enforce timestamp tolerance (default: 300s) to guard against replay attacks.

### 3. Thread-Safe Query Strategy
- Modifying `QueryStringSerializer.ComplexTypeStrategy` in `ConfigScope` must use `[ThreadStatic]` backing fields to prevent cross-thread race conditions during query serialization.

### 4. Deterministic Client Disposal
- `StripeGateway` implements `IDisposable` to properly dispose the underlying `HttpClient` and prevent connection/socket leaks.

### 5. Array Parameter Serialization
- In `.ToUrl` overrides for list queries (e.g. `GetStripeCustomers`), array parameters must be repeated (`include[]=a&include[]=b`), not joined with commas, conforming to Stripe's API specification.

---

## Common Modification Scenarios

1. **Processing a Payment**
   ```csharp
   using var gateway = new StripeGateway(apiKey);
   var charge = await gateway.PostAsync(new ChargeStripeCustomer {
       Customer = customerId,
       Amount = 2500, // $25.00
       Currency = "usd",
       Description = "Monthly Subscription",
   });
   ```

2. **Handling Verified Webhooks**
   ```csharp
   var json = await request.GetRawBodyAsync();
   var sigHeader = request.Headers["Stripe-Signature"];
   var stripeEvent = StripeWebhookUtils.ConstructEvent(json, sigHeader, webhookSecret);
   ```
