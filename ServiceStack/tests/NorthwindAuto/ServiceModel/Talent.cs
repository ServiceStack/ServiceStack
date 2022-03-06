using ServiceStack;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;

namespace TalentBlazor.ServiceModel;

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'><path fill='currentColor' d='M5 3a3 3 0 1 1 6 0a3 3 0 0 1-6 0zm7.001 4h-.553l-3.111 6.316L9.5 7.5L8 6L6.5 7.5l1.163 5.816L4.552 7h-.554c-1.999 0-1.999 1.344-1.999 3v5h12v-5c0-1.656 0-3-1.999-3z'/></svg>")]
public class Contact : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    public string DisplayName => FirstName + " " + LastName;
    [Format(FormatMethods.IconRounded)]
    public string ProfileUrl { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [IntlNumber(Currency = NumberCurrency.USD)]
    public int? SalaryExpectation { get; set; }

    public string JobType { get; set; }
    public int AvailabilityWeeks { get; set; }
    public EmploymentType PreferredWorkType { get; set; }
    public string PreferredLocation { get; set; }

    public string Email { get; set; }
    public string Phone { get; set; }

    public string About { get; set; }

    [Reference]
    public List<JobApplication> Applications { get; set; }
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 28 28'><path fill='currentColor' d='M13.11 2.293a1.5 1.5 0 0 1 1.78 0l9.497 7.005c1.124.83.598 2.578-.74 2.7H4.353c-1.338-.122-1.863-1.87-.74-2.7l9.498-7.005ZM14 8.999a1.5 1.5 0 1 0 0-3a1.5 1.5 0 0 0 0 3Zm5.5 4h2.499v6h-2.5v-6Zm-2 6v-6H15v6h2.5ZM13 19v-6h-2.5v6H13Zm-4.499 0v-6h-2.5v6h2.5Zm-2.25 1a3.25 3.25 0 0 0-3.25 3.25v.5a.752.752 0 0 0 .75.751h20.497a.75.75 0 0 0 .75-.75v-.5a3.25 3.25 0 0 0-3.25-3.25H6.252Z'/></svg>")]
public class Job : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [Reference]
    public List<JobApplication> Applications { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }

    [IntlNumber(Currency = NumberCurrency.USD)]
    public int SalaryRangeLower { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD)]
    public int SalaryRangeUpper { get; set; }

    public EmploymentType EmploymentType { get; set; }
    public string Company { get; set; }
    public string Location { get; set; }

    public DateTime Closing { get; set; }
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Casual,
    Contract
}

[Icon(Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" aria-hidden=\"true\" role=\"img\" width=\"1em\" height=\"1em\" preserveAspectRatio=\"xMidYMid meet\" viewBox=\"0 0 512 512\"><path fill=\"currentColor\" d=\"M256 32C114.6 32 0 125.1 0 240c0 49.6 21.4 95 57 130.7C44.5 421.1 2.7 466 2.2 466.5c-2.2 2.3-2.8 5.7-1.5 8.7S4.8 480 8 480c66.3 0 116-31.8 140.6-51.4c32.7 12.3 69 19.4 107.4 19.4c141.4 0 256-93.1 256-208S397.4 32 256 32zM128 272c-17.7 0-32-14.3-32-32s14.3-32 32-32s32 14.3 32 32s-14.3 32-32 32zm128 0c-17.7 0-32-14.3-32-32s14.3-32 32-32s32 14.3 32 32s-14.3 32-32 32zm128 0c-17.7 0-32-14.3-32-32s14.3-32 32-32s32 14.3 32 32s-14.3 32-32 32z\"/></svg>")]
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

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M18 19H6v-1.4c0-2 4-3.1 6-3.1s6 1.1 6 3.1M12 7a3 3 0 0 1 3 3a3 3 0 0 1-3 3a3 3 0 0 1-3-3a3 3 0 0 1 3-3m0-4a1 1 0 0 1 1 1a1 1 0 0 1-1 1a1 1 0 0 1-1-1a1 1 0 0 1 1-1m7 0h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2Z'/></svg>")]
public class JobApplication : AuditBase
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
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M18 11c1.49 0 2.87.47 4 1.26V8c0-1.11-.89-2-2-2h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h7.68A6.995 6.995 0 0 1 18 11zm-8-7h4v2h-4V4z'/><path fill='currentColor' d='M18 13c-2.76 0-5 2.24-5 5s2.24 5 5 5s5-2.24 5-5s-2.24-5-5-5zm1.65 7.35L17.5 18.2V15h1v2.79l1.85 1.85l-.7.71z'/></svg>")]
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

    public JobApplicationStatus? Status { get; set; }

    public string Description { get; set; }

    public DateTime EventDate { get; set; }

}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M22 3H2C.9 3 0 3.9 0 5v14c0 1.1.9 2 2 2h20c1.1 0 1.99-.9 1.99-2L24 5c0-1.1-.9-2-2-2zM8 6c1.66 0 3 1.34 3 3s-1.34 3-3 3s-3-1.34-3-3s1.34-3 3-3zm6 12H2v-1c0-2 4-3.1 6-3.1s6 1.1 6 3.1v1zm3.85-4h1.64L21 16l-1.99 1.99A7.512 7.512 0 0 1 16.28 14c-.18-.64-.28-1.31-.28-2s.1-1.36.28-2a7.474 7.474 0 0 1 2.73-3.99L21 8l-1.51 2h-1.64c-.22.63-.35 1.3-.35 2s.13 1.37.35 2z'/></svg>")]
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

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><path fill='currentColor' d='M5.8 12.2V6H2C.9 6 0 6.9 0 8v6c0 1.1.9 2 2 2h1v3l3-3h5c1.1 0 2-.9 2-2v-1.82a.943.943 0 0 1-.2.021h-7V12.2zM18 1H9c-1.1 0-2 .9-2 2v8h7l3 3v-3h1c1.1 0 2-.899 2-2V3c0-1.1-.9-2-2-2z'/></svg>")]
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

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

