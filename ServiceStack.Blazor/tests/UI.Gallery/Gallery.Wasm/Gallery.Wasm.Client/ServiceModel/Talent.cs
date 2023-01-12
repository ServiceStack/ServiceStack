using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = Icons.Contact)]
public class Contact
{
    [AutoIncrement]
    public int Id { get; set; }

    [Computed]
    public string DisplayName => FirstName + " " + LastName;
    [Format(FormatMethods.IconRounded)]
    public string ProfileUrl { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [Format(FormatMethods.Currency)]
    public int? SalaryExpectation { get; set; }

    public string JobType { get; set; }
    public int AvailabilityWeeks { get; set; }
    public EmploymentType PreferredWorkType { get; set; }
    public string PreferredLocation { get; set; }

    [Format(FormatMethods.LinkEmail, Options = 
        @"{target:'_self',subject:'New Job Opportunity',
           body:'We have an exciting new opportunity...', cls:'text-green-600'}")]
    public string Email { get; set; }
    [Format(FormatMethods.LinkPhone)]
    public string Phone { get; set; }
    public List<string>? Skills { get; set; }
    public string About { get; set; }

    [Reference]
    public List<JobApplication> Applications { get; set; }
}

[Icon(Svg = Icons.Job)]
public class Job : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public string Company { get; set; }
    public string Location { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD)]
    public int SalaryRangeLower { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD)]
    public int SalaryRangeUpper { get; set; }
    public string Description { get; set; }
    [Reference]
    public List<JobApplication> Applications { get; set; } = new();
    public DateTime Closing { get; set; }
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Casual,
    Contract
}

[Icon(Svg = Icons.Comment)]
public class JobApplicationComment : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    [Reference, Format(FormatMethods.Hidden)]
    public AppUser AppUser { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }
    public string Comment { get; set; }
}

[Icon(Svg = Icons.Application)]
public class JobApplication
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(Job))]
    public int JobId { get; set; }
    [References(typeof(Contact))]
    public int ContactId { get; set; }

    [Reference]
    public Job Position { get; set; }

    [Reference]
    public Contact Applicant { get; set; }

    [Reference]
    public List<JobApplicationComment> Comments { get; set; }

    public DateTime AppliedDate { get; set; }

    public JobApplicationStatus ApplicationStatus { get; set; }

    [Reference]
    public List<JobApplicationAttachment> Attachments { get; set; }

    [Reference]
    public List<JobApplicationEvent> Events { get; set; }

    [Reference, Ref(Model = nameof(PhoneScreen), RefId = nameof(Id))]
    public PhoneScreen PhoneScreen { get; set; }

    [Reference, Ref(Model = nameof(Interview), RefId = nameof(Id))]
    public Interview Interview { get; set; }

    [Reference, Ref(Model = nameof(JobOffer), RefId = nameof(Id))]
    public JobOffer JobOffer { get; set; }
}

[Icon(Svg = Icons.Event)]
public class JobApplicationEvent : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    [Reference, Format(FormatMethods.Hidden)]
    public AppUser AppUser { get; set; }

    public string Description { get; set; }

    public JobApplicationStatus? Status { get; set; }

    public DateTime EventDate { get; set; }

}

