# Security and Hardening Improvements in ServiceStack

## Summary
This document summarizes modernization, null-safety, reliability, and bug fixes applied to the core `ServiceStack` framework library (`ServiceStack.csproj`).

---

## 1. Request Pipeline & Resolver Safety
- **`RequestExtensions.cs`**:
  - **`TryResolveInternal<T>`**: Guarded against null resolvers when `request is IHasResolver hasResolver` has a null `Resolver` or when `Service.GlobalResolver` is null, ensuring `TryResolve` returns `default` without throwing `NullReferenceException`.
  - **`GetRuntimeConfig<T>`**: Added `HostContext.AppHost != null` check so it safely returns `defaultValue` when AppHost is uninitialized.
  - **`RegisterForDispose`**: Safely verified `request.OriginalRequest is Microsoft.AspNetCore.Http.HttpRequest` before casting, falling back cleanly to `request.SetItem` rather than throwing `InvalidCastException` for mock, basic, or non-ASP.NET Core requests.

---

## 2. Host Context & AppHost Lifecycle
- **`HostContext.cs`**:
  - Added backing field `testMode` to support setting `HostContext.TestMode = true` in standalone unit test environments without requiring a full running AppHost.
  - Added null guard in `GetDefaultNamespace()` when `ServiceStackHost.Instance == null` to prevent `AssertAppHost()` from throwing `ConfigurationErrorsException`.
  - In `Reset()`, reset static `testMode = null` and `defaultOperationNamespace = null` so unit test state does not leak across AppHost lifecycles.
  - Added null-conditional access to `VirtualFileSources` accessors (`FileSystemVirtualFiles`, `MemoryVirtualFiles`, `GistVirtualFiles`).
- **`ServiceStackHost.cs`**:
  - Wrapped individual callbacks in `OnDisposeCallbacks` in try-catch blocks to prevent a failing callback from aborting container disposal, static state cleanup, and event unsubscription.

---

## 3. Session & Service Resilience
- **`ServiceExtensions.cs`**:
  - Fixed inverted condition bug in `SessionAs<TUserSession>` and `SessionAsAsync<TUserSession>` where `if (!Equals(mockSession, default(TUserSession)))` mistakenly discarded resolved mock sessions. Aligned with correct pattern `if (Equals(mockSession, default(TUserSession)))`.
  - Guarded against null service/request in `GetSessionId` and null AppHost in cache accessors.
- **`Service.cs`**:
  - Added safe navigation to `ServiceStackHost.Instance?.Container` in `GetResolver()`.
  - Guarded `GetService<T>()`, `GetRequiredService<T>()`, and `GetServices<T>()` against uninitialized `Request`.
- **`SessionFeature.cs`**:
  - Safely checked `session is IAuthSession authSession` and `HostContext.AppHost != null` before invoking `OnSessionFilter` in `GetOrCreateSession` and `GetOrCreateSessionAsync`, avoiding `InvalidCastException` when custom non-`IAuthSession` types are stored.

---

## 4. Real-Time & Messaging Hardening
- **`ServerEventsFeature.cs`**:
  - Implemented `IAsyncDisposable` on `MemoryServerEvents` (`ValueTask IAsyncDisposable.DisposeAsync()`) for modern asynchronous disposal workflows.
- **`Messaging/BackgroundMqService.cs`**:
  - In `GetStats()`, guarded `lock (workers)` to ensure `workers != null` before locking, avoiding `NullReferenceException` on `lock(null)`.
  - In `BackgroundMqWorker.Stop()`, added `cts?.Cancel()` to prevent NRE if worker is stopped after disposal.
  - In `BackgroundMqWorker.RunAsync`, added null-check on `cts != null && !cts.IsCancellationRequested`.

---

## 5. Web, HTTP, and Proxy Hardening
- **`HttpResult.cs`**:
  - In `DeleteCookie`, guarded retrieval of cookies from `req?.Response as IHttpResponse` to prevent `InvalidCastException` or NRE when using mock or basic requests.
- **`Host/Cookies.cs`**:
  - In `UseSecureCookie`, replaced `HostContext.Config?.UseSecureCookies` with `HostContext.AppHost?.Config?.UseSecureCookies` to avoid asserting AppHost presence, and added null-safe check `httpRes.Request?.IsSecureConnection == true`.
- **`Testing/MockHttpResponse.cs`**:
  - In constructor, changed `HostContext.AssertAppHost().GetCookies(this)` to `HostContext.AppHost?.GetCookies(this) ?? new Cookies(this)` to allow creating mock responses in isolated tests without an active AppHost.
- **`HttpResponseExtensionsInternal.cs`**:
  - Set `Content-Length` header on `ReadOnlyMemory<byte>` responses matching `byte[]` behavior.
  - Added support for `Memory<byte>` payloads.
- **`ProxyFeature.cs`**:
  - Avoided disposing caller's `httpReq.InputStream` directly; only dispose transformed intermediate streams.
  - Handled `WebException` when `webEx.Response` is null (e.g., DNS error, timeout), writing `502 Bad Gateway` instead of silent return.
- **`ServiceRoutesExtensions.cs`**:
  - In `IsSubclassOfRawGeneric`, added `toCheck != null` in while loop so inspecting interface types (where `BaseType` becomes `null`) does not throw `NullReferenceException`.
  - In `PropertyName`, safely unwrapped lambda unary operands and member expressions.
- **`CommandsFeature.cs`**:
  - Added null guard to `Median` extension method (`if (nums == null) return 0;`).
- **`ServiceStackDiagnostics.cs`**:
  - Safe-checked `listener?.IsEnabled(name) == true` in `Supports`.

---

## 6. Multi-Targeting & Compilation
- **`ServiceStack.csproj`**:
  - Standardized target framework define constants (`NET6_0`, `NET8_0`, `NET10_0`).

---

## 7. Authentication & Authorization Subsystem (`ServiceStack/Auth`)
- **`AuthProvider.cs`**:
  - Fixed bug in `LogoutAsync` where `if (service is IAuthSessionExtended sessionExt)` was checked instead of `session`, which prevented custom session `OnLogoutAsync` hooks from executing.
  - Added null-safe navigation on `feature?.HtmlLogoutRedirect` in `LogoutAsync`.
  - Awaited asynchronous OAuth provider loading: `await userAuthProvider.LoadUserOAuthProviderAsync(session, oAuthToken).ConfigAwait()`.
- **`SaltedHash.cs`**:
  - Synchronized `ComputeHash` via `lock (HashProvider)` to prevent concurrent mutation of internal cryptographic algorithm state.
  - Implemented timing attack resistant equality check via `CryptUtils.FixedTimeEquals`.
  - Guarded against malformed salt lengths (`Salt.Length < SalthLength`) and caught both `FormatException` and `ArgumentException` in `VerifyHashString`.
- **`PasswordHasher.cs` & `AuthProviderExtensions.cs`**:
  - Handled invalid base64 gracefully by catching `FormatException` in `VerifyPassword` instead of throwing unhandled 500 errors.
  - Added fallback to `HostContext.TryResolve<IHashProvider>() ?? new SaltedHash()`.
- **`DigestAuthFunctions.cs` & `DigestAuthProvider.cs`**:
  - Disposed `MD5` instances with `using var md5 = MD5.Create()`.
  - Replaced equality comparison with `CryptUtils.FixedTimeEquals` to prevent digest timing attacks.
  - Added safe dictionary lookups for all required digest info keys.
  - Converted `AuthenticateService` resolving to `await using var authService`.
- **`OAuth2Provider.cs` & `GithubAuthProvider.cs`**:
  - Awaited response body extraction in `GithubAuthProvider` error logging (`await webException.GetResponseBodyAsync(token).ConfigAwait()`).
  - Added safe dictionary extraction for access tokens and guarded against null `WebException.Response`.
