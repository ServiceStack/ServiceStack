using Chinook.ServiceModel;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Html;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

[Tag("Test")]
public class AllTypes : IReturn<AllTypes>
{
    public int Id { get; set; }
    public int? NullableId { get; set; }
    public bool Boolean { get; set; }
    public byte Byte { get; set; }
    public short Short { get; set; }
    public int Int { get; set; }
    public long Long { get; set; }
    public UInt16 UShort { get; set; }
    public uint UInt { get; set; }
    public ulong ULong { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }
    public string String { get; set; }
    public DateTime DateTime { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public Guid Guid { get; set; }
    public Char Char { get; set; }
    public KeyValuePair<string, string> KeyValuePair { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public TimeSpan? NullableTimeSpan { get; set; }
    [Input(Type=Input.Types.Tag)]
    public List<string> StringList { get; set; }
    public string[] StringArray { get; set; }
    public Dictionary<string, string> StringMap { get; set; }
    public Dictionary<int, string> IntStringMap { get; set; }
    public SubType SubType { get; set; }
    public byte?[] NullableBytes { get; set; }
}

[Tag("Test")]
public class AllCollectionTypes : IReturn<AllCollectionTypes>
{
    public int[] IntArray { get; set; }
    public List<int> IntList { get; set; }

    public string[] StringArray { get; set; }
    public List<string> StringList { get; set; }

    public float[] FloatArray { get; set; }
    public List<double> DoubleList { get; set; }

    public byte[] ByteArray { get; set; }
    public char[] CharArray { get; set; }
    public List<decimal> DecimalList { get; set; }

    public Poco[] PocoArray { get; set; }
    public List<Poco> PocoList { get; set; }

    public Dictionary<string, List<Poco>> PocoLookup { get; set; }
    public Dictionary<string, List<Dictionary<string, Poco>>> PocoLookupMap { get; set; }
}

public class HelloAllTypes : IReturn<HelloAllTypesResponse>
{
    public string Name { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }
}

public class HelloAllTypesResponse
{
    public string Result { get; set; }
    public AllTypes AllTypes { get; set; }
    public AllCollectionTypes AllCollectionTypes { get; set; }
}

public class HelloReturnVoid : IReturnVoid
{
    public int Id { get; set; }
}

public class Poco
{
    public string Name { get; set; }
}

public abstract class HelloBase
{
    public int Id { get; set; }
}

public class SubType
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ProfileGen {}
public class ProfileGenResponse {}


public class CreateMqBooking : AuditBase, ICreateDb<Booking>, IReturn<IdResponse>
{
    [Description("Name this Booking is for"), ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal Cost { get; set; }
    public DateTime BookingStartDate { get; set; }
    [FieldCss(Label = "text-green-800", Input = "bg-green-100")]
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea"), FieldCss(Field="col-span-12 text-center", Input = "bg-green-100")]
    public string? Notes { get; set; }
}

// [Route("/albums", "POST")]
// public class CreateAlbums : IReturn<IdResponse>, IPost, ICreateDb<Albums>
// {
//     [ValidateNotEmpty]
//     public string Title { get; set; }
//     [ValidateGreaterThan(0)]
//     public long ArtistId { get; set; }
// }

// public class QueryAlbums : QueryDb<Albums>, IGet
// {
//     public long? AlbumId { get; set; }
// }
// public class Albums
// {
//     public long AlbumId { get; set; }
//     public string Title { get; set; }
//     public long ArtistId { get; set; }
// }


[Route("/throw/{Type}")]
public class ThrowType : IReturn<ThrowTypeResponse>
{
    public string? Type { get; set; }
    public string? Message { get; set; }
}

public class ThrowTypeResponse
{
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/throwvalidation")]
public class ThrowValidation : IReturn<ThrowValidationResponse>
{
    public int Age { get; set; }
    public string Required { get; set; }
    public string Email { get; set; }
}

public class ThrowValidationResponse
{
    public int Age { get; set; }
    public string Required { get; set; }
    public string Email { get; set; }

    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/echo/types")]
public class EchoTypes : IReturn<EchoTypes>
{
    public byte Byte { get; set; }
    public short Short { get; set; }
    public int Int { get; set; }
    public long Long { get; set; }
    public ushort UShort { get; set; }
    public uint UInt { get; set; }
    public ulong ULong { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public decimal Decimal { get; set; }
    public string String { get; set; }
    public DateTime DateTime { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public Guid Guid { get; set; }
    public char Char { get; set; }
}

public class HelloList : IReturn<List<ListResult>>
{
    public List<string> Names { get; set; }
}
public class ListResult
{
    public string Result { get; set; }
}

public class ThrowValidationValidator : AbstractValidator<ThrowValidation>
{
    public ThrowValidationValidator()
    {
        RuleFor(x => x.Age).InclusiveBetween(1, 120);
        RuleFor(x => x.Required).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class TestServices : Service
{
    public object Any(HelloAllTypes request)
    {
        return new HelloAllTypesResponse
        {
            AllTypes = request.AllTypes,
            AllCollectionTypes = request.AllCollectionTypes,
            Result = request.Name
        };
    }
    
    public object Any(ThrowType request)
    {
        switch (request.Type ?? "Exception")
        {
            case "Exception":
                throw new Exception(request.Message ?? "Server Error");
            case "NotFound":
                throw HttpError.NotFound(request.Message ?? "What you're looking for isn't here");
            case "Unauthorized":
                throw HttpError.Unauthorized(request.Message ?? "You shall not pass!");
            case "Conflict":
                throw HttpError.Conflict(request.Message ?? "We haz Conflict!");
            case "NotImplementedException":
                throw new NotImplementedException(request.Message ?? "Not implemented yet, try again later");
            case "ArgumentException":
                throw new ArgumentException(request.Message ?? "Client Argument Error");
            case "AuthenticationException":
                throw new AuthenticationException(request.Message ?? "We haz issue Authenticating");
            case "UnauthorizedAccessException":
                throw new UnauthorizedAccessException(request.Message ?? "You shall not pass!");
            case "OptimisticConcurrencyExceptieon":
                throw new OptimisticConcurrencyException(request.Message ?? "Sorry too optimistic");
            case "UnhandledException":
                throw new FileNotFoundException(request.Message ?? "File was never here");
            case "RawResponse":
                Response.StatusCode = 418;
                Response.StatusDescription = request.Message ?? "On a tea break";
                Response.Close();
                break;
        }

        return request;
    }

    public object Any(ThrowValidation request)
    {
        return request.ConvertTo<ThrowValidationResponse>();
    }    
    
    public object Any(AllTypes request) => request;

    public object Any(AllCollectionTypes request) => request;
    
    // public IAutoQueryDb AutoQuery { get; set; }
    // public Task<object> Post(CreateMqBooking request) => AutoQuery.CreateAsync(request, base.Request);
    
    public object Any(ProfileGen request)
    {
        var client = new JsonApiClient("https://chinook.locode.dev");
        var api = client.Api(new QueryAlbums { Take = 5 });

        var errorApi = client.Api(new CreateAlbums());

        var json = "https://chinook.locode.dev/api/QueryAlbums?Take=5".GetJsonFromUrl();
        "https://chinook.locode.dev/api/CreateAlbums".PostToUrl(new { Title = "New2", ArtistId = 2 });

        try
        {
            "https://chinook.locode.dev/api/CreateAlbums".PostToUrl(new { ArtistId = "Error" });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        Cache.Set("foo", "bar");
        Cache.Set("bax", 1);
        Cache.Set("qux", new Poco { Name = nameof(Poco) });
        
        Redis.IncrementValueBy("incr", 10);
        
        Redis.Hashes["hash"].AddRange(new Dictionary<string, string> {
            {"foo","bar"}, 
            {"baz","1"}
        });

        Db.Insert(new Booking {
            Name = "4th of the Bookings",
            RoomType = RoomType.Single,
            RoomNumber = 44,
            Cost = 400,
            BookingStartDate = DateTime.UtcNow.AddDays(10),
            BookingEndDate = DateTime.UtcNow.AddDays(10 + 7),
        }.WithAudit("admin@email.com"));

        var mqRequest = new CreateMqBooking {
            Name = "John Smith",
            RoomType = RoomType.Queen,
            Cost = 200,
            RoomNumber = 101,
            BookingStartDate = DateTime.Now,
            CreatedDate = DateTime.Now,
            CreatedBy = "employee@email.com",
            ModifiedDate = DateTime.Now,
            ModifiedBy = "employee@email.com",
        };
        PublishMessage(mqRequest);
        
        Gateway.Send(mqRequest);
        
        return new ProfileGenResponse();
    }

    public void Any(HelloReturnVoid request) {}

    public object Any(EchoTypes request) => request;
    
    public object Any(HelloList request) => request.Names.Map(name => new ListResult { Result = name });
}