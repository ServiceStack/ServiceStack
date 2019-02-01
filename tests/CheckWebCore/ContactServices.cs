using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using CheckWebCore.ServiceModel;
using ServiceStack;
using ServiceStack.FluentValidation;

namespace CheckWebCore
{
    namespace Data
    {
        public class Contact
        {
            public int Id { get; set; }
            public int UserAuthId { get; set; }
            public string Name { get; set; }
            public string Company { get; set; }
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
            public string Name { get; set; }
            public string Company { get; set; }
            public int Age { get; set; }
        }
        public class CreateContactResponse 
        {
            public ResponseStatus ResponseStatus { get; set; }
        }

        [Route("/contacts/{Id}", "POST PUT")]
        public class UpdateContact : IReturn<UpdateContactResponse>
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Company { get; set; }
            public int Age { get; set; }
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
            public class Contact
            {
                public int Id { get; set; }
                public int UserAuthId { get; set; }
                public string Name { get; set; }
                public string Company { get; set; }
                public int Age { get; set; }
            }
        }
    }
    
    public class CreateContactValidator : AbstractValidator<CreateContact>
    {
        public CreateContactValidator()
        {
            RuleFor(r => r.Name).NotEmpty();
            RuleFor(r => r.Age).GreaterThan(13).WithMessage("Contacts must be older than 13");
            RuleFor(r => r.Company).NotEmpty();
        }
    }
    
    public class UpdateContactValidator : AbstractValidator<UpdateContact>
    {
        public UpdateContactValidator()
        {
            RuleFor(r => r.Id).GreaterThan(0);
            RuleFor(r => r.Name).NotEmpty();
            RuleFor(r => r.Age).GreaterThan(13).WithMessage("Contacts must be older than 13");
            RuleFor(r => r.Company).NotEmpty();
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
            return new CreateContactResponse();
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

        public object GetHtml(DeleteContact request) // only called (and takes precedence) by html GET requests
        {
            Any(request);
            return HttpResult.Redirect(Request.GetView()); //added by [DefaultView]
        }

        public void Any(ResetContacts request) => Contacts.Clear();
    }

    [DefaultView("/validation/server/contacts/edit")]
    public class UpdateContactServices : Service
    {
        public object Any(UpdateContact request)
        {
            if (!ContactServices.Contacts.TryGetValue(request.Id, out var contact))
                throw HttpError.NotFound("Contact was not found");
            
            if (contact.UserAuthId != this.GetUserId())
                throw HttpError.Forbidden("You don't have permission to modify this contact");

            contact.PopulateWith(request);
            contact.ModifiedDate = DateTime.UtcNow;
            
            return new UpdateContactResponse();
        }

        public object AnyHtml(UpdateContact request) // only called (and takes precedence) by html requests
        {
            Any(request);
            return HttpResult.Redirect("/validation/server/contacts");
        }
    }

    public static class ContactServiceExtensions
    {
        public static int GetUserId(this Service service) => int.Parse(service.GetSession().UserAuthId);

        public static ServiceModel.Types.Contact ToDto(this Data.Contact from) => from.ConvertTo<ServiceModel.Types.Contact>();
    }

}