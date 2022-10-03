using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;

namespace TalentBlazor.ServiceModel;

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path fill='currentColor' d='M10.277 2.084a.5.5 0 0 0-.554 0a15.05 15.05 0 0 1-6.294 2.421A.5.5 0 0 0 3 5v4.5c0 3.891 2.307 6.73 6.82 8.467a.5.5 0 0 0 .36 0C14.693 16.23 17 13.39 17 9.5V5a.5.5 0 0 0-.43-.495a15.05 15.05 0 0 1-6.293-2.421ZM10 9.5a2 2 0 1 1 0-4a2 2 0 0 1 0 4Zm0 5c-2.5 0-3.5-1.25-3.5-2.5A1.5 1.5 0 0 1 8 10.5h4a1.5 1.5 0 0 1 1.5 1.5c0 1.245-1 2.5-3.5 2.5Z'/></svg>")]
public class AppUser : IUserAuth
{
    [AutoIncrement]
    public int Id { get; set; }
    public string DisplayName { get; set; }

    [Index]
    [Format(FormatMethods.LinkEmail)]
    public string Email { get; set; }

    // Custom Properties
    [Format(FormatMethods.IconRounded)]
    public string ProfileUrl { get; set; }

    public Department Department { get; set; }
    public string Title { get; set; }
    public string JobArea { get; set; }
    public string Location { get; set; }
    public int Salary { get; set; }
    public string About { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string LastLoginIp { get; set; }

    // UserAuth Properties
    public string UserName { get; set; }
    public string PrimaryEmail { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Company { get; set; }
    public string Country { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string BirthDateRaw { get; set; }
    public string Address { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Culture { get; set; }
    public string FullName { get; set; }
    public string Gender { get; set; }
    public string Language { get; set; }
    public string MailAddress { get; set; }
    public string Nickname { get; set; }
    public string PostalCode { get; set; }
    public string TimeZone { get; set; }
    public string Salt { get; set; }
    public string PasswordHash { get; set; }
    public string DigestHa1Hash { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int InvalidLoginAttempts { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public DateTime? LockedDate { get; set; }
    public string RecoveryToken { get; set; }

    //Custom Reference Data
    public int? RefId { get; set; }
    public string RefIdStr { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public enum Department
{
    None,
    Marketing,
    Accounts,
    Legal,
    HumanResources,
}
