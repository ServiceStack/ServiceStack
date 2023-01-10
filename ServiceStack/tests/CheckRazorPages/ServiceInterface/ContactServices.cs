using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using MyApp.ServiceModel;
using MyApp.ServiceModel.Types;
using ServiceStack.FluentValidation;
using ServiceStack.Script;

namespace MyApp.ServiceInterface;

// Fluent Validation Example
public class UpdateContactValidator : AbstractValidator<UpdateContact>
{
    public UpdateContactValidator()
    {
        RuleFor(r => r.Id).GreaterThan(0);
        RuleFor(r => r.Title).NotEqual(Title.Unspecified).WithMessage("Please choose a title");
        RuleFor(r => r.Name).NotEmpty();
        RuleFor(r => r.Color).Must(x => x.IsValidColor()).WithMessage("Must be a valid color");
        RuleFor(r => r.FilmGenres).NotEmpty().WithMessage("Please select at least 1 genre");
        RuleFor(r => r.Age).GreaterThan(13).WithMessage("Contacts must be older than 13");
    }
}

public class ContactServices : Service
{
    private static int Counter = 0;
    internal static readonly ConcurrentDictionary<int, Data.Contact> Contacts = new();

    public async Task<object> Any(GetContacts request)
    {
        var userId = await this.GetUserIdAsync();
        return new GetContactsResponse
        {
            Results = Contacts.Values
                .Where(x => x.UserAuthId == userId)
                .OrderByDescending(x => x.Id)
                .Map(x => x.ConvertTo<Contact>())
        };
    }

    public async Task<object> Any(GetContact request) =>
        Contacts.TryGetValue(request.Id, out var contact) && contact.UserAuthId == await this.GetUserIdAsync()
            ? new GetContactResponse { Result = contact.ConvertTo<Contact>() }
            : HttpError.NotFound($"Contact was not found");

    public async Task<object> Any(CreateContact request) 
    {
        var newContact = request.ConvertTo<Data.Contact>();
        newContact.Id = Interlocked.Increment(ref Counter);
        newContact.UserAuthId = await this.GetUserIdAsync();
        newContact.CreatedDate = newContact.ModifiedDate = DateTime.UtcNow;

        var contacts = Contacts.Values.ToList();
        var alreadyExists = contacts.Any(x => x.UserAuthId == newContact.UserAuthId && x.Name == request.Name);
        if (alreadyExists)
            throw new ArgumentException($"You already have a contact named '{request.Name}'", nameof(request.Name));
        
        Contacts[newContact.Id] = newContact;
        return new CreateContactResponse { Result = newContact.ConvertTo<Contact>() };
    }

    public async Task Any(DeleteContact request)
    {
        if (Contacts.TryGetValue(request.Id, out var contact) && contact.UserAuthId == await this.GetUserIdAsync())
            Contacts.TryRemove(request.Id, out _);
    }

    public async Task<object> Any(UpdateContact request)
    {
        if (!ContactServices.Contacts.TryGetValue(request.Id, out var contact) || contact.UserAuthId != await this.GetUserIdAsync())
            throw HttpError.NotFound("Contact was not found");

        contact.PopulateWith(request);
        contact.ModifiedDate = DateTime.UtcNow;
        
        return request.Continue != null 
            ? HttpResult.Redirect(request.Continue)
            : new UpdateContactResponse();
    }
}

public static class ContactServiceExtensions // DRY reusable logic used in Services and Validators
{
    public static async Task<int> GetUserIdAsync(this Service service) => 
        int.Parse((await service.GetSessionAsync()).UserAuthId);

    public static bool IsValidColor(this string color) => !string.IsNullOrEmpty(color) && 
        (color.FirstCharEquals('#')
            ? int.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            : Color.FromName(color).IsKnownColor);
}
