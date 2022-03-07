#nullable enable

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests;

[Icon(Svg =
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 28 28'><path fill='currentColor' d='M13.11 2.293a1.5 1.5 0 0 1 1.78 0l9.497 7.005c1.124.83.598 2.578-.74 2.7H4.353c-1.338-.122-1.863-1.87-.74-2.7l9.498-7.005ZM14 8.999a1.5 1.5 0 1 0 0-3a1.5 1.5 0 0 0 0 3Zm5.5 4h2.499v6h-2.5v-6Zm-2 6v-6H15v6h2.5ZM13 19v-6h-2.5v6H13Zm-4.499 0v-6h-2.5v6h2.5Zm-2.25 1a3.25 3.25 0 0 0-3.25 3.25v.5a.752.752 0 0 0 .75.751h20.497a.75.75 0 0 0 .75-.75v-.5a3.25 3.25 0 0 0-3.25-3.25H6.252Z'/></svg>")]
public class Job : ServiceStack.AuditBase
{
    [AutoIncrement] public int Id { get; set; }

    [Reference] public List<JobApplication> Applications { get; set; }

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

[Icon(Svg =
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M18 19H6v-1.4c0-2 4-3.1 6-3.1s6 1.1 6 3.1M12 7a3 3 0 0 1 3 3a3 3 0 0 1-3 3a3 3 0 0 1-3-3a3 3 0 0 1 3-3m0-4a1 1 0 0 1 1 1a1 1 0 0 1-1 1a1 1 0 0 1-1-1a1 1 0 0 1 1-1m7 0h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2Z'/></svg>")]
public class JobApplication : ServiceStack.AuditBase
{
    [AutoIncrement] public int Id { get; set; }

    [References(typeof(Job))] public int JobId { get; set; }
    [References(typeof(Contact))] public int ContactId { get; set; }

    [Reference] public Job Position { get; set; }

    [Reference] public Contact Applicant { get; set; }

    public DateTime AppliedDate { get; set; }

    [Reference] public List<JobApplicationAttachment> Attachments { get; set; }

    [Reference, Ref(Model = nameof(PhoneScreen), RefId = nameof(Id))]
    public PhoneScreen PhoneScreen { get; set; }
}

[Icon(Svg =
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 15 15'><path fill='currentColor' d='M0 4.5V0h1v4.5a1.5 1.5 0 1 0 3 0v-3a.5.5 0 0 0-1 0V5H2V1.5a1.5 1.5 0 1 1 3 0v3a2.5 2.5 0 0 1-5 0Z'/><path fill='currentColor' fill-rule='evenodd' d='M12.5 0H6v4.5A3.5 3.5 0 0 1 2.5 8H1v5.5A1.5 1.5 0 0 0 2.5 15h10a1.5 1.5 0 0 0 1.5-1.5v-12A1.5 1.5 0 0 0 12.5 0ZM11 4H7v1h4V4Zm0 3H7v1h4V7Zm-7 3h7v1H4v-1Z' clip-rule='evenodd'/></svg>")]
public class JobApplicationAttachment
{
    [AutoIncrement] public int Id { get; set; }

    [References(typeof(JobApplication))] public int JobApplicationId { get; set; }

    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string ContentType { get; set; }
    public long ContentLength { get; set; }
}

[Icon(Svg =
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M22 3H2C.9 3 0 3.9 0 5v14c0 1.1.9 2 2 2h20c1.1 0 1.99-.9 1.99-2L24 5c0-1.1-.9-2-2-2zM8 6c1.66 0 3 1.34 3 3s-1.34 3-3 3s-3-1.34-3-3s1.34-3 3-3zm6 12H2v-1c0-2 4-3.1 6-3.1s6 1.1 6 3.1v1zm3.85-4h1.64L21 16l-1.99 1.99A7.512 7.512 0 0 1 16.28 14c-.18-.64-.28-1.31-.28-2s.1-1.36.28-2a7.474 7.474 0 0 1 2.73-3.99L21 8l-1.51 2h-1.64c-.22.63-.35 1.3-.35 2s.13 1.37.35 2z'/></svg>")]
public class PhoneScreen : ServiceStack.AuditBase
{
    [AutoIncrement] public int Id { get; set; }

    [References(typeof(JobApplication))] public int JobApplicationId { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string Notes { get; set; }
}

[Icon(Svg =
    "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'><path fill='currentColor' d='M5 3a3 3 0 1 1 6 0a3 3 0 0 1-6 0zm7.001 4h-.553l-3.111 6.316L9.5 7.5L8 6L6.5 7.5l1.163 5.816L4.552 7h-.554c-1.999 0-1.999 1.344-1.999 3v5h12v-5c0-1.656 0-3-1.999-3z'/></svg>")]
public class Contact : ServiceStack.AuditBase
{
    [AutoIncrement] public int Id { get; set; }

    public string DisplayName => FirstName + " " + LastName;
    [Format(FormatMethods.IconRounded)] public string ProfileUrl { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [IntlNumber(Currency = NumberCurrency.USD)]
    public int? SalaryExpectation { get; set; }

    [Reference] public List<JobApplication> Applications { get; set; }
}

/* AutoQuery APIs */

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

    public EmploymentType EmploymentType { get; set; }
    public string Company { get; set; }
    public string Location { get; set; }

    public DateTime Closing { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateJob : IPatchDb<Job>, IReturn<Job>
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int? SalaryRangeLower { get; set; }
    public int? SalaryRangeUpper { get; set; }

    [Input(Type = "textarea"), FieldCss(Field = "col-span-12 text-center")]
    public string? Description { get; set; }

    public EmploymentType? EmploymentType { get; set; }
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
}

public class TestReferences : IReturn<TestReferences>
{
}

public class AutoQueryCrudReferencesServices : Service
{
    public object Any(TestReferences request) => request;
}

public class AutoQueryCrudReferencesTests
{
    private ServiceStackHost appHost;

    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(AutoQueryCrudReferencesTests), typeof(AutoQueryCrudReferencesServices))
        {
        }

        public override void Configure(Container container)
        {
            container.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
                ":memory:", SqliteDialect.Provider));

            SetConfig(new HostConfig
            {
                AdminAuthSecret = "secret",
            });

            Plugins.Add(new AutoQueryFeature());

            using var db = container.Resolve<IDbConnectionFactory>().Open();
            db.CreateTable<Job>();
            db.CreateTable<JobApplication>();
            db.CreateTable<JobApplicationAttachment>();
            db.CreateTable<Contact>();
            db.CreateTable<PhoneScreen>();

            var memFs = GetVirtualFileSource<MemoryVirtualFiles>();
            Plugins.Add(new FilesUploadFeature(
                new UploadLocation("applications", memFs, maxFileCount: 3, maxFileBytes: 10_000_000,
                    resolvePath: ctx => ctx.GetLocationPath(ctx.GetDto<IHasJobId>().JobId + $"/{ctx.DateSegment}/{ctx.FileName}"),
                    readAccessRole:RoleNames.AllowAnon, writeAccessRole:RoleNames.AllowAnon)));
        }
    }

    public AutoQueryCrudReferencesTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    JsonApiClient CreateClient() => new JsonApiClient(Config.ListeningOn)
        .Apply(c => c.AddHeader(Keywords.AuthSecret, "secret"));

    [Test]
    public void Upload_FileUploads_into_AutoQuery_References()
    {
        var client = CreateClient();
        var createJob = new CreateJob
        {
            Title = "Test Job",
            Description = "Test Job Description",
            SalaryRangeLower = 100000,
            SalaryRangeUpper = 200000,
            Company = "Test Company",
            Closing = DateTime.UtcNow.AddDays(10),
            Location = "Remote",
            EmploymentType = EmploymentType.FullTime,
        };
        var createJobApi = client.Api(createJob);
        Assert.That(createJobApi.Succeeded);
        
        var createContact = new CreateContact
        {
            FirstName = "Test",
            LastName = "Contact",
            ProfileUrl = "/profiles/users/1.jpg",
            SalaryExpectation = 200000,
        };
        var createContactApi = client.Api(createContact);
        Assert.That(createContactApi.Succeeded);

        var createJobApplication = new CreateJobApplication
        {
            JobId = createJobApi.Response!.Id,
            AppliedDate = DateTime.UtcNow,
            ContactId = createContactApi.Response!.Id,
        };

        var fileContents = new MemoryStream("ABC".ToUtf8Bytes());

        var fileName = "application.txt";
        var response = client.PostFileWithRequest<JobApplication>(fileContents, fileName, createJobApplication, 
            fieldName: nameof(CreateJobApplication.Attachments));
        response.PrintDump();

        var jobId = createJobApi.Response.Id;
        var jobApi = client.Api(new QueryJob { Id = jobId });
        Assert.That(jobApi.Response!.Results.Count, Is.EqualTo(1));
        jobApi.Response.Results[0].PrintDump();

        var contactApi = client.Api(new QueryContacts { Id = createContactApi.Response.Id });
        Assert.That(contactApi.Response!.Results.Count, Is.EqualTo(1));
        contactApi.Response!.Results[0].PrintDump();

        var jobAppId = response.Id;
        var jobApplicationApi = client.Api(new QueryJobApplication { Id = jobAppId });
        Assert.That(jobApplicationApi.Response!.Results.Count, Is.EqualTo(1));
        jobApplicationApi.Response!.Results[0].PrintDump();

        var appAttachments = jobApplicationApi.Response!.Results[0].Attachments;
        Assert.That(appAttachments.Count, Is.EqualTo(1));
        var attachment = appAttachments[0];
        Assert.That(attachment.Id, Is.GreaterThan(0));
        Assert.That(attachment.JobApplicationId, Is.EqualTo(jobAppId));
        Assert.That(attachment.FilePath, Is.EqualTo($"/uploads/applications/{jobId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}"));
        Assert.That(attachment.FileName, Is.EqualTo(fileName));
        Assert.That(attachment.ContentType, Is.EqualTo(MimeTypes.GetMimeType(fileName)));
        Assert.That(attachment.ContentLength, Is.GreaterThan(0));

        var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
        var file = memFs.GetAllFiles().FirstOrDefault(x => x.Name == fileName);
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.ReadAllText(), Is.EqualTo("ABC"));
    }
}

#endif