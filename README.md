See [servicestack.net/features](http://servicestack.net/features) for an overview.

Join the [ServiceStack Google+ Community](https://plus.google.com/u/0/communities/112445368900682590445) or
follow [@ServiceStack](http://twitter.com/servicestack) for project updates. 

### Simple, Fast, Versatile and full-featured Services Framework

ServiceStack is a simple, fast, versatile and highly-productive full-featured [Web](http://razor.servicestack.net) and 
[Web Services](https://github.com/ServiceStack/ServiceStack/wiki/Service-Stack-Web-Services) Framework that's 
thoughtfully-architected to [reduce artificial complexity](https://github.com/ServiceStack/ServiceStack/wiki/Auto-Query#why-not-complexity) and promote 
[remote services best-practices](https://github.com/ServiceStack/ServiceStack/wiki/Advantages-of-message-based-web-services) 
with a [message-based design](https://github.com/ServiceStack/ServiceStack/wiki/What-is-a-message-based-web-service%3F) 
that allows for maximum re-use where ServiceStack Services are able to be consumed via an array of built-in fast data formats (inc. 
[JSON](https://github.com/ServiceStack/ServiceStack.Text), 
XML, 
[CSV](https://github.com/ServiceStack/ServiceStack/wiki/ServiceStack-CSV-Format), 
[JSV](https://github.com/ServiceStack/ServiceStack.Text/wiki/JSV-Format), 
[ProtoBuf](https://github.com/ServiceStack/ServiceStack/wiki/Protobuf-format) and 
[MsgPack](https://github.com/ServiceStack/ServiceStack/wiki/MessagePack-Format)) 
as well as XSD/WSDL for [SOAP endpoints](https://github.com/ServiceStack/ServiceStack/wiki/SOAP-support) and 
[Rabbit MQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ) and 
[Redis MQ](https://github.com/ServiceStack/ServiceStack/wiki/Messaging-and-Redis) hosts. 

Your same Services also serve as the Controller in ServiceStack's [Smart Razor Views](http://razor.servicestack.net/)
reducing the effort to serve both 
[Web and Single Page Apps](https://github.com/ServiceStackApps/LiveDemos) as well as 
[Rich Desktop and Mobile Clients](https://github.com/ServiceStackApps/HelloMobile).

ServiceStack Services also maximize productivity for consumers providing an 
[instant end-to-end typed API without code-gen](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) enabling
the most productive development experience for developing .NET to .NET Web Services.

### [Generate Instant Typed API's from a Remote Url!](https://github.com/ServiceStack/ServiceStack/wiki/Add-ServiceStack-Reference)

ServiceStack now integrates with all Major IDE's used for creating the best native experiences on the most popular platforms 
to enable a highly productive dev workflow for consuming Web Services, making ServiceStack the ideal back-end choice for powering 
rich, native iPhone and iPad Apps on iOS with Swift, Mobile and Tablet Apps on the Android platform with Java, OSX Desktop Appications 
as well as targetting the most popular .NET PCL platforms including Xamarin.iOS, Xamarin.Android, Windows Store, WPF, WinForms and Silverlight: 

<img src="https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/add-ss-reference-ides.png" align="right" />

#### [VS.NET integration with ServiceStackVS](https://visualstudiogallery.msdn.microsoft.com/5bd40817-0986-444d-a77d-482e43a48da7)

Providing instant Native Typed API's for 
[C#](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference), 
[F#](https://github.com/ServiceStack/ServiceStack/wiki/FSharp-Add-ServiceStack-Reference), 
[VB.NET](https://github.com/ServiceStack/ServiceStack/wiki/VB.Net-Add-ServiceStack-Reference) 
and [TypeScript](https://github.com/ServiceStack/ServiceStack/wiki/TypeScript-Add-ServiceStack-Reference) 
directly in Visual Studio for the 
[most popular .NET platforms](https://github.com/ServiceStackApps/HelloMobile) including iOS and Android using 
[Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and 
[Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) on Windows.

#### [Xamarin Studio integration with ServiceStackXS](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio)

Providing [C# Native Types](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference) 
support for developing iOS and Android mobile Apps using 
[Xamarin.iOS](https://github.com/ServiceStackApps/HelloMobile#xamarinios-client) and 
[Xamarin.Android](https://github.com/ServiceStackApps/HelloMobile#xamarinandroid-client) with 
[Xamarin Studio](http://xamarin.com/studio) on OSX. The **ServiceStackXS** plugin also provides a rich web service 
development experience developing Client applications with 
[Mono Develop on Linux](https://github.com/ServiceStack/ServiceStack/wiki/CSharp-Add-ServiceStack-Reference#xamarin-studio-for-linux)

#### [Xcode integration with ServiceStackXC Plugin](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference)

Providing [an instant Native Typed API in Swift](https://github.com/ServiceStack/ServiceStack/wiki/Swift-Add-ServiceStack-Reference) 
including generic Service Clients enabling a highly-productive workflow and effortless consumption of Web Services from 
native iOS and OSX Applications - directly from within Xcode!

#### [Android Studio integration with ServiceStackIDEA](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference)

Providing [an instant Native Typed API in Java](https://github.com/ServiceStack/ServiceStack/wiki/Java-Add-ServiceStack-Reference) 
including idiomatic Java Generic Service Clients supporting Sync and Async Requests by levaraging Android's AsyncTasks to enable the
creation of services-rich and responsive native Java Mobile Apps on the Android platform - directly from within Android Studio!

## Simple REST service example

This example is also available as a [stand-alone integration test](https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.WebHost.Endpoints.Tests/NewApiTodos.cs):

```csharp
//Web Service Host Configuration
public class AppHost : AppSelfHostBase
{
    public AppHost() : base("TODOs Tests", typeof(Todo).Assembly) {}

    public override void Configure(Container container)
    {
        container.Register(new TodoRepository());
    }
}

//REST Resource DTO
[Route("/todos")]
[Route("/todos/{Ids}")]
public class Todos : IReturn<List<Todo>>
{
    public long[] Ids { get; set; }
    public Todos(params long[] ids)
    {
        this.Ids = ids;
    }
}

[Route("/todos", "POST")]
[Route("/todos/{Id}", "PUT")]
public class Todo : IReturn<Todo>
{
    public long Id { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }
    public bool Done { get; set; }
}

public class TodosService : Service
{
    public TodoRepository Repository { get; set; }  //Injected by IOC

    public object Get(Todos request)
    {
        return request.Ids.IsEmpty()
            ? Repository.GetAll()
            : Repository.GetByIds(request.Ids);
    }

    public object Post(Todo todo)
    {
        return Repository.Store(todo);
    }

    public object Put(Todo todo)
    {
        return Repository.Store(todo);
    }

    public void Delete(Todos request)
    {
        Repository.DeleteByIds(request.Ids);
    }
}
```

### [Calling the above TODO REST service from any C#/.NET Client](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client)

```csharp
//no code-gen required, can re-use above DTO's

var client = new JsonServiceClient(BaseUri);
List<Todo> all = client.Get(new Todos());     		// Count = 0

var todo = client.Post(
    new Todo { Content = "New TODO", Order = 1 }); 	// todo.Id = 1
all = client.Get(new Todos());						// Count = 1

todo.Content = "Updated TODO";
todo = client.Put(todo);							// todo.Content = Updated TODO

client.Delete(new Todos(todo.Id));
all = client.Get(new Todos());						// Count = 0
```

### Calling the TODO REST service from jQuery

    $.getJSON(baseUri + "/todos", function(todos) {
    	alert(todos.length == 1);
    });

### Calling the TODO REST service from [Dart JsonClient](https://github.com/mythz/DartJsonClient)

    var client = new JsonClient(baseUri);
    client.todos()
    	.then((todos) => alert(todos.length == 1)); 

That's all the application code required to create a simple REST web service.

## Getting Started

 * [Start with the **Getting Started** section on the Wiki](https://github.com/ServiceStack/ServiceStack/wiki)
 * [Example Apps and Demos](http://stackoverflow.com/questions/15862634/in-what-order-are-the-servicestack-examples-supposed-to-be-grokked/15869816#15869816)
 * [Community resources](https://github.com/ServiceStack/ServiceStack/wiki/Community-Resources)

### [Release Notes](https://github.com/ServiceStack/ServiceStack/blob/master/release-notes.md)

## Download

If you have [NuGet](http://nuget.org) installed, the easiest way to get started is to: 

### [Install ServiceStack via NuGet](https://servicestack.net/download).

_Latest v4+ on NuGet is a commercial release with [free quotas](https://servicestack.net/download#free-quotas)._

### [Docs and Downloads for older v3 BSD releases](https://github.com/ServiceStackV3/ServiceStackV3)

### Examples

**The [Definitive list of Example Projects, Use-Cases, Demos, Starter Templates](http://stackoverflow.com/a/15869816)**
    
## Download published NuGet binaries without NuGet

GitHub has disabled its download feature so currently NuGet is the best way to get ServiceStack published releases.
For environments that don't have NuGet installed (e.g. OSX/Linux) you can still download the published binaries by 
extracting them from the published NuGet packages. The url to download a nuget package is: 

    http://packages.nuget.org/api/v1/package/{PackageName}/{Version}
    
 So to get the core ServiceStack and ServiceStack.Text libs in OSX/Linux (or using gnu tools for Windows) you can just do:

    wget -O ServiceStack http://packages.nuget.org/api/v1/package/ServiceStack/3.9.71
    unzip ServiceStack 'lib/*'
    
    wget -O ServiceStack.Text http://packages.nuget.org/api/v1/package/ServiceStack.Text/3.9.71
    unzip ServiceStack.Text 'lib/*'

which will download and extract the dlls into your local local `lib/` folder.

## Copying

Since September 2013, ServiceStack source code is available under GNU Affero General Public License/FOSS License Exception, see license.txt in the source. 
Alternative commercial licensing is also available, see https://servicestack.net/pricing for details.

## Contributing

Contributors need to approve the [Contributor License Agreement](https://docs.google.com/forms/d/16Op0fmKaqYtxGL4sg7w_g-cXXyCoWjzppgkuqzOeKyk/viewform) before any code will be reviewed, see the [Contributing wiki](https://github.com/ServiceStack/ServiceStack/wiki/Contributing) for more details. All contributions must include tests verifying the desired behavior.

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
  - [HtmlCompressor](https://code.google.com/p/htmlcompressor/) [(License)](http://www.apache.org/licenses/LICENSE-2.0)
  - [JSMin](https://github.com/douglascrockford/JSMin/blob/master/jsmin.c) [(License)](http://www.apache.org/licenses/LICENSE-2.0)
  - [RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream) [(License)](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream/blob/master/LICENSE)

## Similar open source projects

Similar Open source .NET projects for developing or accessing web services include:

 * [Nancy Fx](http://nancyfx.org) - A Sinatra-inspired lightweight Web Framework for .NET:
 * [Fubu MVC](http://mvc.fubu-project.org) - A "Front Controller" pattern-style MVC framework designed for use in web applications built on ASP.NET:
 * [Rest Sharp](http://restsharp.org) - An open source REST client for .NET

## Find out More

Follow [@ServiceStack](http://twitter.com/ServiceStack) and 
[+ServiceStack](https://plus.google.com/u/0/communities/112445368900682590445) for project updates.

-----

## Core Team

 - [mythz](https://github.com/mythz) (Demis Bellot)
 - [layoric](https://github.com/layoric) (Darren Reid) / [@layoric](https://twitter.com/layoric)
 - [arxisos](https://github.com/arxisos) (Steffen Müller) / [@arxisos](https://twitter.com/arxisos)
 - [desunit](https://github.com/desunit) (Sergey Bogdanov) / [@desunit](https://twitter.com/desunit)

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

Runs on both Mono and .NET _(Live preview hosted on Mono / Ubuntu)_