- **`ApiKeyAuthProvider.cs`**:
  - Fixed thread-static buffer reuse in `CreateApiKey` when requested `sizeBytes` did not match allocated array length.
- **`JwtAuthProviderReader.cs` & `JwtAuthProvider.cs`**:
  - Guarded `Cookies?.DeleteCookie(...)` in catch blocks.
  - Handled null `refreshToken` in `GetAccessTokenService` returning `HttpError.Unauthorized` rather than throwing `ArgumentNullException`.
- **`UserAuthRepositoryAsyncWrapper.cs`**:
  - Implemented `IDisposable` and `IAsyncDisposable` forwarding to the inner repository to prevent connection and resource leaks.
- **`RegisterService.cs` & `RegisterServiceBase.cs`**:
  - Normalized usernames and emails using `ToLowerInvariant()`.
  - Guarded against null `authRepo` before invoking repository methods.
- **`UserAuth.cs`**:
  - Removed duplicate `ClaimTypes.HomePhone` and `ClaimTypes.MobilePhone` registrations in `ConvertSessionToClaims`.
- **`SocialExtensions.cs`**:
  - Normalized Gravatar email with `Trim().ToLowerInvariant()`, disposed MD5 instance, and guarded null inputs.

---

## 7. Caching Subsystem Hardening & Reliability
- **`MemoryCacheClient.cs`**:
  - Guarded against `DivideByZeroException` in `IncrHit` by checking `CleaningInterval > 0`.
  - Added null / empty guards in `RemoveAll`, `GetAll<T>`, and `SetAll<T>` to safely handle null parameters without throwing `NullReferenceException`.
  - Fixed race condition and non-deterministic return value in `UpdateCounter` by returning `Convert.ToInt64(entry.Value)` directly from the `AddOrUpdate` result rather than reading from a mutated local variable across threads.
  - Hardened pattern conversion in `ConvertToRegex` by escaping all regex metacharacters (`.`, `$`, `^`, `{`, `[`, `(`, `|`, `)`, `+`, `\`) while translating `*` to `.*` and `?` to `.+`.
  - Added ReDoS protection with a 2-second regex match timeout and wrapped regex execution in `RemoveByRegex` and `GetKeysByRegex` with try-catch blocks.
- **`CacheClientAsyncWrapper.cs`**:
  - Implemented `IDisposable` forwarding `Cache.Dispose()` to prevent resource leaks when synchronous containers dispose async wrappers.
  - In `DisposeAsync`, awaited `Cache is IAsyncDisposable asyncDisposable` before falling back to `Cache?.Dispose()`.
  - Delegated `RemoveByPatternAsync` and `RemoveByRegexAsync` to `Cache as IRemoveByPatternAsync` when implemented.
  - Corrected `GetKeysByPatternAsync` to check if `Cache is ICacheClientAsync asyncCache` and stream keys with cancellation token support, falling back safely to null-checked sync keys.
  - Added null guards in `RemoveAllAsync` and `SetAllAsync`.
- **`CacheClientWithPrefix.cs` & `CacheClientWithPrefixAsync.cs`**:
  - Fixed `GetAll<T>` and `GetAllAsync<T>` to strip prefixes from returned dictionary keys using `RemovePrefix`, ensuring callers receive the exact keys they requested instead of tenant-prefixed keys.
  - Added `IDisposable` implementation in `CacheClientWithPrefixAsync` forwarding to `(cache as IDisposable)?.Dispose()`.
  - Replaced unsafe hard cast `((IRemoveByPatternAsync)cache)` in `RemoveByPatternAsync` and `RemoveByRegexAsync` with safe type checks.
  - Added null and empty guards to `GetAll`, `GetAllAsync`, `SetAll`, `SetAllAsync`, `RemoveAll`, and `RemoveAllAsync`.
- **`MultiCacheClient.cs`**:
  - Guarded constructors against null or empty client collections with `ArgumentNullException`.
  - Fixed copy-paste bug in `SetAsync(key, value, expiresIn, token)` which erroneously invoked `AddAsync` instead of `SetAsync`.
  - Added null checks to `GetAll`, `GetAllAsync`, `SetAll`, `SetAllAsync`, `RemoveAll`, and `RemoveAllAsync`.
  - Guarded against null enumeration in `GetKeysByPatternAsync` and added cancellation token support.
- **`CacheClientExtensions.cs` & `HttpCacheFeature.cs`**:
  - Added null-safe conditional access `HostContext.GetPlugin<HttpCacheFeature>()?.ShouldAddLastModifiedToOptimizedResults() == true` across cache evaluation methods to prevent `NullReferenceException` when `HttpCacheFeature` is not registered.
  - Guarded `GetAllContentCacheKeys` against null or empty input keys.
  - Added null guards for `HostContext.AppHost` and resolved cache client in `HttpCacheFeature.CacheAndWriteResponse`.

---

## 8. Configuration Subsystem Hardening & Data Integrity
- **`AppSettings.cs`**:
  - Guarded `ConfigurationManagerWrapper.GetAllKeys()` against null `ConfigurationManager.AppSettings.AllKeys` on .NET Framework.
  - Hardened `RuntimeAppSettings.Get<T>` with null guards on `name` and `Settings`, safe handling of null lambda return values without throwing for value types, and safe conversion via `ConvertTo<T>` with graceful fallback to `defaultValue`.
- **`AppSettingsBase.cs`**:
  - Null-guarded `settings?.Get(name)` and `settings?.GetAllKeys()` when `AppSettingsBase` is instantiated with null settings.
  - Guarded `GetNullableString` against null `name`.
  - Fixed setting corruption in `AppSettingsUtils.SaveAppSetting`: replaced loose `line.StartsWith(name)` with exact delimiter/token matching (`line.StartsWith(name + " ") || line.StartsWith(name + "\t") || line == name`), preventing prefix-sharing settings (e.g. `Host` vs `HostName`) from overwriting each other.
- **`DictionarySettings.cs`**:
  - Guarded constructor `DictionarySettings(IEnumerable<KeyValuePair<string, string>> map)` against null input.
  - Returned a defensive snapshot copy in `GetAll()` (`new Dictionary<string, string>(instance.Map)`) to protect internal configuration state against caller mutation and concurrent modification exceptions.
  - Guarded `DictionaryWrapper.Set<T>` against null keys.
- **`EnvironmentVariableSettings.cs`**:
  - Guarded `Environment.GetEnvironmentVariable(key)` against null keys to avoid `ArgumentNullException`.
  - Filtered null keys when mapping environment variables.
- **`MultiAppSettings.cs`**:
  - Guarded `MultiSettingsWrapper` constructor against null or empty `appSettings` array.
  - Filtered `appSettings.Where(x => x != null)` across `Get`, `GetAllKeys()`, `Set`, and `Get<T>` to safely handle arrays containing null providers.
- **`ConfigUtils.cs`**:
  - Handled null and empty values in `GetListFromAppSettingValue` returning an empty list instead of throwing `NullReferenceException`.
  - Preserved colons in setting values (such as URLs and times) in `GetDictionaryFromAppSettingValue` and `GetKeyValuePairsFromAppSettingValue` using `item.Split(new[] { KeyValueSeperator }, 2)`.
  - Threw descriptive `FormatException` for items missing colons, ensuring `GetDictionary` properly surfaces `ConfigurationErrorsException`.
  - Added thread-safe double-checked lock on `GetAppSettingsMap()` and verified `File.Exists(appConfigPath)` before reading.
- **`NetCoreAppSettings.cs`**:
  - Guarded constructor against null `configuration` instance.
  - Provided null-safe fallbacks for `GetDictionary` and `GetKeyValuePairs` to prevent null reference exceptions.
  - Guarded `Exists`, `GetString`, and `GetSection` against null keys.

---

## 9. Formats Subsystem Hardening & Deserialization Safety
- **`XmlSerializerFormat.cs`**:
  - Fixed critical reflection bug in `Deserialize`: replaced `new XmlSerializer(type.GetType())` with `new XmlSerializer(type)` (previously attempted to construct a serializer for `System.RuntimeType` instead of the target DTO).
  - Fixed invalid cast in `Deserialize`: replaced invalid `(Type)serializer.Deserialize(stream)` with `serializer.Deserialize(stream)`.
  - Added null guards for `type`, `stream`, and `response`.
- **`HtmlFormat.cs`**:
  - Hardened `EncodeForJavaScriptString` to escape `<` (`\u003c`), `>` (`\u003e`), and `&` (`\u0026`) to prevent script-tag termination breakouts (`</script>`) and XSS in rendered HTML templates.
  - Fixed token replacement bug in `ReplaceTokens` where `EncodeForJavaScriptString(req.TryResolve<IAuthMetadataProvider>()?.GetProfileUrl(null)) ?? JwtClaimTypes.DefaultProfileUrl` was returning empty string `""` on null because `EncodeForJavaScriptString(null)` returns `""`, preventing the default fallback from applying; changed to evaluate fallback prior to encoding.
  - Guarded `ReplaceTokens` against null `HostContext.AppHost` and uninitialized mock requests.
  - Guarded null references for `AppHost?.ViewEngines`, operation names, `AppHost?.GetPlugin<PredefinedRoutesFeature>()`, and `AppHost as ServiceStackHost` in `SerializeToStreamAsync`.
- **`CsvFormat.cs`**:
  - Guarded against duplicate `Content-Disposition` response headers if already present.
  - Provided fallback operation name `"data"` if `req.OperationName` is null or empty.
  - Guarded `SerializeToStream` against null `stream` and null `request`.
- **`JsonlFormat.cs`**:
  - Guarded `SerializeToStream` against null `stream` and null `request`.
- **`SoapFormat.cs`**:
  - Guarded `ExportSoapOperationTypes` against null `operationTypes` collection and null items.
  - Guarded `ExportSoapType` against null `type`.
  - Guarded `WriteSoapMessage` against null `req`, `outputStream`, `req.Dto`, and `req.GetSoapMessage()?.Headers`.

---

## 10. Funq IoC Container Hardening & Concurrency Safety
- **`Container.cs`**:
  - Hardened `Dispose` by safely checking `if (wr?.Target is IDisposable disposable)` instead of casting weak reference target, preventing `InvalidCastException` or `NullReferenceException` if instances are garbage collected before container disposal.
  - Synchronized `services.Values` iteration under `lock (services)` during disposal to eliminate concurrency modifications (`InvalidOperationException: Collection was modified`).
  - Added null safety check `childContainers.Pop()?.Dispose()`.
- **`Container.Adapter.cs`**:
  - Added immediate null check in `TryResolve(Type type)` to return null rather than throwing `ArgumentNullException` on dictionary key lookup.
  - Added null check in `Exists(Type type)` returning false, and safely handled reflection method lookup via `FirstOrDefault`.
  - Guarded `AutoWire(object instance)` and `AutoWire(Container container, object instance)` against null instances.
  - Added null/empty guards in `GetLazyResolver(params Type[] types)` to safely return null when unsupported argument counts or null types are supplied.
  - Guarded `RequiredResolve(Type type, Type ownerType)` against null types and null owner types.
- **`Container.ServiceCollection.cs`**:
  - Guarded `Add(ServiceDescriptor item)` and `CreateFactory(ServiceDescriptor item)` against null `item` by throwing `ArgumentNullException(nameof(item))`.
- **`ServiceEntry.Generic.cs`**:
  - Guarded `RequestContext.Instance?.Items` in `ReuseScope.Request` getter and setter to prevent `NullReferenceException` when resolving request-scoped services outside active HTTP contexts.
  - Guarded `RequestContext.Instance?.TrackDisposable` in `InitializeInstance`.
- **`ServiceKey.cs`**:
  - Guarded constructor against null `factoryType` in hash code calculation (`(factoryType?.GetHashCode() ?? 0)`).
  - Hardened static `Equals` against null references and validated case-sensitive ordinal names.
- **`ResolutionException.cs`**:
  - Guarded `missingServiceType?.FullName ?? "null"` in constructors to prevent NRE during exception creation.

---

## 11. Messaging Subsystem Modernization & Concurrency Safety
- **`TransientMessageServiceBase.cs`**:
  - Synchronized `RegisterHandler<T>` and `RegisteredTypes` using `lock (handlerMap)` to prevent race conditions during concurrent handler registrations and type discovery.
  - Added null safety checks in `GetStats()` and `GetStatsDescription()` to guard against `messageHandlers == null` when called on stopped or unstarted services, preventing `NullReferenceException`.
  - Guarded `DisposeMessageHandler` against `messageHandlers == null` and race conditions on service teardown.
- **`InMemoryTransientMessageService.cs`**:
  - Overrode `Dispose()` to safely detach `Factory.MqFactory.MessageReceived -= factory_MessageReceived`, preventing event handler memory leaks and duplicate invocations after disposal.
- **`InMemoryTransientMessageFactory.cs`**:
  - Implemented `SendAllOneWay(IEnumerable<object> requests)` to iterate and dispatch batch one-way requests instead of throwing `NotImplementedException`.
  - Added null guards in `Publish(string queueName, IMessage message)` for null/empty queue names and null messages.
  - Guarded `Publish<T>(IMessage<T> message)` against null messages before queue name resolution.
  - Added null parameter guards in `SendOneWay(object requestDto)` and `SendOneWay(string queueName, object requestDto)`.
- **`MessageHandler.cs`**:
  - Guarded `ProcessMessage(IMessageQueueClient mqClient, IMessage<T> message)` and `ProcessMessage(IMessageQueueClient mqClient, object mqResponse)` against null `mqClient` or null `message`.
  - Guarded `DefaultInExceptionHandler` against null `message`, null `ex`, and null `mqHandler.MqClient` to prevent cascaded exceptions during MQ failure handling.
- **`BackgroundMqService.cs`**:
  - Guarded `unknownQueues` publishing: initialized lazily with `(unknownQueues ??= new BlockingCollection<IMessage>()).Add(msg)` to prevent `NullReferenceException` when published before `Start()` or after `Stop()`.
  - Handled `OperationCanceledException` cleanly in `BackgroundMqWorker.Run` consuming loop to allow background worker threads to exit gracefully upon cancellation without unobserved task exceptions.

---

## 12. Validation Subsystem Modernization & Hardening
- **`ValidatorCache.cs`**:
  - Guarded `ValidatorCache.GetValidator(IRequest httpReq, Type type)` against null `type` and null reflected `MethodInfo`, safely returning null instead of throwing `ArgumentNullException` or `NullReferenceException`.
  - Guarded `ValidatorCache<T>.GetValidator(IRequest httpReq)` against null `httpReq`, returning null safely.
  - Used null-conditional operator on `httpReq?.PathInfo` in logger exception format string to prevent cascaded NRE during error logging.
- **`MultiRuleSetValidatorSelector.cs`**:
  - Initialized `rulesetsToExecute` with `[]` fallback in constructor when passed null.
  - Guarded `CanExecute` against null `rule` and null `rule.RuleSets`, returning `true` for unconstrained rules per FluentValidation semantics without crashing on null rule or ruleset references.
- **`ExecOnlyOnce.cs`**:
  - Added null check with `ArgumentNullException(nameof(forType))` in constructor overloads before invoking `forType.GetOperationName()`.
  - Added `isDisposed` flag in `Dispose()` to guarantee idempotency and prevent duplicate rollback operations or multi-disposal side effects.
- **`ValidationFilters.cs`**:
  - Guarded `RequestFilterAsync` against null `requestDto`, returning immediately.
  - Guarded `ResponseFilterAsync` against `req?.Dto == null` before resolving validator from container.
  - Guarded `validationResult.Errors` index access (`validationResult.Errors.Count > 0 ? validationResult.Errors[0].ErrorCode : null`) preventing index out of bounds exceptions.
- **`ValidationFeature.cs`**:
  - Guarded `GetRequestErrorBody(object request)` returning `""` when `request == null`.
  - Guarded `ValidateRequest` and `ValidateRequestAsync` against null `requestDto` or null `req`.
  - Initialized `vfe.Meta ??= new Dictionary<string, string>()` before assigning `error.Severity`.
  - Synchronized `Init(Assembly[] assemblies)` under `lock (RegisteredAssemblies)` to ensure thread safety during validator scanning, and guarded against null assemblies array or null elements.
  - Guarded `GetAllValidateRulesAsync` against null resolver / validation source.
  - Guarded `ApplyValidationRules` against `HostContext.AppHost == null`.
- **`Validators.cs`**:
  - Guarded `ScriptConditionValidator` against null `HostContext.AppHost` and null `context`.
  - In `Validators.Reset()`, reset `DelayConfiguringPropertyRules = []` to prevent rule accumulation across test runs.
  - Guarded `RegisterRequestRulesFor` against null `type`.
  - Guarded `AddTypeValidator` against null `to` or `attr`.
  - Guarded `ToPageResult` against null `context`.
  - Guarded `HasValidateRequestAttributes(Type type)` and `HasValidateAttributes(Type type)` against null `type` returning `false`.
- **`TypeValidators.cs`**:
  - Guarded `ScriptValidator.IsValidAsync` against null `HostContext.AppHost`.
  - In `TypeValidator.ResolveErrorMessage`, checked `appHost?.ScriptContext != null`, handled `dto == null` safely (`dto?.GetType().Name ?? "Request"`), and safely converted evaluated expression via `?.ToString()`.
  - Defaulted null roles/permissions collections to `[]` in `HasRolesValidator`, `HasAnyRoleValidator`, and `HasPermissionsValidator`.
- **`ValidateScripts.cs`**:
  - Guarded `RegularExpression` against null `regex` (`regex ?? ""`).
  - Guarded `HasRoles`, `HasAnyRole`, `HasPermission`, and `HasPermissions` against null string arguments.
- **`MemoryValidationSource.cs`**:
  - Guarded `GetValidationRules(Type type)` against null `type`, returning empty array.
  - Guarded `GetAllValidateRulesAsync(string typeName)` against null `typeName`, returning empty list.
  - Guarded `SaveValidationRules(List<ValidationRule> validateRules)` against null list and null entries within the list.
  - Guarded `GetValidateRulesByIdsAsync` and `DeleteValidationRulesAsync` against null or empty `ids`.
- **`ValidationResultExtensions.cs`**:
  - Guarded `CustomStateAsDictionary` against null `error` and null `FormattedMessagePlaceholderValues`.
  - Guarded `ToErrorResult` against null `result` and null `result.Errors`, and skipped null error elements.
  - Guarded `ToException` against null `result`.
- **`ValidatorUtils.cs`**:
  - Guarded `Init` against null `validator` or `rule`, returning validator safely.
  - Guarded `RemoveValidatorSuffix` against null `name`.

## 13. ServiceStack.Host & Handlers Subsystem Hardening

- **`ContainerResolveCache.cs`**:
  - Guarded `GenerateServiceFactory(Type)` against `type == null` throwing explicit `ArgumentNullException`.
  - Guarded `PopulateInstance(IResolver, object)` against `instance == null` (returning null) and `resolver == null` (returning instance).
  - Guarded `CreateInstance(IResolver, Type, bool)` against null `type` or null `resolver` returning null.
- **`InMemoryRollingRequestLogger.cs`**:
  - Guarded null response in `CreateEntry`: `request.Response?.StatusCode ?? 0`, `request.Response?.StatusDescription`, and `isClosed = request.Response?.IsClosed == true`.
  - Safely resolved `request.GetSessionId()` only when `HostContext.AppHost != null`, avoiding uninitialized AppHost exceptions.
  - Guarded `apiKeyNameFn(apiKey)?.ToString()`.
  - Guarded `request.Response?.Items.TryGetValue(...)` and `(request.Response?.StatusCode ?? 200) < 400`.
  - Fixed potential null reference in `ExcludeResponseTypes` evaluation.
  - Guarded `SerializableItems` against null items dictionary.
  - Clamped negative values in `GetLatestLogs(int? take)` via `Math.Max(0, take.Value)`.
- **`RestPath.cs`**:
  - Fixed route literal hashing bug where `sbHashKey.Append(i + PathSeparator + literalsToMatch)` appended `"System.String[]"` instead of the matching literal `literalsToMatch[i]`.
  - Guarded `GetPathPartsForMatching(string pathInfo)` returning `TypeConstants.EmptyStringArray` on null or empty path info.
  - Guarded `IsVariable(string name)` against null `name` or null `VariablesNames`.
  - Guarded `GetHashCode()` against null `UniqueMatchHashKey`.
- **`ContentTypes.cs`**:
  - Added standalone fallback in `ContentTypeSerializers` and `ContentTypeDeserializers` when `HostContext.AppHost == null` using `JsonSerializer.SerializeToStream` / `DeserializeFromStream`.
  - Guarded `SerializeToBytes` and `SerializeToString` against `req == null`.
  - Replaced blocking `.Wait()` and `.Result` with `.GetAwaiter().GetResult()`.
  - Guarded `httpReq?.Response` before setting DTO or allowing sync I/O.
  - Replaced throwing `HostContext.Config.BufferSyncSerializers` with safe `HostContext.AppHost?.Config?.BufferSyncSerializers == true`.
- **`ServiceController.cs`**:
  - Guarded `GetResponseType` against null `requestType` or null `mi`.
  - Guarded `IsRequestType` and `IsServiceType` against null `type` returning `false`.
  - Guarded `IsServiceAction(ActionMethod mi)` and `IsServiceAction(string actionName)` against null inputs.
  - Guarded `GetServiceRequestTypes` and `GetAutoBatchedRequestTypes` against null collections.
- **`ServiceRunner.cs`**:
  - Validated `actionContext ?? throw new ArgumentNullException(nameof(actionContext))`.
  - Guarded null `requestDto` in strict mode check.
  - Guarded `HostContext.AppHost?.OnLogRequest(...)`.
  - Replaced `.Result` with `.GetAwaiter().GetResult()` in synchronous execution wrappers.
- **`RestHandler.cs`**:
  - In `GetSanitizedPathInfo`, guarded against null `pathInfo` and checked `HostContext.AppHost?.Config?.AllowRouteContentTypeExtensions == true`.
  - In `ProcessRequestAsync`, guarded `HostContext.AppHost?.OnAfterAwait(httpReq)`.
- **`GenericHandler.cs`**:
  - Guarded `HostContext.AppHost?.OnAfterAwait(...)`, `HostContext.AppHost?.Config?.WriteErrorsToResponse`, and `HostContext.AppHost?.ApplyResponseConvertersAsync(...)`.
- **`HttpAsyncTaskHandler.cs`**:
  - Replaced `.Wait()` with `.GetAwaiter().GetResult()` in `ProcessRequest`.
  - Preserved exception stack trace on rethrow in `HandleException`.
- **`CustomActionHandler.cs`**:
  - Replaced incorrect `NullReferenceException` with standard `ArgumentNullException(nameof(action))` in both synchronous and asynchronous constructors.
  - Guarded `httpRes?.ApplyGlobalResponseHeaders()` and `httpRes?.EndHttpHandlerRequest()`.
- **`CustomResponseHandler.cs`**:
  - Replaced generic `Exception` with `InvalidOperationException` when action is missing.
  - Guarded `httpRes?.WriteToResponse(httpReq, response)` against null response.
- **`NotFoundHttpHandler.cs`**:
  - Guarded `HostContext.AppHost?.OnLogError(...)` and null `request` / `response` references.
- **`ForbiddenHttpHandler.cs`**:
  - Replaced throwing `HostContext.Config.DebugMode` with safe `HostContext.DebugMode`.
  - Guarded null `request` and null `response` references.
- **`StaticFileHandler.cs`**:
  - Guarded `HostContext.AppHost?.VirtualFiles?.GetFile(virtualPath)` in path constructor.
  - Guarded `appHost?.Config` access and `request?.Headers?[HttpHeaders.Range]` against uninitialized AppHost or missing request/headers.
- **`SoapHandler.cs`**:
  - Converted blocking `.Result` and `.Wait()` inside async `ExecuteMessage` method to proper `await ...ConfigAwait()`.
  - Replaced top-level `.Wait()` / `.Result` with `.GetAwaiter().GetResult()`.
  - Added `ArgumentNullException(nameof(requestType))` check to `EmptyResponse`.
- **`BasicRequest.cs`**:
  - Guarded `GetService(Type)` returning `null` when `serviceType == null`.
  - Guarded `GetHeader(string)` returning `null` on null header name or null headers collection.
  - Guarded `GetRawBody()` against null `Message`.
  - Guarded `Authorization` property getter and setter against null `Headers`.
  - Guarded `PopulateWith(IRequest)` against null request.
  - Safely called `HostContext.VirtualFileSources?.GetFile` and `GetDirectory`.
- **`BasicResponse.cs`**:
  - Fixed uninitialized backing stream bug in `Write(string)` by ensuring `OutputStream.Write(...)` is called instead of `ms.Write(...)`.
  - Guarded `Write(string)` against null text.
  - Guarded `AddHeader`, `RemoveHeader`, and `GetHeader` against null header names.
- **`Cookies.cs` & `CookiesExtensions`**:
  - In `ToCookieOptions()`, `ToHttpCookie()`, and `AsHeaderValue()`, used `HostContext.AppHost?.Config ?? new HostConfig()` to allow standalone/offline cookie formatting without requiring an initialized AppHost.
  - Guarded against null cookie arguments throwing `ArgumentNullException` or returning null.
  - Guarded `httpRes?.Request?.IsSecureConnection` in `UseSecureCookie`.
- **`HttpFile.cs`**:
  - Guarded `HttpFile(IHttpFile file)` with `ArgumentNullException(nameof(file))`.
  - Guarded `HttpFileContent(HttpContent content)` with `ArgumentNullException(nameof(content))`.
- **`HttpRequestAuthentication.cs`**:
  - Added standalone fallback in `GetAuthorization`, `GetBearerToken`, and `GetJwtToken` when `HostContext.AppHost == null`.

## 14. ServiceStack.Metadata Subsystem Modernization & Hardening

- **`ServiceMetadata.cs`**:
  - **Fixed Critical Filter Cache Bug in `GetDtoTypes(Func<Type,bool> include)`**: Separated filtered DTO queries from `allDtos` global caching so filtered queries do not corrupt the global `allDtos` cache, and calling `GetAllDtos()` beforehand does not bypass the filter on subsequent queries.
  - Made `restPaths` constructor parameter optional (`List<RestPath>? restPaths = null`) and guarded `AfterInit()` against null `restPaths`.
  - Added null guards in `Add(Type serviceType, Type requestType, Type? responseType)` throwing `ArgumentNullException` for invalid service or request types.
  - Guarded `GetOperationsByTag(string tag)` and `GetOperationsByTags(string[] tags)` against null arguments, returning empty lists safely.
  - Guarded `GetOperationType(string operationTypeName)` against null input and safely invoked `.MakeArrayType()`.
  - Guarded `IsAuthorized` and `IsAuthorizedAsync` overloads against null `operation`, null `session`, and uninitialized `HostContext.AppHost`.
  - Guarded `IsVisible` and `CanAccess` overloads against null `operationName`, null `requestType`, and null `httpReq`.
  - Initialized `duplicateTypeNames = []` and non-nullable `OperationDto.Name` and `OperationDto.ServiceName` properties to resolve compiler warnings CS8618.
  - Guarded `CreateRequestFromUrl` against null `relativeOrAbsoluteUrl` throwing `ArgumentNullException`.
  - Guarded `GetMetadataTypesForOperation` against null `HostContext.AppHost` and null `MetadataFeature`, falling back to `new NativeTypesFeature().MetadataTypesConfig`.
  - Guarded `ToOperationDto` against null `operation.ServiceType`, null `Routes`, and null `Tags`.
- **`BaseMetadataHandler.cs`**:
  - Replaced generic `throw new Exception("Could not find operation: ...")` with `throw HttpError.NotFound(...)`.
  - Replaced throwing `HostContext.Config` calls with safe `HostContext.AppHost?.Config?.HandlerFactoryPath` and `HostContext.AppHost?.Config?.AllowRouteContentTypeExtensions == true`.
  - Guarded `ConvertToHtml(string? text)` against null input, returning `""`.
  - Added array bounds and null checks in `AppendType` when iterating `EnumNames`, `EnumMemberValues`, `EnumValues`, and `EnumDescriptions`.
  - Guarded `AssertAccess` against null `appHost` and null `appHost.MetadataPagesConfig`.
  - Safely accessed `HostContext.AppHost?.Config?.ServiceEndpointsMetadataConfig` in `RenderOperationAsync`.
- **`MetadataPagesConfig.cs`**:
  - Guarded constructor against null `contentTypeFormats` and null `metadataConfig`, using `StringComparer.OrdinalIgnoreCase`.
  - Guarded `GetMetadataConfig` against null `format` and null `ignoredFormats`.
  - Guarded `IsVisible` and `CanAccess` overloads against null `metadata`.
  - Guarded `AlwaysHideInMetadata` against null `operationName` and null `metadata.OperationNamesMap`.
- **`IndexOperationsControl.cs`**:
  - In `RenderRow`, guarded against null `operationName`, null `MetadataConfig`, null `Request`, null `GetOperation`, and null operation instance.
  - Safely checked `HostContext.AppHost?.GetPlugin<MetadataFeature>()` and `HostContext.AppHost?.HasUi() == true`.
  - Guarded `CreateIcons` against null roles and permissions collections on `Operation`.
  - Safely accessed `HostContext.AppHost?.StartUpErrors` and `MetadataConfig` in `RenderAsync`.
  - Guarded `ToAbsoluteUrls` against null `linksMap` and null `Request`.
- **`OperationControl.cs`**:
  - Safely navigated `HostContext.AppHost?.Config?.HandlerFactoryPath` and guarded null `MetadataConfig` in `RequestUri`.
  - Guarded `RenderAsync` against null `HttpRequest`, null `MetadataConfig`, and null `ContentFormat`.
  - Hardened `GetHttpRequestTemplate` with case-insensitive comparisons and null checks on route verbs.
- **`MetadataFeature.cs`**:
  - In `GetHandler`, guarded against null `req` and null or empty `req.PathInfo`.
  - In `GetHandlerForPathParts`, guarded against null elements in `pathParts`, null `HostContext.AppHost`, and null `ContentTypeFormats`.
  - In `ToAppMetadata`, guarded against null `req`, null `response.Api`, null `response.Api.Operations`, and null `response.Plugins`.
  - In `LocalizeMetadata`, guarded against null `response.App`, null `response.Api.Types`, and null `response.Api.Operations`.
  - In `RemovePluginLink` and `RemoveDebugLink`, guarded against null `metadata` and null `href`.

## 15. ServiceStack.NativeTypes Hardening & Modernization
- **`NativeTypesFeature.cs`**:
  - Guarded `ExportAttribute` against null `attributeType` and null `converter` throwing `ArgumentNullException`.
  - In `GetGenerator()`, safely navigated `HostContext.AppHost?.TryResolve<INativeTypesMetadata>()` with fallback `new NativeTypesMetadata(HostContext.AppHost?.Metadata ?? new ServiceMetadata([]), MetadataTypesConfig)` to eliminate `ConfigurationErrorsException` when running outside an active AppHost.
  - In `Register(IAppHost appHost)`, guarded null `appHost` safely and resolved `appHost.TryResolve<INativeTypesMetadata>()`.
- **`NativeTypesService.cs`**:
  - In `GetBaseUrl(baseUrl)`, safely resolved `HostContext.GetPlugin<NativeTypesFeature>()?.MetadataTypesConfig?.BaseUrl ?? Request?.GetBaseUrl() ?? "/"` instead of throwing `ConfigurationErrorsException`.
  - In `ResolveMetadataTypes`, safely pattern-matched `(nativeTypesMetadata as NativeTypesMetadata)?.GetGenerator(typesConfig) ?? ...` to eliminate `InvalidCastException` when custom `INativeTypesMetadata` implementations are injected.
  - In `ExportMissingSystemTypes`, replaced raw `.Add()` with `.AddIfNotExists(typeof(KeyValuePair<,>))` to prevent duplicate key/type registrations.
- **`NativeTypesMetadata.cs`**:
  - In `GetConfig(NativeTypesBase req)`, initialized defaults and safely fallback created `req ??= new NativeTypesBase()` to avoid NRE.
  - In operation tag filtering, safely checked `(meta?.IsVisible(req, op) != true) && exportTags.All(...)`.
  - In `RemoveIgnoredTypes`, guarded against null `metadata` (returning empty list) and null `config`, and safely handled null `operation.Request` via `x.Request == null || x.Request.IgnoreType(config, includeList)`.
- **`StringBuilderWrapper.cs`**:
  - Guarded constructor against null `StringBuilder` by falling back to `new StringBuilder()`.
  - Clamped indent levels to `Math.Max(0, indent)` in constructor and `UnIndent()` to prevent negative indent padding errors.
  - Rewrote `Chop(char c)` to eliminate `IndexOutOfRangeException` when the character is absent or when called on empty buffers.
- **`GenerateDtos.cs`**:
  - Guarded `ParseReference` against null `source` throwing `InvalidDataException`.
  - Safely dispatched `ExecuteService` via pattern matching `appHost is ServiceStackHost ssh ? ssh.ExecuteService(...) : HostContext.ServiceController.Execute(...)`.
- **`ILangGenerator.cs`**:
  - In `GenerateSourceCode` extension methods, safely navigated `HostContext.AppHost?.GetPlugin<NativeTypesFeature>()` and fallback to `new NativeTypesMetadata(...)` when AppHost is uninitialized.
  - Added case-insensitive matching `(lang?.ToLowerInvariant())` for all supported languages.
- **All 16 Language Generators (`CSharp`, `TypeScript`, `Mjs`, `CommonJs`, `Dart`, `FSharp`, `Go`, `Java`, `Kotlin`, `Php`, `Python`, `Ruby`, `Rust`, `Swift`, `VbNet`, `Zig`)**:
  - Safely resolved formatter with `request?.TryResolve<INativeTypesFormatter>()` instead of throwing `NullReferenceException` when `request == null`.
  - Guarded `defaultValue` and `includeOptions` against null `request` (`request?.QueryString[...]`).
  - In `DartGenerator.cs`, safely handled null/relative `BaseUrl` in `dtosName` calculation without throwing `ArgumentNullException`.
  - In `KotlinGenerator.cs`, added `.Safe()` guards when iterating `type.Implements`, `type.Properties`, and safely checked `feature?.ShouldInitializeCollection(type)`.
  - Eliminated unused variable compiler warnings (CS0219) in `PhpGenerator.cs` (`var i = 0;`) and `MjsGenerator.cs` (`var wasAdded = false;`).
- **`ServiceMetadata.cs`**:
  - Guarded `ForceInclude` extension methods against null `HostContext.AppHost` to prevent `ConfigurationErrorsException` during standalone code generation.

## 16. ServiceStack.ServerEvents Hardening & Modernization
- **`ServerEventsFeature.cs`**:
  - In `Register(IAppHost appHost)`, guarded against null `appHost` to prevent `NullReferenceException`.
  - In raw HTTP handlers, added safe null checks on `httpReq.PathInfo` and endpoint path strings.
  - Replaced eager `host.Resolve<IServerEvents>()` in AppHost dispose callback with safe `host.TryResolve<IServerEvents>()?.Stop()`.
  - Switched endpoint mapping from direct unvalidated cast `(appHost as IAppHostNetCore).MapEndpoints(...)` to safe pattern matching `if (appHost is IAppHostNetCore netCoreHost) netCoreHost.MapEndpoints(...)`.
  - Promoted `CanAccessSubscription` to public for external extensibility and testability, adding null checks on `sub` and `req`.
- **`ServerEventsHandler`**:
  - Guarded `ProcessRequestAsync` against null `req`, null `res`, and uninitialized `IServerEvents`.
  - Instantiated `TaskCompletionSource<bool>` with `TaskCreationOptions.RunContinuationsAsynchronously` and completed it via `tcs.TrySetResult(true)` to eliminate `InvalidOperationException` on concurrent completion.
  - In `AddSessionParamsIfAny`, safely guarded `HostContext.AppHost?.Config?.AllowSessionIdsInHttpParams == true` and `req?.QueryString`.
- **`ServerEventsHeartbeatHandler` & SSE Services**:
  - In `ServerEventsHeartbeatHandler`, guarded against null `req`, null `res`, null `feature`, and sanitized subscription ID inputs.
  - In `ServerEventsSubscribersService`, `ServerEventsUnRegisterService`, and `UpdateEventSubscriberService`, added null request DTO checks and dependency injection fallbacks via `TryResolve<IServerEvents>()`.
- **`MemoryServerEvents`**:
  - Guarded `NotifyChannelsAsync`, `FlushNopToChannelsAsync`, and `GetSubscriptionsDetails` against null channel arrays and null channel elements.
  - Guarded `Pulse` and `PulseAsync` against null or empty subscription IDs.
  - In `SubscribeToChannels` and `UnsubscribeFromChannels`, guarded against null/empty channel items and null subscription channel lists.
  - In `RegisterAsync` and `UnRegister`/`UnRegisterAsync`, added null subscription guards and null subscription ID handling.
  - In `DoAsyncTasks`, wrapped all callback invocations (`OnUpdateAsync`, `NotifyUpdateAsync`, `OnUnsubscribeAsync`, `NotifyLeaveAsync`, `OnRemoveSubscriptionAsync`) in try/catch blocks with error counter tracking to prevent callback exceptions from halting subsequent queue processing.
- **`EventSubscription`**:
  - Hardened `CanWrite()` to verify `!Disposing && response != null && !response.IsClosed`.
  - In `IsClosed`, safely navigated `this.response?.IsClosed == true`.
  - In `SerializeDictionary`, guarded against null dictionary, null keys, and null values.
- **`ServerEventExtensions`**:
  - Replaced unsafe cast `((EventSubscription)sub).Request.RequestAttributes.HasFlag(...)` in `IsGrpc()` with safe pattern matching `sub is EventSubscription { Request: { } req } && req.RequestAttributes.HasFlag(...)` to prevent `InvalidCastException` when custom `IEventSubscription` mocks/implementations are used.
  - Hardened `HasChannel` and `HasAnyChannel` to handle null subscriptions and null channel collections safely.
  - Guarded all `Notify*` extension method overloads against null `server` and null `message` inputs.

---

## 17. ServiceStack.Formats & Content Types Modernization & Hardening
- **`XmlSerializerFormat.cs`**:
  - Replaced un-cached dynamic `new XmlSerializer(type)` instantiations with thread-safe `ConcurrentDictionary<Type, XmlSerializer> SerializerCache` and `GetSerializer(type)` resolver to prevent heavy reflection overhead and dynamic assembly generation memory leaks under high loads.
  - Guarded `Serialize` and `Deserialize` against null streams and null types.
  - Guarded `Register(IAppHost appHost)` against null `appHost`.
- **`CsvFormat.cs`**:
  - Guarded `Register(IAppHost appHost)` against null `appHost`.
  - Added null guards in `SerializeToStream` for null DTO and null/closed streams.
  - Added null-safe request inspection in global response filter `req != null && req.ResponseContentType.MatchesContentType(MimeTypes.Csv)`.
- **`HtmlFormat.cs`**:
  - Guarded `Register(IAppHost appHost)` against null `appHost`.
  - In `SerializeToStreamAsync`, added null checks and safe fallback `AppHost ?? HostContext.AppHost` when resolving virtual file sources.
  - Hardened `MiniProfiler.Profiler.RenderIncludes()?.ToString() ?? ""` against null profiler instances.
  - Safely formatted URLs in `GetAbsoluteUrl` and guarded against null request inputs.
- **`JsonlFormat.cs`**:
  - Guarded `Register(IAppHost appHost)` against null `appHost`.
  - Guarded `SerializeToStream` against null `request`, null DTO, and null/closed stream.
- **`SoapFormat.cs`**:
  - Guarded `Register(IAppHost appHost)` against null `appHost`.
  - Safely verified `appHost.ContentTypes as ContentTypes` before configuring SOAP serializers.
  - In `WriteSoapMessage`, added null guards for null `message` and null `outputStream`.
- **`ContentTypes.cs`**:
  - In `GetFormatContentType(string format)`, added null/empty check before dictionary lookup to avoid key exceptions.
  - In `Register` and `RegisterAsync`, validated non-empty `contentType` and guarded `format != null` before assigning to `ContentTypeFormats[format]`.
  - In `Remove(string contentType)`, safely handled null or un-normalizable content types.
  - In `SetContentTypeSerializer` and `SetContentTypeDeserializer`, guarded against null/empty `contentType`.
  - In `SerializeUnknownContentType`, guarded `req?.Response != null` and `stream == null || stream == Stream.Null`, formatting safe fallback error messages when `req.ResponseContentType` is null.
  - In `SerializeToBytes`, `SerializeToString`, and `SerializeToStreamAsync`, added null request, null response, and null/closed stream guards.
  - In `GetStreamSerializer`, `GetStreamSerializerAsync`, `GetStreamDeserializer`, and `GetStreamDeserializerAsync`, guarded against null normalized content type keys before accessing dictionary lookup.
  - In `DeserializeFromString` and `DeserializeFromStream`, guarded against null types, empty inputs, null content types, and null/closed streams.
- **`ContentFormat.cs`**:
  - In `GetRequestAttribute(string httpMethod)`, guarded against null `httpMethod` returning `RequestAttributes.None`, and normalized using culture-invariant `httpMethod.ToUpperInvariant()`.
- **`JsonDataContractSerializer.cs` & `Deserialize.cs`**:
  - In `SerializeToStream<T>` and `BclSerializeToStream<T>`, guarded against null objects and null/closed streams, adding string serialization fallback when `TextSerializer` does not implement `IStringStreamSerializer`.
  - In `BclDeserializeFromString`, `DeserializeFromStream<T>`, and `DeserializeFromStream(Type, Stream)`, added null guards for null JSON string, null streams, and null types, falling back to reading stream to string when `TextSerializer` does not implement `IStringStreamSerializer`.

## 18. RequestLogs & Logging Subsystem Modernization
- **`InMemoryRollingRequestLogger.cs`**:
  - Guarded against null `request` in `Log` with immediate exit.
  - Safe navigation for `request?.Dto` in `ShouldSkip`.
  - Hardened rolling queue capacity maintenance under high concurrency using a while-loop `while (capacity > 0 && logEntries.Count > capacity) logEntries.TryDequeue(out _);`.
  - Defensive null-coalescing on `request.Headers?.ToDictionary() ?? new()`, `request.Items`, `request.FormData?.ToDictionary()`, and `request.Response?.Items`.
  - Null-safe predicates in `ExcludeRequestType`, `HideRequestBodyForRequestDtoTypes`, and `ExcludeResponseTypes`.
  - Guarded null keys in `entry.ExceptionData.Keys` during `IgnoreFilter` filtering.
  - Defensive handling for null input in `ToSerializableErrorResponse`.
  - Clamped negative values in `GetLatestLogs` with `Math.Max(0, take.Value)`.
- **`RequestLogsFeature.cs`**:
  - Added null guard in `DefaultIgnoreFilter(object o)` returning `false` on null input.
  - Added null guards on `appHost` in `Register` and `BeforePluginsLoaded`.
  - Fallback logger resolution `appHost.TryResolve<IRequestLogger>() ?? RequestLogger ?? new InMemoryRollingRequestLogger(Capacity)`.
  - Safe null-coalesced array conversion for `ExcludeRequestDtoTypes`, `HideRequestBodyForRequestDtoTypes`, and `ExcludeResponseTypes`.
- **`CsvRequestLogger.cs`**:
  - Implemented `IDisposable` with `timer?.Dispose(); Flush();` to ensure flush and clean resource shutdown.
  - Added public `Flush()` method for synchronous buffer flush.
  - Guarded `HostContext.Config?.WebHostPhysicalPath ?? "."`.
  - Fixed logging bug in `OnFlush` to log the target file name rather than the logger instance.
  - Guarded CSV row slicing in `WriteLogs` to handle single-line or header-only output safely.
  - Clamped pagination in `GetLatestLogs` to prevent negative count exceptions.
- **`RedisErrorLoggerFeature.cs`**:
  - Null guard in `Register(IAppHost appHost)` on null host.
  - Safe navigation `httpReq?.OperationName ?? request?.GetType().Name ?? "Unknown"` in `HandleServiceException`.
  - Defensive null handling for exception and fallback operation name in `LogErrorInRedis`.
- **`RequestLogsService.cs`**:
  - In `AssertRequiredRole`, skip role assertion if `AccessRole` is null/empty or equals `RoleNames.AllowAnon` to prevent false 401 Unauthorized responses.
  - In `Any(RequestLogs)`, default null request to empty instance, clamp `Take` and `Skip` to non-negative values, and resolve fallback logger.
  - In `Any(GetAnalyticsReports)`, guarded against null report objects, null IP lists, null user collections, and null user items.

---

## 19. Commands and Background Jobs Subsystem Modernization & Hardening
- **`CommandsFeature.cs`**:
  - Replaced unsafe null-forgiving log operations (`Log!.LogDebug`, `Log!.LogError`) with safe null-conditional `Log?.` invocations.
  - Hardened `Register` and `BeforePluginsLoaded` to guard against null `appHost`.
  - Resolved `Log` via safe cast `(appHost as IAppHostNetCore)?.App?.ApplicationServices?.GetService<ILogger<CommandsFeature>>() ?? appHost.TryResolve<ILogger<CommandsFeature>>()`, preventing `InvalidCastException` when running outside ASP.NET Core hosts (e.g. `BasicAppHost` or self-hosts).
  - Enforced strict lower bounds on queue capacities (`ResultsCapacity > 0`, `FailuresCapacity > 0`, `TimingsCapacity > 0`).
  - Added null check on `result` and default fallback `result.Name ??= "Unknown"` in `AddCommandResult`.
  - In `CommandsService.AssertRequiredRole`: bypassed role assertion if `feature.AccessRole == RoleNames.AllowAnon` or empty, preventing false 401 Unauthorized responses for anonymous-enabled configurations.
  - In `CommandsService.Any(ViewCommands)`: defaulted `request ??= new ViewCommands()` and clamped `Skip` and `Take` with `Math.Max(0, ...)`.
  - In `CommandsService.Any(ExecuteCommand)`: validated non-empty `request.Command` and safely handled commands with null request types (`commandInfo.Request?.Type ?? typeof(NoArgs)`).
- **`JobLogger.cs`**:
  - In `Log<TState>`: guarded custom formatter invocations `formatter != null ? formatter(state, exception) : state?.ToString()`, supporting both null and non-null formatters safely.
  - Guarded `jobs?.UpdateJobStatus(...)` in `Log` against null background job manager.
  - Guarded `UpdateProgress`, `UpdateStatus`, and `UpdateLog` against null `jobs` manager.
- **`JobUtils.cs`**:
  - Guarded `ToBackgroundJob(this object arg)` with `ArgumentNullException.ThrowIfNull(arg)`.
  - Guarded `PopulateJob` with `if (from == null || to == null) return to;`.
  - Guarded `ToJobSummary` with `if (from == null) return null;`.
  - Replaced unvalidated cast in `GetCancellationToken` with type pattern matching `oToken is CancellationToken token ? token : default`.
  - Guarded `GetBackgroundJob` and `CreateJobLogger` against null `IRequest` inputs.
- **`ApiToolRegistry.cs`**:
  - In `CanAccess`: safely handled null `tool` returning `false`, and null `req` returning `true` only when no auth attributes are present.
  - Guarded metadata inspection `HostContext.Metadata?.Operations` against null host metadata.
  - In `Search`: clamped `take = Math.Max(0, take)`.
  - In `ExecuteAsync`: added explicit argument null checks for `tool` and `req`.
- **`BackgroundsJobFeature.cs`**:
  - Guarded `Register(IAppHost appHost)` against null `appHost` and used safe cast `AppHost ??= appHost as IAppHostNetCore;`.
- **`AdminJobServices.cs` & `DbJobsAdminServices.cs`**:
  - In `AssertRequiredRole`: bypassed role assertion if `feature.AccessRole == RoleNames.AllowAnon` or empty.
  - In `AdminJobDashboard`: defaulted `request ??= new AdminJobDashboard()` and used safe `DateTime.TryParse` / `int.TryParse` in `ToDate`.
  - In `AdminGetJobProgress`: clamped log slicing `job.Logs[Math.Clamp(request.LogStart.Value, 0, job.Logs.Length)..]` to prevent `ArgumentOutOfRangeException`.
  - In `ToSummaries(List<JobStat>)` and `ToSummaries(List<HourStat>)`: guarded against null stats collections, safely returning empty list `[]`.
- **`SqliteRequestLogger.cs`**:
  - In `AssertRequiredRole`: bypassed role assertion if `feature.AccessRole == RoleNames.AllowAnon` or empty.
  - In `Register`: guarded against null `appHost` and used safe cast `AppHost ??= appHost as IAppHostNetCore;`.
  - In `QueryLogs`: defaulted `request ??= new RequestLogs()`, clamped `take = Math.Max(0, request.Take ?? MaxLimit)` and `skip = Math.Max(0, request.Skip)`.

---

## 20. AutoQueryData Subsystem Modernization & Hardening
- **`AutoQueryDataConditions.cs`**:
  - In `InCollectionCondition`, `CaseInsensitiveInCollectionCondition`, and `InBetweenCondition`: excluded `string` from `IEnumerable` collection iteration (`if (b is not IEnumerable bValues || b is string)`), preventing strings from being erroneously treated as `IEnumerable<char>`.
  - In `InBetweenCondition`: safely returned `false` on non-enumerable inputs or lists without exactly 2 items instead of throwing unhandled `ArgumentException`.
  - In `CompareTypeUtils.CoerceDouble`: corrected invalid cast `(long?)` to `(double?)` on boxed `double` return from `Convert.ChangeType`, preventing `InvalidCastException`.
  - In `CompareTypeUtils.CoerceLong`, `CoerceDouble`, and `CoerceString`: added null guards before invoking `o.GetType()` to prevent `NullReferenceException`.
- **`PocoDataSource.cs`**:
  - Fixed inversion bug in `TryDeleteByIds<TId>` where non-existent IDs incremented `itemsRemoved` while actually removed items were uncounted.
  - Fixed value comparison in `Save`: replaced reference equality `itemId == defaultValue` with `Equals(itemId, defaultValue)`, ensuring boxed default value types (e.g. `0`) are assigned new IDs.
  - Guarded `FindIndexById(object id)` against null `id` inputs.
  - Added argument null checks in constructor and methods (`Add`, `TryUpdate`, `TryDelete`, `Save`).
- **`AutoQueryDataFeature.cs`**:
  - In `Register`: guarded against null `appHost` and replaced unsafe cast `((ServiceStackHost)appHost)` with pattern matching `if (appHost is ServiceStackHost ssHost)` to prevent `InvalidCastException` on non-`ServiceStackHost` hosts.
  - In `GenerateMissingServices`: guarded `AutoQueryServiceBaseType` and used `.FirstOrDefault(...)` when selecting generic service methods.
  - In `DataQuery<T>`: navigated `context?.Dto` and `context?.DynamicParams`, clamped `Limit` and `Take` to non-negative values, and guarded `OrderByPrimaryKey` when no primary key property exists on `T`.
  - In `AutoQueryDataServiceBase.Exec`: guarded against null `Request` when executed in background tasks or unit tests.
  - In `AutoQueryData.Execute`: aligned generic query cache key lookup `genericAutoQueryCache.TryGetValue(requestDtoType, ...)` with cache insertion `[requestDtoType] = instance`.
  - In `AutoQueryData.Filter`: guarded against null `dto`.
  - In `MemoryDataSource`: used null-safe `req?.GetRequestParams()`.
  - In `QueryDataSource`: clamped `ApplyLimits` and guarded `Count` against null data source.
  - In `AutoQueryDataExtensions`: guarded against null `feature`, null `request`, null `cache`, and null `sourceFn`.
- **`AutoQueryDataServiceSource.cs`**:
  - In `ServiceSource<T>`: guarded against null `requestDto` and null `cache`.
  - In `GetResults<T>` and `GetResults`: guarded against null `response` and checked `pi.GetGetMethod() != null` before invoking reflected getter.
- **`AutoCrudOperation.cs`**:
  - Added null guards in `ToHttpMethod`, `GetAutoQueryGenericDefTypes`, `GetAutoQueryDtoType`, `GetAutoCrudDtoType`, `GetModelType`, `GetViewModelType`, `HasNamedConnection`, and `IsRequestDto`.
- **`AutoQueryMetadataFeature.cs`**:
  - In `Register`: guarded against null `appHost` and null `AutoQueryViewerConfig`.
  - In `AutoQueryMetadataService.AnyAsync`: guarded against null `feature`, null `config`, null `Request`, and null `userSession`.
  - Replaced `inheritArgs.First()` / `inheritArgs.Last()` with `inheritArgs.FirstOrDefault()` / `inheritArgs.LastOrDefault()`.

