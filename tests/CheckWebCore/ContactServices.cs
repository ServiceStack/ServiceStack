using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Threading;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Templates;

namespace CheckWebCore
{
    namespace Data
    {
        using ServiceModel.Types;
        
        public class Contact // Data Model
        {
            public int Id { get; set; }
            public int UserAuthId { get; set; }
            public Title Title { get; set; }
            public string Name { get; set; }
            public string Color { get; set; }
            public int Age { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime ModifiedDate { get; set; }
        }
    }

    namespace ServiceModel
    {
        using Types;
        
        [Route("/contacts", "GET")]
        public class GetContacts : IReturn<GetContactsResponse> {}
        public class GetContactsResponse 
        {
            public List<Contact> Results { get; set; }
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Route("/contacts/{Id}", "GET")]
        public class GetContact : IReturn<GetContactResponse >
        {
            public int Id { get; set; }
        }
        public class GetContactResponse 
        {
            public Contact Result { get; set; }
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Route("/contacts", "POST")]
        public class CreateContact : IReturn<CreateContactResponse>
        {
            public Title Title { get; set; }
            public string Name { get; set; }
            public string Color { get; set; }
            public int Age { get; set; }
            public bool Agree { get; set; }
        }
        public class CreateContactResponse 
        {
            public Contact Result { get; set; }
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Route("/contacts/{Id}", "POST PUT")]
        public class UpdateContact : IReturn<UpdateContactResponse>
        {
            public int Id { get; set; }
            public Title Title { get; set; }
            public string Name { get; set; }
            public string Color { get; set; }
            public int Age { get; set; }
            
            public string Continue { get; set; }
            public string ErrorView { get; set; }
        }
        public class UpdateContactResponse 
        {
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Route("/contacts/{Id}", "DELETE")]
        [Route("/contacts/{Id}/delete")]
        public class DeleteContact : IReturnVoid
        {
            public int Id { get; set; }
        }

        [Route("/contacts/reset")]
        public class ResetContacts {}

        namespace Types
        {
            public class Contact // DTO
            {
                public int Id { get; set; }
                public int UserAuthId { get; set; }
                public Title Title { get; set; }
                public string Name { get; set; }
                public string Color { get; set; }
                public int Age { get; set; }
            }

            public enum Title
            {
                Unspecified=0,
                [Description("Mr.")] Mr,
                [Description("Mrs.")] Mrs,
                [Description("Miss.")] Miss
            }
        }
    }

    namespace ServiceInterface
    {
        using ServiceModel;
        using ServiceModel.Types;
        
        public class CreateContactValidator : AbstractValidator<CreateContact>
        {
            public CreateContactValidator()
            {
                RuleFor(r => r.Title).NotEqual(Title.Unspecified).WithMessage("Please choose a title");
                RuleFor(r => r.Name).NotEmpty();
                RuleFor(r => r.Age).GreaterThan(13).WithMessage("Contacts must be older than 13");
                RuleFor(r => r.Color).Must(x => x.IsValidColor()).WithMessage("Must be a valid color");
                RuleFor(x => x.Agree).Equal(true).WithMessage("You must agree before submitting");
            }
        }
        
        public class UpdateContactValidator : AbstractValidator<UpdateContact>
        {
            public UpdateContactValidator()
            {
                RuleFor(r => r.Id).GreaterThan(0);
                RuleFor(r => r.Title).NotEqual(Title.Unspecified).WithMessage("Please choose a title");
                RuleFor(r => r.Name).NotEmpty();
                RuleFor(r => r.Age).GreaterThan(13).WithMessage("Contacts must be older than 13");
                RuleFor(r => r.Color).Must(x => x.IsValidColor()).WithMessage("Must be a valid color");
            }
        }
    
        [Authenticate]
        [DefaultView("/validation/server/contacts")]
        public class ContactServices : Service
        {
            private static int Counter = 0;
    
            internal static ConcurrentDictionary<int, Data.Contact> Contacts = new ConcurrentDictionary<int, Data.Contact>();
    
