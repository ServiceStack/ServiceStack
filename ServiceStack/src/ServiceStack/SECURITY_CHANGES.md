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


