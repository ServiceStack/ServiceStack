using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class Movie
{
    [PrimaryKey]
    public virtual string MovieID { get; set; } = string.Empty;
    public virtual int MovieNo { get; set; }
    public virtual string? Name { get; set; }
    public virtual string? Description { get; set; }
    public virtual string? MovieRef { get; set; }
}

[Tag("Movies")]
public class MovieGETRequest : IReturn<Movie>
{
    [Description("Unique Id of the movie"), ValidateNotEmpty]
    [Input(Required = true)]
    public string MovieID { get; set; } = string.Empty;
}

[Tag("Movies")]
[Field(nameof(MovieID), Disabled = true)]
[Field(nameof(MovieNo), Disabled = true)]
[Field(nameof(MovieRef), Ignore = true)]
public class MoviePOSTRequest : Movie, IReturn<Movie>
{
    // [Input(Ignore = true)]
    // public override string? MovieRef { get; set; }
}