[Icon(Svg = Icons.PhoneScreen)]
public class PhoneScreen : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    [Reference, Format(FormatMethods.Hidden)]
    public AppUser AppUser { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    [ReferenceField(typeof(JobApplication), nameof(JobApplicationId))]
    public JobApplicationStatus? ApplicationStatus { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Icon(Svg = Icons.Interview)]
public class Interview : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [IntlRelativeTime]
    public DateTime BookingTime { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    [Reference, Format(FormatMethods.Hidden)]
    public AppUser AppUser { get; set; }

    [ReferenceField(typeof(JobApplication), nameof(JobApplicationId))]
    public JobApplicationStatus? ApplicationStatus { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Icon(Svg = Icons.Offer)]
public class JobOffer : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [IntlNumber(Currency = NumberCurrency.USD)]
    public int SalaryOffer { get; set; }

    public string Currency { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    [References(typeof(AppUser))]
    public int AppUserId { get; set; }

    [Reference, Format(FormatMethods.Hidden)]
    public AppUser AppUser { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

public enum JobApplicationStatus
{
    [Description("Application was received")]
    Applied,
    [Description("Advanced to phone screening")]
    PhoneScreening,
    [Description("Completed phone screening")]
    PhoneScreeningCompleted,
    [Description("Advanced to interview")]
    Interview,
    [Description("Interview was completed")]
    InterviewCompleted,
    [Description("Advanced to offer")]
    Offer,
    [Description("Application was denied")]
    Disqualified
}

[Tag("Talent")]
public class QueryJobApplicationAttachments : QueryDb<JobApplicationAttachment>
{
    public int? Id { get; set; }
}

[Icon(Svg = Icons.Attachment)]
public class JobApplicationAttachment
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    public string FileName { get; set; }
    [Format(FormatMethods.Attachment)]
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    [Format(FormatMethods.Bytes)]
    public long ContentLength { get; set; }
}

[Tag("Talent")]
public class QueryContacts : QueryDb<Contact>
{
    public int? Id { get; set; }
}

[Tag("Talent")]
public class CreateContact : ICreateDb<Contact>, IReturn<Contact>
{
    [ValidateNotEmpty]
    public string FirstName { get; set; } = string.Empty;
    [ValidateNotEmpty]
    public string LastName { get; set; } = string.Empty;
    [Input(Type = "file"), UploadTo("profiles")]
    public string? ProfileUrl { get; set; }
    public int? SalaryExpectation { get; set; }
    [ValidateNotEmpty]
    public string JobType { get; set; } = string.Empty;
    public int AvailabilityWeeks { get; set; }
    public EmploymentType PreferredWorkType { get; set; }
    [ValidateNotEmpty]
    public string PreferredLocation { get; set; } = string.Empty;
    [ValidateNotEmpty]
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Skills { get; set; }
    [Input(Type = "textarea")]
    [FieldCss(Field = "col-span-12 text-center", Input = "h-48", Label = "text-xl text-indigo-700")]
    public string? About { get; set; }
}

[Tag("Talent")]
public class UpdateContact : IPatchDb<Contact>, IReturn<Contact>
{
    public int Id { get; set; }
    [ValidateNotEmpty]
    public string? FirstName { get; set; }
    [ValidateNotEmpty]
    public string? LastName { get; set; }
    [Input(Type = "file"), UploadTo("profiles")]
    public string? ProfileUrl { get; set; }
    public int? SalaryExpectation { get; set; }
    [ValidateNotEmpty]
    public string? JobType { get; set; }
    public int? AvailabilityWeeks { get; set; }
    public EmploymentType? PreferredWorkType { get; set; }
    public string? PreferredLocation { get; set; }
    [ValidateNotEmpty]
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Skills { get; set; }
    [Input(Type = "textarea")]
    [FieldCss(Field = "col-span-12 text-center", Input = "h-48", Label= "text-xl text-indigo-700")]
    public string? About { get; set; }
}

[Tag("Talent")]
public class DeleteContact : IDeleteDb<Contact>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag("Talent")]
public class QueryJobs : QueryDb<Job>
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }
}


