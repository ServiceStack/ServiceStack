Join the [google group](http://groups.google.com/group/servicestack) or
follow [@demisbellot](http://twitter.com/demisbellot) and [@ServiceStack](http://twitter.com/servicestack)
for twitter updates. You can also join a growing crowd of ServiceStack users on [JabbR](http://jabbr.net/#/rooms/servicestack) if you want to chat.

Service Stack is a high-performance .NET web services framework _(including a number of high-performance sub-components: see below)_ 
that simplifies the development of XML, JSON, JSV and WCF SOAP [Web Services](https://github.com/ServiceStack/ServiceStack/wiki/Service-Stack-Web-Services). 
For more info check out [servicestack.net](http://www.servicestack.net).

Simple REST service example
=========================== 

```csharp
//Web Service Host Configuration
public class AppHost : AppHostBase
{
	public AppHost() : base("Backbone.js TODO", typeof(TodoService).Assembly) {}

	public override void Configure(Funq.Container container)
	{
		//Register Web Service dependencies
		container.Register(new TodoRepository());

		//Register user-defined REST-ful routes			
		Routes
		  .Add<Todo>("/todos")
		  .Add<Todo>("/todos/{Id}");
	}
}


//REST Resource DTO
public class Todo 
{
	public long Id { get; set; }
	public string Content { get; set; }
	public int Order { get; set; }
	public bool Done { get; set; }
}

//Todo REST Service implementation
public class TodoService : RestServiceBase<Todo>
{
	public TodoRepository Repository { get; set; }  //Injected by IOC

	public override object OnGet(Todo request)
	{
		if (request.Id != default(long))
			return Repository.GetById(request.Id);

		return Repository.GetAll();
	}

	public override object OnPost(Todo todo)
	{
		return Repository.Store(todo);
	}

	public override object OnPut(Todo todo)
	{
		return Repository.Store(todo);
	}

	public override object OnDelete(Todo request)
	{
		Repository.DeleteById(request.Id);
		return null;
	}
}
```

### Calling the above TODO REST service from any C#/.NET Client

```csharp
//no code-gen required, can re-use above DTO's

var restClient = new JsonServiceClient("http://localhost/Backbone.Todo");
var all = restClient.Get<List<Todo>>("/todos"); // Count = 0

var todo = restClient.Post<Todo>("/todos", new Todo { Content = "New TODO", Order = 1 }); //todo.Id = 1
all = restClient.Get<List<Todo>>("/todos"); // Count = 1

todo.Content = "Updated TODO";
todo = restClient.Post<Todo>("/todos", todo); // todo.Content = Updated TODO

restClient.Delete<Todo>("/todos/" + todo.Id);
all = restClient.Get<List<Todo>>("/todos"); // Count = 0
```

### Calling the TODO REST service from jQuery

    $.getJSON("http://localhost/Backbone.Todo/todos", function(todos) {
    	alert(todos.length == 1);
    });


That's all the application code required to create a simple REST web service.

##Live Demo of Backbone TODO app (running on Linux/MONO):

**[http://www.servicestack.net/Backbone.Todos/](http://www.servicestack.net/Backbone.Todos/)**

Preview links using just the above code sample with (live demo running on Linux):

ServiceStack's strong-typed nature allows it to infer a greater intelligence of your web services and is able to provide a 
host of functionality for free, out of the box without any configuration required:

  * Host on different formats and endpoints: [XML](http://www.servicestack.net/Backbone.Todos/todos?format=xml), 
    [JSON](http://www.servicestack.net/Backbone.Todos/todos?format=json), [JSV](http://www.servicestack.net/Backbone.Todos/todos?format=jsv),
    [CSV](http://www.servicestack.net/Backbone.Todos/todos?format=csv) 
    
  * [A HTML5 Report format to view your webservics data in a human-friendly view](http://www.servicestack.net/Backbone.Todos/todos?format=html)
  
  * [An auto generated api metadata page, with links to your web service XSD's and WSDL's](http://www.servicestack.net/Backbone.Todos/metadata)
  
## Getting Started

 * **[Read the documentation on the ServiceStack Wiki](https://github.com/ServiceStack/ServiceStack/wiki)**
 * [Community resources](https://github.com/ServiceStack/ServiceStack/wiki/Community-Resources)

## Download

If you have [NuGet](http://nuget.org) installed, the easiest way to get started is to install ServiceStack via NuGet:

If you just want ServiceStack hosted at `/` - Create an empty ASP.NET Web Application and
![Install-Pacakage ServiceStack.Host.Mvc](http://www.servicestack.net/img/nuget-servicestack.host.aspnet.png)

Otherwise if you want to host ServiceStack Side-by-Side with MVC: Hosted at `/api` - Create an empty MVC Web Application and
![Install-Pacakage ServiceStack.Host.Mvc](http://www.servicestack.net/img/nuget-servicestack.host.mvc.png)

The above packages include a complete working Backbone.Todos demo, if just want the binaries (without config) use:
![Install-Pacakage ServiceStack](http://www.servicestack.net/img/nuget-servicestack.png)

To help get started you should also download the ServiceStack.Examples projects (includes dlls, demos and starter templates):

  * **[ServiceStack.Examples/downloads/](https://github.com/ServiceStack/ServiceStack.Examples/downloads)**

If you prefer not to use NuGet and just want to download the latest release binaries get them at:

  * **[ServiceStack/downloads/](https://github.com/ServiceStack/ServiceStack/downloads)**

Alternatively if you want keep up with the latest version you can always use the power of Git :)

    git clone git://github.com/ServiceStack/ServiceStack.git

[Release notes for major releases](https://github.com/ServiceStack/ServiceStack/wiki/Release-Notes)

# Features of a modern web services framework

 Developed in the modern age, Service Stack provides an alternate, cleaner POCO-driven way of creating web services, featuring:

### A DRY, strongly-typed 'pure model' REST Web Services Framework
Unlike other web services frameworks ServiceStack let's you develop web services using strongly-typed models and DTO's.
This lets ServiceStack and other tools to have a greater intelligence about your services allowing:

- [Multiple serialization formats (JSON, XML, JSV and SOAP with extensible plugin model for more)](http://servicestack.net/ServiceStack.Hello/servicestack/metadata)
- [A single re-usable C# Generic Client (In JSON, JSV, XML and SOAP flavours) that can talk to all your services.](https://github.com/ServiceStack/ServiceStack.Extras/blob/master/doc/UsageExamples/UsingServiceClients.cs)
- [Re-use your Web Service DTOs (i.e. no code-gen) on your client applications so you're never out-of-sync](https://github.com/ServiceStack/ServiceStack.Extras/blob/master/doc/UsageExamples/UsingServiceClients.cs)
- [Automatic serialization of Exceptions in your DTOs ResponseStatus](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.ServiceInterface/ServiceBase.cs#L154)
- [The possibility of a base class for all your services to put high-level application logic (i.e security, logging, etc)](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.ServiceInterface/ServiceBase.cs#L24)
- [Highly testable, your in-memory unit tests for your service can also be used as integration tests](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.IntegrationTests/Tests/WebServicesTests.cs)
- [Built-in rolling web service error logging (if Redis is Configured in your AppHost)](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.ServiceInterface/ServiceBase.cs#L122)
- [Rich REST and HTML support on all web services with x-www-form-urlencoded & multipart/form-data (i.e. FORM posts and file uploads)](http://servicestack.net/ServiceStack.Hello/)

## Define web services following Martin Fowlers Data Transfer Object Pattern:

Service Stack was heavily influenced by [**Martin Fowlers Data Transfer Object Pattern**](http://martinfowler.com/eaaCatalog/dataTransferObject.html):

>When you're working with a remote interface, such as Remote Facade (388), each call to it is expensive. 
>As a result you need to reduce the number of calls, and that means that you need to transfer more data 
>with each call. One way to do this is to use lots of parameters. 
>However, this is often awkward to program - indeed, it's often impossible with languages such as Java 
>that return only a single value.
>
>The solution is to create a Data Transfer Object that can hold all the data for the call. It needs to be serializable to go across the connection. 
>Usually an assembler is used on the server side to transfer data between the DTO and any domain objects.

The Request and Response DTO's used to define web services in ServiceStack are standard `DataContract` POCO's while the implementation just needs to inherit from a testable and dependency-free `IService<TRequestDto>`. As a bonus for keeping your DTO's in a separate dependency-free .dll, you're able to re-use them in your C#/.NET clients providing a strongly-typed API without any code-gen what-so-ever. Also your DTO's *define everything* Service Stack does not pollute your web services with any additional custom artefacts or markup.

Service Stack re-uses the custom artefacts above and with zero-config and without imposing any extra burden on the developer adds discover-ability and provides hosting of your web service on a number of different physical end-points which as of today includes: XML (+REST), JSON (+REST), JSV (+REST) and SOAP 1.1 / SOAP 1.2.

<a name="anti-wcf"></a>
### WCF the anti-DTO Web Services Framework
Unfortunately this best-practices convention is effectively discouraged by Microsoft's WCF SOAP Web Services framework as they encourage you to develop API-specific RPC method calls by mandating the use method signatures to define your web services API. This results in less re-usable, more client-sepcfic APIs that encourages more remote method calls. 

Unhappy with this perceived anit-pattern in WCF, ServiceStack was born providing a Web Sevice framework that embraces best-practices for calling remote services, using config-free, convention-based DTO's.


### Full support for unit and integration tests
Your application logic should not be tied to a third party vendor's web service host platform.
In Service Stack they're not, your web service implementations are host and end-point ignorant, dependency-free and can be unit-tested independently of ASP.NET, Service Stack or its IOC.

Without any code changes unit tests written can be re-used and run as integration tests simply by switching the IServiceClient used to point to a configured end-point host.

### Built-in Funq IOC container
Configured to auto-wire all of your web services with your registered dependencies.
[Funq](http://funq.codeplex.com) was chosen for it's [high-performance](http://www.codeproject.com/Articles/43296/Introduction-to-Munq-IOC-Container-for-ASP-NET.aspx), low footprint and intuitive full-featured minimalistic API.

### Encourages development of message-style, re-usable and batch-full web services
Entire POCO types are used to define the request and response DTO's to promote the creation well-defined coarse-grained web services. Message-based interfaces are best-practices when dealing with out-of-process calls as they are can batch more work using less network calls and are ultimately more re-usable as the same operation can be called using different calling semantics. This is in stark contrast to WCF's Operation or Service contracts which encourage RPC-style, application-specific web services by using method signatures to define each operation.

As it stands in general-purpose computing today, there is nothing more expensive you can do than a remote network call. Although easier for the newbie developer, by using _methods_ to define web service operations, WCF is promoting bad-practices by encouraging them to design and treat web-service calls like normal function calls even though they are millions of times slower. Especially at the app-server tier, nothing hurts performance and scalability of your client and server than multiple dependent and synchronous web service calls.

Batch-full, message-based web services are ideally suited in development of SOA services as they result in fewer, richer and more re-usable web services that need to be maintained. RPC-style services normally manifest themselves from a *client perspective* that is the result of the requirements of a single applications data access scenario. Single applications come and go over time while your data and services are poised to hang around for the longer term. Ideally you want to think about the definition of your web service from a *services and data perspective* and how you can expose your data so it is more re-usable by a number of your clients.

### Cross Platform Web Services Framework
With Mono on Linux now reaching full-maturity, Service Stack runs on .NET or Linux with Mono and can either be hosted inside an ASP.NET Web Application, Windows service or Console application running in or independently of a Web Server.

### Low Coupling for maximum accessibility and testability
No coupling between the transport's endpoint and your web service's payload. You can re-use your existing strongly-typed web service DTO's with any .NET client using the available Soap, Xml and Json Service Clients - giving you a strongly-typed API while at the same time avoiding the need for any generated code.

  * The most popular web service endpoints are configured by default. With no extra effort, each new web service created is immediately available and [discoverable](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Metadata) on the following end points:
    * [XML (+REST)](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Xml/Metadata?op=GetFactorial)
    * [JSON (+REST)](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Json/Metadata?op=GetFactorial)
    * [JSV (+REST)](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Jsv/Metadata?op=GetFactorial)
    * [SOAP 1.1](http://www.servicestack.net/ServiceStack.Hello/servicestack/soap11)
    * [SOAP 1.2](http://www.servicestack.net/ServiceStack.Hello/servicestack/soap12)
  * View the [Service Stack endpoints page](https://github.com/ServiceStack/ServiceStack/wiki/Service-Stack-Web-Services) for our recommendations on which endpoint to use and when.

# High Performance Sub Projects
Also included in ServiceStack are libraries that are useful in the development of high performance web services:

 * [ServiceStack.Text](https://github.com/ServiceStack/ServiceStack.Text) - The home of ServiceStack's JSON and JSV text serializers, the fastest text serializers for .NET
   * [JsonSerializer](http://www.servicestack.net/mythz_blog/?p=344) - The fastest JSON Serializer for .NET. Over 3 times faster than other .NET JSON serialisers.
   * [TypeSerializer](https://github.com/ServiceStack/ServiceStack.Text) - The JSV-format, a fast, compact text serializer that is very resilient to schema changes and is:
       * 3.5x quicker and 2.6x smaller than the .NET XML DataContractSerializer and
       * 5.3x quicker and 1.3x smaller than the .NET JSON DataContractSerializer - _[view the detailed benchmarks](http://www.servicestack.net/benchmarks/NorthwindDatabaseRowsSerialization.1000000-times.2010-02-06.html)_

 * [ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis) - An API complete C# [Redis](http://code.google.com/p/redis/) client with native support for persisting C# POCO objects.
   * You can find links to the latest windows builds at the end of [this StackOverflow Answer on Redis](http://stackoverflow.com/questions/1777103/what-nosql-solutions-are-out-there-for-net/2760282#2760282)
   * [Redis Admin UI](http://www.servicestack.net/mythz_blog/?p=381) - An Ajax GUI admin tool to help visualize your Redis data.

 * [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) - A convention-based, configuration free lightweight ORM that uses attributes from DataAnnotations to infer the table schema. Currently supports both Sqlite and SqlServer.

 * [Caching](https://github.com/ServiceStack/ServiceStack/wiki/Caching) - A common interface for caching with providers for:
   * Memcached
   * In Memory Cache
   * Redis


## Find out More
 * Twitter: to get updates of ServiceStack, follow [@demisbellot](http://twitter.com/demisbellot) or [@ServiceStack](http://twitter.com/ServiceStack) on twitter.
 * Email any questions to _demis.bellot@gmail.com_

## Future Roadmap
Service Stack is under continuous improvement and is always adding features that are useful for high-performance, scalable and resilient web service scenarios. See the 
[trello board](https://trello.com/board/servicestack-features-bugs/4e9fbbc91065f8e9c805641c) to get information about the current project status and upcoming features.

## Similar open source projects
Similar Open source .NET projects for developing or accessing web services include:

 * Open Rasta - REST-ful web service framwork:
     * [http://trac.caffeine-it.com/openrasta](http://trac.caffeine-it.com/openrasta)

 * Rest Sharp - an open source REST client for .NET
     * [http://restsharp.org](http://restsharp.org)

## OSS Libraries used

ServiceStack includes source code of the great libraries below for some of its core functionality. 
Each library is released under its respective licence:

  - [Mono](https://github.com/mono/mono) [(License)](https://github.com/mono/mono/blob/master/LICENSE)
  - [Funq IOC](http://funq.codeplex.com) [(License)](http://funq.codeplex.com/license)
  - [Fluent Validation](http://fluentvalidation.codeplex.com) [(License)](http://fluentvalidation.codeplex.com/license)
  - [Mini Profiler](http://code.google.com/p/mvc-mini-profiler/) [(License)](http://www.apache.org/licenses/LICENSE-2.0)
  - [Dapper](http://code.google.com/p/dapper-dot-net/) [(License)](http://www.apache.org/licenses/LICENSE-2.0)
  - [TweetStation's OAuth library](https://github.com/migueldeicaza/TweetStation) [(License)](https://github.com/migueldeicaza/TweetStation/blob/master/LICENSE)
  - [MarkdownSharp](http://code.google.com/p/markdownsharp/) [(License)](http://opensource.org/licenses/mit-license.php)
  - [MarkdownDeep](https://github.com/toptensoftware/markdowndeep) [(License)](http://www.toptensoftware.com/markdowndeep/license)

## Sponsors

![JetBrains dotTrace](http://www.jetbrains.com/profiler/features/dt/dt1/dt210x60_white.gif)

http://www.jetbrains.com/profiler/

## Core Team

 - [mythz](https://github.com/mythz) (Demis Bellot)
 - [arxisos](https://github.com/arxisos) (Steffen Müller)
 - [desunit](https://github.com/desunit) (Sergey Bogdanov)

## Contributors 
A big thanks to GitHub and all of ServiceStack's contributors:

 - [bman654](https://github.com/bman654) (Brandon Wallace)
 - [Iristyle](https://github.com/Iristyle) (Ethan Brown)
 - [superlogical](https://github.com/superlogical) (Jake Scott)
 - [itamar82](https://github.com/itamar82)
 - [chadwackerman](https://github.com/chadwackerman)
 - [derfsplat](https://github.com/derfsplat)
 - [JohnACarruthers](https://github.com/JohnACarruthers) (John Carruthers)
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
 - [davrot](https://github.com/davrot) (David Roth)
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
 - [DavidChristiansen](https://github.com/DavidChristiansen) (David Christiansen)  
 - [PaulECoyote](https://github.com/PaulECoyote) (Paul Evans)
 - [kongo2002](https://github.com/kongo2002) (Gregor Uhlenheuer)
 - [BrannonKing](https://github.com/BrannonKing) (Brannon King)
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

***

Runs on both Mono and .NET 3.5. _(Live preview hosted on Mono / CentOS)_

