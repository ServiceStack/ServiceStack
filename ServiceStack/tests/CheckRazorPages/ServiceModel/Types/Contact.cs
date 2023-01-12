using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel.Types;

public class Contact 
{
    public int Id { get; set; }
    public int UserAuthId { get; set; }
    public Title Title { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public FilmGenres[]? FilmGenres { get; set; }
    public int Age { get; set; }
}

public enum Title
{
    Unspecified=0,
    [Description("Mr.")] Mr,
    [Description("Mrs.")] Mrs,
    [Description("Miss.")] Miss
}

public enum FilmGenres
{
    Action,
    Adventure,
    Comedy,
    Drama,
}