            public object Any(GetContacts request)
            {
                var userId = this.GetUserId();
                return new GetContactsResponse
                {
                    Results = Contacts.Values
                        .Where(x => x.UserAuthId == userId)
                        .OrderByDescending(x => x.Id)
                        .Map(x => x.ToDto())
                };
            }
    
            public object Any(GetContact request) =>
                Contacts.TryGetValue(request.Id, out var contact) && contact.UserAuthId == this.GetUserId()
                    ? (object)new GetContactResponse { Result = contact.ToDto() }
                    : HttpError.NotFound($"Contact was not found");
    
            public object Any(CreateContact request) 
            {
                var newContact = request.ConvertTo<Data.Contact>();
                newContact.Id = Interlocked.Increment(ref Counter);
                newContact.UserAuthId = this.GetUserId();
                newContact.CreatedDate = newContact.ModifiedDate = DateTime.UtcNow;
    
                lock (Contacts)
                {
                    var alreadyExists = Contacts.Values.Any(x => x.UserAuthId == newContact.UserAuthId && x.Name == request.Name);
                    if (alreadyExists)
                        throw new ArgumentException($"You already have a contact named '{request.Name}'", nameof(request.Name));
                }
                
                Contacts[newContact.Id] = newContact;
                return new CreateContactResponse { Result = newContact.ToDto() };
            }
    
            public object AnyHtml(CreateContact request)
            {
                Any(request);
                return HttpResult.Redirect(Request.GetView());
            }
    
            public void Any(DeleteContact request)
            {
                if (Contacts.TryGetValue(request.Id, out var contact) && contact.UserAuthId == this.GetUserId())
                {
                    Contacts.TryRemove(request.Id, out _);
                }
            }
    
            public object GetHtml(DeleteContact request) // only called by html GET requests where it takes precedence
            {
                Any(request);
                return HttpResult.Redirect(Request.GetView()); //added by [DefaultView]
            }
    
            public void Any(ResetContacts request) => Contacts.Clear();
        }
    
        // Example of single 'pure' API supporting multiple HTML UIs
        [ErrorView(nameof(UpdateContact.ErrorView))] // Display ErrorView if HTML request results in an Exception
        public class UpdateContactServices : Service
        {
            public object Any(UpdateContact request)
            {
                if (!ContactServices.Contacts.TryGetValue(request.Id, out var contact) || contact.UserAuthId != this.GetUserId())
                    throw HttpError.NotFound("Contact was not found");
    
                contact.PopulateWith(request);
                contact.ModifiedDate = DateTime.UtcNow;
                
                return request.Continue != null 
                    ? (object) HttpResult.Redirect(request.Continue)
                    : new UpdateContactResponse();
            }
        }
    
        public static class ContactServiceExtensions
        {
            public static int GetUserId(this Service service) => int.Parse(service.GetSession().UserAuthId);
    
            public static Contact ToDto(this Data.Contact from) => from.ConvertTo<Contact>();
            
            public static bool IsValidColor(this string color) => !string.IsNullOrEmpty(color) && 
              (color.FirstCharEquals('#')
                  ? int.TryParse(color.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
                  : Color.FromName(color).IsKnownColor);
        }
    
        /// <summary>
        /// Custom filters for maintaining re-usable data sources and UI snippets
        /// </summary>
        public class ContactServiceFilters : TemplateFilter
        {
            static Dictionary<string, string> Colors = new Dictionary<string, string>
            {
                {"#ffa4a2","Red"},
                {"#b2fab4","Green"},
                {"#9be7ff","Blue"}
            };
    
            public Dictionary<string, string> contactColors() => Colors;
    
            private static List<KeyValuePair<string, string>> Titles => Enum.GetValues(typeof(Title)).Cast<Title>()
                .Where(x => x != Title.Unspecified)
                .Map(x => KeyValuePair.Create(
                        x.ToString(),
                        typeof(Title).GetMember(x.ToString())[0].GetDescription()));
    
            public List<KeyValuePair<string, string>> contactTitles() => Titles;
        }
    }
    

}