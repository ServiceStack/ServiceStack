using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

/// <summary>
/// Public User DTO
/// </summary>
[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'><path fill='currentColor' d='M12 4a4 4 0 0 1 4 4a4 4 0 0 1-4 4a4 4 0 0 1-4-4a4 4 0 0 1 4-4m0 10c4.42 0 8 1.79 8 4v2H4v-2c0-2.21 3.58-4 8-4'/></svg>")]
[Alias("AspNetUsers")]
public class User
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileUrl { get; set; }
}

[ValidateIsAdmin]
public class QueryUsers : QueryDb<User>
{
    public string? Id { get; set; }
}