[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreateJob : ICreateDb<Job>, IReturn<Job>
{
    public string Title { get; set; }

    [ValidateGreaterThan(0)]
    public int SalaryRangeLower { get; set; }
    [ValidateGreaterThan(0)]
    public int SalaryRangeUpper { get; set; }
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center", Input = "h-48")]
    public string Description { get; set; }

    public EmploymentType EmploymentType { get; set; }
    public string Company { get; set; }
    public string Location { get; set; }

    public DateTime Closing { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
public class UpdateJob : IPatchDb<Job>, IReturn<Job>
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int? SalaryRangeLower { get; set; }
    public int? SalaryRangeUpper { get; set; }
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center", Input = "h-48")]
    public string? Description { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteJob : IDeleteDb<Job>, IReturn<Job>
{
    public int Id { get; set; }
}


[Tag("Talent")]
public class QueryJobApplications : QueryDb<JobApplication>
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }

    public int? JobId { get; set; }
}

[Tag("Talent")]
public class CreateJobApplication : ICreateDb<JobApplication>, IReturn<JobApplication>
{
    [ValidateGreaterThan(0)]
    public int JobId { get; set; }
    [ValidateGreaterThan(0)]
    public int ContactId { get; set; }
    public DateTime AppliedDate { get; set; }
    public JobApplicationStatus ApplicationStatus { get; set; }
    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment> Attachments { get; set; }
}

[Tag("Talent")]
public class UpdateJobApplication : IPatchDb<JobApplication>, IReturn<JobApplication>
{
    public int Id { get; set; }
    public int? JobId { get; set; }
    public int? ContactId { get; set; }
    public DateTime? AppliedDate { get; set; }
    public JobApplicationStatus? ApplicationStatus { get; set; }
    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment>? Attachments { get; set; }
}

[Tag("Talent")]
public class DeleteJobApplication : IDeleteDb<JobApplication>, IReturnVoid
{
    public int Id { get; set; }
}


[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class QueryPhoneScreens : QueryDb<PhoneScreen>
{
    public int? Id { get; set; }
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreatePhoneScreen : ICreateDb<PhoneScreen>, IReturn<PhoneScreen>
{
    [ValidateGreaterThan(0)]
    public int JobApplicationId { get; set; }
    [ValidateGreaterThan(0, Message = "An employee to perform the phone screening must be selected.")]
    public int AppUserId { get; set; }

    public JobApplicationStatus ApplicationStatus { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
public class UpdatePhoneScreen : IPatchDb<PhoneScreen>, IReturn<PhoneScreen>
{
    public int Id { get; set; }
    public int? JobApplicationId { get; set; }
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? Notes { get; set; }
    public JobApplicationStatus? ApplicationStatus { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class QueryInterviews : QueryDb<Interview>
{
    public int? Id { get; set; }
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreateInterview : ICreateDb<Interview>, IReturn<Interview>
{
    [ValidateNotNull]
    public DateTime? BookingTime { get; set; }
    [ValidateGreaterThan(0)]
    public int JobApplicationId { get; set; }
    [ValidateGreaterThan(0, Message = "An employee to perform interview must be selected.")]
    public int AppUserId { get; set; }

    public JobApplicationStatus ApplicationStatus { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
public class UpdateInterview : IPatchDb<Interview>, IReturn<Interview>
{
    [ValidateGreaterThan(0)]
    public int Id { get; set; }
    public int? JobApplicationId { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? Notes { get; set; }

    public JobApplicationStatus? ApplicationStatus { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobOffers : QueryDb<JobOffer>
{
    public int? Id { get; set; }
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreateJobOffer : ICreateDb<JobOffer>, IReturn<JobOffer>
{
    [ValidateGreaterThan(0)]
    public int SalaryOffer { get; set; }

    [Input(Type = "select", EvalAllowableValues = "AppData.Currencies")]
    public string Currency { get; set; }

    [ValidateGreaterThan(0)]
    public int JobApplicationId { get; set; }

    public JobApplicationStatus ApplicationStatus { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea")]
    public string Notes { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class UpdateJobOffer : IPatchDb<JobOffer>, IReturn<JobOffer>
{
    public int? Id { get; set; }
    public int? SalaryOffer { get; set; }

    [Input(Type = "select", EvalAllowableValues = "AppData.Currencies")]
    public string Currency { get; set; }
    public int? JobApplicationId { get; set; }

    public JobApplicationStatus? ApplicationStatus { get; set; }
    [Input(Type="textarea")]
    public string? Notes { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobApplicationEvents : QueryDb<JobApplicationEvent>
{
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
public class CreateJobApplicationEvent : ICreateDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>
{
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
public class UpdateJobApplicationEvent : IPatchDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>
{
    public int Id { get; set; }
    public JobApplicationStatus? Status { get; set; }

    public string? Description { get; set; }

    public DateTime? EventDate { get; set; }

}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteJobApplicationEvent : IDeleteDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>, IReturnVoid
{
}

[Tag("Talent")]
public class QueryAppUsers : QueryDb<AppUser>
{
    public string? EmailContains { get; set; }
    public string? FirstNameContains { get; set; }
    public string? LastNameContains { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobApplicationComments : QueryDb<JobApplicationComment>
{
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditCreate)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class CreateJobApplicationComment : ICreateDb<JobApplicationComment>, IReturn<JobApplicationComment>
{
    [ValidateGreaterThan(0)]
    public int JobApplicationId { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Comment { get; set; } = string.Empty;
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditModify)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class UpdateJobApplicationComment : IPatchDb<JobApplicationComment>, IReturn<JobApplicationComment>
{
    public int Id { get; set; }

    public int? JobApplicationId { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? Comment { get; set; }
}

[Tag("Talent")]
[ValidateIsAuthenticated]
[AutoApply(Behavior.AuditSoftDelete)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class DeleteJobApplicationComment : IDeleteDb<JobApplicationComment>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag("Talent")]
public class TalentStats : IGet, IReturn<TalentStatsResponse>
{
}

public class TalentStatsResponse
{
    public long TotalJobs { get; set; }
    public long TotalContacts { get; set; }
    public int AvgSalaryExpectation { get; set; }
    public int AvgSalaryLower { get; set; }
    public int AvgSalaryUpper { get; set; }
    public decimal PreferredRemotePercentage { get; set; }
}
