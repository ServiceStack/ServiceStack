using MyApp.ServiceModel.Types;

namespace MyApp.Data;

/// <summary>
/// Example of using separate DataModel and DTO
/// </summary>
public class Contact
{
    public int Id { get; set; }
    public int UserAuthId { get; set; }
    public Title Title { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public FilmGenres[]? FilmGenres { get; set; }
    public int Age { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
