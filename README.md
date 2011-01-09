Service Stack is a high-performance .NET web services framework _(including a number of high-performance sub-components: see below)_ 
that simplifies the development of XML, JSON, JSV and WCF SOAP [Web Services](https://github.com/mythz/ServiceStack/wiki/Service-Stack-Web-Services). 
For more info check out [servicestack.net](http://www.servicestack.net).
For twitter updates follow <a href="http://twitter.com/demisbellot">@demisbellot</a> or <a href="http://twitter.com/servicestack">@ServiceStack</a>

Simple web service example
==========================

    [DataContract]
    public class GetFactorial
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [DataContract]
    public class GetFactorialResponse
    {
        [DataMember]
        public long Result { get; set; }
    }

    public class GetFactorialService : IService<GetFactorial>
    {
        public object Execute(GetFactorial request)
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        }

        static long GetFactorial(long n)
        {
            return n > 1 ? n * GetFactorial(n - 1) : 1;
        } 
    }

### Calling the service from any C#/.NET Client
    //no code-gen required, can re-use above DTO's
    var serviceClient = new XmlServiceClient("http://localhost/ServiceStack.Examples.Host.Web/ServiceStack/");
    var response = this.ServiceClient.Send<GetFactorialResponse>(new GetFactorial { ForNumber = 3 });
    Console.WriteLine("Result: {0}", response.Result);

### Calling the service from a Java Script client i.e. Ajax

    var serviceClient = new JsonServiceClient("http://localhost/ServiceStack.Examples.Host.Web/ServiceStack/");
    serviceClient.getFromService({GetFactorial: { ForNumber: 3 }}, function(e) {
      alert(e.Result);
    });


That's all the application code required to create a simple web service.

Preview links using just the above code sample with (live demo running on Linux):
[XML](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Xml/SyncReply/GetFactorial?ForNumber=3),
[JSON](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Json/SyncReply/GetFactorial?ForNumber=3),
[JSV](http://www.servicestack.net/ServiceStack.Examples.Host.Web/ServiceStack/Jsv/SyncReply/GetFactorial?ForNumber=3&debug) 
Check out the [live demo](http://www.servicestack.net/ServiceStack.Examples.Clients/) with
[full source code](http://code.google.com/p/servicestack/source/browse/#svn/trunk/ServiceStack.Examples).


Download
========

To start developing web services with Service Stack we recommend starting with the ServiceStack.Examples project (includes ServiceStack.dlls):

  * **[ServiceStack.Examples/downloads/](https://github.com/mythz/ServiceStack.Examples/downloads)**

If you already have ServiceStack and just want to download the latest release binaries get them at:

  * **[ServiceStack/downloads/](https://github.com/mythz/ServiceStack/downloads)**

Alternatively if you want keep up with the latest version you can always use the power of Git :)

    git clone git://github.com/mythz/ServiceStack.git


## Getting Started
Online tutorials that walks you through developing and calling web services is available here:

 * [Creating a Hello World Web service](http://www.servicestack.net/ServiceStack.Hello/)
 * [Calling Web Services from MonoTouch](http://www.servicestack.net/monotouch/remote-info/)


# Features of a modern web services framework

Developed in the modern age, Service Stack provides an alternate, cleaner POCO-driven way of creating web services, featuring:

### Define your web services in a code-first approach using DSL-like POCO's
Service Stack was designed with a top-down view, i.e. we identified the minimum amount of effort required to implement a web service and ensured it remained that way.

What are the minimum number or artefacts required to implement a web service? From our point of view it is the `Request` and `Response` DTO's (which on their own define the web service contract or interface) and the actual implementation of the web service which would take in a `Request` and in most cases return the `Response`. As it turns out these remain the only classes required to create a functional web service in Service Stack.

The Request and Response DTO's are standard `DataContract` POCO's while the implementation just needs to inherit from a testable and dependency-free `IService<TRequestDto>`. As a bonus for keeping your DTO's in a separate dependency-free .dll, you're able to re-use them in your C#/.NET clients providing a strongly-typed API without any code-gen what-so-ever. Also your DTO's *define everything* Service Stack does not pollute your web services with any additional custom artefacts or markup.

Service Stack re-uses the custom artefacts above and with zero-config and without imposing any extra burden on the developer adds discover-ability and provides hosting of your web service on a number of different physical end-points which as of today includes: XML (+REST), JSON (+REST), JSV (+REST) and SOAP 1.1 / SOAP 1.2.

### Full support for unit and integration tests
Your application logic should not be tied to a third party vendor's web service host platform.
In Service Stack they're not, your web service implementations are host and end-point ignorant, dependency-free and can be unit-tested independently of ASP.NET, Service Stack or its IOC.

Without any code changes unit tests written can be re-used and run as integration tests simply by switching the IServiceClient used to point to a configured end-point host.

### Built-in Funq IOC container
Configured to auto-wire all of your web services with your registered dependencies.
[http://funq.codeplex.com Funq] was chosen for it's high-performance, low footprint and intuitive full-featured minimalistic API.

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
  * View the [Service Stack endpoints page](https://github.com/mythz/ServiceStack/wiki/Service-Stack-Web-Services) for our recommendations on which endpoint to use and when.

# High Performance Sub Projects
Also included in ServiceStack are libraries that are useful in the development of high performance web services:

 * [ServiceStack.Text](https://github.com/mythz/ServiceStack.Text) - The home of ServiceStack's JSON and JSV text serializers, the fastest text serializers for .NET
   * [JsonSerializer](http://www.servicestack.net/mythz_blog/?p=344) - The fastest JSON Serializer for .NET. Over 3 times faster than other .NET JSON serialisers.
   * [TypeSerializer](https://github.com/mythz/ServiceStack.Text) - The JSV-format, a fast, compact text serializer that is very resilient to schema changes and is:
       * 3.5x quicker and 2.6x smaller than the .NET XML DataContractSerializer and
       * 5.3x quicker and 1.3x smaller than the .NET JSON DataContractSerializer - _[view the detailed benchmarks](http://www.servicestack.net/benchmarks/NorthwindDatabaseRowsSerialization.1000000-times.2010-02-06.html)_

 * [ServiceStack.Redis](https://github.com/mythz/ServiceStack.Redis) - An API complete C# [Redis](http://code.google.com/p/redis/) client with native support for persisting C# POCO objects.
   *  You can download the latest [RedisWindowsDownload Windows build for the Redis Server here].
   * [Redis Admin UI](http://www.servicestack.net/mythz_blog/?p=381) - An Ajax GUI admin tool to help visualize your Redis data.

 * [OrmLite](https://github.com/mythz/ServiceStack.OrmLite) - A convention-based, configuration free lightweight ORM that uses attributes from DataAnnotations to infer the table schema. Currently supports both Sqlite and SqlServer.

 * [Caching](https://github.com/mythz/ServiceStack/wiki/Caching) - A common interface for caching with providers for:
   * Memcached
   * In Memory Cache
   * Redis


## Find out More
 * Twitter: to get updates of ServiceStack, follow [@demisbellot](http://twitter.com/demisbellot) or [@ServiceStack](http://twitter.com/ServiceStack) on twitter.
 * Email any questions to _demis.bellot@gmail.com_

## Future Roadmap
Service Stack is under continuous improvement and is always adding features that are useful for high-performance, scalable and resilient web service scenarios. This is the current road map but is open to change.
If you have suggestions for new features or want to prioritize the existing ones below: [http://code.google.com/p/servicestack/issues/entry you can leave feedback here].

 * Add an opt-in durable Message Queue service processing of [AsyncOneWay] requests (with In Memory, Redis and RabbitMQ implementations)
 * Enable [ProtoBuf.NET](http://code.google.com/p/protobuf-net/), TypeSerializer and CSV (for tabular datasets) web service endpoints
 * Integrate the Spark View Engine and enable a HTML endpoint so web services can also return a HTML human-friendly view
 * New REST web service endpoint developed in the 'Spirit of REST' with partitioning of GET / POST / PUT / DELETE requests and utilizing 'Accept mimetype' HTTP request header to determine the resulting content type.
 * Code generated proxy classes for Objective-C, Java Script / Action Script (and Java or C++ if there's enough interest) clients.
 * Develop a completely managed HTTP Web Service Host (at the moment looking at building on top of [Kayak HTTP](http://kayakhttp.com))
 * Add support for 'Web Sockets' protocol

## Similar open source projects
Similar Open source .NET projects for developing or accessing web services include:

 * Open Rasta - REST-ful web service framwork:
     * [http://trac.caffeine-it.com/openrasta](http://trac.caffeine-it.com/openrasta)

 * Rest Sharp - an open source REST client for .NET
     * [http://restsharp.org](http://restsharp.org)


## Troubleshooting

For IIS 6.0-only web servers (i.e. without IIS 7 and compatibility-mode) IIS and ASP.NET requires mapping an extension in order to embed ServiceStack. You can use a default handled ASP.NET extension like *.ashx e.g.

	<add path="servicestack.ashx" type="ServiceStack.WebHost.Endpoints.ServiceStackHttpHandlerFactory, ServiceStack" verb="*"/>

Which will change your urls will now look like:

	http://localhost/servicestack.ashx/xml/syncreply/Hello?Name=World



***

Runs on both Mono and .NET 3.5. _(Live preview hosted on Mono / CentOS)_


