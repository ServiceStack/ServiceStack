# ServiceStack.MsgPack features

MessagePack content negotiation, typed HTTP transport and byte serialization helpers.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.MsgPack.csproj](ServiceStack.MsgPack.csproj) for conditional compilation and dependencies.
- This is a binary HTTP format plugin, not the gRPC transport. Wire compatibility depends on serializer configuration and DTO contracts.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Server content format | Register binary request/response serialization with ServiceStack. | [MsgPackFormat.cs](MsgPackFormat.cs) |
| Typed binary client | Call DTO services using the binary content type. | [MsgPackServiceClient.cs](MsgPackServiceClient.cs) |
| Serialization helpers | Serialize values to bytes and deserialize typed values. | [MsgPackExtensions.cs](MsgPackExtensions.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [messagepack-format.md](../../../../docs.servicestack.net/MyApp/_pages/messagepack-format.md)
- [formats.md](../../../../docs.servicestack.net/MyApp/_pages/formats.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
