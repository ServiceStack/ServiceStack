# v4.0.42 Release Notes

## New JsonHttpClient!

The new `JsonHttpClient` is an alternative to the existing generic typed `JsonServiceClient` for consuming ServiceStack Services 
which instead of `HttpWebRequest` is based on Microsoft's latest async `HttpClient` (from [Microsoft.Net.Http](https://www.nuget.org/packages/Microsoft.Net.Http) on NuGet). 

`JsonHttpClient` implements the full [IServiceClient API](https://gist.github.com/mythz/4683438240820b522d39) making it an easy drop-in replacement for your existing `JsonServiceClient` 
where in most cases it can simply be renamed to `JsonHttpClient`, e.g:

```csharp
//IServiceClient client = new JsonServiceClient("http://techstacks.io");
IServiceClient client = new JsonHttpClient("http://techstacks.io");
```

Which can then be [used as normal](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client):

```csharp
var response = await client.GetAsync(new GetTechnology { Slug = "servicestack" });
```

### Install

JsonHttpClient can be downloaded from NuGet at:

    > Install-Package ServiceStack.HttpClient

### PCL Support

JsonHttpClient also comes in PCL flavour and can be used on the same platforms as the 
[existing PCL Service Clients](https://github.com/ServiceStackApps/HelloMobile) enabling the same clean and productive development experience on popular mobile platforms like 
[Xamarin.iOS](http://developer.xamarin.com/guides/ios/) and [Xamarin.Android](http://developer.xamarin.com/guides/android/).

### [ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient)

One of the primary benefits of being based on `HttpClient` is being able to make use of 
[ModernHttpClient](https://github.com/paulcbetts/ModernHttpClient) which provides a thin wrapper around iOS's native `NSURLSession` or `OkHttp` client on Android, offering improved stability for 3G mobile connectivity.

To enable, install [ModernHttpClient](https://www.nuget.org/packages/ModernHttpClient) then set the 
Global HttpMessageHandler Factory to configure all `JsonHttpClient` instances to use ModernHttpClient's `NativeMessageHandler`: 

```csharp
JsonHttpClient.GlobalHttpMessageHandlerFactory = () => new NativeMessageHandler();
```

Alternatively, you can configure a single client instance to use ModernHttpClient with:

```csharp
client.HttpMessageHandler = new NativeMessageHandler();
```

### Differences with JsonServiceClient

Whilst our goal is to retain the same behavior in both clients, there are some differences resulting from using HttpClient where the Global and Instance Request and Response Filters are instead passed HttpClients `HttpRequestMessage` and `HttpResponseMessage`. 

Also, all API's are **Async** under-the-hood where any Sync API's that doesn't return a `Task<T>` just blocks on the Async `Task.Result` response. As this can dead-lock in certain environments we recommend sticking with the Async API's unless safe to do otherwise. 

## Encrypted Messaging!

One of the benefits of adopting a message-based design is being able to easily layer functionality and generically add value to all Services, we've seen this recently with [Auto Batched Requests](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Batched-Requests) which automatically enables each Service to be batched and executed in a single HTTP Request. Similarly the new Encrypted Messaging feature 
enables a secure channel for all Services (inc Auto Batched Requests :) offering protection to clients who can now easily send and receive encrypted messages over unsecured HTTP!

### Encrypted Messaging Overview

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/encrypted-messaging.png)

### Configuration

Encrypted Messaging support is enabled by registering the plugin:

```csharp
Plugins.Add(new EncryptedMessagesFeature {
    PrivateKeyXml = ServerRsaPrivateKeyXml
});
```

Where `PrivateKeyXml` is the Servers RSA Private Key Serialized as XML. If you don't have an existing one, a new one can be generated with:

```csharp
var rsaKeyPair = RsaUtils.CreatePublicAndPrivateKeyPair();
string ServerRsaPrivateKeyXml = rsaKeyPair.PrivateKey;
```

Once generated, it's important the Private Key is kept confidential as anyone with access will be able to decrypt 
the encrypted messages! Whilst most [obfuscation efforts are ultimately futile](http://stackoverflow.com/a/6018247/85785) the goal should be to contain the private key to your running Web Application, limiting access as much as possible.

Once registered, the EncryptedMessagesFeature enables the 2 Services below:

 - `GetPublicKey` - Returns the Serialized XML of your Public Key (extracted from the configured Private Key)
 - `EncryptedMessage` - The Request DTO which encapsulates all encrypted Requests (can't be called directly)

### Giving Clients the Public Key

To communicate clients need access to the Server's Public Key, it doesn't matter who has accessed the Public Key only that clients use the **real** Servers Public Key. It's therefore not advisable to download the Public Key over unsecure `http://` where traffic can potentially be intercepted and the key spoofed, subjecting them to a [Man-in-the-middle attack](https://en.wikipedia.org/wiki/Man-in-the-middle_attack). 

It's safer instead to download the public key over a trusted `https://` url where the servers origin is verified by a trusted [CA](https://en.wikipedia.org/wiki/Certificate_authority). Sharing the Public Key over Dropbox, Google Drive, OneDrive or other encrypted channels are also good options.

Since `GetPublicKey` is just a ServiceStack Service it's easily downloadable using a Service Client:

```csharp
var client = new JsonServiceClient(BaseUrl);
string publicKeyXml = client.Get(new GetPublicKey());
```

If the registered `EncryptedMessagesFeature.PublicKeyPath` has been changed from its default `/publickey`, it can be dowloaded with:

```csharp
string publicKeyXml = client.Get<string>("/custom-publickey"); // or with HttpUtils:
string publicKeyXml = BaseUrl.CombineWith("/custom-publickey").GetStringFromUrl();
```

> To help with verification the SHA256 Hash of the PublicKey is returned in `X-PublicKey-Hash` HTTP Header

### Encrypted Service Client

Once they have the Server's Public Key, clients can use it to get an `EncryptedServiceClient` via the `GetEncryptedClient()` extension method on `JsonServiceClient` or new `JsonHttpClient`, e.g:

```csharp
var client = new JsonServiceClient(BaseUrl);
IEncryptedClient encryptedClient = client.GetEncryptedClient(publicKeyXml);
```

Once configured, clients have access to the familiar typed Service Client API's and productive workflow they're used to with the generic Service Clients, sending typed Request DTO's and returning the typed Response DTO's - rendering the underlying encrypted messages a transparent implementation detail:

```csharp
HelloResponse response = encryptedClient.Send(new Hello { Name = "World" });
response.Result.Print(); //Hello, World!
```

REST Services Example:

```csharp
HelloResponse response = encryptedClient.Get(new Hello { Name = "World" });
```

Auto-Batched Requests Example:

```csharp
var requests = new[] { "Foo", "Bar", "Baz" }.Map(x => new HelloSecure { Name = x });
var responses = encryptedClient.SendAll(requests);
```

When using the `IEncryptedClient`, the entire Request and Response bodies are encrypted including Exceptions which continue to throw a populated `WebServiceException`:

```csharp
try
{
    var response = encryptedClient.Send(new Hello());
}
catch (WebServiceException ex)
{
    ex.ResponseStatus.ErrorCode.Print(); //= ArgumentNullException
    ex.ResponseStatus.Message.Print();   //= Value cannot be null. Parameter name: Name
}
```

### Authentication with Encrypted Messaging

Many encrypted messaging solutions use Client Certificates which Servers can use to cryptographically verify a client's identity - providing an alternative to HTTP-based Authentication. We've decided against using this as it would've forced an opinionated implementation and increased burden of PKI certificate management and configuration onto Clients and Servers - reducing the applicability and instant utility of this feature.

We can instead leverage the existing Session-based Authentication Model in ServiceStack letting clients continue to use the existing Auth functionality and Auth Providers they're already used to, e.g:

```csharp
var authResponse = encryptedClient.Send(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "test@gmail.com",
    Password = "p@55w0rd",
});
```

Encrypted Messages have their cookies stripped so they're no longer visible in the clear which minimizes their exposure to Session hijacking. This does pose the problem of how we can call authenticated Services if the encrypted HTTP Client is no longer sending Session Cookies? 

Without the use of clear-text Cookies or HTTP Headers there's no longer an *established Authenticated Session* for the `encryptedClient` to use to make subsequent Authenticated requests. What we can do  instead is pass the Session Id in the encrypted body for Request DTO's that implement the new `IHasSessionId` interface, e.g:

```csharp
[Authenticate]
public class HelloAuthenticated : IReturn<HelloAuthenticatedResponse>, IHasSessionId
{
    public string SessionId { get; set; }
    public string Name { get; set; }
}

var response = encryptedClient.Send(new HelloAuthenticated {
    SessionId = authResponse.SessionId,
    Name = "World"
});
```

Here we're injecting the returned Authenticated `SessionId` to access the `[Authenticate]` protected Request DTO. However remembering to do this for every authenticated request can get tedious, a nicer alternative is just setting it once on the `encryptedClient` which will then use it to automatically populate any `IHasSessionId` Request DTO's:

```csharp
encryptedClient.SessionId = authResponse.SessionId;

var response = encryptedClient.Send(new HelloAuthenticated {
    Name = "World"
});
```

> Incidentally this feature is now supported in **all Service Clients**

### Combined Authentication Strategy

Another potential use-case is to only use Encrypted Messaging when sending any sensitive information and the normal Service Client for other requests. In which case we can Authenticate and send the user's password with the `encryptedClient`:

```csharp
var authResponse = encryptedClient.Send(new Authenticate {
    provider = CredentialsAuthProvider.Name,
    UserName = "test@gmail.com",
    Password = "p@55w0rd",
});
```

But then fallback to using the normal `IServiceClient` for subsequent requests. But as the `encryptedClient` doesn't receive cookies we'd need to set it explicitly on the client ourselves with:

```csharp
client.SetCookie("ss-id", authResponse.SessionId);
```

After which the ServiceClient "establishes an authenticated session" and can be used to make Authenticated requests, e.g:

```csharp
var response = await client.GetAsync(new HelloAuthenticated { Name = "World" });
```

> Note: EncryptedServiceClient is unavailable in PCL Clients

### [Hybrid Encryption Scheme](https://en.wikipedia.org/wiki/Hybrid_cryptosystem)

The Encrypted Messaging Feature follows a [Hybrid Cryptosystem](https://en.wikipedia.org/wiki/Hybrid_cryptosystem) which uses RSA Public Keys for [Asymmetric Encryption](https://en.wikipedia.org/wiki/Public-key_cryptography) combined with the performance of AES [Symmetric Encryption](https://en.wikipedia.org/wiki/Symmetric-key_algorithm) making it suitable for encrypting large message payloads. 

The key steps in the process are outlined below:

  1. The Client creates a new `IEncryptedClient` configured with the Server **Public Key**
  2. The Client uses the `IEncryptedClient` to send a Request DTO:
    1. A new 256-bit Symmetric **AES Key** and [IV](https://en.wikipedia.org/wiki/Initialization_vector) is generated
    2. The **AES Key** and **IV** bytes are merged, encrypted with the Servers **Public Key** and Base64 encoded
    3. The Request DTO is serialized into JSON and packed with the current **Timestamp**, **Verb** and **Operation** and encrypted with the new **AES Key**
  3. The `IEncryptedClient` uses the underlying JSON Service Client to send the `EncryptedMessage` to the remote Server
  4. The `EncryptedMessage` is picked up and decrypted by the EncryptedMessagingFeature **Request Converter**:
    1. The **AES Key** is decrypted with the Servers **Private Key**
    2. The **IV** is checked against the nonce Cache, verified it's never been used before, then cached 
    3. The unencrypted **AES Key** is used to decrypt the **EncryptedBody**
    4. The **timestamp** is verified it's not older than `EncryptedMessagingFeature.MaxRequestAge`
    5. Any expired nonces are removed. (The **timestamp** and **IV** are used to prevent replay attacks)
    6. The JSON body is deserialized and resulting **Request DTO** returned from the Request Converter
  5. The converted **Request DTO** is executed in ServiceStack's Request Pipeline as normal
  6. The **Response DTO** is picked up by the EncryptedMessagingFeature **Response Converter**:
    1. Any **Cookies** set during the Request are removed
    2. The **Response DTO** is serialized with the **AES Key** and returned in an `EncryptedMessageResponse`
  7. The `IEncryptedClient` decrypts the `EncryptedMessageResponse` with the **AES Key**
    1. The **Response DTO** is extracted and returned to the caller

A visual of how this all fits together in captured in the high-level diagram below:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/encrypted-messaging.png)

 - Components in **Yellow** show the encapsulated Encrypted Messaging functionality where all encryption and decryption is performed
 - Components in **Blue** show Unencrypted DTO's
 - Components in **Green** show Encrypted content:
    - The AES Key and IV in **Dark Green** is encrypted by the client using the Server's Public Key
    - The EncryptedRequest in **Light Green** is encrypted with a new AES Key generated by the client on each Request
 - Components in **Dark Grey** depict existing ServiceStack functionality where Requests are executed as normal through the [Service Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) and [Request Pipeline](https://github.com/ServiceStack/ServiceStack/wiki/Order-of-Operations)

All Request and Response DTO's get encrypted and embedded in the `EncryptedMessage` and `EncryptedMessageResponse` DTO's below:

```csharp
public class EncryptedMessage : IReturn<EncryptedMessageResponse>
{
    public string EncryptedSymmetricKey { get; set; }
    public string EncryptedBody { get; set; }
}

public class EncryptedMessageResponse
{
    public string EncryptedBody { get; set; }
}
```

The diagram also expands the `EncryptedBody` Content containing the **EncryptedRequest** consisting of the following parts:

 - **Timestamp** - Unix Timestamp of the Request
 - **Verb** - Target HTTP Method
 - **Operation** - Request DTO Name
 - **JSON** - Request DTO serialized as JSON

### Source Code

 - The Client implementation is available in [EncryptedServiceClient.cs](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Client/EncryptedServiceClient.cs)
 - The Server implementation is available in [EncryptedMessagesFeature.cs](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/EncryptedMessagesFeature.cs)
 - The Crypto Utils used are available in the [RsaUtils.cs](https://github.com/ServiceStack/ServiceStack/blob/b4a2a1f74936c8a100b688cbdbca08ff5b212cbe/src/ServiceStack.Client/CryptUtils.cs#L31) and [AesUtils.cs](https://github.com/ServiceStack/ServiceStack/blob/b4a2a1f74936c8a100b688cbdbca08ff5b212cbe/src/ServiceStack.Client/CryptUtils.cs#L189)
 - Tests are available in [EncryptedMessagesTests.cs](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/UseCases/EncryptedMessagesTests.cs)

## Request and Response Converters

The Encrypted Messaging Feature takes advantage of new Converters that let you change the Request DTO and Response DTO's that get used in ServiceStack's Request Pipeline where:

Request Converters are executed directly after any [Custom Request Binders](https://github.com/ServiceStack/ServiceStack/wiki/Serialization-deserialization#create-a-custom-request-dto-binder):

```csharp
appHost.RequestConverters.Add((req, requestDto) => {
    //Return alternative Request DTO or null to retain existing DTO
});
```

Response Converters are executed directly after the Service:

```csharp
appHost.ResponseConverters.Add((req, response) =>
    //Return alternative Response or null to retain existing Service response
});
```

In addition to the converters above, Plugins can now register new callbacks in `IAppHost.OnEndRequestCallbacks` which gets fired at the end of a request.

## [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

### Eclipse Integration!

We've further expanded our support for Java with our new **ServiceStackEclipse** plugin providing cross-platform [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) integration with Eclipse on Windows, OSX and Linux!

#### Install from Eclipse Marketplace

To install, search for **ServiceStack** in the Eclipse Marketplace at `Help > Eclipse Marketplace`:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/ss-eclipse-install-win.gif)

Find the **ServiceStackEclipse** plugin, click **Install** and follow the wizard to the end, restarting to launch Eclipse with the plugin loaded!

> **ServiceStackEclipse** is best used with Java Maven Projects where it automatically adds the **ServiceStack.Java** client library to your Maven Dependencies and when your project is set to **Build Automatically**, are then downloaded and registered, so you're ready to start consuming ServiceStack Services with the new `JsonServiceClient`!

#### Eclipse Add ServiceStack Reference

Just like Android Studio you can right-click on a Java Package to open the **Add ServiceStack Reference...** dialog from the Context Menu:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/add-reference-demo.gif)

Complete the dialog to add the remote Servers generated Java DTO's to your selected Java package and the `net.servicestack.client` dependency to your Maven dependencies.

#### Eclipse Update ServiceStack Reference

Updating a ServiceStack Reference works as normal where you can change any of the available options in the header comments, save, then right-click on the file in the File Explorer and click on **Update ServiceStack Reference** in the Context Menu:
 
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackeclipse/update-reference-demo.gif)

### ServiceStack IDEA IntelliJ Plugin 

The **ServiceStackIDEA** plugin has added support for IntelliJ Maven projects giving Java devs a productive and familiar development experience whether they're creating Android Apps or pure cross-platform Java clients.

#### Install ServiceStack IDEA from the Plugin repository

The ServiceStack IDEA is now available to install directly from within IntelliJ or Android Studio IDE Plugins Repository, to Install Go to: 

 1. `File -> Settings...` Main Menu Item
 2. Select **Plugins** on left menu then click **Browse repositories...** at bottom
 3. Search for **ServiceStack** and click **Install plugin**
 4. Restart to load the installed ServiceStack IDEA plugin

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackidea/android-plugin-download.gif)

### ssutil.exe - Command line ServiceStack Reference tool

Add ServiceStack Reference is also moving beyond our growing list of supported IDEs and is now available in a single cross-platform .NET command-line **.exe** making it easy for build servers and automated tasks or command-line runners of your favorite text editors to easily Add and Update ServiceStack References!

To Get Started download **ssutil.exe** and open a command prompt to the containing directory:

#### Download [ssutil.exe](https://github.com/ServiceStack/ServiceStackVS/raw/master/dist/ssutil.exe)

#### ssutil.exe Usage:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/ssutil-help.png)

**Adding a new ServiceStack Reference**

To create a new ServiceStack Reference, pass the remote ServiceStack **BaseUrl** then specify both which `-file` and `-lang` you want, e.g:

    ssutil http://techstacks.io -file TechStacks -lang CSharp

Executing the above command fetches the C# DTOs and saves them in a local file named `TechStacks.dtos.cs`.

**Available Languages**

 - CSharp
 - FSharp
 - VbNet
 - Java
 - Swift
 - TypeScript.d

**Update existing ServiceStack Reference**

Updating a ServiceStack Reference is even easier we just specify the path to the existing generated DTO's. E.g. Update the `TechStacks.dtos.cs` we just created with:

    ssutil TechStacks.dtos.cs

### [Using Xamarin.Auth with ServiceStack](https://github.com/ServiceStackApps/TechStacksAuth)

[Xamarin.Auth](https://components.xamarin.com/gettingstarted/xamarin.auth) 
is an extensible Component and provides a good base for handling authenticating with ServiceStack from Xamarin platforms. To show how to make use of it we've created the [TechStacksAuth](https://github.com/ServiceStackApps/TechStacksAuth) example repository containing a custom `WebAuthenticator` we use to call our remote ServiceStack Web Application and reuse its existing OAuth integration. 

Here's an example using `TwitterAuthProvider`:

![](https://github.com/ServiceStack/Assets/raw/master/img/apps/TechStacks/xamarin-android-auth-demo.gif)

Checkout the [TechStacksAuth](https://github.com/ServiceStackApps/TechStacksAuth) repo for the docs and source code.

### Swift

Unfortunately the recent release of Xcode 6.4 and Swift 1.2 still haven't fixed the [earlier regression added in Xcode 6.3 and Swift 1.2](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2015/release-notes.md#swift-native-types-upgraded-to-swift-12) where the Swift compiler segfaults trying to compile Extensions to Types with a Generic Base Class. The [swift-compiler-crashes](https://github.com/practicalswift/swift-compiler-crashes) repository is reporting this is now fixed in Swift 2.0 / Xcode 7 beta but as that wont be due till later this year we've decided to improve the experience by not generating any types with the problematic Generic Base Types from the generated DTO's by default. This is configurable with:

```swift
//ExcludeGenericBaseTypes: True
```

Any types that were omitted from the generated DTO's will be emitted in comments, using the format:

```swift
//Excluded: {TypeName}
```

### C#, F#, VB.NET Service Reference

The C#, F# and VB.NET Native Type providers can emit `[GeneratedCode]` attributes with:

```csharp
AddGeneratedCodeAttributes: True
```

This is useful for skipping any internal Style Cop rules on generated code.

## Service Clients

### Custom Client Caching Strategy

New `ResultsFilter` and `ResultsFilterResponse` delegates have been added to all Service Clients allowing clients to employ a custom caching strategy. 

Here's a basic example implementing a cache for all **GET** Requests:

```csharp
var cache = new Dictionary<string, object>();

client.ResultsFilter = (type, method, uri, request) => {
    if (method != HttpMethods.Get) return null;
    object cachedResponse;
    cache.TryGetValue(uri, out cachedResponse);
    return cachedResponse;
};
client.ResultsFilterResponse = (webRes, response, method, uri, request) => {
    if (method != HttpMethods.Get) return;
    cache[uri] = response;
};

//Subsequent requests returns cached result
var response1 = client.Get(new GetCustomer { CustomerId = 5 });
var response2 = client.Get(new GetCustomer { CustomerId = 5 }); //cached response
```

The `ResultsFilter` delegate is executed with the context of the request before the request is made. Returning a value of type `TResponse` short-circuits the request and returns that response. Otherwise the request continues and its response passed into the `ResultsFilterResponse` delegate where it can be cached. 

#### New ServiceClient API's

The following new API's were added to all .NET Service Clients:

 - `SetCookie()` - Sets a Cookie on the clients `CookieContainer`
 - `GetCookieValues()` - Return all site Cookies in a string Dictionary
 - `CustomMethodAsync()` - Call any Custom HTTP Method Asynchronously

### Implicit Versioning

Similar to the behavior of `IHasSessionId` above, Service Clients that have specified a `Version` number, e.g:

```csharp
client.Version = 2;
```

Will populate that version number in all Request DTO's implementing `IHasVersion`, e.g:

```csharp
public class Hello : IReturn<HelloResponse>, IHasVersion {
    public int Version { get; set; }
    public string Name { get; set; }
}

client.Version = 2;
client.Get(new Hello { Name = "World" });  // Hello.Version=2
```

#### Version Abbreviation Convention

A popular convention for specifying versions in API requests is with the `?v=1` QueryString which ServiceStack now uses as a fallback for populating any Request DTO's that implement `IHasVersion` (as above).

> Note: as ServiceStack's message-based design promotes forward and backwards-compatible Service API designs, our recommendation is to only consider implementing versioning when necessary, at which point check out our [recommended versioning strategy](http://stackoverflow.com/a/12413091/85785).

## Cancellable Requests Feature

The new Cancellable Requests Feature makes it easy to design long-running Services that are cancellable with an external Web Service Request. To enable this feature, register the `CancellableRequestsFeature` plugin:

```csharp
Plugins.Add(new CancellableRequestsFeature());
```

### Designing a Cancellable Service

Then in your Service you can wrap your implementation within a disposable `ICancellableRequest` block which encapsulates a Cancellation Token that you can watch to determine if the Request has been cancelled, e.g: 

```csharp
public object Any(TestCancelRequest req)
{
    using (var cancellableRequest = base.Request.CreateCancellableRequest())
    {
        //Simulate long-running request
        while (true)
        {
            cancellableRequest.Token.ThrowIfCancellationRequested();
            Thread.Sleep(100);
        }
    }
}
```

### Cancelling a remote Service

To be able to cancel a Server request on the client, the client must first **Tag** the request which it does by assigning the `X-Tag` HTTP Header with a user-defined string in a Request Filter before calling a cancellable Service, e.g:

```csharp
var tag = Guid.NewGuid().ToString();
var client = new JsonServiceClient(baseUri) {
    RequestFilter = req => req.Headers[HttpHeaders.XTag] = tag
};

var responseTask = client.PostAsync(new TestCancelRequest());
```

Then at anytime whilst the Service is still executing the remote request can be cancelled by calling the `CancelRequest` Service with the specified **Tag**, e.g: 

```csharp
var cancelResponse = client.Post(new CancelRequest { Tag = tag });
```

If it was successfully cancelled it will return a `CancelRequestResponse` DTO with the elapsed time of how long the Service ran for. Otherwise if the remote Service had completed or never existed it will throw **404 Not Found** in a `WebServiceException`.

## Include Aggregates in AutoQuery

AutoQuery now supports running additional Aggregate queries on the queried result-set. 
To include aggregates in your Query's response specify them in the `Include` property of your AutoQuery Request DTO, e.g:

    var response = client.Get(new Query Rockstars { Include = "COUNT(*)" })

Or in the `Include` QueryString param if you're calling AutoQuery Services from a browser, e.g:

    /rockstars?include=COUNT(*)

The result is then published in the `QueryResponse<T>.Meta` String Dictionary and is accessible with:

    response.Meta["COUNT(*)"] //= 7

By default any of the functions in the SQL Aggregate whitelist can be referenced: 

    AVG, COUNT, FIRST, LAST, MAX, MIN, SUM

Which can be added to or removed from by modifying `SqlAggregateFunctions` collection, e.g, you can allow usage of a `CustomAggregate` SQL Function with:

    Plugins.Add(new AutoQueryFeature { 
        SqlAggregateFunctions = { "CustomAggregate" }
    })

### Aggregate Query Usage

The syntax for aggregate functions is modelled after their usage in SQL so they should be instantly familiar. 
At its most basic usage you can just specify the name of the aggregate function which will use `*` as a default argument so you can also query `COUNT(*)` with: 

    ?include=COUNT

It also supports SQL aliases:

    COUNT(*) Total
    COUNT(*) as Total

Which is used to change what key the result is saved into:

    response.Meta["Total"]

Columns can be referenced by name:

    COUNT(LivingStatus)

If an argument matches a column in the primary table the literal reference is used as-is, if it matches a column in a joined table it's replaced with its fully-qualified reference and when it doesn't match any column, Numbers are passed as-is otherwise its automatically escaped and quoted and passed in as a string literal.

The `DISTINCT` modifier can also be used, so a complex example looks like:

    COUNT(DISTINCT LivingStatus) as UniqueStatus

Which saves the result of the above function in:

    response.Meta["UniqueStatus"]

Any number of aggregate functions can be combined in a comma-delimited list:

    Count(*) Total, Min(Age), AVG(Age) AverageAge

Which returns results in:

    response.Meta["Total"]
    response.Meta["Min(Age)"]
    response.Meta["AverageAge"]

#### Aggregate Query Performance

Surprisingly AutoQuery is able to execute any number of Aggregate functions without performing any additional queries as previously to support paging, a `Total` needed to be executed for each AutoQuery. Now the `Total` query is combined with all other aggregate functions and executed in a single query.

### AutoQuery Response Filters

The Aggregate functions feature is built on the new `ResponseFilters` support in AutoQuery which provides a new extensibility option enabling customization and additional metadata to be attached to AutoQuery Responses. As the Aggregate Functions support is itself a Response Filter in can disabled by clearing them:

```csharp
Plugins.Add(new AutoQueryFeature {
    ResponseFilters = new List<Action<QueryFilterContext>>()
})
```

The Response Filters are executed after each AutoQuery and gets passed the full context of the executed query, i.e:

```csharp
class QueryFilterContext
{
    IDbConnection Db             // The ADO.NET DB Connection
    List<Command> Commands       // Tokenized list of commands
    IQuery Request               // The AutoQuery Request DTO
    ISqlExpression SqlExpression // The AutoQuery SqlExpression
    IQueryResponse Response      // The AutoQuery Response DTO
}
```

Where the `Commands` property contains the parsed list of commands from the `Include` property, tokenized into the structure below:

```csharp
class Command 
{
    string Name
    List<string> Args
    string Suffix
}
```

With this we could add basic calculator functionality to AutoQuery with the custom Response Filter below:

```csharp
Plugins.Add(new AutoQueryFeature {
    ResponseFilters = {
        ctx => {
            var supportedFns = new Dictionary<string, Func<int, int, int>>(StringComparer.OrdinalIgnoreCase)
            {
                {"ADD",      (a,b) => a + b },
                {"MULTIPLY", (a,b) => a * b },
                {"DIVIDE",   (a,b) => a / b },
                {"SUBTRACT", (a,b) => a - b },
            };
            var executedCmds = new List<Command>();
            foreach (var cmd in ctx.Commands)
            {
                Func<int, int, int> fn;
                if (!supportedFns.TryGetValue(cmd.Name, out fn)) continue;
                var label = !string.IsNullOrWhiteSpace(cmd.Suffix) ? cmd.Suffix.Trim() : cmd.ToString();
                ctx.Response.Meta[label] = fn(int.Parse(cmd.Args[0]), int.Parse(cmd.Args[1])).ToString();
                executedCmds.Add(cmd);
            }
            ctx.Commands.RemoveAll(executedCmds.Contains);
        }        
    }
})
```

Which now lets users perform multiple basic arithmetic operations with any AutoQuery request!

```csharp
var response = client.Get(new QueryRockstars {
    Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
});

response.Meta["ADD(6,2)"]      //= 8
response.Meta["SixTimesTwo"]   //= 12
response.Meta["Subtract(6,2)"] //= 4
response.Meta["TheDivide"]     //= 3
```

### Untyped SqlExpression

If you need to introspect or modify the executed `ISqlExpression`, it’s useful to access it as a `IUntypedSqlExpression` so its non-generic API's are still accessible without having to convert it back into its concrete generic `SqlExpression<T>` Type, e.g:

```csharp
IUntypedSqlExpression q = ctx.SqlExpression.GetUntypedSqlExpression()
    .Clone();
```

> Cloning the SqlExpression allows you to modify a copy that won't affect any other Response Filter.

### AutoQuery Property Mapping

AutoQuery can map `[DataMember]` property aliases on Request DTO's to the queried table, e.g:

```csharp
public class QueryPerson : QueryBase<Person>
{
    [DataMember("first_name")]
    public string FirstName { get; set; }
}

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

Which can be queried with:

    ?first_name=Jimi

or by setting the global `JsConfig.EmitLowercaseUnderscoreNames=true` convention:

```csharp
public class QueryPerson : QueryBase<Person>
{
    public string LastName { get; set; }
}
```

Where it's also queryable with:

    ?last_name=Hendrix

## OrmLite

### Dynamic Result Sets

There's new support for returning unstructured resultsets letting you Select `List<object>` instead of having results mapped to a concrete Poco class, e.g:

```csharp
db.Select<List<object>>(db.From<Poco>()
  .Select("COUNT(*), MIN(Id), MAX(Id)"))[0].PrintDump();
```

Output of objects in the returned `List<object>`:

    [
        10,
        1,
        10
    ]

You can also Select `Dictionary<string,object>` to return a dictionary of column names mapped with their values, e.g:

```csharp
db.Select<Dictionary<string,object>>(db.From<Poco>()
  .Select("COUNT(*) Total, MIN(Id) MinId, MAX(Id) MaxId"))[0].PrintDump();
```

Output of objects in the returned `Dictionary<string,object>`:

    {
        Total: 10,
        MinId: 1,
        MaxId: 10
    }

and can be used for API's returning a **Single** row result:

```csharp
db.Single<List<object>>(db.From<Poco>()
  .Select("COUNT(*) Total, MIN(Id) MinId, MAX(Id) MaxId")).PrintDump();
```

or use `object` to fetch an unknown **Scalar** value:

```csharp
object result = db.Scalar<object>(db.From<Poco>().Select(x => x.Id));
```

### New DB Parameters API's

To enable even finer-grained control of parameterized queries we've added new overloads that take a collection of IDbDataParameter's:

```csharp
List<T> Select<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T Single<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T Scalar<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> Column<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
IEnumerable<T> ColumnLazy<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
HashSet<T> ColumnDistinct<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
Dictionary<K, List<V>> Lookup<K, V>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> SqlList<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
List<T> SqlColumn<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
T SqlScalar<T>(string sql, IEnumerable<IDbDataParameter> sqlParams)
```

> Including Async equivalents for each of the above Sync API's.

The new API's let you execute parameterized SQL with finer-grained control over the `IDbDataParameter` used, e.g:

```csharp
IDbDataParameter pAge = db.CreateParam("age", 40, dbType:DbType.Int16);
db.Select<Person>("SELECT * FROM Person WHERE Age > @pAge", new[] { pAge });
```

The new `CreateParam()` extension method above is a useful helper for creating custom IDbDataParameter's.

### Customize null values

The new `OrmLiteConfig.OnDbNullFilter` lets you to replace DBNull values with a custom value, so you could convert all `null` strings to be populated with `"NULL"` using:

```csharp
OrmLiteConfig.OnDbNullFilter = fieldDef => 
    fieldDef.FieldType == typeof(string)
        ? "NULL"
        : null;
```

### Case Insensitive References

If you're using a case-insensitive database you can tell OrmLite to match on case-insensitive POCO references with:

```csharp
OrmLiteConfig.IsCaseInsensitive = true;
```

### Enhanced CaptureSqlFilter

CaptureSqlFilter now tracks DB Parameters used in each query which can be used to quickly found out what SQL your DB calls generate by surrounding DB access in a using scope like:

```csharp
using (var captured = new CaptureSqlFilter())
using (var db = OpenDbConnection())
{
    db.Where<Person>(new { Age = 27 });

    captured.SqlCommandHistory[0].PrintDump();
}
```

Emits the Executed SQL along with any DB Parameters: 

    {
        Sql: "SELECT ""Id"", ""FirstName"", ""LastName"", ""Age"" FROM ""Person"" WHERE ""Age"" = @Age",
        Parameters: 
        {
            Age: 27
        }
    }

### Other OrmLite Features

 - New `IncludeFunctions = true` T4 Template configuration for generating Table Valued Functions added by [@mikepugh](https://github.com/mikepugh)
 - New `OrmLiteConfig.SanitizeFieldNameForParamNameFn` can be used to support sanitizing field names with non-ascii values into legal DB Param names

## Authentication

### Enable Session Ids on QueryString

Setting `Config.AllowSessionIdsInHttpParams=true` will allow clients to specify the `ss-id`, `ss-pid` Session Cookies on the QueryString or FormData. This is useful for getting Authenticated SSE Sessions working in IE9 which needs to rely on SSE Polyfills that's unable to send Cookies or Custom HTTP Headers.

The [SSE-polyfills Chat Demo](http://chat.servicestack.net/default_ieshim) has an example of adding the Current Session Id on the [JavaScript SSE EventSource Url](https://github.com/ServiceStackApps/Chat/blob/master/src/Chat/default_ieshim.cshtml#L93):

```js
var source = new EventSource('/event-stream?channels=@channels&ss-id=@(base.GetSession().Id));
```

### In Process Authenticated Requests

You can enable the `CredentialsAuthProvider` to allow **In Process** requests to Authenticate without a Password with:

```csharp
new CredentialsAuthProvider {
    SkipPasswordVerificationForInProcessRequests = true,
}
```

When enabled this lets **In Process** Service Requests to login as a specified user without needing to provide their password. 

For example this could be used to create an [Intranet Restricted](https://github.com/ServiceStack/ServiceStack/wiki/Restricting-Services) **Admin-Only** Service that lets you login as another user so you can debug their account without knowing their password with:

```csharp
[RequiredRole("Admin")]
[Restrict(InternalOnly=true)]
public class ImpersonateUser 
{
    public string UserName { get; set; }
}

public object Any(ImpersonateUser request)
{
    using (var service = base.ResolveService<AuthenticateService>()) //In Process
    {
        return service.Post(new Authenticate {
            provider = AuthenticateService.CredentialsProvider,
            UserName = request.UserName,
        });
    }
}
```

> Your Services can use the new `Request.IsInProcessRequest()` to identify Services that were executed in-process.

### New CustomValidationFilter Filter

The new `CustomValidationFilter` is available on each `AuthProvider` and can be used to add custom validation logic where returning any non-null response will short-circuit the Auth Process and return the response to the client. 

The Validation Filter receives the full [AuthContext](https://github.com/ServiceStack/ServiceStack/blob/dd938c284ea509c4cdfab0e416c489aae7877981/src/ServiceStack/Auth/AuthProvider.cs#L415-L424) captured about the Authentication Request. 

So if you're under attack you could use this filter to Rick Roll North Korean hackers :)

```csharp
Plugins.Add(new AuthFeature(..., 
    new IAuthProvider[] {
        new CredentialsAuthProvider {
            CustomValidationFilter = authCtx => 
                authCtx.Request.UserHostAddress.StartsWith("175.45.17")
                    ? HttpResult.Redirect("https://youtu.be/dQw4w9WgXcQ")
                    : null
        }   
    }));
```

### UserName Validation

The UserName validation for all Auth Repositories have been consolidated in a central location, configurable at:

```csharp
Plugins.Add(new AuthFeature(...){
    ValidUserNameRegEx = new Regex(@"^(?=.{3,20}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled),
})
```

> Note: the default UserName RegEx above was increased from 15 chars limit to 20 chars

Instead of RegEx you can choose to validate using a Custom Predicate. The example below ensures UserNames don't include specific chars:

```csharp
Plugins.Add(new AuthFeature(...){
    IsValidUsernameFn = userName => userName.IndexOfAny(new[] { '@', '.', ' ' }) == -1
})
```

### Overridable Hash Provider

The `IHashProvider` used to generate and verify password hashes and salts in each UserAuth Repository is now overridable from its default with:

```csharp
container.Register<IHashProvider>(c => 
    new SaltedHash(HashAlgorithm:new SHA256Managed(), theSaltLength:4));
```

### Configurable Session Expiry

Permanent and Temporary Sessions can now be configured separately with different Session Expiries, configurable on either  `AuthFeature` or `SessionFeature` plugins, e.g:

```csharp
new AuthFeature(...) {
    SessionExpiry = TimeSpan.FromDays(7 * 2),
    PermanentSessionExpiry = TimeSpan.FromDays(7 * 4),
}
```

The above defaults configures Temporary Sessions to last a maximum of 2 weeks and Permanent Sessions lasting 4 weeks.

## Redis

 - New `PopItemsFromSet(setId, count)` API added by [@scottmcarthur](https://github.com/scottmcarthur)
 - The `SetEntry()` API's for setting string values in `IRedisClient` have been deprecated in favor of new `SetValue()` API's. 
 - Large performance improvement for saving large values (>8MB)  
 - Internal Buffer pool now configurable with `RedisConfig.BufferLength` and `RedisConfig.BufferPoolMaxSize`

## ServiceStack.Text

 - Use `JsConfig.ExcludeDefaultValues=true` to reduce payloads by omitting properties with default values
 - Text serializers now serialize any property or field annotated with the `[DataMember]` attribute, inc. private fields.
 - Updated `RecyclableMemoryStream` to the latest version, no longer throws an exception for disposing streams twice
 - Added support for serializing collections with base types

#### T[].NewArray() Usage

The `NewArray()` extension method reduces boilerplate required in modifying and returning an Array, it's useful when modifying configuration using fixed-size arrays, e.g:

```csharp
JsConfig.IgnoreAttributesNamed = JsConfig.IgnoreAttributesNamed.NewArray(
     with: typeof(ScriptIgnoreAttribute).Name,
  without: typeof(JsonIgnoreAttribute).Name);
```

Which is equivalent to the imperative alternative:

```csharp
var attrNames = new List<string>(JsConfig.IgnoreAttributesNamed) { 
    typeof(ScriptIgnoreAttribute).Name 
};
attrNames.Remove(typeof(JsonIgnoreAttribute).Name));
JsConfig.IgnoreAttributesNamed = attrNames.ToArray();
```

#### byte[].Combine() Usage

The `Combine(byte[]...)` extension method combines multiple byte arrays into a single byte[], e.g:

```csharp
byte[] bytes = "FOO".ToUtf8Bytes().Combine(" BAR".ToUtf8Bytes(), " BAZ".ToUtf8Bytes());
bytes.FromUtf8Bytes() //= FOO BAR BAZ
```

## Swagger

Swagger support has received a number of fixes and enhancements which now generates default params for DTO properties that aren't attributed with `[ApiMember]` attribute. Specifying a single `[ApiMember]` attribute reverts back to the existing behavior of only showing `[ApiMember]` DTO properties.

You can now Exclude **properties** from being listed in Swagger when using:

```csharp
[IgnoreDataMember]
```

Exclude **properties** from being listed in Swagger Schema Body with:

```csharp
[ApiMember(ExcludeInSchema=true)]
```

Or exclude entire Services from showing up in Swagger or any other Metadata Services (i.e. Metadata Pages, Postman, NativeTypes, etc) by annotating **Request DTO's** with:

```csharp
[Exclude(Feature.Metadata)]
```

## SOAP

There's finer-grain control available over which **Operations** and **Types** are exported in SOAP WSDL's and XSD's by overriding the new `ExportSoapOperationTypes()` and `ExportSoapType()` methods in your AppHost.

You can also exclude specific Request DTO's from being emitted in WSDL's and XSD's with:

```csharp
[Exclude(Feature.Soap)]
public class HiddenFromSoap { .. } 
```

You can also override and customize how the SOAP Message Responses are written, here's a basic example:

```csharp
public override WriteSoapMessage(Message message, Stream outputStream)
{
    using (var writer = XmlWriter.Create(outputStream, Config.XmlWriterSettings))
    {
        message.WriteMessage(writer);
    }
}
```

> The default [WriteSoapMessage](https://github.com/ServiceStack/ServiceStack/blob/fb08f5cb408ece66f203f677a4ec14ee9aad78ae/src/ServiceStack/ServiceStackHost.Runtime.cs#L484) implementation also raises a ServiceException and writes any returned response to a buffered Response Stream (if configured).

## Minor Enhancements

 - The Validation Features `AbstractValidator<T>` base class now implements `IRequiresRequest` and gets injected with the current Request
 - Response DTO is available at `IResponse.Dto`
 - Use `IResponse.ClearCookies()` to clear any cookies added during the Request pipeline before their written to the response
 - Use `[Exclude(Feature.Metadata)]` to hide Operations from being exposed in Metadata Services, inc. Metadata pages, Swagger, Postman, NativeTypes, etc
 - Use new `container.TryResolve(Type)` to resolve dependencies by runtime `Type`
 - New `debugEnabled` option added to `InMemoryLog` and `EventLogger`
 
### [Simple Customer REST Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/CustomerRestExample.cs)

New stand-alone [Customer REST Example](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/CustomerRestExample.cs) added showing an complete example of creating a Typed Client / Server REST Service with ServiceStack. Overview of this example was [answered on StackOverflow](http://stackoverflow.com/a/30273466/85785).

## Community 

 - [Jacob Foshee](https://twitter.com/82unpluggd) released a [custom short URLs ServiceStack Service](https://github.com/Durwella/UrlShortening) hosted on Azure.

## Breaking changes 

 - The provider ids for `InstagramOAuth2Provider` was renamed to `/auth/instagram` and `MicrosoftLiveOAuth2Provider` was renamed to `/auth/microsoftlive`.
 - All API's that return `HttpWebResponse` have been removed from `IServiceClient` and replaced with source-compatible extension methods 
 - `CryptUtils` have been deprecated and replaced with the more specific `RsaUtils`
 - `System.IO.Compression` functionality was removed from the PCL clients to workaround an [issue on Xamarin platforms](https://forums.servicestack.net/t/xamarin-ios-and-servicestack-dependency-issue/803/11)
 
All external NuGet packages were upgraded to their most recent major version (as done before every release).

# 4.0.40 Release Notes

## Native support for Java and Android Studio!

In our goal to provide a highly productive and versatile Web Services Framework that's ideal for services-heavy Mobile platforms, Service Oriented Architectures and Single Page Apps we're excited to announce new Native Types support for Java providing a terse and productive strong-typed Java API for the worlds most popular mobile platform - [Android](https://www.android.com/)!

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/android-studio-splash.png)

The new native Java types support for Android significantly enhances [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) support for mobile platforms to provide a productive dev workflow for mobile developers on the primary .NET, iOS and Java IDE's:

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/add-ss-reference-ides.png" align="right" />

#### [VS.NET integration with ServiceStackVS](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

Providing [C#](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference), [F#](https://github.com/ServiceStack/ServiceStack/wiki/FSharp-Add-ServiceStack-Reference), [VB.NET](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference) and [TypeScript](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference) Native Types support in Visual Studio for the [most popular platforms](https://github.com/ServiceStackApps/HelloMobile) including iOS and Android using [Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and [Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) on Windows.

#### [Xamarin Studio integration with ServiceStackXS](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio)

Providing [C# Native Types](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference) support for developing iOS and Android mobile Apps using [Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and [Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) with [Xamarin Studio](http://xamarin.com/studio) on OSX. The **ServiceStackXS** plugin also provides a rich web service development experience developing Client applications with [Mono Develop on Linux](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio-for-linux)

#### [Xcode integration with ServiceStackXC Plugin](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference)

Providing [Swift Native Types](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference) support for developing native iOS and OSX Applications with Xcode on OSX.

#### [Android Studio integration with ServiceStackIDEA](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

Providing Java Native Types support for developing pure cross-platform Java Clients or mobile Apps on the Android platform using Android Studio on both Windows and OSX.

## [ServiceStack IDEA Android Studio Plugin](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio)

Like the existing IDE integrations before it, the ServiceStack IDEA plugin provides Add ServiceStack Reference functionality to [Android Studio - the official Android IDE](https://developer.android.com/sdk/index.html). 

### Download and Install Plugin

The [ServiceStack AndroidStudio Plugin](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio) can be downloaded from the JetBrains plugins website at:

### [ServiceStackIDEA.zip](https://plugins.jetbrains.com/plugin/download?pr=androidstudio&updateId=19465)

After downloading the plugin above, install it in Android Studio by:

1. Click on `File -> Settings` in the Main Menu to open the **Settings Dialog**
2. Select **Plugins** settings screen
3. Click on **Install plugin from disk...** to open the **File Picker Dialog**
4. Browse and select the downloaded **ServiceStackIDEA.zip**
5. Click **OK** then Restart Android Studio

[![](https://github.com/ServiceStack/Assets/raw/34925d1b1b1b1856c451b0373139c939801d96ec/img/servicestackidea/android-plugin-install.gif)](https://plugins.jetbrains.com/plugin/7749?pr=androidstudio)

### Java Add ServiceStack Reference

If you've previously used [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) in any of the supported IDE's before, you'll be instantly familiar with Add ServiceStack Reference in Android Studio. The only additional field is **Package**, required in order to comply with Java's class definition rules. 

To add a ServiceStack Reference, right-click (or press `Ctrl+Alt+Shift+R`) on the **Package folder** in your Java sources where you want to add the POJO DTO's. This will bring up the **New >** Items Context Menu where you can click on the **ServiceStack Reference...** Menu Item to open the **Add ServiceStack Reference** Dialog: 

![Add ServiceStack Reference Java Context Menu](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-context-menu.png)

The **Add ServiceStack Reference** Dialog will be partially populated with the selected **Package** from where the Dialog was launched from and the **File Name** defaulting to `dto.java` where the Plain Old Java Object (POJO) DTO's will be added to. All that's missing is the url of the remote ServiceStack instance you wish to generate the DTO's for, e.g: `http://techstacks.io`:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackidea/android-dialog.png)

Clicking **OK** will add the `dto.java` file to your project and modifies the current Project's **build.gradle** file dependencies list with the new **net.servicestack:android** dependency containing the Java JSON ServiceClients which is used together with the remote Servers DTO's to enable its typed Web Services API:

![](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-dialog-example.gif)

> As the Module's **build.gradle** file was modified you'll need to click on the **Sync Now** link in the top yellow banner to sync the **build.gradle** changes which will install or remove any modified dependencies.

### Java Update ServiceStack Reference

Like other Native Type languages, the generated DTO's can be further customized by modifying any of the options available in the header comments:

```
/* Options:
Date: 2015-04-17 15:16:08
Version: 1
BaseUrl: http://techstacks.io

Package: org.layoric.myapplication
GlobalNamespace: techstackdtos
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*
*/
...
```

For example the package name can be changed by uncommenting the **Package:** option with the new package name, then either right-click on the file to bring up the file context menu or use Android Studio's **Alt+Enter** keyboard shortcut then click on **Update ServiceStack Reference** to update the DTO's with any modified options:

![](https://github.com/ServiceStack/Assets/raw/master/img/servicestackidea/android-update-example.gif)

### Java JsonServiceClient API
The goal of Native Types is to provide a productive end-to-end typed API to fascilitate consuming remote services with minimal effort, friction and cognitive overhead. One way we achieve this is by promoting a consistent, forwards and backwards-compatible message-based API that's works conceptually similar on every platform where each language consumes remote services by sending  **Typed DTO's** using a reusable **Generic Service Client** and a consistent client library API.

To maximize knowledge sharing between different platforms, the Java ServiceClient API is modelled after the [.NET Service Clients API](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) as closely as allowed within Java's language and idiomatic-style constraints. 

Thanks to C#/.NET being heavily inspired by Java, the resulting Java `JsonServiceClient` ends up bearing a close resemblance with .NET's Service Clients. The primary differences being due to language limitations like Java's generic type erasure and lack of language features like property initializers making Java slightly more verbose to work with, however as **Add ServiceStack Reference** is able to take advantage of code-gen we're able to mitigate most of these limitations to retain a familiar developer UX.

The [ServiceClient.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/ServiceClient.java) interface provides a good overview on the API available on the concrete [JsonServiceClient](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/JsonServiceClient.java) class:

```java
public interface ServiceClient {
    public <TResponse> TResponse get(IReturn<TResponse> request);
    public <TResponse> TResponse get(IReturn<TResponse> request, Map<String,String> queryParams);
    public <TResponse> TResponse get(String path, Class responseType);
    public <TResponse> TResponse get(String path, Type responseType);
    public HttpURLConnection get(String path);

    public <TResponse> TResponse post(IReturn<TResponse> request);
    public <TResponse> TResponse post(String path, Object request, Class responseCls);
    public <TResponse> TResponse post(String path, Object request, Type responseType);
    public <TResponse> TResponse post(String path, byte[] requestBody, String contentType, Class responseCls);
    public <TResponse> TResponse post(String path, byte[] requestBody, String contentType, Type responseType);
    public HttpURLConnection post(String path, byte[] requestBody, String contentType);

    public <TResponse> TResponse put(IReturn<TResponse> request);
    public <TResponse> TResponse put(String path, Object request, Class responseType);
    public <TResponse> TResponse put(String path, Object request, Type responseType);
    public <TResponse> TResponse put(String path, byte[] requestBody, String contentType, Class responseType);
    public <TResponse> TResponse put(String path, byte[] requestBody, String contentType, Type responseType);
    public HttpURLConnection put(String path, byte[] requestBody, String contentType);

    public <TResponse> TResponse delete(IReturn<TResponse> request);
    public <TResponse> TResponse delete(IReturn<TResponse> request, Map<String,String> queryParams);
    public <TResponse> TResponse delete(String path, Class responseType);
    public <TResponse> TResponse delete(String path, Type responseType);
    public HttpURLConnection delete(String path);
}
```

The primary concession is due to Java's generic type erasure which forces the addition overloads that include a `Class` parameter for specifying the response type to deserialize into as well as a `Type` parameter overload which does the same for generic types. These overloads aren't required for API's that accept a Request DTO annotated with `IReturn<T>` interface marker as we're able to encode the Response Type in code-generated Request DTO classes.

### Java JsonServiceClient Usage
To get started you'll just need an instance of `JsonServiceClient` initialized with the **BaseUrl** of the remote ServiceStack instance you want to access, e.g:

```java
JsonServiceClient client = new JsonServiceClient("http://techstacks.io");
```

> The JsonServiceClient is made available after the [net.servicestack:android](https://bintray.com/servicestack/maven/ServiceStack.Android/view) package is automatically added to your **build.gradle** when adding a ServiceStack reference.

Typical usage of the Service Client is the same in .NET where you just need to send a populated Request DTO and the Service Client will return a populated Response DTO, e.g:

```java
AppOverviewResponse r = client.get(new AppOverview());

ArrayList<Option> allTiers = r.getAllTiers();
ArrayList<TechnologyInfo> topTech = r.getTopTechnologies();
```

As Java doesn't have type inference you'll need to specify the Type when declaring a variable. Whilst the public instance fields of the Request and Response DTO's are accessible directly, the convention in Java is to use the **property getters and setters** that are automatically generated for each DTO property as seen above.

### Custom Example Usage

We'll now go through some of the other API's to give you a flavour of what's available. When preferred you can also consume Services using a custom route by supplying a string containing the route and/or Query String. As no type info is available you'll need to specify the Response DTO class to deserialize the response into, e.g:

```java
OverviewResponse response = client.get("/overview", OverviewResponse.class);
```

The path can either be a relative or absolute url in which case the **BaseUrl** is ignored and the full absolute url is used instead, e.g:

```java
OverviewResponse response = client.get("http://techstacks.io/overview", OverviewResponse.class);
```

When initializing the Request DTO you can take advantage of the generated setters which by default return `this` allowing them to be created and chained in a single expression, e.g:

```java
GetTechnology request = new GetTechnology()
	.setSlug("servicestack");

GetTechnologyResponse response = client.get(request);
```

### AutoQuery Example Usage

You can also send requests composed of both a Typed DTO and untyped String Dictionary by providing a Java Map of additional args. This is typically used when querying [implicit conventions in AutoQuery services](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#implicit-conventions), e.g:

```java
QueryResponse<Technology> response = client.get(new FindTechnologies(),
	Utils.createMap("DescriptionContains","framework"));
```

The `Utils.createMap()` API is included in the [Utils.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Utils.java) static class which contains a number of helpers to simplify common usage patterns and reduce the amount of boiler plate required for common tasks, e.g they can be used to simplify reading raw bytes or raw String from a HTTP Response. Here's how you can download an image bytes using a custom `JsonServiceClient` HTTP Request and load it into an Android Image `Bitmap`:

```java
HttpURLConnection httpRes = client.get("https://servicestack.net/img/logo.png");
byte[] imgBytes = Utils.readBytesToEnd(httpRes);
Bitmap img = BitmapFactory.decodeByteArray(imgBytes, 0, imgBytes.length);
```

### AndroidServiceClient
Unlike .NET, Java doesn't have an established Async story or any language support that simplifies execution and composition of Async tasks, as a result the Async story on Android is fairly fragmented with multiple options built-in for executing non-blocking tasks on different threads including:

 - Thread
 - Executor
 - HandlerThread
 - AsyncTask
 - Service
 - IntentService
 - AsyncQueryHandler
 - Loader

JayWay's Oredev presentation on [Efficient Android Threading](http://www.slideshare.net/andersgoransson/efficient-android-threading) provides a good overview of the different threading strategies above with their use-cases, features and pitfalls. Unfortunately none of the above options enable a Promise/Future-like API which would've been ideal in maintaining a consistent Task-based Async API across all ServiceStack Clients. Of all the above options the new Android [AsyncTask](http://developer.android.com/reference/android/os/AsyncTask.html) ended up the most suitable option, requiring the least effort for the typical Service Client use-case of executing non-blocking WebService Requests and having their results called back on the Main UI thread.

### AsyncResult
To enable an even simpler Async API decoupled from Android, we've introduced a higher-level [AsyncResult](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/AsyncResult.java) class which allows capturing of Async callbacks using an idiomatic anonymous Java class. `AsyncResult` is modelled after [jQuery.ajax](http://api.jquery.com/jquery.ajax/) and allows specifying **success()**, **error()** and **complete()** callbacks as needed.

### AsyncServiceClient API

Using AsyncResult lets us define a pure Java [AsyncServiceClient](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/AsyncServiceClient.java) interface that's decoupled from any specific threading implementation, i.e:

```java
public interface AsyncServiceClient {
    public <T> void getAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void getAsync(IReturn<T> request, final Map<String, String> queryParams, final AsyncResult<T> asyncResult);
    public <T> void getAsync(String path, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void getAsync(String path, final Type responseType, final AsyncResult<T> asyncResult);
    public void getAsync(String path, final AsyncResult<byte[]> asyncResult);

    public <T> void postAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final Object request, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final Object request, final Type responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final byte[] requestBody, final String contentType, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void postAsync(String path, final byte[] requestBody, final String contentType, final Type responseType, final AsyncResult<T> asyncResult);
    public void postAsync(String path, final byte[] requestBody, final String contentType, final AsyncResult<byte[]> asyncResult);

    public <T> void putAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final Object request, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final Object request, final Type responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final byte[] requestBody, final String contentType, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void putAsync(String path, final byte[] requestBody, final String contentType, final Type responseType, final AsyncResult<T> asyncResult);
    public void putAsync(String path, final byte[] requestBody, final String contentType, final AsyncResult<byte[]> asyncResult);

    public <T> void deleteAsync(IReturn<T> request, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(IReturn<T> request, final Map<String, String> queryParams, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(String path, final Class responseType, final AsyncResult<T> asyncResult);
    public <T> void deleteAsync(String path, final Type responseType, final AsyncResult<T> asyncResult);
    public void deleteAsync(String path, final AsyncResult<byte[]> asyncResult);
}
```

The `AsyncServiceClient` interface is implemented by the `AndroidServiceClient` concrete class which behind-the-scenes uses an Android [AsyncTask](http://developer.android.com/reference/android/os/AsyncTask.html) to implement its Async API's. 

Whilst the `AndroidServiceClient` is contained in the **net.servicestack:android** dependency and only works in Android, the `JsonServiceClient` instead is contained in a seperate pure Java **net.servicestack:client** dependency which can be used independently to provide a typed Java API for consuming ServiceStack Services from any Java application.

### Async API Usage
To make use of Async API's in an Android App (which you'll want to do to keep web service requests off the Main UI thread), you'll instead need to use an instance of `AndroidServiceClient` which as it inherits `JsonServiceClient` can be used to perform both Sync and Async requests:
```java
AndroidServiceClient client = new AndroidServiceClient("http://techstacks.io");
```

Like other Service Clients, there's an equivalent Async API matching their Sync counterparts which differs by ending with an **Async** suffix that instead of returning a typed response, fires a **success(TResponse)** or **error(Exception)** callback with the typed response, e.g: 
```java
client.getAsync(new AppOverview(), new AsyncResult<AppOverviewResponse>(){
    @Override
    public void success(AppOverviewResponse r) {
        ArrayList<Option> allTiers = r.getAllTiers();
        ArrayList<TechnologyInfo> topTech = r.getTopTechnologies();
    }
});
```

Which just like the `JsonServiceClient` examples above also provide a number of flexible options to execute Custom Async Web Service Requests, e.g: 
```java
client.getAsync("/overview", OverviewResponse.class, new AsyncResult<OverviewResponse>(){
    @Override
    public void success(OverviewResponse response) {
    }
});
```

Example calling a Web Service with an absolute url:
```java
client.getAsync("http://techstacks.io/overview", OverviewResponse.class, new AsyncResult<OverviewResponse>() {
    @Override
    public void success(OverviewResponse response) {
    }
});
```

#### Async AutoQuery Example
Example calling an untyped AutoQuery Service with additional Dictionary String arguments:
```java
client.getAsync(request, Utils.createMap("DescriptionContains", "framework"),
    new AsyncResult<QueryResponse<Technology>>() {
        @Override
        public void success(QueryResponse<Technology> response) {
        }
    });
```

#### Download Raw Image Async Example
Example downloading raw Image bytes and loading it into an Android Image `Bitmap`:
```java
client.getAsync("https://servicestack.net/img/logo.png", new AsyncResult<byte[]>() {
    @Override
    public void success(byte[] imgBytes) {
        Bitmap img = BitmapFactory.decodeByteArray(imgBytes, 0, imgBytes.length);
    }
});
```

### Typed Error Handling
Thanks to Java also using typed Exceptions for error control flow, error handling in Java will be intantly familiar to C# devs which also throws a typed `WebServiceException` containing the remote servers structured error data:

```java
ThrowType request = new ThrowType()
    .setType("NotFound")
    .setMessage("not here");

try {
	ThrowTypeResponse response = testClient.post(request);
}
catch (WebServiceException webEx) {
    ResponseStatus status = thrownError.getResponseStatus();
	status.getMessage();    //= not here
    status.getStackTrace(); //= (Server StackTrace)
}
```

Likewise structured Validation Field Errors are also accessible from the familar `ResponseStatus` DTO, e.g:
```java
ThrowValidation request = new ThrowValidation()
    .setEmail("invalidemail");

try {
    client.post(request);
} catch (WebServiceException webEx){
    ResponseStatus status = webEx.getResponseStatus();

    ResponseError firstError = status.getErrors().get(0);
    firstError.getErrorCode(); //= InclusiveBetween
    firstError.getMessage();   //= 'Age' must be between 1 and 120. You entered 0.
    firstError.getFieldName(); //= Age
}
```

#### Async Error Handling
Async Error handling differs where in order to access the `WebServiceException` you'll need to implement the **error(Exception)** callback, e.g:
```java
client.postAsync(request, new AsyncResult<ThrowTypeResponse>() {
    @Override
    public void error(Exception ex) {
        WebServiceException webEx = (WebServiceException)ex;
        
        ResponseStatus status = thrownError.getResponseStatus();
        status.getMessage();    //= not here
        status.getStackTrace(); //= (Server StackTrace)
    }
});
```

Async Validation Errors are also handled in the same way: 
```java
client.postAsync(request, new AsyncResult<ThrowValidationResponse>() {
    @Override
    public void error(Exception ex) {
        WebServiceException webEx = (WebServiceException)ex;
        ResponseStatus status = webEx.getResponseStatus();

        ResponseError firstError = status.getErrors().get(0);
        firstError.getErrorCode(); //= InclusiveBetween
        firstError.getMessage();   //= 'Age' must be between 1 and 120. You entered 0.
        firstError.getFieldName(); //= Age
    }
}
```

### JsonServiceClient Error Handlers
To make it easier to generically handle Web Service Exceptions, the Java Service Clients also support static Global Exception handlers by assigning `AndroidServiceClient.GlobalExceptionFilter`, e.g:
```java
AndroidServiceClient.GlobalExceptionFilter = new ExceptionFilter() {
    @Override
    public void exec(HttpURLConnection res, Exception ex) {
    	//...
    }
};
```

As well as local Exception Filters by specifying a handler for `client.ExceptionFilter`, e.g:
```java
client.ExceptionFilter = new ExceptionFilter() {
    @Override
    public void exec(HttpURLConnection res, Exception ex) {
    	//...
    }
};
```

## Introducing [TechStacks Android App](https://github.com/ServiceStackApps/TechStacksAndroidApp)
To demonstrate Java Native Types in action we've ported the Swift [TechStacks iOS App](https://github.com/ServiceStackApps/TechStacksApp) to a native Java Android App to showcase the responsiveness and easy-of-use of leveraging Java Add ServiceStack Reference in Android Projects. 

The Android TechStacks App can be [downloaded for free from the Google Play Store](https://play.google.com/store/apps/details?id=servicestack.net.techstacks):

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-android-app.jpg)](https://play.google.com/store/apps/details?id=servicestack.net.techstacks)

### Data Binding
As there's no formal data-binding solution in Android we've adopted a lightweight iOS-inspired [Key-Value-Observable-like data-binding solution](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference#observing-data-changes) in Android TechStacks in order to maximize knowledge-sharing and ease porting between native Swift iOS and Java Android Apps. 

Similar to the Swift TechStacks iOS App, all web service requests are encapsulated in a single [App.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/techstacks/src/main/java/servicestack/net/techstacks/App.java) class and utilizes Async Service Client API's in order to maintain a non-blocking and responsive UI. 

### Registering for Data Updates
In iOS, UI Controllers register for UI and data updates by implementing `*DataSource` and `*ViewDelegate` protocols, following a similar approach, Android Activities and Fragments register for Async Data callbacks by implementing the Custom interface `AppDataListener` below:

```java
public static interface AppDataListener
{
    public void onUpdate(AppData data, DataType dataType);
}
```

Where Activities or Fragments can then register itself as a listener when they're first created:
```java
@Override
public void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    App.getData().addListener(this);
}
```

### Data Binding Async Service Responses
Then in `onCreateView` MainActivity calls the `AppData` singleton to fire off all async requests required to populate it's UI:
```java
public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle state) {
    App.getData().loadAppOverview();
    ...
}
```

Where `loadAppOverview()` makes an async call to the `AppOverview` Service, storing the result in an AppData instance variable before notifying all registered listeners that `DataType.AppOverview` has been updated:
```java
public AppData loadAppOverview(){
    client.getAsync(new AppOverview(), new AsyncResult<AppOverviewResponse>() {
        @Override
        public void success(AppOverviewResponse response){
            appOverviewResponse = response;
            onUpdate(DataType.AppOverview);
        }
    });
    return this;
}
```
> Returning `this` allows expression chaining, reducing the boilerplate required to fire off multiple requests

Calling `onUpdate()` simply invokes the list of registered listeners with itself and the  enum DataType of what was changed, i.e:
```java
public void onUpdate(DataType dataType){
    for (AppDataListener listener : listeners){
        listener.onUpdate(this, dataType);
    }
}
```

The Activity can then update its UI within the `onUpdate()` callback by re-binding its UI Controls when relevant data has changed, in this case when `AppOverview` response has returned:
```java
@Override
public void onUpdate(App.AppData data, App.DataType dataType) {
    switch (dataType) {
        case AppOverview:
            Spinner spinner = (Spinner)getActivity().findViewById(R.id.spinnerCategory);
            ArrayList<String> categories = map(data.getAppOverviewResponse().getAllTiers(), 
                new Function<Option, String>() {
                    @Override public String apply(Option option) {
                        return option.getTitle();
                    }
                });
            spinner.setAdapter(new ArrayAdapter<>(getActivity(),
                android.R.layout.simple_spinner_item, categories));

            ListView list = (ListView)getActivity().findViewById(R.id.listTopRated);
            ArrayList<String> topTechnologyNames = map(getTopTechnologies(data),
                new Function<TechnologyInfo, String>() {
                    @Override public String apply(TechnologyInfo technologyInfo) {
                        return technologyInfo.getName() + " (" + technologyInfo.getStacksCount() + ")";
                    }
                });
            list.setAdapter(new ArrayAdapter<>(getActivity(),
                android.R.layout.simple_list_item_1, topTechnologyNames));
            break;
    }
}
```

In this case the `MainActivity` home screen re-populates the Technology Category **Spinner** (aka Picker) and the Top Technologies **ListView** controls by assigning a new Android `ArrayAdapter`. 

### Functional Java Utils
The above example also introduces the `map()` functional util we've also included in the **net.servicestack:client** dependency to allow usage of Functional Programming techniques to transform, query and filter data given Android's Java 7 lack of any language or library support for Functional Programming itself. Unfortunately lack of closures in Java forces more boilerplate than otherwise would be necessary as it needs to fallback to use anonymous Type classes to capture delegates. Android Studio also recognizes this pattern as unnecessary noise and will automatically collapse the code into a readable closure syntax, with what the code would've looked like had Java supported closures, e.g:

![Android Studio Collapsed Closure](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/androidstudio-collapse-closure.png)

### [Func.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Func.java) API

The [Func.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/Func.java) static class contains a number of common functional API's providing a cleaner and more robust alternative to working with Data than equivalent imperative code. We can take advantage of **static imports** in Java to import the namespace of all utils with the single import statement below:
```java
import static net.servicestack.client.Func.*;
```

Which will let you reference all the Functional utils below without a Class prefix:
```java
ArrayList<R> map(Iterable<T> xs, Function<T,R> f)
ArrayList<T> filter(Iterable<T> xs, Predicate<T> predicate)
void each(Iterable<T> xs, Each<T> f)
T first(Iterable<T> xs)
T first(Iterable<T> xs, Predicate<T> predicate)
T last(Iterable<T> xs)
T last(Iterable<T> xs, Predicate<T> predicate)
boolean contains(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> skip(Iterable<T> xs, int skip)
ArrayList<T> skip(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> take(Iterable<T> xs, int take)
ArrayList<T> take(Iterable<T> xs, Predicate<T> predicate)
boolean any(Iterable<T> xs, Predicate<T> predicate)
boolean all(Iterable<T> xs, Predicate<T> predicate)
ArrayList<T> expand(Iterable<T>... xss)
T elementAt(Iterable<T> xs, int index)
ArrayList<T> reverse(Iterable<T> xs)
reduce(Iterable<T> xs, E initialValue, Reducer<T,E> reducer)
E reduceRight(Iterable<T> xs, E initialValue, Reducer<T,E> reducer)
String join(Iterable<T> xs, String separator)
ArrayList<T> toList(Iterable<T> xs)
```

### Images and Custom Binary Requests
The TechStacks Android App also takes advantage of the Custom Service Client API's to download images asynchronously. As images can be fairly resource and bandwidth intensive they're stored in a simple Dictionary Cache to minimize any unnecessary CPU and network resources, i.e:
```java
HashMap<String,Bitmap> imgCache = new HashMap<>();
public void loadImage(final String imgUrl, final ImageResult callback) {
    Bitmap img = imgCache.get(imgUrl);
    if (img != null){
        callback.success(img);
        return;
    }

    client.getAsync(imgUrl, new AsyncResult<byte[]>() {
        @Override
        public void success(byte[] imgBytes) {
            Bitmap img = AndroidUtils.readBitmap(imgBytes);
            imgCache.put(imgUrl, img);
            callback.success(img);
        }
    });
}
```

The TechStacks App uses the above API to download screenshots and load their Bitmaps in `ImageView` UI Controls, e.g:

```java
String imgUrl = result.getScreenshotUrl();
final ImageView img = (ImageView)findViewById(R.id.imgTechStackScreenshotUrl);
data.loadImage(imgUrl, new App.ImageResult() {
    @Override public void success(Bitmap response) {
        img.setImageBitmap(response);
    }
});
```

## Java generated DTO Types

Our goal with **Java Add ServiceStack Reference** is to ensure a high-fidelity, idiomatic translation within the constraints of Java language and its built-in libraries, where .NET Server DTO's are translated into clean, conventional Java POJO's where .NET built-in Value Types mapped to their equivalent Java data Type.

To see what this ends up looking up we'll go through some of the [Generated Test Services](http://test.servicestack.net/types/java) to see how they're translated in Java.

### .NET Attributes translated into Java Annotations
By inspecting the `HelloAllTypes` Request DTO we can see that C# Metadata Attributes e.g. `[Route("/all-types")]` are also translated into the typed Java Annotations defined in the **net.servicestack:client** dependency. But as Java only supports defining a single Annotation of the same type, any subsequent .NET Attributes of the same type are emitted in comments.

### Terse, typed API's with IReturn interfaces
Java Request DTO's are also able to take advantage of the `IReturn<TResponse>` interface marker to provide its terse, typed generic API but due to Java's Type erasure the Response Type also needs to be encoded in the Request DTO as seen by the `responseType` field and `getResponseType()` getter:

```java
@Route("/all-types")
public static class HelloAllTypes implements IReturn<HelloAllTypesResponse>
{
    public String name = null;
    public AllTypes allTypes = null;
    
    public String getName() { return name; }
    public HelloAllTypes setName(String value) { this.name = value; return this; }
    public AllTypes getAllTypes() { return allTypes; }
    public HelloAllTypes setAllTypes(AllTypes value) { this.allTypes = value; return this; }

    private static Object responseType = HelloAllTypesResponse.class;
    public Object getResponseType() { return responseType; }
}
```

### Getters and Setters generated for each property
Another noticable feature is the Java getters and setters property convention are generated for each public field with setters returning itself allowing for multiple setters to be chained within a single expression. 

To comply with Gson JSON Serialization rules, the public DTO fields are emitted in the same JSON naming convention as the remote ServiceStack server which for the [test.servicestack.net](http://test.servicestack.net) Web Services, follows its **camelCase** naming convention that is configured in its AppHost with: 
```csharp
JsConfig.EmitCamelCaseNames = true;
```

Whilst the public fields match the remote server JSON naming convention, the getters and setters are always emitted in Java's **camelCase** convention to maintain a consistent API irrespective of the remote server configuration. To minimize API breakage they should be the preferred method to access DTO fields.

### Java Type Converions
By inspecting the `AllTypes` DTO fields we can see what Java Type each built-in .NET Type gets translated into. In each case it selects the most suitable concrete Java datatype available, inc. generic collections. We also see only reference types are used (i.e. instead of their primitive types equivalents) since DTO properties are optional and need to be nullable. 
```java
public static class AllTypes
{
    public Integer id = null;
    public Integer nullableId = null;
    @SerializedName("byte") public Short Byte = null;
    @SerializedName("short") public Short Short = null;
    @SerializedName("int") public Integer Int = null;
    @SerializedName("long") public Long Long = null;
    public Integer uShort = null;
    public Long uInt = null;
    public BigInteger uLong = null;
    @SerializedName("float") public Float Float = null;
    @SerializedName("double") public Double Double = null;
    public BigDecimal decimal = null;
    public String string = null;
    public Date dateTime = null;
    public TimeSpan timeSpan = null;
    public Date dateTimeOffset = null;
    public UUID guid = null;
    @SerializedName("char") public String Char = null;
    public Date nullableDateTime = null;
    public TimeSpan nullableTimeSpan = null;
    public ArrayList<String> stringList = null;
    public ArrayList<String> stringArray = null;
    public HashMap<String,String> stringMap = null;
    public HashMap<Integer,String> intStringMap = null;
    public SubType subType = null;
    ...
}
```

The only built-in Value Type that didn't have a suitable built-in Java equivalent was `TimeSpan`. In this case it uses our new [TimeSpan.java](https://github.com/ServiceStack/ServiceStack.Java/blob/master/src/AndroidClient/client/src/main/java/net/servicestack/client/TimeSpan.java) class which implements the same familiar API available in .NET's `TimeSpan`. 

Something else you'll notice is that some fields are annotated with the `@SerializedName()` Gson annotation. This is automatically added for Java keywords - required since Java doesn't provide anyway to escape keyword identifiers. The first time a Gson annotation is referenced it also automatically includes the required Gson namespace imports. If needed, this can also be explicitly added by with:
```java
JavaGenerator.AddGsonImport = true;
```

### Java Enums
.NET enums are also translated into typed Java enums where basic enums end up as a straight forward transaltion, e.g:
```java
public static enum BasicEnum
{
    Foo,
    Bar,
    Baz;
}
```

Whilst as Java doesn't support integer Enum flags directly the resulting translation ends up being a bit more convoluted:
```java
@Flags()
public static enum EnumFlags
{
    @SerializedName("1") Value1(1),
    @SerializedName("2") Value2(2),
    @SerializedName("4") Value3(4);

    private final int value;
    EnumFlags(final int intValue) { value = intValue; }
    public int getValue() { return value; }
}
```

## Java Native Types Customization
The header comments in the generated DTO's allows for further customization of how the DTO's are generated which can then be updated with any custom Options provided using the **Update ServiceStack Reference** Menu Item in Android Studio. Options that are preceded by a single line Java comment `//` are defaults from the server which can be overridden.

To override a value, remove the `//` and specify the value to the right of the `:`. Any value uncommented will be sent to the server to override any server defaults.
```java
/* Options:
Date: 2015-04-10 12:41:14
Version: 1
BaseUrl: http://techstacks.io

Package: net.servicestack.techstacks
//GlobalNamespace: dto
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*
*/
```
We'll go through and cover each of the above options to see how they affect the generated DTO's:

### Package
Specify the package name that the generated DTO's are in:
```
Package: net.servicestack.techstacks
```
Will generate the package name for the generated DTO's as:
```java
package net.servicestack.techstacks;
```

### GlobalNamespace
Change the name of the top-level Java class containar that all static POJO classes are generated in, e.g changing the `GlobalNamespace` to:
```
GlobalNamespace: techstacksdto
```
Will change the name of the top-level class to `techstacksdto`, e.g:
```java
public class techstacksdto
{
    ...
}
```
Where all static DTO classes can be imported using the wildcard import below:
```java
import net.servicestack.techstacksdto.*;
```

### AddPropertyAccessors
By default **getters** and **setters** are generated for each DTO property, you can prevent this default with:
```
AddPropertyAccessors: false
```
Which will no longer generate any property accessors, leaving just public fields, e.g:
```java
public static class AppOverviewResponse
{
    public Date Created = null;
    public ArrayList<Option> AllTiers = null;
    public ArrayList<TechnologyInfo> TopTechnologies = null;
    public ResponseStatus ResponseStatus = null;
}
```

### SettersReturnThis
To allow for chaining DTO field **setters** returns itself by default, this can be changed to return `void` with:
```
SettersReturnThis: false
```
Which will change the return type of each setter to `void`:
```java
public static class GetTechnology implements IReturn<GetTechnologyResponse>
{
    public String Slug = null;
    
    public String getSlug() { return Slug; }
    public void setSlug(String value) { this.Slug = value; }
}
```

### AddServiceStackTypes
Lets you exclude built-in ServiceStack Types and DTO's from being generated with:
```
AddServiceStackTypes: false
```
This will prevent Request DTO's for built-in ServiceStack Services like `Authenticate` from being emitted.

### AddImplicitVersion
Lets you specify the Version number to be automatically populated in all Request DTO's sent from the client:
```
AddImplicitVersion: 1
```
Which will embed the specified Version number in each Request DTO, e.g:
```java
public static class GetTechnology implements IReturn<GetTechnologyResponse>
{
    public Integer Version = 1;
    public Integer getVersion() { return Version; }
    public GetTechnology setVersion(Integer value) { this.Version = value; return this; }
}
```
This lets you know what Version of the Service Contract that existing clients are using making it easy to implement [ServiceStack's recommended versioning strategy](http://stackoverflow.com/a/12413091/85785).

### IncludeTypes
Is used as a Whitelist that can be used to specify only the types you would like to have code-generated:
```
/* Options:
IncludeTypes: GetTechnology,GetTechnologyResponse
```
Will only generate `GetTechnology` and `GetTechnologyResponse` DTO's, e.g:
```java
public class dto
{
    public static class GetTechnologyResponse { ... }
    public static class GetTechnology implements IReturn<GetTechnologyResponse> { ... }
}
```

### ExcludeTypes
Is used as a Blacklist where you can specify which types you would like to exclude from being generated:
```
/* Options:
ExcludeTypes: GetTechnology,GetTechnologyResponse
```
Will exclude `GetTechnology` and `GetTechnologyResponse` DTO's from being generated.

### DefaultImports
Lets you override the default import packages included in the generated DTO's:
```
java.math.*,java.util.*,net.servicestack.client.*,com.acme.custom.*
```
Will override the default imports with the ones specified, i.e: 
```java
import java.math.*;
import java.util.*;
import net.servicestack.client.*;
import com.acme.custom.*;
```

By default the generated DTO's do not require any Google's Gson-specific serialization hints, but when they're needed e.g. if your DTO's use Java keywords or are attributed with `[DataMember(Name=...)]` the required Gson imports are automatically added which can also be added explicitly with:
```csharp
JavaGenerator.AddGsonImport = true;
```
Which will add the following Gson imports:
```java
import com.google.gson.annotations.*;
import com.google.gson.reflect.*;
```

## ServiceStack Customer Forums moved to Discourse
The ServiceStack Customer Forums have been moved from **Google+** over to [Discourse](http://discourse.org/) which provides better readability, richer markup, support for code samples, better searching and discoverability, etc - basically an overall better option for providing support than Google+ was. The new Customer Forums is available at: 
### https://forums.servicestack.net

ServiceStack Customers will be able to register as a new user by using the same email that's registered in your ServiceStack account or added as a support contact at: http://servicestack.net/account/support

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestack-forums.jpg)](https://forums.servicestack.net/)

## Swift Native Types upgraded to Swift 1.2
The latest stable release of **Xcode 6.3** includes the new Swift 1.2 release that had a number of breaking language changes from the previous version. In this release both the Swift generated types in ServiceStack and the [JsonServiceClient.swift](https://github.com/ServiceStack/ServiceStack.Swift/blob/master/dist/JsonServiceClient.swift) client library have been upgraded to support Swift 1.2 language changes.

Whilst the latest version of Swift fixed a number of stability issues, it also introduced some regressions. Unfortunately one of these regressions affected extensions on generic types that also have `typealias` like what's used when generating generic type responses like the `QueryResponse<T>` that's used in AutoQuery Services. We've submitted a failing test case for this issue with Apple and hopefully it will get resolved in a future release of Swift.

## OrmLite

### Merge Disconnected POCO Data Sets
The new `Merge` extension method can stitch disconnected POCO collections together as per their relationships defined in [OrmLite's POCO References](https://github.com/ServiceStack/ServiceStack.OrmLite#reference-support-poco-style).

For example you can select a collection of Customers who've made an order with quantities of 10 or more and in a separate query select their filtered Orders and then merge the results of these 2 distinct queries together with:
```csharp
//Select Customers who've had orders with Quantities of 10 or more
List<Customer> customers = db.Select<Customer>(q =>
    q.Join<Order>()
     .Where<Order>(o => o.Qty >= 10)
     .SelectDistinct());

//Select Orders with Quantities of 10 or more
List<Order> orders = db.Select<Order>(o => o.Qty >= 10);

customers.Merge(orders); // Merge disconnected Orders with their related Customers

customers.PrintDump();   // Print merged customers and orders datasets
```

### New Multiple Select API's
Add new multi `Select<T1,T2>` and `Select<T1,T2,T3>` select overloads to allow selecting fields from multiple tables, e.g:
```csharp
var q = db.From<FooBar>()
    .Join<BarJoin>()
    .Select<FooBar, BarJoin>((f, b) => new { f.Id, b.Name });

Dictionary<int,string> results = db.Dictionary<int, string>(q);
```

### New OrmLite Naming Strategy
The new `LowercaseUnderscoreNamingStrategy` can be enabled with:
```csharp
OrmLiteConfig.DialectProvider.NamingStrategy = new LowercaseUnderscoreNamingStrategy();
```
### New Signed MySql NuGet Package
Add new **OrmLite.MySql.Signed** NuGet package containing signed MySql versions of .NET 4.0 and .NET 4.5 builds of MySql

## ServiceStack Changes
This release also saw a number of minor changes and enhancements added throughout the ServiceStack Framework libraries which are listed below, grouped under their related sections:

### [ServerEvents](https://github.com/ServiceStack/ServiceStack/wiki/Server-Events)
- ServerEvents Heartbeat is now disabled when underlying `$.ss.eventSource` EventSource is closed 
- `ServerEventFeature.OnCreated` callback can now be used to short-circuit ServerEvent connections with `httpReq.EndResponse()`
- Dropped connections are automatically restarted in C#/.NET `ServerEventsClient`
- Added new `IEventSubscription.UserAddress` field containing IP Address of ServerEvents Client
- ServerEvent requests are now verified that they're made from the original IP Address and returns `403 Forbidden` when invalid. IP Address Validation can be disabled with: `ServerEventsFeature.ValidateUserAddress = false`

#### New Session and Auth API's
- New `FourSquareOAuth2Provider` added by [@kevinhoward](https://github.com/kevinhoward)
- New `IResponse.DeleteSessionCookies()` extension method can be used delete existing Session Cookies
- New `ISession.Remove(key)` and `ISession.RemoveAll()` API's added on `ISession` Bag
- Implemented `IRemoveByPattern.RemoveByPattern(pattern)` on `OrmLiteCacheClient`
- Added new `IUserAuthRepository.DeleteUserAuth()` API and corresponding implementations in all **AuthRepository** providers
- A new .NET 4.5 release of `RavenDbUserAuthRepository` using the latest Raven DB Client libraries is available in the **ServiceStack.Authentication.RavenDb** NuGet package - added by [@kevinhoward](https://github.com/kevinhoward)

#### Session and Auth Changes
- Session Cookie Identifiers are now automatically deleted on Logout (i.e. `/auth/logout`). Can be disabled with `AuthFeature.DeleteSessionCookiesOnLogout = false`
- Auth now uses `SetParam` instead of `AddParam` to override existing QueryString variables in Redirect Urls (i.e. instead of appending to them to the end of the urls)
- Added new `AppHost.TestMode` to allow functionality during testing that's disabled in release mode. For example registering a `AuthUserSession` in the IOC is now disabled by default (as it's only hydrated from Cache not IOC). Can be enabled to simplify testing with `AppHost.TestMode = true`.

### New Generic Logger implementation 
- A new `GenericLogFactory` and `GenericLogger` implementations were added to simplify creation of new `ILog` providers. For example you can create and register a new custom logging implementation to redirect logging to an Xamarin.Android UI Label control with:

#### Android UI Logger Example
```csharp
LogManager.LogFactory = new GenericLogFactory(message => {
    RunOnUiThread(() => {
        lblResults.Text = "{0}  {1}\n".Fmt(DateTime.Now.ToLongTimeString(), message) + lblResults.Text;
    });
});
```

### New WebService Framework API's
- All magic keyword constants used within ServiceStack can now be overridden by reassinging them in the new static `Keywords` class
- Added new `IResponse.Request` property allowing access to `IRequest` from all `IResponse` instances
- Added new `HttpError.Forbidden(message)` convenience method
- Added new virtual `AppHost.ExecuteMessage(IMessage)` API's to be able to override default MQ ExecuteMessage impl
- Added explicit `IVirtual.Referesh()` API to force refresh of underlying `FileInfo` stats
- Added new `Xamarin.Mac20` NuGet profile to support **Xamarin.Mac Unified API** Projects 

### WebService Framework Changes
- Improve performance of processing HTTP Partial responses by using a larger and reusable `byte[]` buffer. The size of buffer used can be customized with: `HttpResultUtils.PartialBufferSize = 32 * 1024`
- Service `IDisposable` dependencies are now immediately released after execution
- Added support for case-insensitive Content-Type's 

### [Auto Batched Requests](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Batched-Requests)
- Added support for Async API's in Auto-Batched Requests
- A new `X-AutoBatch-Completed` HTTP Response Header is added to all Auto-Batched HTTP Responses containing number of individual requests completed

### [Metadata](https://github.com/ServiceStack/ServiceStack/wiki/Metadata-page)
- Added `[Restrict(VisibilityTo = RequestAttributes.None)]` to Postman and Swagger Request DTO's to hide their routes from appearing on metadata pages
- `PreRequestFilters` are now executed in Metadata Page Handlers 

### [Mini Profiler](https://github.com/ServiceStack/ServiceStack/wiki/Built-in-profiling)
- Added support of **Async OrmLite requests** in MiniProfiler
- The Values of Parameterized queries are now shown in MiniProfiler

### [AppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings)
- Added new `AppSettingsBase.Get<T>(name)` API to all [AppSettings providers](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings)

### [Routing](https://github.com/ServiceStack/ServiceStack/wiki/Routing)
- Weighting of Routes can now be customized with the new `RestPath.CalculateMatchScore` delegate

### [Swagger API](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)
- Updated to the latest [Swagger API](https://github.com/ServiceStack/ServiceStack/wiki/Swagger-API)

## ServiceStack.Redis
New Redis Client API's added by [@andyberryman](https://github.com/andyberryman):
```csharp
public interface IRedisClient
{
    ...
    long StoreIntersectFromSortedSets(string intoSetId, string[] setIds, string[] args);
    long StoreUnionFromSortedSets(string intoSetId, string[] setIds, string[] args);
}

public interface IRedisTypedClient<T> 
{
    ...
    long StoreIntersectFromSortedSets(IRedisSortedSet<T> setId, IRedisSortedSet<T>[] setIds, string[] args);
    long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args);
}
```

Added support for `Dictionary<string,string>` API's in `IRedisQueueableOperation` which now allows execution of Dictionary API's in Redis Transactions, e.g"
```csharp
using (var trans = Redis.CreateTransaction()) 
{
    trans.QueueCommand(r => r.GetAllEntriesFromHash(HashKey), x => results = x);
    trans.Commit();
}
```

## ServiceStack.Text
 - JSON Support for `IEnumerable` with mixed types added by [@bcuff](https://github.com/bcuff)
 - Added new `string.SetQueryParam()` and `string.SetHashParam()` HTTP Utils API's 
 - Add range check for inferring valid JavaScript numbers with `JsonObject`

## [Stripe](https://github.com/ServiceStack/Stripe)
The Stripe Gateway also received updates thanks to [@jpasichnyk](https://github.com/jpasichnyk):

 - Added new `Send()` and `Post()` overloads that accepts Stripe's optional `Idempotency-Key` HTTP Header to prevent duplicate processing of resent requests
 - Added new `Type` property in `StripeError` error responses

# v4.0.38 Release Notes

## Native Support for Swift!

We're happy to announce an exciting new addition to [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) with support for Apple's new [Swift Programming Language](https://developer.apple.com/swift/) - providing the most productive way for consuming web services on the worlds most desirable platform!

![Swift iOS, XCode and OSX Banner](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/swift-logo-banner.jpg)

Native Swift support adds compelling value to your existing ServiceStack Services providing an idiomatic and end-to-end typed Swift API that can be effortlessly consumed from iOS and OSX Desktop Apps.

### ServiceStack XCode Plugin

To further maximize productivity we've integrated with XCode IDE to allow iOS and OSX developers to import your typed Services API directly into their XCode projects with the ServiceStack XCode plugin below:

[![ServiceStackXCode.dmg download](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-dmg.png)](https://github.com/ServiceStack/ServiceStack.Swift/raw/master/dist/ServiceStackXcode.dmg)

The ServiceStack XCode Plugin can be installed by dragging it to the XCode Plugins directory:

![ServiceStackXCode.dmg Installer](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestackxcode-installer.png)

Once installed the plugin adds a couple new familiar Menu options to the XCode Menu:

### Swift Add ServiceStack Reference

![XCode Add Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-add-reference.png)

Use the **Add ServiceStack Reference** Menu option to bring up the Add Reference XCode UI Sheet, which just like the Popup Window in VS.NET just needs the Url for your remote ServiceStack instance and the name of the file the generated Swift DTO's should be saved to:

![XCode Add Reference Sheet](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-add-reference-sheet.png)

Clicking **Add Reference** adds 2 files to your XCode project:

 - `JsonServiceClient.swift` - A Swift JSON ServiceClient with API's based on that of [the .NET JsonServiceClient](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client)
 - `{FileName}.dtos.swift` - Your Services DTO Types converted in Swift

You can also customize how the Swift types are generated by uncommenting the desired option with the behavior you want, e.g. to enable [Key-Value Observing (KVO)](https://developer.apple.com/library/ios/documentation/Cocoa/Conceptual/KeyValueObserving/KeyValueObserving.html) in the generated DTO models, uncomment `BaseClass: NSObject` and then click the **Update ServiceStack Reference** Main Menu item to fetch the latest DTO's with all Types inheriting from `NSObject` as seen below:

![XCode Update Reference](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xcode-update-reference.png)

> We've temporarily disabled "Update on Save" functionality as it resulted in an unacceptable typing delay on the watched file. We hope to re-enable this in a future version of XCode which doesn't exhibit degraded performance.

## Swift Native Types

Like most ServiceStack's features our goal with **Add ServiceStack Reference** is to deliver the most value as simply as possible. One way we try to achieve this is by reducing the cognitive load required to use our libraries by promoting a simple but powerful conceptual model that works consistently across differring implementations, environments, langauges as well as UI integration with the various VS.NET, Xamarin Studio and now XCode IDE's - in a recent React conference this was nicely captured with the phrase [Learn once, Write Anywhere](http://agateau.com/2015/learn-once-write-anywhere/).

Whilst each language is subtly different, all implementations work conceptually similar with all using Clean, Typed DTO's sent using a generic Service Gateway to facilitate its end-to-end typed communications. The client gateways also support DTO's from any source whether shared in source or binary form or generated with [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference).

### [JsonServiceClient.swift](https://github.com/ServiceStack/ServiceStack.Swift/blob/master/dist/JsonServiceClient.swift)

With this in mind we were able to provide the same ideal, high-level API we've enjoyed in [.NET's ServiceClients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) into idiomatic Swift as seen with its `ServiceClient` protocol definition below:

```swift
public protocol ServiceClient
{
    func get<T>(request:T, error:NSErrorPointer) -> T.Return?
    func get<T>(request:T, query:[String:String], error:NSErrorPointer) -> T.Return?
    func get<T>(relativeUrl:String, error:NSErrorPointer) -> T?
    func getAsync<T>(request:T) -> Promise<T.Return>
    func getAsync<T>(request:T, query:[String:String]) -> Promise<T.Return>
    func getAsync<T>(relativeUrl:String) -> Promise<T>
    
    func post<T>(request:T, error:NSErrorPointer) -> T.Return?
    func post<Response, Request>(relativeUrl:String, request:Request?, error:NSErrorPointer) -> Response?
    func postAsync<T>(request:T) -> Promise<T.Return>
    func postAsync<Response, Request>(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func put<T>(request:T, error:NSErrorPointer) -> T.Return?
    func put<Response, Request>(relativeUrl:String, request:Request?, error:NSErrorPointer) -> Response?
    func putAsync<T>(request:T) -> Promise<T.Return>
    func putAsync<Response, Request>(relativeUrl:String, request:Request?) -> Promise<Response>
    
    func delete<T>(request:T, error:NSErrorPointer) -> T.Return?
    func delete<T>(request:T, query:[String:String], error:NSErrorPointer) -> T.Return?
    func delete<T>(relativeUrl:String, error:NSErrorPointer) -> T?
    func deleteAsync<T>(request:T) -> Promise<T.Return>
    func deleteAsync<T>(request:T, query:[String:String]) -> Promise<T.Return>
    func deleteAsync<T>(relativeUrl:String) -> Promise<T>
    
    func send<T>(intoResponse:T, request:NSMutableURLRequest, error:NSErrorPointer) -> T?
    func sendAsync<T>(intoResponse:T, request:NSMutableURLRequest) -> Promise<T>
    
    func getData(url:String, error:NSErrorPointer) -> NSData?
    func getDataAsync(url:String) -> Promise<NSData>
}
```

> Generic type constraints omitted for readability

The minor differences are primarily due to differences in Swift which instead of throwing Exceptions uses error codes and `Optional` return types and its lack of any asynchrony language support led us to embed a lightweight and [well-documented Promises](http://promisekit.org/introduction/) implementation in [PromiseKit](https://github.com/mxcl/PromiseKit) which closely matches the `Task<T>` type used in .NET Async API's.

### JsonServiceClient.swift Usage

If you've ever had to make HTTP requests using Objective-C's `NSURLConnection` or `NSURLSession` static classes in iOS or OSX, the higher-level API's in `JsonServiceClient` will feel like a breath of fresh air - which enable the same ideal client API's we've enjoyed in ServiceStack's .NET Clients, in Swift Apps! 

> A nice benefit of using JsonServiceClient over static classes is that Service calls can be easily substituted and mocked with the above `ServiceClient` protocol, making it easy to test or stub out the external Gateway calls whilst the back-end is under development.

To illustrate its usage we'll go through some client code to consume [TechStacks](https://github.com/ServiceStackApps/TechStacks) Services after adding a **ServiceStack Reference** to `http://techstaks.io`:

```swift
var client = JsonServiceClient(baseUrl: "http://techstacks.io")
var response = client.get(AppOverview())
```

Essentially usage is the same as it is in .NET ServiceClients - where it just needs the `baseUrl` of the remote ServiceStack instance, which can then be used to consume remote Services by sending typed Request DTO's that respond in kind with the expected Response DTO.

### Async API Usage

Whilst the sync API's are easy to use their usage should be limited in background threads so they're not blocking the Apps UI whilst waiting for responses. Most of the time when calling services from the Main UI thread you'll want to use the non-blocking async API's, which for the same API looks like:

```swift
client.getAsync(AppOverview())
    .then(body:{(r:AppOverviewResponse) -> Void in 
        ... 
    })
```

Which is very similar to how we'd make async `Task<T>` calls in C# when not using its async/await language syntax sugar. 

> Async callbacks are called back on the main thread, ideal for use in iOS Apps. This behavior is also configurable in the Promise's callback API.

### Typed Error Handling

As Swift doesn't provide `try/catch` Exception Handling, Error handling is a little different in Swift which for most failable API's just returns a `nil` Optional to indicate when the operation didn't succeed. When more information about the error is required, API's will typically accept an additional `NSError` pointer argument to populate with more information about the error. Any additional metadata can be attached to NSError's `userInfo` Dictionary. We also follow this same approach to provide our structured error handling in `JsonServiceClient`.

To illustrate exception handling we'll connect to ServiceStack's Test Services and call the `ThrowType` Service to intentionally throw the error specified, e.g:

#### Sync Error Handling

```swift
var client = JsonServiceClient(baseUrl: "http://test.servicestack.net")

var request = ThrowType()
request.type = "NotFound"
request.message = "custom message"

var error:NSError?

let response = client.post(request, &error)
response //= nil

error!.code //= 404
var status:ResponseStatus = error!.convertUserInfo() //Convert into typed ResponseStatus
status.message //= not here
status.stackTrace //= Server Stack Trace
```

> Note the explicit type definition on the return type is required here as Swift uses it as part of the generic method invocation.

#### Async Error Handling

To handle errors in Async API's we just add a callback on `.catch()` API on the returned Promise, e.g:

```swift
client.postAsync(request)
    .catch({ (error:NSError) -> Void in
        var status:ResponseStatus = error.convertUserInfo()
        //...
    })
```

### JsonServiceClient Error Handlers

Just like in .NET, we can also attach Global or instance error handlers to be able to generically handle all Service Client errors with a custom handler, e.g:

```swift
client.onError = {(e:NSError) in ... }
JsonServiceClient.Global.onError = {(e:NSError) in ... }
```

### Custom Routes

As Swift doesn't support Attributes any exported .NET Attributes are emitted in comments on the Request DTO they apply to, e.g:

```swift
// @Route("/technology/{Slug}")
public class GetTechnology : IReturn { ... }
```

This also means that the Custom Routes aren't used when making Service Requests and instead just uses ServiceStack's built-in [pre-defined routes](https://github.com/ServiceStack/ServiceStack/wiki/Routing#pre-defined-routes). 

But when preferred `JsonServiceClient` can also be used to call Services using Custom Routes, e.g:

```swift
var response:GetTechnologyResponse? = client.get("/technology/servicestack")
```

### JsonServiceClient Options

Other options that can be configured on JsonServiceClient include:

```swift
client.onError = {(e:NSError) in ... }
client.timeout = ...
client.cachePolicy = NSURLRequestCachePolicy.ReloadIgnoringLocalCacheData
client.requestFilter = {(req:NSMutableURLRequest) in ... }
client.responseFilter = {(res:NSURLResponse) in ... }

//static Global configuration
JsonServiceClient.Global.onError = {(e:NSError) in ... }
JsonServiceClient.Global.requestFilter = {(req:NSMutableURLRequest) in ... }
JsonServiceClient.Global.responseFilter = {(res:NSURLResponse) in ... }
```

## Introducing TechStacks iPhone and iPad App!

To illustrate the ease-of-use and utility of ServiceStack's new Swift support we've developed a native iOS App for http://techstacks.io that has been recently published and is now available to download for free on the AppStore:

[![TechStacks on AppStore](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-appstore.png)](https://itunes.apple.com/us/app/techstacks/id965680615?ls=1&mt=8)

The complete source code for the [TechStacks App is available on GitHub](https://github.com/ServiceStackApps/TechStacks) - providing a good example on how easy it is to take advantage of ServiceStack's Swift support to quickly build a rich and responsive Services-heavy native iOS App. 

All remote Service Calls used by the App are encapsulated into a single [AppData.swift](https://github.com/ServiceStackApps/TechStacksApp/blob/master/src/TechStacks/AppData.swift) class and only uses JsonServiceClient's non-blocking Async API's to ensure a Responsive UI is maintained throughout the App.

### MVC and Key-Value Observables (KVO)

If you've ever had to implement `INotifyPropertyChanged` in .NET, you'll find the built-in model binding capabilities in iOS/OSX a refreshing alternative thanks to Objective-C's underlying `NSObject` which automatically generates automatic change notifications for its KV-compliant properties. UIKit and Cocoa frameworks both leverage this feature to enable its [Model-View-Controller Pattern](https://developer.apple.com/library/mac/documentation/General/Conceptual/DevPedia-CocoaCore/MVC.html). 

As keeping UI's updated with Async API callbacks can get unwieldy, we wanted to go through how we're taking advantage of NSObject's KVO support in Service Responses to simplify maintaining dynamic UI's.

### Enable Key-Value Observing in Swift DTO's

Firstly to enable KVO in your Swift DTO's we'll want to have each DTO inherit from `NSObject` which can be done by uncommenting `BaseObject` option in the header comments as seen below:

```
/* Options:
Date: 2015-02-19 22:43:04
Version: 1
BaseUrl: http://techstacks.io

BaseClass: NSObject
...
*/
```
and click the **Update ServiceStack Reference** Menu Option to fetch the updated DTO's.

Then to [enable Key-Value Observing](https://developer.apple.com/library/ios/documentation/Swift/Conceptual/BuildingCocoaApps/AdoptingCocoaDesignPatterns.html#//apple_ref/doc/uid/TP40014216-CH7-XID_8) just mark the response DTO variables with the `dynamic` modifier, e.g:

```swift
public dynamic var allTiers:[Option] = []
public dynamic var overview:AppOverviewResponse = AppOverviewResponse()
public dynamic var topTechnologies:[TechnologyInfo] = []
public dynamic var allTechnologies:[Technology] = []
public dynamic var allTechnologyStacks:[TechnologyStack] = []
```

Which is all that's needed to allow properties to be observed as they'll automatically issue change notifications when they're populated in the Service response async callbacks, e.g:

```swift
func loadOverview() -> Promise<AppOverviewResponse> {
    return client.getAsync(AppOverview())
        .then(body:{(r:AppOverviewResponse) -> AppOverviewResponse in
            self.overview = r
            self.allTiers = r.allTiers
            self.topTechnologies = r.topTechnologies
            return r
        })
}

func loadAllTechnologies() -> Promise<GetAllTechnologiesResponse> {
    return client.getAsync(GetAllTechnologies())
        .then(body:{(r:GetAllTechnologiesResponse) -> GetAllTechnologiesResponse in
            self.allTechnologies = r.results
            return r
        })
}

func loadAllTechStacks() -> Promise<GetAllTechnologyStacksResponse> {
    return client.getAsync(GetAllTechnologyStacks())
        .then(body:{(r:GetAllTechnologyStacksResponse) -> GetAllTechnologyStacksResponse in
            self.allTechnologyStacks = r.results
            return r
        })
}
```

### Observing Data Changes

In your [ViewController](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/HomeViewController.swift) have the datasources for your custom views binded to the desired data (which will initially be empty):

```swift
func pickerView(pickerView: UIPickerView, numberOfRowsInComponent component: Int) -> Int {
    return appData.allTiers.count
}
...
func tableView(tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
    return appData.topTechnologies.count
}
```

Then in `viewDidLoad()` [start observing the properties](https://github.com/ServiceStack/ServiceStack.Swift/blob/67c5c092b92927702f33b6a0669e3aa1de0e2cdc/apps/TechStacks/TechStacks/HomeViewController.swift#L31) your UI Controls are bound to, e.g:

```swift
override func viewDidLoad() {
    ...
    self.appData.observe(self, properties: ["topTechnologies", "allTiers"])
    self.appData.loadOverview()
}
deinit { self.appData.unobserve(self) }
```

In the example code above we're using some custom [KVO helpers](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/AppData.swift#L159-L183) to keep the code required to a minimum.

With the observable bindings in place, the change notifications of your observed properties can be handled by overriding `observeValueForKeyPath()` which passes the name of the property that's changed in the `keyPath` argument that can be used to determine the UI Controls to refresh, e.g:

```swift
override func observeValueForKeyPath(keyPath:String, ofObject object:AnyObject, change:[NSObject:AnyObject],
  context: UnsafeMutablePointer<Void>) {
    switch keyPath {
    case "allTiers":
        self.technologyPicker.reloadAllComponents()
    case "topTechnologies":
        self.tblView.reloadData()
    default: break
    }
}
```

Now that everything's configured, the observables provide an alternative to manually updating UI elements within async callbacks, instead you can now fire-and-forget your async API's and rely on the pre-configured bindings to automatically update the appropriate UI Controls when their bounded properties are updated, e.g:

```swift
self.appData.loadOverview() //Ignore response and use configured KVO Bindings
```

### Images and Custom Binary Requests

In addition to greatly simplifying Web Service Requests, `JsonServiceClient` also makes it easy to fetch any custom HTTP response like Images and other Binary data using the generic `getData()` and `getDataAsync()` NSData API's. This is used in TechStacks to [maintain a cache of all loaded images](https://github.com/ServiceStackApps/TechStacksApp/blob/0fca564e8c06fd1b71f81faee93a2e04c70a219b/src/TechStacks/AppData.swift#L144), reducing number of HTTP requests and load times when navigating between screens:

```swift
var imageCache:[String:UIImage] = [:]

public func loadImageAsync(url:String) -> Promise<UIImage?> {
    if let image = imageCache[url] {
        return Promise<UIImage?> { (complete, reject) in complete(image) }
    }
    
    return client.getDataAsync(url)
        .then(body: { (data:NSData) -> UIImage? in
            if let image = UIImage(data:data) {
                self.imageCache[url] = image
                return image
            }
            return nil
        })
}
```

## TechStacks OSX Desktop App!

As `JsonServiceClient.swift` has no external dependencies and only relies on core `Foundation` classes it can be used anywhere Swift can including OSX Cocoa Desktop and Command Line Apps and Frameworks.

Most of the API's used in TechStacks iOS App are standard typed Web Services calls. We've also developed a TechStacks OSX Desktop to showcase how easy it is to call ServiceStack's dynamic [AutoQuery Services](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query) and how much auto-querying functionality they can provide for free.

E.g. The TechStacks Desktop app is essentially powered with these 2 AutoQuery Services:

```csharp
[Query(QueryTerm.Or)] //change from filtering (default) to combinatory semantics
public class FindTechStacks : QueryBase<TechnologyStack> {}

[Query(QueryTerm.Or)]
public class FindTechnologies : QueryBase<Technology> {}
```

Basically just a Request DTO telling AutoQuery what Table we want to Query and that we want to [change the default Search behavior](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#changing-querying-behavior) to have **OR** semantics. We don't need to specify which properties we can query as the [implicit conventions](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#implicit-conventions) automatically infer it from the table being queried.

The TechStacks Desktop UI is then built around these 2 AutoQuery Services allowing querying against each field and utilizing a subset of the implicit conventions supported:

### Querying Technology Stacks

![TechStack Desktop Search Fields](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-desktop-field.png)

### Querying Technologies

![TechStack Desktop Search Type](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/techstacks-desktop-type.png)

Like the TechStacks iOS App all Service Calls are maintained in a single [AppData.swift](https://github.com/ServiceStackApps/TechStacksDesktopApp/blob/master/src/TechStacksDesktop/AppData.swift) class and uses KVO bindings to update its UI which is populated from these 2 services below:

```swift
func searchTechStacks(query:String, field:String? = nil, operand:String? = nil)
  -> Promise<QueryResponse<TechnologyStack>> {
    self.search = query
    
    let queryString = query.count > 0 && field != nil && operand != nil
        ? [createAutoQueryParam(field!, operand!): query]
        : ["NameContains":query, "DescriptionContains":query]
    
    let request = FindTechStacks<TechnologyStack>()
    return client.getAsync(request, query:queryString)
        .then(body:{(r:QueryResponse<TechnologyStack>) -> QueryResponse<TechnologyStack> in
            self.filteredTechStacks = r.results
            return r
        })
}

func searchTechnologies(query:String, field:String? = nil, operand:String? = nil)
  -> Promise<QueryResponse<Technology>> {
    self.search = query

    let queryString = query.count > 0 && field != nil && operand != nil
        ? [createAutoQueryParam(field!, operand!): query]
        : ["NameContains":query, "DescriptionContains":query]
    
    let request = FindTechnologies<Technology>()
    return client.getAsync(request, query:queryString)
        .then(body:{(r:QueryResponse<Technology>) -> QueryResponse<Technology> in
            self.filteredTechnologies = r.results
            return r
        })
}

func createAutoQueryParam(field:String, _ operand:String) -> String {
    let template = autoQueryOperandsMap[operand]!
    let mergedField = template.replace("%", withString:field)
    return mergedField
}
```

Essentially employing the same strategy for both AutoQuery Services where it builds a query String parameter to send with the request. For incomplete queries, the default search queries both `NameContains` and `DescriptionContains` field conventions returning results where the Search Text is either in `Name` **OR** `Description` fields.

## Swift Generated DTO Types

With Swift support our goal was to ensure a high-fidelity, idiomatic translation within the constraints of Swift language and built-in libraries, where the .NET Server DTO's are translated into clean Swift POSO's (Plain Old Swift Objects :) having their .NET built-in types mapped to their equivalent Swift data type. 

To see what this ended up looking like, we'll peel back behind the covers and look at a couple of the [Generated Swift Test Models](http://test.servicestack.net/types/swift) to see how they're translated in Swift:

```swift
public class AllTypes
{
    required public init(){}
    public var id:Int?
    public var nullableId:Int?
    public var byte:Int8?
    public var short:Int16?
    public var int:Int?
    public var long:Int64?
    public var uShort:UInt16?
    public var uInt:UInt32?
    public var uLong:UInt64?
    public var float:Float?
    public var double:Double?
    public var decimal:Double?
    public var string:String?
    public var dateTime:NSDate?
    public var timeSpan:NSTimeInterval?
    public var dateTimeOffset:NSDate?
    public var guid:String?
    public var char:Character?
    public var nullableDateTime:NSDate?
    public var nullableTimeSpan:NSTimeInterval?
    public var stringList:[String] = []
    public var stringArray:[String] = []
    public var stringMap:[String:String] = [:]
    public var intStringMap:[Int:String] = [:]
    public var subType:SubType?
}

public class AllCollectionTypes
{
    required public init(){}
    public var intArray:[Int] = []
    public var intList:[Int] = []
    public var stringArray:[String] = []
    public var stringList:[String] = []
    public var pocoArray:[Poco] = []
    public var pocoList:[Poco] = []
    public var pocoLookup:[String:[Poco]] = [:]
    public var pocoLookupMap:[String:[String:Poco]] = [:]
}

public enum EnumType : Int
{
    case Value1
    case Value2
}
```

As seen above, properties are essentially mapped to their optimal Swift equivalent. As DTO's can be partially complete all properties are `Optional` except for enumerables which default to an empty collection - making them easier to work with and despite their semantic differences, .NET enums are translated into typed Swift enums.

### Swift Challenges

The current stable version of Swift has several limitations that prevented using similar reflection and metaprogramming/code-gen techniques we're used to with .NET to implement them efficiently in Swift, e.g. Swift has an incomplete reflection API that can't set a property, is unable to cast `Any` (aka object) back to a concrete Swift type, unable to get the string literal for an enum value and we ran into many other Swift compiler limitations that would segfault whilst exploring this strategy.

Some of these limitations could be worked around by having every type inherit from `NSObject` and bridging to use the dynamism in Objective-C API's, but ultimately we decided against depending on `NSObject` or using Swift's built-in reflection API's which we also didn't expect to perform well in iOS's NoJIT environment which doesn't allow caching of reflection access to maintain
optimal runtime performance. 

### Swift Code Generation

As we were already using code-gen to generate the Swift types we could extend it without impacting the Developer UX which we expanded to also include what's essentially an **explicit Reflection API** for each type with API's to support serializing to and from JSON. Thanks to Swift's rich support for extending types we were able to leverage its Type extensions so the implementation details could remain disconnected from the clean Swift type definitions allowing improved readability when inspecting the remote DTO schema's.

We can look at `AllCollectionTypes` to see an example of the code-gen that's generated for each type, essentially emitting explicit readable/writable closures for each property: 

```swift
extension AllCollectionTypes : JsonSerializable
{
    public class var typeName:String { return "AllCollectionTypes" }
    public class func reflect() -> Type<AllCollectionTypes> {
        return TypeConfig.config() ?? TypeConfig.configure(Type<AllCollectionTypes>(
            properties: [
                Type<AllCollectionTypes>.arrayProperty("intArray", get: { $0.intArray }, set: { $0.intArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("intList", get: { $0.intList }, set: { $0.intList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringArray", get: { $0.stringArray }, set: { $0.stringArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("stringList", get: { $0.stringList }, set: { $0.stringList = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoArray", get: { $0.pocoArray }, set: { $0.pocoArray = $1 }),
                Type<AllCollectionTypes>.arrayProperty("pocoList", get: { $0.pocoList }, set: { $0.pocoList = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookup", get: { $0.pocoLookup }, set: { $0.pocoLookup = $1 }),
                Type<AllCollectionTypes>.objectProperty("pocoLookupMap", get: { $0.pocoLookupMap }, set: { $0.pocoLookupMap = $1 }),
            ]))
    }
    public func toJson() -> String {
        return AllCollectionTypes.reflect().toJson(self)
    }
    public class func fromJson(json:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromJson(AllCollectionTypes(), json: json)
    }
    public class func fromObject(any:AnyObject) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromObject(AllCollectionTypes(), any:any)
    }
    public func toString() -> String {
        return AllCollectionTypes.reflect().toString(self)
    }
    public class func fromString(string:String) -> AllCollectionTypes? {
        return AllCollectionTypes.reflect().fromString(AllCollectionTypes(), string: string)
    }
}
```

### Swift Native Types Limitations

Due to the semantic differences and limitations in Swift there are some limitations of what's not supported. Luckily these limitations are mostly [highly-discouraged bad practices](http://stackoverflow.com/a/10759250/85785) which is another reason not to use them. Specifically what's not supported:

#### No `object` or `Interface` properties
When emitting code we'll generate a comment when ignoring these properties, e.g:
```swift
//emptyInterface:IEmptyInterface ignored. Swift doesn't support interface properties
```

#### Base types must be marked abstract
As Swift doesn't support extension inheritance, when using inheritance in DTO's any Base types must be marked abstract.

#### All DTO Type Names must be unique
Required as there are no namespaces in Swift (Also required for F# and TypeScript). ServiceStack only requires Request DTO's to be unique, but our recommendation is for all DTO names to be unique.

#### IReturn not added for Array Responses
As Swift doesn't allow extending generic Arrays with public protocols, the `IReturn` marker that enables the typed ServiceClient API isn't available for Requests returning Array responses. You can workaround this limitation by wrapping the array in a Response DTO whilst we look at other solutions to support this in future.

## Swift Configuration

The header comments in the generated DTO's allows for further customization of how the DTO's are generated which can then be updated with any custom Options provided using the **Update ServiceStack Reference** Menu Item in XCode. Options that are preceded by a Swift single line comment `//` are defaults from the server that can be overridden, e.g:

```swift
/* Options:
Date: 2015-02-22 13:52:26
Version: 1
BaseUrl: http://techstacks.io

//BaseClass: 
//AddModelExtensions: True
//AddServiceStackTypes: True
//IncludeTypes: 
//ExcludeTypes: 
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//DefaultImports: Foundation
*/
```

To override a value, remove the `//` and specify the value to the right of the `:`. Any value uncommented will be sent to the server to override any server defaults.

We'll go through and cover each of the above options to see how they affect the generated DTO's:

### BaseClass
Specify a base class that's inherited by all Swift DTO's, e.g. to enable [Key-Value Observing (KVO)](https://developer.apple.com/library/ios/documentation/Cocoa/Conceptual/KeyValueObserving/KeyValueObserving.html) in the generated DTO models have all types inherit from `NSObject`:

```
/* Options:
BaseClass: NSObject
```

Will change all DTO types to inherit from `NSObject`:

```swift
public class UserInfo : NSObject { ... }
```

### AddModelExtensions
Remove the the code-generated type extensions required to support typed JSON serialization of the Swift types and leave only the clean Swift DTO Type definitions.
```
/* Options:
AddModelExtensions: False
```

### AddServiceStackTypes
Don't generate the types for built-in ServiceStack classes and Services like `ResponseStatus` and `Authenticate`, etc.
```
/* Options:
AddServiceStackTypes: False
```

### IncludeTypes
Is used as a Whitelist that can be used to specify only the types you would like to have code-generated:
```
/* Options:
IncludeTypes: GetTechnology,GetTechnologyResponse
```
Will only generate `GetTechnology` and `GetTechnologyResponse` DTO's:
```swift
public class GetTechnology { ... }
public class GetTechnologyResponse { ... }
```

### ExcludeTypes
Is used as a Blacklist where you can specify which types you would like to exclude from being generated:
```
/* Options:
ExcludeTypes: GetTechnology,GetTechnologyResponse
```
Will exclude `GetTechnology` and `GetTechnologyResponse` DTO's from being generated.

### AddResponseStatus
Automatically add a `ResponseStatus` property on all Response DTO's, regardless if it wasn't already defined:
```
/* Options:
AddResponseStatus: True
```
Will add a `ResponseStatus` property to all Response DTO's:
```swift
public class GetAllTechnologiesResponse
{
    ...
    public var responseStatus:ResponseStatus
}
```

### AddImplicitVersion
Lets you specify the Version number to be automatically populated in all Request DTO's sent from the client: 
```
/* Options:
AddImplicitVersion: 1
```
Will add an initialized `version` property to all Request DTO's:
```swift
public class GetAllTechnologies : IReturn
{
    ...
    public var version:Int = 1
}
```

This lets you know what Version of the Service Contract that existing clients are using making it easy to implement ServiceStack's [recommended versioning strategy](http://stackoverflow.com/a/12413091/85785). 

### InitializeCollections
Whether enumerables should be initialized with an empty collection (default) or changed to use an Optional type:
```
/* Options:
InitializeCollections: False
```
Changes Collection Definitions to be declared as Optional Types instead of being initialized with an empty collection:
```swift
public class ResponseStatus
{
    public var errors:[ResponseError]?
}
```

### DefaultImports
Add additional import statements to the generated DTO's:
```
/* Options:
DefaultImports: UIKit,Foundation
```
Will import the `UIKit` and `Foundation` frameworks:
```swift
import UIKit;
import Foundation;
```

## Improved Add ServiceStack Reference

Whilst extending Add ServiceStack Reference to add support for Swift above we've also made a number of refinements to the existing native type providers including:

 - Improved support for nested classes
 - Improved support from complex generic and inherited generic type definitions
 - Ignored DTO properties are no longer emitted
 - Uncommon Language-specific configuration moved into the native type providers 
 - New DefaultImports option available to TypeScript and Swift native types

### New Include and Exclude Types option added to all languages

You can now control what types are generated by using `ExcludeTypes` which acts as a blacklist excluding those specific types, e.g:

```
ExcludeTypes: ResponseStatus,ResponseError
```

In contrast to ExcludeTypes, if you're only making use of a couple of Services you can use `IncludeTypes` which acts like a White-List ensuring only those specific types are generated, e.g:

```
IncludeTypes: GetTechnologyStacks,GetTechnologyStacksResponse
```

### GlobalNamespace option added in C# and VB.NET projects

F#, TypeScript and Swift are limited to generating all DTO's under a single global namespace, however in most cases this is actually preferred as it strips away the unnecessary details of how the DTO's are organized on the Server (potentially across multiple dlls/namespaces) and presents them under a single configurable namespace to the client.

As it's a nice client feature, we've also added this option to C# and VB.NET native types as well which can be enabled by uncommenting the `GlobalNamespace` option, e.g:

```csharp
/* Options:
Version: 1
BaseUrl: http://techstacks.io

GlobalNamespace: ServiceModels
...
*/

namespace ServiceModels
{
...
}
```

## Integrated HTML, CSS and JavaScript Minification

As part of our quest to provide a complete and productive solution for developing highly responsive Web, Desktop and Mobile Apps, ServiceStack now includes minifiers for compressing HTML, CSS and JavaScript available from the new `Minifiers` class: 

```csharp
var minifiedJs = Minifiers.JavaScript.Compress(js);
var minifiedCss = Minifiers.Css.Compress(css);
var minifiedHtml = Minifiers.Html.Compress(html);

// Also minify in-line CSS and JavaScript
var advancedMinifiedHtml = Minifiers.HtmlAdvanced.Compress(html);
```

> Each minifier implements the lightweight [ICompressor](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Interfaces/ICompressor.cs) interface making it trivial to mock or subtitute with a custom implementation.

#### JS Minifier
For the JavaScript minifier we're using [Ext.Net's C# port](https://github.com/extnet/Ext.NET.Utilities/blob/master/Ext.Net.Utilities/JavaScript/JSMin.cs) of Douglas Crockford's venerable [JSMin](http://crockford.com/javascript/jsmin). 

#### CSS Minifier
The CSS Minifer uses Mads Kristensen simple [CSS Minifer](http://madskristensen.net/post/efficient-stylesheet-minification-in-c). 

#### HTML Compressor
For compressing HTML we're using a [C# Port](http://blog.magerquark.de/c-port-of-googles-htmlcompressor-library/) of Google's excellent [HTML Compressor](https://code.google.com/p/htmlcompressor/) which we've further modified to remove the public API's ugly Java-esque idioms and replaced them with C# properties.

The `HtmlCompressor` also includes a number of well-documented options which can be customized by configuring the available properties on its concrete type, e.g:

```csharp
var htmlCompressor = (HtmlCompressor)Minifier.Html;
htmlCompressor.RemoveComments = false;
```

### Easy win for server generated websites

If your project is not based off one of our optimized Gulp/Grunt.js powered [React JS and AngularJS Single Page App templates](https://github.com/ServiceStack/ServiceStackVS) or configured to use our eariler [node.js-powered Bundler](https://github.com/ServiceStack/Bundler) Web Optimization solution, these built-in minifiers now offers the easiest solution to effortlessly optimize your existing website which is able to work transparently with your existing Razor Views and static `.js`, `.css` and `.html` files without requiring adding any additional external tooling or build steps to your existing development workflow.

### Minify dynamic Razor Views

Minification of Razor Views is easily enabled by specifying `MinifyHtml=true` when registering the `RazorFormat` plugin:

```csharp
Plugins.Add(new RazorFormat {
    MinifyHtml = true,
    UseAdvancedCompression = true,
});
```

Use the `UseAdvancedCompression=true` option if you also want to minify inline js/css, although as this requires a bit more processing you'll want to benchmark it to see if it's providing an overall performance benefit to end users. It's a recommended option if you're caching Razor Pages. Another solution is to minimize the use of in-line js/css and move them to static files to avoid needing in-line js/css compression.

### Minify static `.js`, `.css` and `.html` files

With nothing other than the new minifiers, we can leverage the flexibility in ServiceStack's [Virtual File System](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system) to provide an elegant solution for minifying static `.html`, `.css` and `.js` resources by simply pre-loading a new InMemory Virtual FileSystem with minified versions of existing files and giving the Memory FS a higher precedence so any matching requests serve up the minified version first. We only need to pre-load the minified versions once on StartUp by overriding `GetVirtualPathProviders()` in the AppHost:

```csharp
public override List<IVirtualPathProvider> GetVirtualPathProviders()
{
    var existingProviders = base.GetVirtualPathProviders();
    var memFs = new InMemoryVirtualPathProvider(this);

    //Get existing Local FileSystem Provider
    var fs = existingProviders.First(x => x is FileSystemVirtualPathProvider);

    //Process all .html files:
    foreach (var file in fs.GetAllMatchingFiles("*.html"))
    {
        var contents = Minifiers.HtmlAdvanced.Compress(file.ReadAllText());
        memFs.AddFile(file.VirtualPath, contents);
    }

    //Process all .css files:
    foreach (var file in fs.GetAllMatchingFiles("*.css")
      .Where(file => !file.VirtualPath.EndsWith(".min.css"))) //ignore pre-minified .css
    {
        var contents = Minifiers.Css.Compress(file.ReadAllText());
        memFs.AddFile(file.VirtualPath, contents);
    }

    //Process all .js files
    foreach (var file in fs.GetAllMatchingFiles("*.js")
      .Where(file => !file.VirtualPath.EndsWith(".min.js"))) //ignore pre-minified .js
    {
        try
        {
            var js = file.ReadAllText();
            var contents = Minifiers.JavaScript.Compress(js);
            memFs.AddFile(file.VirtualPath, contents);
        }
        catch (Exception ex)
        {
            //As JSMin is a strict subset of JavaScript, this can fail on valid JS.
            //We can report exceptions in StartUpErrors so they're visible in ?debug=requestinfo
            base.OnStartupException(new Exception("JSMin Error {0}: {1}".Fmt(file.VirtualPath, ex.Message)));
        }
    }

    //Give new Memory FS the highest priority
    existingProviders.Insert(0, memFs);
    return existingProviders;
}
```

A nice benefit of this approach is that it doesn't pollute your project with minified build artifacts, has excellent runtime performance with the minfied contents being served from Memory and as the file names remain the same, the links in HTML don't need to be rewritten to reference the minified versions. i.e. When a request is made it just looks through the registered virtual path providers and returns the first match, which given the Memory FS was inserted at the start of the list, returns the minified version.

### Enabled in [servicestack.net](https://servicestack.net)

As this was an quick and non-invasive feature to add, we've enabled it on all [servicestack.net](https://servicestack.net) Razor views and static files. You can `view-source:https://servicestack.net/` (as url in Chrome, Firefox or Opera) to see an example of the resulting minified output. 

## New [ServiceStack Cookbook](https://www.packtpub.com/application-development/servicestack-cookbook) Released!

<a href="https://www.packtpub.com/application-development/servicestack-cookbook"><img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/servicestack-cookbook.jpg" align="left" vspace="10" width="320" height="380" /></a>

A new [ServiceStack Cookbook](https://www.packtpub.com/application-development/servicestack-cookbook) was just released by ThoughtWorker [@kylehodgson](https://twitter.com/kylehodgson) and our own [Darren Reid](https://twitter.com/layoric). 

The ServiceStack Cookbook includes over 70 recipes on creating message-based Web Services and Apps including leveraging OrmLite to build fast, testable and maintainable Web APIs - focusing on solving real-world problems that are a pleasure to create, maintain and consume with ServiceStack.

<img src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7" width="700" height="1">

## Support for RecyclableMemoryStream

The Bing team recently [opensourced their RecyclableMemoryStream](http://www.philosophicalgeek.com/2015/02/06/announcing-microsoft-io-recycablememorystream/) implementation which uses pooled, reusable byte buffers to help minimize GC pressure. To support switching to the new `RecyclableMemoryStream` we've changed most of ServiceStack's MemoryStream usages to use the new `MemoryStreamFactory.GetStream()` API's, allowing ServiceStack to be configured to use the new `RecyclableMemoryStream` implementation with:

```csharp
 MemoryStreamFactory.UseRecyclableMemoryStream = true;
```

Which now changes `MemoryStreamFactory.GetStream()` to return instances of `RecyclableMemoryStream`, e.g:

```csharp
using (var ms = (RecyclableMemoryStream)MemoryStreamFactory.GetStream()) { ... }
```

> To reduce dependencies and be able to support PCL clients we're using an [interned and PCL-compatible version of RecyclableMemoryStream](https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/RecyclableMemoryStream.cs) in ServiceStack.Text which has its auditing features disabled.

### RecyclableMemoryStream is strict

Whilst the announcement says `RecyclableMemoryStream` is a drop-in replacement for `MemoryStream`, this isn't strictly true as it enforces stricter rules on how you can use MemoryStreams. E.g. a common pattern when using a MemoryStream with a `StreamWriter` or `StreamReader` is, since it's also disposable, to enclose it in a nested using block as well:

```csharp
using (var ms = MemoryStreamFactory.GetStream())
using (var writer = new StreamWriter(ms)) {
...
}
```

But this has the effect of closing the stream twice which is fine in `MemoryStream` but throws an `InvalidOperationException` when using `RecyclableMemoryStream`. If you find this happening in your Application you can use the new `MemoryStreamFactory.MuteDuplicateDisposeExceptions=true` option we've added as a stop-gap to mute these exceptions until you're able to update your code to prevent this.

### Doesn't allow reading from closed streams 

Another gotcha when switching over to `RecyclableMemoryStream` is that once you close the Stream you'll no longer be able to read from it. This makes it incompatible to use with .NET's built-in `GZipStream` and `DeflateStream` classes which can only be read after its closed, having the effect of closing the underlying MemoryStream - preventing being able to access the compressed bytes.

Where possible we've refactored ServiceStack to use `MemoryStreamFactory.GetStream()` and adhered to its stricter usage so ServiceStack can be switched over to use it with `MemoryStreamFactory.UseRecyclableMemoryStream=true`, although we still have some more optimization work to be able to fully take advantage of it by changing our usage of `ToArray()` to use the more optimal `GetBuffer()` and `Length` API's where possible.

## Authentication

 - New `MicrosoftLiveOAuth2Provider` Microsoft Live OAuth2 Provider added by [@ivanfioravanti](https://github.com/ivanfioravanti)
 - New `InstagramOAuth2Provider` Instagram OAuth2 Provider added by [@ricardobrandao](https://github.com/ricardobrandao)

### New Url Filters added to all AuthProviders

New Url Filters have been added to all AuthProvider redirects letting you inspect or customize and decorate any redirect urls that are forwarded to the remote OAuth Server or sent back to the authenticating client. The list different Url Filters available in all AuthProviders include:

```csharp
new AuthProvider {
    PreAuthUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    AccessTokenUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    SuccessRedirectUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    FailedRedirectUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
    LogoutUrlFilter = (authProvider, redirectUrl) => customize(redirectUrl),
}
```

## OrmLite

 - Custom Decimal precision with `[DecimalLength(precision,scale)]` added to all OrmLite RDBMS Providers
 - Sqlite persists and queries DateTime's using LocalTime

## Messaging

 - Added new docs showing how to populate Session Ids to [make authenticated MQ requests](https://github.com/ServiceStack/ServiceStack/wiki/Messaging#authenticated-requests-via-mq).
 - Request/Reply MQ Requests for Services with no response will send the Request DTO back to `ReplyTo` Queue instead of the [default .outq topic](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ#messages-with-no-responses-are-sent-to-outq-topic).

## Misc

 - Default 404 Handler for HTML now emits Error message in page body
 - New `Dictionary<string, object> Items` were added to all Response Contexts which can be used to transfer metadata through the response pipeline 
 - `BufferedStream` is now accessible on concrete Request/Response Contexts
 - Added new `GetKeysByPattern()` API to `MemoryCacheClient`
 - Allow `DTO.ToAbsoluteUrl()` extension method to support ASP.NET requests without needing to configure `Config.WebHostUrl`. Self Hosts can use the explicit `DTO.ToAbsoluteUrl(IRequest)` API.
 - New `HostContext.TryGetCurrentRequest()` Singleton returns Current Request for ASP.NET hosts, `null` for Self Hosts. 
    - `HostContext.GetCurrentRequest()` will throw for Self Hosts which don't provide singleton access to the current HTTP Request
 - Added new `string.CollapseWhitespace()` extension method to collapse multiple white-spaces into a single space.

### Using ServiceStack as a Proxy

The new `Config.SkipFormDataInCreatingRequest` option instructs ServiceStack to skip reading from the Request's **FormData** on initialization (to support `X-Http-Method-Override` Header) so it avoids forced loading of the Request InputStream allowing ServiceStack to be used as a HTTP proxy with:

```csharp
RawHttpHandlers.Add(_ => new CustomActionHandler((req, res) => {
    var bytes = req.InputStream.ReadFully();
    res.OutputStream.Write(bytes, 0, bytes.Length);
}));
```

## NuGet dependency updates

 - Npgsql updated to 2.2.4.3
 - NLog updated to v3.2.0.0

## Updated Versioning Strategy

To make it easier for developers using interim [pre-release packages on MyGet](https://github.com/ServiceStack/ServiceStack/wiki/MyGet) upgrade to the official NuGet packages once they're released, we've started using odd version numbers (e.g **v4.0.37**) for pre-release MyGet builds and even numbers (e.g. **v4.0.38**) for official released packages on NuGet.

## Breaking changes

 - `void` or `null` responses return `204 NoContent` by default, can be disabled with `Config.Return204NoContentForEmptyResponse = false`
 - Failed Auth Validations now clear the Users Session
 - `ServiceExtensions.RequestItemsSessionKey` moved to `SessionFeature.RequestItemsSessionKey`

# v4.0.36 Release Notes

## Xamarin Unified API Support

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/xamarin-unifiedapi.png" align="right" vspace="10" width="300" />

We have a short release cycle this release to be able to release the [ServiceStack PCL ServiceClients](https://github.com/ServiceStackApps/HelloMobile) support for 
[Xamarin's Unified API](http://developer.xamarin.com/guides/cross-platform/macios/unified/) to everyone as quickly as possible. As [announced on their blog](http://blog.xamarin.com/xamarin.ios-unified-api-with-64-bit-support/), Xamarin has released the stable build of Xamarin.iOS Unified API with 64-bit support. As per [Apple's deadlines](https://developer.apple.com/news/?id=12172014b) **new iOS Apps** published after **February 1st** must include 64-bit support, this deadline extends to updates of **existing Apps** on **June 1st**. One of the benefits of upgrading is being able to share code between iOS and OSX Apps with Xamarin.Mac.

Support for Unified API was added in addition to the existing 32bit monotouch.dll which used the **MonoTouch** NuGet profile. Xamarin Unified API instead uses the new **Xamarin.iOS10** NuGet profile. For new Apps this works transparently where you can add a NuGet package reference and it will automatically reference the appropriate build. 

    PM> Install-Package ServiceStack.Client

Existing iOS proejcts should follow Xamarin's [Updating Existing iOS Apps](http://developer.xamarin.com/guides/cross-platform/macios/updating_ios_apps/) docs, whilst the HelloMobile project has docs on using [ServiceStack's ServiceClients with iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client).

## Add ServiceStack Reference meets Xamarin Studio!

Our enhancements to [Add ServiceStack Reference](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference) continue, this time extended to support [Xamarin Studio!](http://xamarin.com/studio)

With the new [ServiceStackXS Add-In](http://addins.monodevelop.com/Project/Index/154) your Service Consumers can now generate typed DTO's of your remote ServiceStack Services directly from within Xamarin Studio, which together with the **ServiceStack.Client** NuGet package provides an effortless way to enable an end-to-end Typed API from within Xamarin C# projects.

### Installing ServiceStackXS

Installation is straightforward if you've installed Xamarin Add-ins before, just go to `Xamarin Studio -> Add-In Manager...` from the Menu and then search for `ServiceStack` from the **Gallery**:

![](https://github.com/ServiceStack/Assets/blob/master/img/servicestackvs/servicestack%20reference/ssxs-mac-install.gif)

### Adding a ServiceStack Reference

Once installed, adding a ServiceStack Reference is very similar to [ServiceStackVS in VS.NET](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#add-servicestack-reference) where you can just click on `Add -> Add ServiceStack Reference...` on the project's context menu to bring up the familiar Add Reference dialog. After adding the `BaseUrl` of the remote ServiceStack instance, click OK to add the generated DTO's to your project using the name specified:

![](https://github.com/ServiceStack/Assets/blob/master/img/servicestackvs/servicestack%20reference/ssxs-mac-add-reference.gif)

### Updating the ServiceStack Reference

As file watching isn't supported yet, to refresh the generated DTO's you'll need to click on its `Update ServiceStack Reference` from the items context menu.

### Developing with pleasure on Linux!

One of the nice benefits of creating an Xamarin Studio Add-in is that we're also able to bring the same experience to .NET Developers on Linux! Which works similar to OSX where you can install ServiceStackXS from the Add-in Gallery - Here's an example using Ubuntu:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/servicestack%20reference/ssxs-ubuntu-install.gif)

Then **Add ServiceStack Reference** is accessible in the same way:

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/servicestackvs/servicestack%20reference/ssxs-ubuntu-add-ref.gif)

## Sitemap Feature

A good SEO technique for helping Search Engines index your website is to tell them where the can find all your content using [Sitemaps](https://support.google.com/webmasters/answer/156184?hl=en). Sitemaps are basic xml documents but they can be tedious to maintain manually, more so for database-driven dynamic websites. 

The `SitemapFeature` reduces the effort required by letting you add Site Urls to a .NET collection of `SitemapUrl` POCO's. 
In its most basic usage you can populate a single Sitemap with urls of your Website Routes, e.g:

```csharp
Plugins.Add(new SitemapFeature
{
    UrlSet = db.Select<TechnologyStack>()
        .ConvertAll(x => new SitemapUrl {
            Location = new ClientTechnologyStack { Slug = x.Slug }.ToAbsoluteUri(),
            LastModified = x.LastModified,
            ChangeFrequency = SitemapFrequency.Weekly,
        })
});
```

The above example uses [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) to generate a collection of `SitemapUrl` entries containing Absolute Urls for all [techstacks.io Technology Pages](http://techstacks.io/tech). This is another good showcase for the [Reverse Routing available on Request DTO's](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing) which provides a Typed API for generating Urls without any additional effort.

Once populated your sitemap will be available at `/sitemap.xml` which looks like:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
<url>
  <loc>http://techstacks.io/the-guardian</loc>
  <lastmod>2015-01-14</lastmod>
  <changefreq>weekly</changefreq>
</url>
...
</urlset>
```

Which you can checkout in this [live Sitemap example](http://techstacks.io/sitemap-techstacks.xml).

### Multiple Sitemap Indexes

For larger websites, Sitemaps also support multiple [Sitemap indexes](https://support.google.com/webmasters/answer/75712?hl=en) which lets you split sitemap urls across multiple files. To take advantage of this in `SitemapFeature` you would instead populate the `SitemapIndex` collection with multiple `Sitemap` entries. An example of this is in the full [Sitemap used by techstacks.io](https://github.com/ServiceStackApps/TechStacks/blob/a114348e905b4334e93a5408c2fb76c5fb589501/src/TechStacks/TechStacks/AppHost.cs#L90-L128):

```csharp
Plugins.Add(new SitemapFeature
{
    SitemapIndex = {
        new Sitemap {
            AtPath = "/sitemap-techstacks.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<TechnologyStack>(q => q.OrderByDescending(x => x.LastModified))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientTechnologyStack { Slug = x.Slug }.ToAbsoluteUri(),
                    LastModified = x.LastModified,
                    ChangeFrequency = SitemapFrequency.Weekly,
                }),
        },
        new Sitemap {
            AtPath = "/sitemap-technologies.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<Technology>(q => q.OrderByDescending(x => x.LastModified))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientTechnology { Slug = x.Slug }.ToAbsoluteUri(),
                    LastModified = x.LastModified,
                    ChangeFrequency = SitemapFrequency.Weekly,
                })
        },
        new Sitemap
        {
            AtPath = "/sitemap-users.xml",
            LastModified = DateTime.UtcNow,
            UrlSet = db.Select<CustomUserAuth>(q => q.OrderByDescending(x => x.ModifiedDate))
                .Map(x => new SitemapUrl
                {
                    Location = new ClientUser { UserName = x.UserName }.ToAbsoluteUri(),
                    LastModified = x.ModifiedDate,
                    ChangeFrequency = SitemapFrequency.Weekly,
                })
        }
    }
});
```

Which now generates the following `<sitemapindex/>` at [/sitemap.xml](http://techstacks.io/sitemap.xml):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
<sitemap>
  <loc>http://techstacks.io/sitemap-techstacks.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
<sitemap>
  <loc>http://techstacks.io/sitemap-technologies.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
<sitemap>
  <loc>http://techstacks.io/sitemap-users.xml</loc>
  <lastmod>2015-01-15</lastmod>
</sitemap>
</sitemapindex>
```

With each entry linking to the urlset for each Sitemap:

 - [techstacks.io/sitemap-techstacks.xml](http://techstacks.io/sitemap-techstacks.xml)
 - [techstacks.io/sitemap-technologies.xml](http://techstacks.io/sitemap-technologies.xml)
 - [techstacks.io/sitemap-users.xml](http://techstacks.io/sitemap-users.xml)

## Razor

Services can now specify to return Content Pages for HTML clients (i.e. browsers) by providing the `/path/info` to the Razor Page:

```csharp
public object Any(Request request)
{
    ...
    return new HttpResult(responseDto) {
        View = "/content-page.cshtml"
    }
}
```

`HttpResult.View` was previously limited to names of Razor Views in the `/Views` folder.

## [Techstacks](http://techstacks.io)

Whilst not specifically Framework features, we've added some features to [techstacks.io](http://techstacks.io) that may be interesting for ServiceStack Single Page App developers:

### Server Generated HTML Pages

Whilst we believe Single Page Apps offer the more responsive UI, we've also added a server html version of [techstacks.io](http://techstacks.io) which we serve to WebCrawlers like **Googlebot** so they're better able to properly index content in the AngularJS SPA website. It also provides a good insight into the UX difference between a Single Page App vs Server HTML generated websites. Since [techstacks.io](http://techstacks.io) is running on modest hardware (i.e. IIS on shared **m1.small** EC2 instance with a shared **micro** RDS PostgreSQL backend) the differences are more visible with the AngularJS version still being able to yield a snappy App-like experience whilst the full-page reloads of the Server HTML version is clearly visible on each request.

The code to enable this is in [ClientRoutesService.cs](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks.ServiceInterface/ClientRoutesService.cs) which illustrates a simple technique used to show different versions of your website which by default is enabled implicitly for `Googlebot` User Agents, or can be toggled explicitly between by visiting the routes below:

  - [techstacks.io?html=client](http://techstacks.io?html=client)
  - [techstacks.io?html=server](http://techstacks.io?html=server)

These links determine whether you'll be shown the AngularJS version or the Server HTML Generated version of the Website. We can see how this works by exploring how the technology pages are implemented which handle both the technology index:

  - http://techstacks.io/tech

as well as individual technology pages, e.g:

  - http://techstacks.io/tech/redis
  - http://techstacks.io/tech/servicestack

First we need to create empty Request DTO's to capture the client routes (as they were only previously configured in AngularJS routes):

```csharp
[Route("/tech")]
public class ClientAllTechnologies {}

[Route("/tech/{Slug}")]
public class ClientTechnology
{
    public string Slug { get; set; }
}
```

Then we implement ServiceStack Services for these routes. The `ShowServerHtml()` helper method is used to determine whether 
to show the AngularJS or Server HTML version of the website which it does by setting a permanent cookie when 
`techstacks.io?html=server` is requested (or if the UserAgent is `Googlebot`). 
Every subsequent request then contains the `html=server` Cookie and so will show the Server HTML version. 
Users can then go to `techstacks.io?html=client` to delete the cookie and resume viewing the default AngularJS version:

```csharp
public class ClientRoutesService : Service
{
    public bool ShowServerHtml()
    {
        if (Request.GetParam("html") == "client")
        {
            Response.DeleteCookie("html");
            return false;
        }

        var serverHtml = Request.UserAgent.Contains("Googlebot")
            || Request.GetParam("html") == "server";

        if (serverHtml)
            Response.SetPermanentCookie("html", "server");

        return serverHtml;
    }

    public object AngularJsApp()
    {
        return new HttpResult {
            View = "/default.cshtml"
        };
    }

    public object Any(ClientAllTechnologies request)
    {
        return !ShowServerHtml()
            ? AngularJsApp()
            : new HttpResult(base.ExecuteRequest(new GetAllTechnologies())) {
                View = "AllTech"
            };
    }

    public object Any(ClientTechnology request)
    {
        return !ShowServerHtml()
            ? AngularJsApp()
            : new HttpResult(base.ExecuteRequest(new GetTechnology { Reload = true, Slug = request.Slug })) {
                View = "Tech"
            };
    }
}
```

The difference between which Website to display boils down to which Razor page to render, where for AngularJS we return the `/default.cshtml` 
Home Page where the client routes then get handled by AngularJS. Whereas for the Server HTML version, it just renders the appropriate Razor View for that request.

The `base.ExecuteRequest(new GetAllTechnologies())` API lets you execute a ServiceStack Service internally by just passing the 
`GetAllTechnologies` Request DTO. The Resposne DTO returned by the Service is then passed as a view model to the `/Views/AllTech.cshtml` Razor View. 

AngularJS declarative HTML pages holds an advantage when maintaining multiple versions of a websites as porting AngularJS views to Razor is relatively 
straight-forward process, basically consisting of converting Angular `ng-attributes` to `@Razor` statements, as can be seen in the client vs server 
versions of [techstacks.io/tech](http://techstacks.io/tech) index page:

  - [/partials/tech/latest.html](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks/partials/tech/latest.html)
  - [/Views/Tech/AllTech.cshtml](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks/Views/Tech/AllTech.cshtml)

### Twitter Updates

Another way to increase user engagement of your website is by posting Twitter Updates, [techstacks.io](http://techstacks.io) does this whenever anyone adds a new Technology or Technology Stack by posting a status update to [@webstacks](https://twitter.com/webstacks). The [code to make authorized Twitter API requests](https://github.com/ServiceStackApps/TechStacks/blob/master/src/TechStacks/TechStacks.ServiceInterface/TwitterUpdates.cs) ends up being fairly lightweight as it can take advantage of ServiceStack's built-in support for Twitter OAuth.

> We'd also love for others to Sign In and add their Company's Technology Stack on [techstacks.io](http://techstacks.io) so everyone can get a better idea what technologies everyone's using.

## ServiceStack.Text

CSV Serializer now supports serializing `List<dynamic>`:

```csharp
int i = 0;
List<dynamic> rows = new[] { "Foo", "Bar" }.Map(x => (object) new { Id = i++, Name = x });
rows.ToCsv().Print();
```

Or `List<object>`:

```csharp
List<object> rows = new[] { "Foo", "Bar" }.Map(x => (object) new { Id = i++, Name = x });
rows.ToCsv().Print();
```

Both will Print:

    Id,Name
    0,Foo
    1,Bar

## ServiceStackVS Updated 

[ServiceStackVS](https://github.com/ServiceStack/ServiceStackVS) received another minor bump, the latest version can be [downloaded from the Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7).

## Breaking Changes

### AuthProvider Validation moved to AuthFeature

Like other Plugin options the configuration of validating unique Emails as been moved from `AuthProvider.ValidateUniqueEmails` to:

```csharp
Plugins.Add(new AuthFeature(...) {
    ValidateUniqueEmails = true,
    ValidateUniqueUserNames = false
});
```

This includes the new `ValidateUniqueUserNames` option to specify whether or not the UserNames from different OAuth Providers should be unique (validation is disabled by default).

### PooledRedisClientsManager Db is nullable

In order to be able to specify what redis **DB** the `PooledRedisClientsManager` should use on the connection string (e.g: `localhost?db=1`) we've changed `PooledRedisClientsManager.Db` to be an optional `long?`. If you're switching between multiple Redis DB's in your Redis Clients you should explicitly specify what Db should be the default so that Redis Clients retrieved from the pool are automatically reset to that DB, with either:

```csharp
new PooledRedisClientsManager(initialDb:1);
```

or via the connection string:

```csharp
new PooledRedisClientsManager("localhost?db=1");
```

---

## [2014 Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/docs/2014/release-notes.md)


