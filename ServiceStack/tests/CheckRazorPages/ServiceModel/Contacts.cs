using MyApp.ServiceModel.Types;

namespace MyApp.ServiceModel;

// [Route("/contacts", "GET")]
[ValidateIsAuthenticated]
public class GetContacts : IGet, IReturn<GetContactsResponse> {}
public class GetContactsResponse 
{
    public List<Contact>? Results { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

// [Route("/contacts/{Id}", "GET")]
[ValidateIsAuthenticated]
public class GetContact : IGet, IReturn<GetContactResponse >
{
    public int Id { get; set; }
}
public class GetContactResponse 
{
    public Contact? Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

//[Route("/contacts", "POST")]
[ValidateIsAuthenticated]
public class CreateContact : IPost, IReturn<CreateContactResponse>
{
    // Declarative Validation Example 
    [ValidateNotEmpty(Message = "Please choose a title")]
    public Title? Title { get; set; }
    [ValidateNotEmpty]
    public string? Name { get; set; }
    [Validate("ValidColor()", Message = "Must be a valid color")]
    public string? Color { get; set; }
    [ValidateNotEmpty(Message = "Please select at least 1 genre")]
    public FilmGenres[]? FilmGenres { get; set; }
    [ValidateGreaterThan(13, Message = "Contacts must be older than 13")]
    public int Age { get; set; }
    [ValidateEqual(true, Message = "You must agree before submitting")]
    public bool Agree { get; set; }
    
    public string? Continue { get; set; }
}
public class CreateContactResponse 
{
    public Contact? Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

//[Route("/contacts/{Id}", "POST PUT")]
[ValidateIsAuthenticated]
public class UpdateContact : IPatch, IReturn<UpdateContactResponse>
{
    public int Id { get; set; }
    public Title Title { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public FilmGenres[]? FilmGenres { get; set; }
    public int Age { get; set; }
    
    public string? Continue { get; set; }
    public string? ErrorView { get; set; }
}
public class UpdateContactResponse 
{
    public ResponseStatus? ResponseStatus { get; set; }
}

// [Route("/contacts/{Id}", "DELETE")]
// [Route("/contacts/{Id}/delete", "POST")] // more accessible from HTML
[ValidateIsAuthenticated]
public class DeleteContact : IDelete, IReturnVoid
{
    public int Id { get; set; }
    public string? Continue { get; set; }
}
