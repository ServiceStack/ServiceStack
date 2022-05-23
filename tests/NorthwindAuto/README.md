# Northwind Auto

An empty Example App only configured with `northwind.sqlite` database and AutoQuery to showcase [AutoGen to instantly servicify existing systems](https://docs.servicestack.net/servicify) by generating AutoQuery Services for all configured RDBMS Tables including [Typed Services for the most popular Web, Mobile & Desktop languages](https://docs.servicestack.net/add-servicestack-reference) that's maintainable by an instant UI in [ServiceStack Studio](https://docs.servicestack.net/studio).

![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/apps/NorthwindAuto.png)

### Code Generation of AutoQuery & CRUD Services

Now with [AutoCrud](https://docs.servicestack.net/autogen) we can add a lot more value in this area as AutoCrud's declarative nature allows us to easily generate AutoQuery & Crud Services by just emitting declarative Request DTOs.

You can then add the generated DTOs to your ServiceModel's to quickly enable AutoQuery Services for your existing databases.

<img src="https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/svg/servicify.svg" width="100%">

To enable this feature you you just need to initialize `GenerateCrudServices` in your `AutoQueryFeature` plugin, e.g:

```csharp
Plugins.Add(new AutoQueryFeature {
    MaxLimit = 1000,
    GenerateCrudServices = new GenerateCrudServices {}
});
```

If you don't have an existing database, you can quickly test this out with a Northwind SQLite database available from [https://github.com/NetCoreApps/NorthwindAuto](github.com/NetCoreApps/NorthwindAuto):

    $ x download NetCoreApps/NorthwindAuto

As you'll need to use 2 terminal windows, I'd recommend opening the project with **VS Code** which has great multi-terminal support:

    $ code NorthwindAuto

The important parts of this project is the registering the OrmLite DB Connection, the above configuration and the local **northwind.sqlite** database, i.e:

```csharp
container.AddSingleton<IDbConnectionFactory>(c =>
    new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

Plugins.Add(new AutoQueryFeature {
    MaxLimit = 1000,
    GenerateCrudServices = new GenerateCrudServices {}
});
```

#### Generating AutoQuery Types & Services

The development experience is essentially the same as [Add ServiceStack Reference](https://docs.servicestack.net/add-servicestack-reference) where you'll need to run the .NET Core App in 1 terminal:

    $ dotnet run

Then use the `x` dotnet tool to download all the AutoQuery & Crud Services for all tables in the configured DB connection:

    $ x csharp https://localhost:5001 -path /crud/all/csharp

#### Updating Generated Services

If your RDBMS schema changes you'd just need to restart your .NET Core App, then you can update all existing `dtos.cs` with:

    $ x csharp

i.e. the same experience as updating normal DTOs.

You can do the same for all other ServiceStack's supported languages as shown in autodto at the start of this release.

## AutoRegister AutoGen AutoQuery Services

To recap we've now got an integrated scaffolding solution where we can quickly generate code-first AutoQuery Services and integrate them into our App to quickly build an AutoQuery Service layer around our existing database.

But we can raise the productivity level even higher by instead of manually importing the code-generated Services into our project we just tell ServiceStack to do it for us.

This is what the magical `AutoRegister` flag does for us:

```csharp
Plugins.Add(new AutoQueryFeature {
    GenerateCrudServices = new GenerateCrudServices {
        AutoRegister = true,
        //....
    }
});
```

### Instantly Servicify Northwind DB with gRPC

To show the exciting potential of this feature we'll demonstrate one valuable use-case of creating a [grpc](https://docs.servicestack.net/grpc) project, mixing in AutoQuery configuration to instantly Servicifying the Northwind DB, browsing the generated Services from ServiceStack's [Metadata Page](https://docs.servicestack.net/metadata-page), explore the gRPC RPC Services `.proto` then create a new Dart App to consume the gRPC Services:

> YouTube: [youtu.be/5NNCaWMviXU](https://youtu.be/5NNCaWMviXU)

[![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/release-notes/v5.9/autogen-grpc.png)](https://youtu.be/5NNCaWMviXU)