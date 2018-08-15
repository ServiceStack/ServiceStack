Follow [@ServiceStack](https://twitter.com/servicestack) or join the [Google+ Community](https://plus.google.com/communities/112445368900682590445)
for updates, or [StackOverflow](http://stackoverflow.com/questions/ask) or the [Customer Forums](https://forums.servicestack.net/) for support.

> View the [Release Notes](https://servicestack.net/release-notes) for latest features or see [servicestack.net/features](https://servicestack.net/features) for an overview.

### Simple, Fast, Versatile and full-featured Services Framework

ServiceStack is a simple, fast, versatile and highly-productive full-featured [Web](http://razor.servicestack.net) and 
[Web Services](http://docs.servicestack.net/web-services.html) Framework that's 
thoughtfully-architected to [reduce artificial complexity](http://docs.servicestack.net/why-not-odata.html#why-not-complexity) and promote 
[remote services best-practices](http://docs.servicestack.net/advantages-of-message-based-web-services.html) 
with a [message-based design](http://docs.servicestack.net/what-is-a-message-based-web-service.html) 
that allows for maximum re-use that can leverage an integrated 
[Service Gateway](http://docs.servicestack.net/service-gateway.html) 
for the creation of loosely-coupled 
[Modularized Service](http://docs.servicestack.net/modularizing-services.html) Architectures.
ServiceStack Services are consumable via an array of built-in fast data formats (inc. 
[JSON](https://github.com/ServiceStack/ServiceStack.Text), 
XML, 
[CSV](http://docs.servicestack.net/csv-format.html), 
[JSV](http://docs.servicestack.net/json-jsv-and-xml.html), 
[ProtoBuf](http://docs.servicestack.net/protobuf-format.html), 
[Wire](http://docs.servicestack.net/wire-format.html) and 
[MsgPack](http://docs.servicestack.net/messagepack-format.html)) 
as well as XSD/WSDL for [SOAP endpoints](http://docs.servicestack.net/soap-support.html) and 
[Rabbit MQ](http://docs.servicestack.net/rabbit-mq.html), 
[Redis MQ](http://docs.servicestack.net/messaging-and-redis.html) and
[Amazon SQS](https://github.com/ServiceStack/ServiceStack.Aws#sqsmqserver) MQ hosts. 

Its design and simplicity focus offers an unparalleled suite of productivity features that can be declaratively enabled 
without code, from creating fully queryable Web API's with just a single Typed Request DTO with
[Auto Query](http://docs.servicestack.net/autoquery.html) supporting 
[every major RDBMS](https://github.com/ServiceStack/ServiceStack.OrmLite#8-flavours-of-ormlite-is-on-nuget) 
to the built-in support for
[Auto Batched Requests](http://docs.servicestack.net/auto-batched-requests.html) 
or effortlessly enabling rich [HTTP Caching](http://docs.servicestack.net/http-caching.html) and
[Encrypted Messaging](http://docs.servicestack.net/encrypted-messaging.html) 
for all your existing services via [Plugins](http://docs.servicestack.net/plugins.html).

Your same Services also serve as the Controller in ServiceStack's [Smart Razor Views](http://razor.servicestack.net/)
reducing the effort to serve both 
[Web and Single Page Apps](https://github.com/ServiceStackApps/LiveDemos) as well as 
[Rich Desktop and Mobile Clients](https://github.com/ServiceStackApps/HelloMobile) that are able to deliver instant interactive 
experiences using ServiceStack's real-time [Server Events](http://docs.servicestack.net/server-events.html).

ServiceStack Services also maximize productivity for consumers providing an 
[instant end-to-end typed API without code-gen](http://docs.servicestack.net/csharp-client.html) enabling
the most productive development experience for developing .NET to .NET Web Services.

### [Generate Instant Typed APIs from within all Major IDEs!](http://docs.servicestack.net/add-servicestack-reference.html)

ServiceStack now integrates with all Major IDE's used for creating the best native experiences on the most popular platforms 
to enable a highly productive dev workflow for consuming Web Services, making ServiceStack the ideal back-end choice for powering 
rich, native iPhone and iPad Apps on iOS with Swift, Mobile and Tablet Apps on the Android platform with Java, OSX Desktop Appications 
as well as targetting the most popular .NET PCL platforms including Xamarin.iOS, Xamarin.Android, Windows Store, WPF, WinForms and Silverlight: 

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/wikis/ide-ss-plugin-logos.png" align="right" />

#### [VS.NET integration with ServiceStackVS](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

Providing instant Native Typed API's for 
[C#](http://docs.servicestack.net/csharp-add-servicestack-reference.html), 
[TypeScript](http://docs.servicestack.net/typescript-add-servicestack-reference.html),
[F#](http://docs.servicestack.net/fsharp-add-servicestack-reference.html) and 
[VB.NET](http://docs.servicestack.net/vbnet-add-servicestack-reference.html) 
directly in Visual Studio for the 
[most popular .NET platforms](https://github.com/ServiceStackApps/HelloMobile) including iOS and Android using 
[Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and 
[Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) on Windows.

#### [Xamarin Studio integration with ServiceStackXS](http://docs.servicestack.net/csharp-add-servicestack-reference.html#xamarin-studio)

Providing [C# Native Types](http://docs.servicestack.net/csharp-add-servicestack-reference.html) 
support for developing iOS and Android mobile Apps using 
[Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and 
[Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) with 
[Xamarin Studio](https://www.xamarin.com/studio) on OSX. The **ServiceStackXS** plugin also provides a rich web service 
development experience developing Client applications with 
[Mono Develop on Linux](http://docs.servicestack.net/csharp-add-servicestack-reference.html#xamarin-studio-for-linux)

#### [Xcode integration with ServiceStackXC Plugin](http://docs.servicestack.net/swift-add-servicestack-reference.html)

Providing [an instant Native Typed API in Swift](http://docs.servicestack.net/swift-add-servicestack-reference.html) 
including generic Service Clients enabling a highly-productive workflow and effortless consumption of Web Services from 
native iOS and OSX Applications - directly from within Xcode!

#### [Android Studio integration with ServiceStackIDEA](http://docs.servicestack.net/java-add-servicestack-reference.html)

Providing [an instant Native Typed API in Java](http://docs.servicestack.net/java-add-servicestack-reference.html) 
and [Kotlin](http://docs.servicestack.net/kotlin-add-servicestack-reference.html)
including idiomatic Java Generic Service Clients supporting Sync and Async Requests by levaraging Android's AsyncTasks to enable the creation of services-rich and responsive native Java or Kotlin Mobile Apps on the Android platform - directly from within Android Studio!

#### [IntelliJ integration with ServiceStackIDEA](http://docs.servicestack.net/java-add-servicestack-reference.html#install-servicestack-idea-from-the-plugin-repository)

The ServiceStack IDEA plugin is installable directly from IntelliJ's Plugin repository and enables seamless integration with IntelliJ Java Maven projects for genearting a Typed API to quickly and effortlessly consume remote ServiceStack Web Services from pure cross-platform Java or Kotlin Clients.

#### [Eclipse integration with ServiceStackEclipse](https://github.com/ServiceStack/ServiceStack.Java/tree/master/src/ServiceStackEclipse#eclipse-integration-with-servicestack)

The unmatched productivity offered by [Java Add ServiceStack Reference](http://docs.servicestack.net/java-add-servicestack-reference.html) is also available in the 
[ServiceStackEclipse IDE Plugin](https://github.com/ServiceStack/ServiceStack.Java/tree/master/src/ServiceStackEclipse#eclipse-integration-with-servicestack) that's installable 
from the [Eclipse MarketPlace](https://marketplace.eclipse.org/content/servicestackeclipse) to provide deep integration of Add ServiceStack Reference with Eclipse Java Maven Projects
enabling Java Developers to effortlessly Add and Update the references of their evolving remote ServiceStack Web Services.

#### [servicestack-cli - Simple command-line utilities for ServiceStack](http://docs.servicestack.net/add-servicestack-reference.html#simple-command-line-utilities-for-servicestack)

In addition to our growing list of supported IDE's, the [servicestack-cli](https://github.com/ServiceStack/servicestack-cli)
cross-platform command-line npm scripts makes it easy for build servers, automated tasks and command-line runners of your 
favorite text editors to easily Add and Update ServiceStack References!

## Simple Customer Database REST Services Example

This example is also available as a [stand-alone integration test](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/CustomerRestExample.cs):

```csharp
//Web Service Host Configuration
public class AppHost : AppSelfHostBase
{
    public AppHost() 
        : base("Customer REST Example", typeof(CustomerService).Assembly) {}

    public override void Configure(Container container)
    {
        //Register which RDBMS provider to use
        container.Register<IDbConnectionFactory>(c => 
            new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

        using (var db = container.Resolve<IDbConnectionFactory>().Open())
        {
            //Create the Customer POCO table if it doesn't already exist
            db.CreateTableIfNotExists<Customer>();
        }
    }
}

//Web Service DTO's
[Route("/customers", "GET")]
public class GetCustomers : IReturn<GetCustomersResponse> {}

public class GetCustomersResponse
{
    public List<Customer> Results { get; set; } 
}

[Route("/customers/{Id}", "GET")]
public class GetCustomer : IReturn<Customer>
{
    public int Id { get; set; }
}

[Route("/customers", "POST")]
public class CreateCustomer : IReturn<Customer>
{
    public string Name { get; set; }
}

[Route("/customers/{Id}", "PUT")]
public class UpdateCustomer : IReturn<Customer>
{
    public int Id { get; set; }

    public string Name { get; set; }
}

[Route("/customers/{Id}", "DELETE")]
public class DeleteCustomer : IReturnVoid
{
    public int Id { get; set; }
}

// POCO DB Model
public class Customer
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

//Web Services Implementation
public class CustomerService : Service
{
    public object Get(GetCustomers request)
    {
        return new GetCustomersResponse { Results = Db.Select<Customer>() };
    }

    public object Get(GetCustomer request)
    {
        return Db.SingleById<Customer>(request.Id);
    }

    public object Post(CreateCustomer request)
    {
        var customer = new Customer { Name = request.Name };
        Db.Save(customer);
        return customer;
    }

    public object Put(UpdateCustomer request)
    {
        var customer = Db.SingleById<Customer>(request.Id);
        if (customer == null)
            throw HttpError.NotFound("Customer '{0}' does not exist".Fmt(request.Id));

        customer.Name = request.Name;
        Db.Update(customer);

        return customer;
    }

    public void Delete(DeleteCustomer request)
    {
        Db.DeleteById<Customer>(request.Id);
    }
}

```

### [Calling the above REST Service from any C#/.NET Client](http://docs.servicestack.net/csharp-add-servicestack-reference.html)

> No code-gen required, can re-use above Server DTOs:

```csharp
var client = new JsonServiceClient(BaseUri);

//GET /customers
var all = client.Get(new GetCustomers());                         // Count = 0

//POST /customers
var customer = client.Post(new CreateCustomer { Name = "Foo" });

//GET /customer/1
customer = client.Get(new GetCustomer { Id = customer.Id });      // Name = Foo

//GET /customers
all = client.Get(new GetCustomers());                             // Count = 1

//PUT /customers/1
customer = client.Put(
    new UpdateCustomer { Id = customer.Id, Name = "Bar" });       // Name = Bar

//DELETE /customers/1
client.Delete(new DeleteCustomer { Id = customer.Id });

//GET /customers
all = client.Get(new GetCustomers());                             // Count = 0
```

Same code also works with [Android, iOS, Xamarin.Forms, UWP and WPF clients](https://github.com/ServiceStackApps/HelloMobile).

> [F#](http://docs.servicestack.net/fsharp-add-servicestack-reference.html) and 
[VB.NET](http://docs.servicestack.net/vbnet-add-servicestack-reference.html) can re-use same 
[.NET Service Clients](http://docs.servicestack.net/csharp-client.html) and DTO's

### [Calling from TypeScript](http://docs.servicestack.net/typescript-add-servicestack-reference.html#ideal-typed-message-based-api)

```ts
const client = new JsonServiceClient(baseUrl);
const { results } = await client.get(new GetCustomers());
```

### [Calling from Swift](http://docs.servicestack.net/swift-add-servicestack-reference.html#jsonserviceclientswift)

```swift
let client = JsonServiceClient(baseUrl: BaseUri)

client.getAsync(GetCustomers())
    .then {
        let results = $0.results;
    }
```

### [Calling from Java](http://docs.servicestack.net/java-add-servicestack-reference.html#jsonserviceclient-usage)

```java
JsonServiceClient client = new JsonServiceClient(BaseUri);

GetCustomersResponse response = client.get(new GetCustomers());
List<Customer> results = response.results; 
```

### [Calling from Kotlin](http://docs.servicestack.net/kotlin-add-servicestack-reference.html#jsonserviceclient-usage)

```kotlin
val client = JsonServiceClient(BaseUri)

val response = client.get(GetCustomers())
val results = response.results
```

### [Calling from Dart](http://docs.servicestack.net/dart-add-servicestack-reference)

```dart
var client = new JsonServiceClient(BaseUri);

var response = await client.get(GetCustomers());
var results = client.results;
```

### [Calling from jQuery using TypeScript Definitions](http://docs.servicestack.net/typescript-add-servicestack-reference.html#typescript-interface-definitions)

```js
$.getJSON($.ss.createUrl("/customers", request), request, (r: GetCustomersResponse) => {
    var results = r.results;
});
```

Using TypeScript Definitions with Angular HTTP Client:

```ts
this.http.get<GetCustomersResponse>(createUrl('/customers', request)).subscribe(r => {
    this.results = r.results;
});
```

### Calling from jQuery

```js
$.getJSON(baseUri + "/customers", function(r) {
	var results = r.results;
});
```

That's all the application code required to create and consume a simple database-enabled REST Web Service!

## Getting Started

 * [Start with the **Getting Started** section](http://docs.servicestack.net/create-your-first-webservice.html)
 * [Example Apps and Demos](https://github.com/ServiceStackApps/LiveDemos)
 * [Community resources](http://docs.servicestack.net/community-resources.html)

### [Release Notes](https://servicestack.net/release-notes)

## Download

If you have [NuGet](http://www.nuget.org/) installed, the easiest way to get started is to: 

### [Install ServiceStack via NuGet](https://servicestack.net/download).

_Latest v4+ on NuGet is a [commercial release](https://servicestack.net/pricing) with [free quotas](https://servicestack.net/download#free-quotas)._

### [Docs and Downloads for older v3 BSD releases](https://github.com/ServiceStackV3/ServiceStackV3)

### [Live Demos](https://github.com/ServiceStackApps/LiveDemos)

**The [Definitive list of Example Projects, Use-Cases, Demos, Starter Templates](https://github.com/ServiceStackApps/LiveDemos)**
    
## Copying

Since September 2013, ServiceStack source code is available under GNU Affero General Public License/FOSS License Exception, see license.txt in the source. 
Alternative commercial licensing is also available, see https://servicestack.net/pricing for details.

## Contributing

Contributors need to approve the [Contributor License Agreement](https://docs.google.com/forms/d/16Op0fmKaqYtxGL4sg7w_g-cXXyCoWjzppgkuqzOeKyk/viewform) before any code will be reviewed, see the [Contributing docs](http://docs.servicestack.net/contributing.html) for more details. All contributions must include tests verifying the desired behavior.

## OSS Libraries used

ServiceStack includes source code of the great libraries below for some of its core functionality. 
Each library is released under its respective licence:

  - [Mono](https://github.com/mono/mono) [(MIT License)](https://github.com/mono/mono/blob/master/LICENSE)
  - [Funq IOC](http://funq.codeplex.com) [(MS-PL License)](https://opensource.org/licenses/MS-PL)
  - [Fluent Validation](https://github.com/JeremySkinner/FluentValidation) [(Apache License 2.0)](https://github.com/JeremySkinner/FluentValidation/blob/master/License.txt)
  - [Mini Profiler](https://github.com/MiniProfiler/dotnet) [(MIT License)](https://github.com/MiniProfiler/dotnet/blob/master/LICENSE.txt)
  - [Dapper](https://github.com/StackExchange/Dapper) [(Apache License 2.0)](http://www.apache.org/licenses/LICENSE-2.0)
  - [TweetStation's OAuth library](https://github.com/migueldeicaza/TweetStation) [(MIT License)](https://github.com/migueldeicaza/TweetStation/blob/master/LICENSE)
  - [MarkdownSharp](https://code.google.com/archive/p/markdownsharp) [(MIT License)](https://opensource.org/licenses/mit-license.php)
  - [MarkdownDeep](https://github.com/toptensoftware/markdowndeep) [(Apache License 2.0)](http://www.toptensoftware.com/markdowndeep/license)
  - [HtmlCompressor](https://code.google.com/archive/p/htmlcompressor) [(Apache License 2.0)](http://www.apache.org/licenses/LICENSE-2.0)
  - [JSMin](https://github.com/douglascrockford/JSMin/blob/master/jsmin.c) [(Apache License 2.0)](http://www.apache.org/licenses/LICENSE-2.0)
  - [RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream) [(MIT License)](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream/blob/master/LICENSE)
  - [ASP.NET MVC](https://github.com/aspnet/Mvc) [(Apache License 2.0)](https://github.com/aspnet/Mvc/blob/release/2.2/LICENSE.txt)
  - [CoreFX](https://github.com/dotnet/corefx) [(MIT License)](https://github.com/dotnet/corefx/blob/master/LICENSE.TXT)

## Find out More

Follow [@ServiceStack](https://twitter.com/ServiceStack) and 
[+ServiceStack](https://plus.google.com/u/0/communities/112445368900682590445) for project updates.

-----

## Core Team

 - [mythz](https://github.com/mythz) (Demis Bellot)
 - [layoric](https://github.com/layoric) (Darren Reid) / [@layoric](https://twitter.com/layoric)
 - [xplicit](https://github.com/xplicit) (Sergey Zhukov) / [@quantumcalc](https://twitter.com/quantumcalc)
 - [desunit](https://github.com/desunit) (Sergey Bogdanov) / [@desunit](https://twitter.com/desunit)
 - [arxisos](https://github.com/arxisos) (Steffen Müller) / [@arxisos](https://twitter.com/arxisos)

## Contributors 

A big thanks to GitHub and all of ServiceStack's contributors:

 - [bman654](https://github.com/bman654) (Brandon Wallace)
 - [iristyle](https://github.com/iristyle) (Ethan Brown)
 - [superlogical](https://github.com/superlogical) (Jake Scott)
 - [itamar82](https://github.com/itamar82)
 - [chadwackerman](https://github.com/chadwackerman)
 - [derfsplat](https://github.com/derfsplat)
 - [johnacarruthers](https://github.com/johnacarruthers) (John Carruthers)
 - [mvitorino](https://github.com/mvitorino) (Miguel Vitorino)
 - [bsiegel](https://github.com/bsiegel) (Brandon Siegel)
 - [mdavid](https://github.com/mdavid) (M. David Peterson)
 - [lhaussknecht](https://github.com/lhaussknecht) (Louis Haussknecht)
 - [grendello](https://github.com/grendello) (Marek Habersack)
 - [SteveDunn](https://github.com/SteveDunn) (Steve Dunn)
 - [kcherenkov](https://github.com/kcherenkov) (Konstantin Cherenkov)
 - [timryan](https://github.com/timryan) (Tim Ryan)
 - [letssellsomebananas](https://github.com/letssellsomebananas) (Tymek Majewski)
 - [danbarua](https://github.com/danbarua) (Dan Barua)
 - [JonCanning](https://github.com/JonCanning) (Jon Canning)
 - [paegun](https://github.com/paegun) (James Gorlick)
 - [pvasek](https://github.com/pvasek) (pvasek)
 - [derfsplat](https://github.com/derfsplat) (derfsplat)
 - [justinrolston](https://github.com/justinrolston) (Justin Rolston)
 - [danmiser](https://github.com/danmiser) (Dan Miser)
 - [danatkinson](https://github.com/danatkinson) (Dan Atkinson)
 - [brainless83](https://github.com/brainless83) (Thomas Grassauer)
 - [angelcolmenares](https://github.com/angelcolmenares) (angel colmenares)
 - [dbeattie71](https://github.com/dbeattie71) (Derek Beattie)
 - [danielwertheim](https://github.com/danielwertheim) (Daniel Wertheim)
 - [greghroberts](https://github.com/greghroberts) (Gregh Roberts)
 - [int03](https://github.com/int03) (Selim Selçuk)
 - [andidog](https://github.com/AndiDog) (AndiDog)
 - [chuckb](https://github.com/chuckb) (chuckb)
 - [niemyjski](https://github.com/niemyjski) (Blake Niemyjski)
 - [mj1856](https://github.com/mj1856) (Matt Johnson)
 - [matthieugd](https://github.com/matthieugd) (Matthieu)
 - [tomaszkubacki](https://github.com/tomaszkubacki) (Tomasz Kubacki)
 - [e11137](https://github.com/e11137) (Rogelio Canedo)
 - [davidroth](https://github.com/davidroth) (David Roth)
 - [meebey](https://github.com/meebey) (Mirco Bauer)
 - [codedemonuk](https://github.com/codedemonuk) (Pervez Choudhury)
 - [jrosskopf](https://github.com/jrosskopf) (Joachim Rosskopf)
 - [friism](https://github.com/friism) (Michael Friis)
 - [mp3125](https://github.com/mp3125)
 - [aurimas86](https://github.com/aurimas86)
 - [parnham](https://github.com/parnham) (Dan Parnham)
 - [yeurch](https://github.com/yeurch) (Richard Fawcett)
 - [damianh](https://github.com/damianh) (Damian Hickey)
 - [freeman](https://github.com/freeman) (Michel Rasschaert)
 - [kvervo](https://github.com/kvervo) (Kvervo)
 - [pauldbau](https://github.com/pauldbau) (Paul Du Bois)
 - [justinpihony](https://github.com/JustinPihony) (Justin Pihony) 
 - [bokmadsen](https://github.com/bokmadsen) (Bo Kingo Damgaard)
 - [dragan](https://github.com/dragan) (Dale Ragan)
 - [sneal](https://github.com/sneal) (Shawn Neal)
 - [johnsheehan](https://github.com/johnsheehan) (John Sheehan)
 - [jschlicht](https://github.com/jschlicht) (Jared Schlicht)
 - [kumarnitin](https://github.com/kumarnitin) (Nitin Kumar)
 - [davidchristiansen](https://github.com/davidchristiansen) (David Christiansen)  
 - [paulecoyote](https://github.com/paulecoyote) (Paul Evans)
 - [kongo2002](https://github.com/kongo2002) (Gregor Uhlenheuer)
 - [brannonking](https://github.com/brannonking) (Brannon King)
 - [alexandrerocco](https://github.com/alexandrerocco) (Alexandre Rocco)
 - [cbarbara](https://github.com/cbarbara)
 - [assaframan](https://github.com/assaframan) (Assaf Raman)
 - [csakshaug](https://github.com/csakshaug) (Christian Sakshaug)
 - [johnman](https://github.com/johnman)
 - [jarroda](https://github.com/jarroda)
 - [ssboisen](https://github.com/ssboisen) (Simon Skov Boisen)
 - [paulduran](https://github.com/paulduran) (Paul Duran)
 - [pruiz](https://github.com/pruiz) (Pablo Ruiz García)
 - [fantasticjamieburns](https://github.com/fantasticjamieburns)
 - [pseabury](https://github.com/pseabury)
 - [kevingessner](https://github.com/kevingessner) (Kevin Gessner)
 - [iskomorokh](https://github.com/iskomorokh) (Igor Skomorokh)
 - [royjacobs](https://github.com/royjacobs) (Roy Jacobs)
 - [robertmircea](https://github.com/robertmircea) (Robert Mircea)
 - [markswiatek](https://github.com/markswiatek) (Mark Swiatek)
 - [flq](https://github.com/flq) (Frank Quednau)
 - [ashd](https://github.com/ashd) (Ash D)
 - [thanhhh](https://github.com/thanhhh)
 - [algra](https://github.com/algra) (Alexey Gravanov)
 - [jimschubert](https://github.com/jimschubert) (Jim Schubert)
 - [gkathire](https://github.com/gkathire)
 - [mikaelwaltersson](https://github.com/mikaelwaltersson) (Mikael Waltersson)
 - [asunar](https://github.com/asunar) (Alper)
 - [chucksavage](https://github.com/chucksavage) (Chuck Savage)
 - [sashagit](https://github.com/sashagit) (Sasha)
 - [froyke](https://github.com/froyke) (Froyke)
 - [dbhobbs](https://github.com/dbhobbs) (Daniel Hobbs)
 - [bculberson](https://github.com/bculberson) (Brad Culberson)
 - [awr](https://github.com/awr) (Andrew)
 - [pingvinen](https://github.com/pingvinen) (Patrick)
 - [citndev](https://github.com/CITnDev) (Sebastien Curutchet)
 - [cyberprune](https://github.com/cyberprune)
 - [jorbor](https://github.com/jorbor) (Jordan Hayashi)
 - [bojanv55](https://github.com/bojanv55)
 - [i-e-b](https://github.com/i-e-b) (Iain Ballard)
 - [pietervp](https://github.com/pietervp) (Pieter Van Parys)
 - [franklinwise](https://github.com/franklinwise)
 - [ckasabula](https://github.com/ckasabula) (Chuck Kasabula)
 - [dortzur](https://github.com/dortzur) (Dor Tzur)
 - [allenarthurgay](https://github.com/allenarthurgay) (Allen Gay)
 - [viceberg](https://github.com/vIceBerg) 
 - [vansha](https://github.com/vansha) (Ivan Korneliuk)
 - [aaronlerch](https://github.com/aaronlerch) (Aaron Lerch)
 - [glikoz](https://github.com/glikoz)
 - [danielcrenna](https://github.com/danielcrenna) (Daniel Crenna)
 - [stevegraygh](https://github.com/stevegraygh) (Steve Graygh)
 - [jrmitch120](https://github.com/jrmitch120) (Jeff Mitchell)
 - [manuelnelson](https://github.com/manuelnelson) (Manuel Nelson)
 - [babcca](https://github.com/babcca) (Petr Babicka)
 - [jgeurts](https://github.com/jgeurts) (Jim Geurts)
 - [driis](https://github.com/driis) (Dennis Riis)
 - [gshackles](https://github.com/gshackles) (Greg Shackles)
 - [jsonmez](https://github.com/jsonmez) (John Sonmez)
 - [dchurchland](https://github.com/dchurchland) (David Churchland)
 - [softwx](https://github.com/softwx) (Steve Hatchett)
 - [ggeurts](https://github.com/ggeurts) (Gerke Geurts)
 - [andrewrissing](https://github.com/AndrewRissing) (Andrew Rissing)
 - [jjavery](https://github.com/jjavery) (James Javery)
 - [suremaker](https://github.com/suremaker) (Wojtek)
 - [cheesebaron](https://github.com/cheesebaron) (Tomasz Cielecki)
 - [mikkelfish](https://github.com/mikkelfish) (Mikkel Fishman)
 - [johngibb](https://github.com/johngibb) (John Gibb)
 - [stabbylambda](https://github.com/stabbylambda) (David Stone)
 - [mikepugh](https://github.com/mikepugh) (Mike Pugh)
 - [permalmberg](https://github.com/permalmberg) (Per Malmberg)
 - [adamralph](https://github.com/adamralph) (Adam Ralph)
 - [shamsulamry](https://github.com/shamsulamry) (Shamsul Amry)
 - [peterlazzarino](https://github.com/peterlazzarino) (Peter Lazzarino)
 - [kevin-montrose](https://github.com/kevin-montrose) (Kevin Montrose)
 - [msarchet](https://github.com/msarchet) (Michael Sarchet)
 - [jeffgabhart](https://github.com/jeffgabhart) (Jeff Gabhart)
 - [pkudinov](https://github.com/pkudinov) (Pavel Kudinov)
 - [permalmberg](https://github.com/permalmberg) (Per Malmberg)
 - [namman](https://github.com/namman) (Nick Miller)
 - [leon-andria](https://github.com/leon-andria) (Leon Andria)
 - [kkolstad](https://github.com/kkolstad) (Kenneth Kolstad)
 - [electricshaman](https://github.com/electricshaman) (Jeff Smith)
 - [ecgan](https://github.com/ecgan) (Gan Eng Chin)
 - [its-tyson](https://github.com/its-tyson) (Tyson Stolarski)
 - [tischlda](https://github.com/tischlda) (David Tischler)
 - [connectassist](https://github.com/connectassist) (Carl Healy)
 - [starteleport](https://github.com/starteleport)
 - [jfoshee](https://github.com/jfoshee) (Jacob Foshee)
 - [nardin](https://github.com/nardin) (Mamaev Michail)
 - [cliffstill](https://github.com/cliffstill)
 - [somya](https://github.com/somya) (Somya Jain)
 - [thinkbeforecoding](https://github.com/thinkbeforecoding) (Jérémie Chassaing)
 - [paksys](https://github.com/paksys) (Khalil Ahmad)
 - [mcguinness](https://github.com/mcguinness) (Karl McGuinness)
 - [jpasichnyk](https://github.com/jpasichnyk) (Jesse Pasichnyk)
 - [waynebrantley](https://github.com/waynebrantley) (Wayne Brantley)
 - [dcartoon](https://github.com/dcartoon) (Dan Cartoon)
 - [alexvodovoz](https://github.com/alexvodovoz) (Alex Vodovoz)
 - [jluchiji](https://github.com/jluchiji) (Denis Luchkin-Zhou)
 - [grexican](https://github.com/grexican)
 - [akoslukacs](https://github.com/akoslukacs) (Ákos Lukács)
 - [medianick](https://github.com/medianick) (Nick Jones)
 - [arhoads76](https://github.com/arhoads76)
 - [dylanvdmerwe](https://github.com/dylanvdmerwe) (Dylan v.d Merwe)
 - [mattiasw2](https://github.com/mattiasw2) (Mattias)
 - [paultyng](https://github.com/paultyng) (Paul Tyng)
 - [h2oman](https://github.com/h2oman) (Jason Waterman)
 - [anewton](https://github.com/anewton) (Allen Newton)
 - [sami1971](https://github.com/sami1971)
 - [russellchadwick](https://github.com/russellchadwick) (Russell Chadwick)
 - [cyberzed](https://github.com/cyberzed) (Stefan Daugaard Poulsen)
 - [filipw](https://github.com/filipw) (Filip Wojcieszyn)
 - [ghuntley](https://github.com/ghuntley) (Geoffrey Huntley)
 - [baramuse](https://github.com/baramuse)
 - [pdegenhardt](https://github.com/pdegenhardt) (Phil Degenhardt)
 - [captncraig](https://github.com/captncraig) (Craig Peterson)
 - [abattery](https://github.com/abattery) (Jae sung Chung)
 - [biliktamas79](https://github.com/biliktamas79)
 - [garuma](https://github.com/garuma) (Jérémie Laval)
 - [dsimunic](https://github.com/dsimunic)
 - [adamfowleruk](https://github.com/adamfowleruk) (Adam Fowler)
 - [bfriesen](https://github.com/bfriesen) (Brian Friesen)
 - [roryf](https://github.com/roryf) (Rory Fitzpatrick)
 - [stefandevo](https://github.com/stefandevo)
 - [gdassac](https://github.com/gdassac)
 - [metal10k](https://github.com/metal10k)
 - [cmelgarejo](https://github.com/cmelgarejo)
 - [skaman](https://github.com/skaman)
 - [rossipedia](https://github.com/rossipedia) (Bryan J. Ross)
 - [wimatihomer](https://github.com/wimatihomer) (Wim Pool)
 - [sword-breaker](https://github.com/sword-breaker)
 - [adebisi-fa](https://github.com/adebisi-fa) (Adebisi Foluso A.)
 - [mbischoff](https://github.com/mbischoff) (M. Bischoff)
 - [ivanfioravanti](https://github.com/ivanfioravanti) (Ivan Fioravanti)
 - [inhibition](https://github.com/inhibition) (Keith Hassen)
 - [joshearl](https://github.com/joshearl) (Josh Earl)
 - [friism](https://github.com/friism) (Michael Friis)
 - [corkupine](https://github.com/corkupine)
 - [bchavez](https://github.com/bchavez) (Brian Chavez)
 - [nhhagen](https://github.com/nhhagen) (Niels Henrik Hagen)
 - [daggmano](https://github.com/daggmano) (Darren Oster)
 - [chappoo](https://github.com/chappoo) (Steve Chapman)
 - [julrichkieffer](https://github.com/julrichkieffer) (Julrich Kieffer)
 - [adamclarsen](https://github.com/adamclarsen) (Adam Larsen)
 - [joero74](https://github.com/joero74) (Joerg Rosenkranz)
 - [ddotlic](https://github.com/ddotlic) (Drazen Dotlic)
 - [chrismcv](https://github.com/chrismcv) (Chris McVittie)
 - [marcioalthmann](https://github.com/marcioalthmann) (Márcio Fábio Althmann)
 - [mmertsock](https://github.com/mmertsock) (Mike Mertsock)
 - [johnkamau](https://github.com/johnkamau) (John Kamau)
 - [uhaciogullari](https://github.com/uhaciogullari) (Ufuk Hacıoğulları)
 - [davybrion](https://github.com/davybrion) (Davy Brion)
 - [aleshi](https://github.com/aleshi) (Alexander Shiryaev)
 - [alexandryz](https://github.com/alexandryz) (Alexandr Zaozerskiy)
 - [mistobaan](https://github.com/mistobaan) (Fabrizio Milo)
 - [niemyjski](https://github.com/niemyjski) (Blake Niemyjski)
 - [alexandernyquist](https://github.com/alexandernyquist) (Alexander Nyquist)
 - [mcduck76](https://github.com/mcduck76)
 - [kojoru](https://github.com/kojoru)
 - [jeremy-bridges](https://github.com/jeremy-bridges) (Jeremy Bridges)
 - [andreabalducci](https://github.com/andreabalducci) (Andrea Balducci)
 - [robertthegrey](https://github.com/RobertTheGrey) (Robert Greyling)
 - [robertbeal](https://github.com/robertbeal) (Robert Beal)
 - [improvedk](https://github.com/improvedk) (Mark Rasmussen)
 - [foresterh](https://github.com/foresterh) (Jamie Houston)
 - [peterkahl](https://github.com/peterkahl) (Peter Kahl)
 - [helgel](https://github.com/helgel)
 - [anthonycarl](https://github.com/anthonycarl) (Anthony Carl)
 - [mrjul](https://github.com/mrjul) (Julien Lebosquain)
 - [pwhe23](https://github.com/pwhe23) (Paul Wheeler)
 - [aleksd](https://github.com/aleksd)
 - [miketrebilcock](https://github.com/miketrebilcock) (Mike Trebilcock)
 - [markwoodhall](https://github.com/markwoodhall) (Mark Woodhall)
 - [theonlylawislove](https://github.com/theonlylawislove) (Paul Knopf)
 - [callumvass](https://github.com/callumvass) (Callum Vass)
 - [bpruitt-goddard](https://github.com/bpruitt-goddard)
 - [gregpakes](https://github.com/gregpakes) (Greg Pakes)
 - [caspiancanuck](https://github.com/caspiancanuck) (Caspian Canuck)
 - [merwer](https://github.com/merwer)
 - [pavelsavara](https://github.com/pavelsavara) (Pavel Savara)
 - [markwalls](https://github.com/markwalls) (Mark Walls)
 - [prasannavl](https://github.com/prasannavl) (Prasanna Loganathar)
 - [wilfrem](https://github.com/wilfrem)
 - [emiba](https://github.com/emiba)
 - [lucky-ly](https://github.com/lucky-ly) (Dmitry Svechnikov)
 - [hhandoko](https://github.com/hhandoko) (Herdy Handoko)
 - [datawingsoftware](https://github.com/datawingsoftware)
 - [tal952](https://github.com/tal952)
 - [bretternst](https://github.com/bretternst)
 - [kevinhoward](https://github.com/kevinhoward) (Kevin Howard)
 - [mattbutton](https://github.com/mattbutton) (Matt Button)
 - [torbenrahbekkoch](https://github.com/torbenrahbekkoch) (Torben Rahbek Koch)
 - [pilotmartin](https://github.com/pilotmartin) (Pilot Martin)
 - [catlion](https://github.com/catlion)
 - [tstade](https://github.com/tstade) (Toft Stade)
 - [niltz](https://github.com/niltz) (Jeff Sawatzky)
 - [nhalm](https://github.com/nhalm)
 - [fhurta](https://github.com/fhurta) (Filip Hurta)
 - [discobanan](https://github.com/discobanan)
 - [x-cray](https://github.com/x-cray)
 - [jeremistadler](https://github.com/jeremistadler) (Jeremi Stadler)
 - [bangbite](https://github.com/bangbite)
 - [felipesabino](https://github.com/felipesabino) (Felipe Sabino)
 - [xelom](https://github.com/xelom) (Arıl Bozoluk)
 - [shiweichuan](https://github.com/shiweichuan) (Weichuan Shi)
 - [kojoru](https://github.com/kojoru) (Konstantin Yakushev)
 - [eddiegroves](https://github.com/eddiegroves) (Eddie Groves)
 - [fetters5](https://github.com/fetters5)
 - [rcollette](https://github.com/rcollette) (Richard Collette)
 - [urihendler](https://github.com/urihendler) (Uri Hendler)
 - [laurencee](https://github.com/laurencee) (Laurence Evans)
 - [m-andrew-albright](https://github.com/m-andrew-albright) (Andrew Albright)
 - [lee337](https://github.com/lee337) (Lee Venkatsamy)
 - [kaza](https://github.com/kaza)
 - [mishfit](https://github.com/mishfit)
 - [rfvgyhn](https://github.com/rfvgyhn) (Chris)
 - [caioproiete](https://github.com/caioproiete) (Caio Proiete)
 - [sjuxax](https://github.com/sjuxax) (Jeff Cook)
 - [madaleno](https://github.com/madaleno) (Luis Madaleno)
 - [yavosh](https://github.com/yavosh) (Yavor Shahpasov)
 - [fvoncina](https://github.com/fvoncina) (Facundo Voncina)
 - [devrios](https://github.com/devrios) (Dev Rios)
 - [bfkelsey](https://github.com/bfkelsey) (Ben Kelsey)
 - [maksimenko](https://github.com/maksimenko)
 - [dixon](https://github.com/dixon) (Jarrod Dixon)
 - [kal](https://github.com/kal) (Kal Ahmed)
 - [mhanney](https://github.com/mhanney) (Michael Hanney)
 - [bcms](https://github.com/bcms)
 - [mgravell](https://github.com/mgravell) (Marc Gravell)
 - [lafama](https://github.com/lafama) (Denis Ndwiga)
 - [jamesgroat](https://github.com/jamesgroat) (James Groat)
 - [jamesearl](https://github.com/jamesearl) (James Cunningham)
 - [remkoboschker](https://github.com/remkoboschker) (Remko Boschker)
 - [shelakel](https://github.com/shelakel)
 - [schmidt4brains](https://github.com/schmidt4brains) (Doug Schmidt)
 - [joplaal](https://github.com/joplaal)
 - [aifdsc](https://github.com/aifdsc) (Stephan Desmoulin)
 - [nicklarsen](https://github.com/nicklarsen) (NickLarsen)
 - [connectassist](https://github.com/connectassist) (Carl Healy)
 - [et1975](https://github.com/et1975) (Eugene Tolmachev)
 - [barambani](https://github.com/barambani)
 - [nhalm](https://github.com/et1975)


***

## Similar open source projects

Similar Open source .NET projects for developing or accessing web services include:

 * [Nancy Fx](http://nancyfx.org) - A Sinatra-inspired lightweight Web Framework for .NET:
 * [Fubu MVC](https://fubumvc.github.io/) - A "Front Controller" pattern-style MVC framework designed for use in web applications built on ASP.NET:
 * [Rest Sharp](http://restsharp.org) - An open source REST client for .NET