public enum JobApplicationStatus
{
    Applied,
    PhoneScreening,
    PhoneScreeningCompleted,
    Interview,
    InterviewCompleted,
    Offer,
    Disqualified
}

[Tag("Talent")]
public class QueryJobApplicationAttachment : QueryDb<JobApplicationAttachment>
{
    public int? Id { get; set; }
}

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 15 15'><path fill='currentColor' d='M0 4.5V0h1v4.5a1.5 1.5 0 1 0 3 0v-3a.5.5 0 0 0-1 0V5H2V1.5a1.5 1.5 0 1 1 3 0v3a2.5 2.5 0 0 1-5 0Z'/><path fill='currentColor' fill-rule='evenodd' d='M12.5 0H6v4.5A3.5 3.5 0 0 1 2.5 8H1v5.5A1.5 1.5 0 0 0 2.5 15h10a1.5 1.5 0 0 0 1.5-1.5v-12A1.5 1.5 0 0 0 12.5 0ZM11 4H7v1h4V4Zm0 3H7v1h4V7Zm-7 3h7v1H4v-1Z' clip-rule='evenodd'/></svg>")]
public class JobApplicationAttachment
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(JobApplication))]
    public int JobApplicationId { get; set; }

    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryContacts : QueryDb<Contact>
{
    public int? Id { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
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
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? About { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateContact : IUpdateDb<Contact>, IReturn<Contact>
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
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? About { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteContact : IDeleteDb<Contact>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryJob : QueryDb<Job>
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }
}


[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreateJob : ICreateDb<Job>, IReturn<Job>
{
    public string Title { get; set; }

    public int SalaryRangeLower { get; set; }
    public int SalaryRangeUpper { get; set; }
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Description { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateJob : IUpdateDb<Job>, IReturn<Job>
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int SalaryRangeLower { get; set; }
    public int SalaryRangeUpper { get; set; }
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Description { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteJob : IDeleteDb<Job>, IReturn<Job>
{
    public int Id { get; set; }
}


[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobApplication : QueryDb<JobApplication>
{
    public int? Id { get; set; }
    public List<int>? Ids { get; set; }

    public int? JobId { get; set; }
}

public interface IHasJobId
{
    public int JobId { get; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreateJobApplication : ICreateDb<JobApplication>, IReturn<JobApplication>, IHasJobId
{
    public int JobId { get; set; }
    public int ContactId { get; set; }
    public DateTime AppliedDate { get; set; }
    public JobApplicationStatus ApplicationStatus { get; set; }
    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment> Attachments { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateJobApplication : IUpdateDb<JobApplication>, IReturn<JobApplication>, IHasJobId
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int ContactId { get; set; }
    public DateTime AppliedDate { get; set; }
    public JobApplicationStatus ApplicationStatus { get; set; }
    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment> Attachments { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteJobApplication : IDeleteDb<JobApplication>, IReturnVoid
{
    public int Id { get; set; }
}


[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryPhoneScreen : QueryDb<PhoneScreen>
{
    public int? Id { get; set; }
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreatePhoneScreen : ICreateDb<PhoneScreen>, IReturn<PhoneScreen>
{
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }
    [ValidateNotEmpty(Message = "An employee to perform the phone screening must be selected.")]
    public int AppUserId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdatePhoneScreen : IUpdateDb<PhoneScreen>, IReturn<PhoneScreen>
{
    [ValidateNotEmpty]
    public int Id { get; set; }
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryInterview : QueryDb<Interview>
{
    public int? Id { get; set; }
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreateInterview : ICreateDb<Interview>, IReturn<Interview>
{
    [ValidateNotNull]
    public DateTime? BookingTime { get; set; }
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }
    [ValidateNotEmpty(Message = "An employee to perform interview must be selected.")]
    public int AppUserId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateInterview : IUpdateDb<Interview>, IReturn<Interview>
{
    [ValidateNotEmpty]
    public int Id { get; set; }
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobAppEvents : QueryDb<JobApplicationEvent>
{
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreateJobApplicationEvent : ICreateDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>
{
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateJobApplicationEvent : IUpdateDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>
{
}

[Tag("Talent")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteJobApplicationEvent : IDeleteDb<JobApplicationEvent>,
    IReturn<JobApplicationEvent>, IReturnVoid
{
}

[Tag("Talent")]
public class QueryAppUser : QueryDb<AppUser>
{
    public string? EmailContains { get; set; }
    public string? FirstNameContains { get; set; }
    public string? LastNameContains { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditQuery)]
public class QueryJobApplicationComments : QueryDb<JobApplicationComment>
{
    public int? JobApplicationId { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class CreateJobApplicationComment : ICreateDb<JobApplicationComment>, IReturn<JobApplicationComment>
{
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }

    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Comment { get; set; } = string.Empty;
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class UpdateJobApplicationComment : IUpdateDb<JobApplicationComment>, IReturn<JobApplicationComment>
{
    [ValidateNotEmpty]
    public int Id { get; set; }
    [ValidateNotEmpty]
    public int JobApplicationId { get; set; }
    [ValidateNotEmpty]
    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? Comment { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditSoftDelete)]
[AutoPopulate(nameof(JobApplicationComment.AppUserId), Eval = "userAuthId")]
public class DeleteJobApplicationComment : IDeleteDb<JobApplicationComment>, IReturnVoid
{
    [ValidateNotEmpty]
    public int Id { get; set; }
}
