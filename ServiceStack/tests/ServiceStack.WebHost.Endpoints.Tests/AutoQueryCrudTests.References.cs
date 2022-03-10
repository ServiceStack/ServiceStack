#nullable enable

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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

[Tag("Talent")]
[AutoApply(Behavior.AuditCreate)]
public class CreateJobApplication : ICreateDb<JobApplication>, IReturn<JobApplication>
{
    public int JobId { get; set; }
    public int ContactId { get; set; }
    public DateTime AppliedDate { get; set; }

    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment> Attachments { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class UpdateJobApplication : IUpdateDb<JobApplication>, IReturn<JobApplication>
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int ContactId { get; set; }
    public DateTime AppliedDate { get; set; }

    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment> Attachments { get; set; }
}

[Tag("Talent")]
[AutoApply(Behavior.AuditModify)]
public class PatchJobApplication : IPatchDb<JobApplication>, IReturn<JobApplication>
{
    public int Id { get; set; }
    public int? JobId { get; set; }
    public int? ContactId { get; set; }
    public DateTime? AppliedDate { get; set; }
    [Input(Type = "file"), UploadTo("applications")]
    public List<JobApplicationAttachment>? Attachments { get; set; }
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

[Route("/upload-single-string/{Id}")]
public class UploadToSingleString : IPost, IReturn<UploadToSingleString>
{
    public int Id { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public string UploadPath { get; set; }
}

[Route("/upload-multi-string/{Id}")]
public class UploadToMultiString : IPost, IReturn<UploadToMultiString>
{
    public int Id { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public List<string> UploadPaths { get; set; }
}

[Route("/upload-single-poco/{Id}")]
public class UploadToSinglePoco : IPost, IReturn<UploadToSinglePoco>
{
    public int Id { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public UploadedFile UploadedFile { get; set; }
}

[Route("/upload-multi-poco/{Id}")]
public class UploadToMultiPoco : IPost, IReturn<UploadToMultiPoco>
{
    public int Id { get; set; }

    [Input(Type = "file"), UploadTo("profiles")]
    public List<UploadedFile> UploadedFiles { get; set; }
}

public class MultipartRequest : IPost, IReturn<MultipartRequest>
{
    public int Id { get; set; }
    public string String { get; set; }
    public Contact Contact { get; set; }
    [MultiPartField(MimeTypes.Json)]
    public PhoneScreen PhoneScreen { get; set; }
    [MultiPartField(MimeTypes.Csv)]
    public List<Contact> Contacts { get; set; }
    [UploadTo("profiles")]
    public string ProfileUrl { get; set; }
    [UploadTo("applications")]
    public List<UploadedFile> UploadedFiles { get; set; } 
}

public class FileUploadTestServices : Service
{
    public object Any(UploadToSingleString request) => request;
    public object Any(UploadToMultiString request) => request;
    public object Any(UploadToSinglePoco request) => request;
    public object Any(UploadToMultiPoco request) => request;
    public object Any(MultipartRequest request) => request;
}

public class AutoQueryCrudReferencesTests
{
    private ServiceStackHost appHost;

    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(AutoQueryCrudReferencesTests), 
            typeof(AutoQueryCrudReferencesServices),
            typeof(FileUploadTestServices)) {}

        public override void Configure(Container container)
        {
            container.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
                ":memory:", SqliteDialect.Provider));

            SetConfig(new HostConfig
            {
                AdminAuthSecret = "secret",
                DebugMode = true,
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
                new UploadLocation("profiles", memFs),
                new UploadLocation("applications", memFs, maxFileCount: 3, maxFileBytes: 10_000_000,
                    resolvePath: ctx => ctx.GetLocationPath((ctx.Dto is CreateJobApplication create
                        ? $"job/{create.JobId}"
                        : $"app/{ctx.Dto.GetId()}") + $"/{ctx.DateSegment}/{ctx.FileName}"),
                    readAccessRole:RoleNames.AllowAnon, writeAccessRole:RoleNames.AllowAnon)
            ));
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

    JsonApiClient CreateAuthClient() => new JsonApiClient(Config.ListeningOn)
        .Apply(c => c.AddHeader(Keywords.AuthSecret, "secret"));

    JsonApiClient CreateAnonClient() => new(Config.ListeningOn);

    [Test]
    public void Does_not_allow_anon_access_by_default()
    {
        var anonClient = CreateAnonClient();
        var ms = new MemoryStream("ABC".ToUtf8Bytes());
        try
        {
            anonClient.PostFile<UploadToSingleString>("/upload-single-string/1", ms, "auth-test.txt", 
                fieldName:nameof(UploadToSingleString.UploadPath));
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            Assert.That(e.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }

        var authClient = CreateAuthClient();
        ms.Position = 0;
        var response = authClient.PostFile<UploadToSingleString>("/upload-single-string/1", ms, "auth-test.txt", 
            fieldName:nameof(UploadToSingleString.UploadPath));
        
        try
        {
            anonClient.Get<string>(response.UploadPath);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            Assert.That(e.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }
        
        var contents = authClient.Get<string>(response.UploadPath);
        Assert.That(contents, Is.EqualTo("ABC"));
    }

    [Test]
    public void Can_upload_to_single_string()
    {
        var client = CreateAuthClient();

        var ms = new MemoryStream("ABC".ToUtf8Bytes());
        var response = client.PostFile<UploadToSingleString>("/upload-single-string/1", ms, $"{nameof(UploadToSingleString)}.txt",
            fieldName:nameof(UploadToSingleString.UploadPath));
        response.PrintDump();
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.UploadPath, Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/{nameof(UploadToSingleString)}.txt"));

        var contents = client.Get<string>(response.UploadPath);
        Assert.That(contents, Is.EqualTo("ABC"));
    }

    [Test]
    public void Can_upload_to_string_list()
    {
        var client = CreateAuthClient();

        var ms = new MemoryStream("ABC".ToUtf8Bytes());
        var response = client.PostFile<UploadToMultiString>("/upload-multi-string/1", ms, $"{nameof(UploadToMultiString)}.txt",
            fieldName:nameof(UploadToMultiString.UploadPaths));
        response.PrintDump();
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.UploadPaths[0], Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/{nameof(UploadToMultiString)}.txt"));

        var contents = client.Get<string>(response.UploadPaths[0]);
        Assert.That(contents, Is.EqualTo("ABC"));
    }

    [Test]
    public void Can_upload_to_single_poco()
    {
        var client = CreateAuthClient();

        var ms = new MemoryStream("ABC".ToUtf8Bytes());
        var response = client.PostFile<UploadToSinglePoco>("/upload-single-poco/1", ms, $"{nameof(UploadToSinglePoco)}.txt",
            fieldName:nameof(UploadToSinglePoco.UploadedFile));
        response.PrintDump();
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.UploadedFile.FilePath, Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/{nameof(UploadToSinglePoco)}.txt"));

        var contents = client.Get<string>(response.UploadedFile.FilePath);
        Assert.That(contents, Is.EqualTo("ABC"));
    }

    [Test]
    public void Can_upload_to_multi_poco()
    {
        var client = CreateAuthClient();

        var ms = new MemoryStream("ABC".ToUtf8Bytes());
        var response = client.PostFile<UploadToMultiPoco>("/upload-multi-poco/1", ms, $"{nameof(UploadToMultiPoco)}.txt",
            fieldName:nameof(UploadToMultiPoco.UploadedFiles));
        response.PrintDump();
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.UploadedFiles[0].FilePath, Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/{nameof(UploadToMultiPoco)}.txt"));

        var contents = client.Get<string>(response.UploadedFiles[0].FilePath);
        Assert.That(contents, Is.EqualTo("ABC"));
    }

    [Test]
    public void Upload_FileUploads_into_AutoQuery_References()
    {
        var client = CreateAuthClient();
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
        var contactId = createContactApi.Response!.Id;
        
        /* ICreateDb */
        var createJobApplication = new CreateJobApplication
        {
            JobId = createJobApi.Response!.Id,
            AppliedDate = DateTime.UtcNow,
            ContactId = contactId,
        };
        
        var fileName = "application.txt";
        var fileContents = new MemoryStream("ABC".ToUtf8Bytes());
        var response = client.PostFileWithRequest<JobApplication>(fileContents, fileName, createJobApplication, 
            fieldName: nameof(createJobApplication.Attachments));
        // response.PrintDump();

        var jobId = createJobApi.Response.Id;
        var jobApi = client.Api(new QueryJob { Id = jobId });
        Assert.That(jobApi.Response!.Results.Count, Is.EqualTo(1));
        // jobApi.Response.Results[0].PrintDump();

        var contactApi = client.Api(new QueryContacts { Id = createContactApi.Response.Id });
        Assert.That(contactApi.Response!.Results.Count, Is.EqualTo(1));
        // contactApi.Response!.Results[0].PrintDump();

        var jobAppId = response.Id;
        var jobApplicationApi = client.Api(new QueryJobApplication { Id = jobAppId });
        Assert.That(jobApplicationApi.Response!.Results.Count, Is.EqualTo(1));
        // jobApplicationApi.Response!.Results[0].PrintDump();

        var appAttachments = jobApplicationApi.Response!.Results[0].Attachments;
        Assert.That(appAttachments.Count, Is.EqualTo(1));
        var attachment = appAttachments[0];
        Assert.That(attachment.Id, Is.GreaterThan(0));
        Assert.That(attachment.JobApplicationId, Is.EqualTo(jobAppId));
        Assert.That(attachment.FilePath, Is.EqualTo($"/uploads/applications/job/{jobId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}"));
        Assert.That(attachment.FileName, Is.EqualTo(fileName));
        Assert.That(attachment.ContentType, Is.EqualTo(MimeTypes.GetMimeType(fileName)));
        Assert.That(attachment.ContentLength, Is.GreaterThan(0));

        var memFs = appHost.VirtualFileSources.GetMemoryVirtualFiles();
        var file = memFs.GetAllFiles().FirstOrDefault(x => x.Name == fileName);
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.ReadAllText(), Is.EqualTo("ABC"));

        /* IUpdateDb */
        var fileName2 = "application2.txt";
        var fileContents2 = new MemoryStream("DEF".ToUtf8Bytes());
        var updateJobApplication = new UpdateJobApplication {
            Id = jobAppId,
            JobId = jobId,
            ContactId = contactId,
            AppliedDate = DateTime.UtcNow.AddDays(1),
        };
        response = client.PostFileWithRequest<JobApplication>(fileContents2, fileName2, updateJobApplication, 
            fieldName: nameof(updateJobApplication.Attachments));
        // response.PrintDump();
        
        var file2 = memFs.GetAllFiles().FirstOrDefault(x => x.Name == fileName2);
        Assert.That(file2, Is.Not.Null);
        Assert.That(file2!.ReadAllText(), Is.EqualTo("DEF"));
        
        jobApplicationApi = client.Api(new QueryJobApplication { Id = jobAppId });
        // jobApplicationApi.Response!.Results[0].PrintDump();
        
        appAttachments = jobApplicationApi.Response!.Results[0].Attachments;
        Assert.That(appAttachments.Count, Is.EqualTo(2));
        attachment = appAttachments[1];
        Assert.That(attachment.Id, Is.GreaterThan(0));
        Assert.That(attachment.JobApplicationId, Is.EqualTo(jobAppId));
        Assert.That(attachment.FilePath, Is.EqualTo($"/uploads/applications/app/{jobAppId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName2}"));
        Assert.That(attachment.FileName, Is.EqualTo(fileName2));
        Assert.That(attachment.ContentType, Is.EqualTo(MimeTypes.GetMimeType(fileName2)));
        Assert.That(attachment.ContentLength, Is.GreaterThan(0));
        
        /* IPatchDb */
        var fileName3 = "application3.txt";
        var fileContents3 = new MemoryStream("GHI".ToUtf8Bytes());
        var patchJobApplication = new PatchJobApplication {
            Id = jobAppId,
        };
        response = client.PostFileWithRequest<JobApplication>(fileContents3, fileName3, patchJobApplication, 
            fieldName: nameof(patchJobApplication.Attachments));
        // response.PrintDump();
        
        var file3 = memFs.GetAllFiles().FirstOrDefault(x => x.Name == fileName3);
        Assert.That(file3, Is.Not.Null);
        Assert.That(file3!.ReadAllText(), Is.EqualTo("GHI"));
        
        jobApplicationApi = client.Api(new QueryJobApplication { Id = jobAppId });
        jobApplicationApi.Response!.Results[0].PrintDump();
        
        appAttachments = jobApplicationApi.Response!.Results[0].Attachments;
        Assert.That(appAttachments.Count, Is.EqualTo(3));
        attachment = appAttachments[2];
        Assert.That(attachment.Id, Is.GreaterThan(0));
        Assert.That(attachment.JobApplicationId, Is.EqualTo(jobAppId));
        Assert.That(attachment.FilePath, Is.EqualTo($"/uploads/applications/app/{jobAppId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileName3}"));
        Assert.That(attachment.FileName, Is.EqualTo(fileName3));
        Assert.That(attachment.ContentType, Is.EqualTo(MimeTypes.GetMimeType(fileName3)));
        Assert.That(attachment.ContentLength, Is.GreaterThan(0));

        /* Test File Upload APIs */
        var files = new[] { fileContents, fileContents2, fileContents3 };
        for (var i = 0; i < appAttachments.Count; i++)
        {
            var filePath = appAttachments[i].FilePath;
            var contents = client.Get<string>(filePath);
            Assert.That(contents, Is.EqualTo(files[i].ReadToEnd()));

            var updated = new MemoryStream("updated".ToUtf8Bytes());

            client.PutFile<ReplaceFileUploadResponse>(filePath, updated, appAttachments[i].FileName);
            contents = client.Get<string>(filePath);
            Assert.That(contents, Is.EqualTo(updated.ReadToEnd()));

            client.Delete<byte[]>(filePath);
            try
            {
                client.Get<string>(filePath);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.NotFound)));
            }
        }
    }

    [Test]
    public void Can_send_Multipart_Requests()
    {
        var client = CreateAuthClient();
        
        using var content = new MultipartFormDataContent()
            .AddParam(nameof(MultipartRequest.Id), 1)
            .AddParam(nameof(MultipartRequest.String), "foo")
            .AddParam(nameof(MultipartRequest.Contact), new Contact { Id = 1, FirstName = "First", LastName = "Last" })
            .AddJsonParam(nameof(MultipartRequest.PhoneScreen), new PhoneScreen { Id = 3, JobApplicationId = 1, Notes = "The Notes"})
            .AddCsvParam(nameof(MultipartRequest.Contacts), new[] {
                new Contact { Id = 2, FirstName = "First2", LastName = "Last2" },
                new Contact { Id = 3, FirstName = "First3", LastName = "Last3" },
            })
            .AddFile(nameof(MultipartRequest.ProfileUrl), "profile.txt", new MemoryStream("ABC".ToUtf8Bytes()))
            .AddFile(nameof(MultipartRequest.UploadedFiles), "uploadedFiles1.txt", new MemoryStream("DEF".ToUtf8Bytes()))
            .AddFile(nameof(MultipartRequest.UploadedFiles), "uploadedFiles2.txt", new MemoryStream("GHI".ToUtf8Bytes()));
        
        var api = client.ApiForm<MultipartRequest>(typeof(MultipartRequest).ToApiUrl(), content);
        if (!api.Succeeded) api.Error.PrintDump();
        
        Assert.That(api.Succeeded);
        var response = api.Response!;
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.String, Is.EqualTo("foo"));
        Assert.That(response.Contact.Id, Is.EqualTo(1));
        Assert.That(response.Contact.FirstName, Is.EqualTo("First"));
        Assert.That(response.Contacts[0].Id, Is.EqualTo(2));
        Assert.That(response.Contacts[0].FirstName, Is.EqualTo("First2"));
        Assert.That(response.Contacts[1].Id, Is.EqualTo(3));
        Assert.That(response.Contacts[1].FirstName, Is.EqualTo("First3"));
        Assert.That(response.ProfileUrl, Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/profile.txt"));
        Assert.That(response.UploadedFiles.Count, Is.EqualTo(2));
        Assert.That(response.UploadedFiles[0].FilePath, Is.EqualTo($"/uploads/applications/app/1/{DateTime.UtcNow:yyyy/MM/dd}/uploadedFiles1.txt"));
        Assert.That(response.UploadedFiles[1].FilePath, Is.EqualTo($"/uploads/applications/app/1/{DateTime.UtcNow:yyyy/MM/dd}/uploadedFiles2.txt"));
    }

    [Test]
    public async Task Can_send_Multipart_Requests_Async()
    {
        var client = CreateAuthClient();
        
        using var content = new MultipartFormDataContent()
            .AddParam(nameof(MultipartRequest.Id), 1)
            .AddParam(nameof(MultipartRequest.String), "foo")
            .AddParam(nameof(MultipartRequest.Contact), new Contact { Id = 1, FirstName = "First", LastName = "Last" })
            .AddJsonParam(nameof(MultipartRequest.PhoneScreen), new PhoneScreen { Id = 3, JobApplicationId = 1, Notes = "The Notes"})
            .AddCsvParam(nameof(MultipartRequest.Contacts), new[] {
                new Contact { Id = 2, FirstName = "First2", LastName = "Last2" },
                new Contact { Id = 3, FirstName = "First3", LastName = "Last3" },
            })
            .AddFile(nameof(MultipartRequest.ProfileUrl), "async-profile.txt", new MemoryStream("ABC".ToUtf8Bytes()))
            .AddFile(nameof(MultipartRequest.UploadedFiles), "async-uploadedFiles1.txt", new MemoryStream("DEF".ToUtf8Bytes()))
            .AddFile(nameof(MultipartRequest.UploadedFiles), "async-uploadedFiles2.txt", new MemoryStream("GHI".ToUtf8Bytes()));
        
        var api = await client.ApiFormAsync<MultipartRequest>(typeof(MultipartRequest).ToApiUrl(), content);
        if (!api.Succeeded) api.Error.PrintDump();
        
        Assert.That(api.Succeeded);
        var response = api.Response!;
        Assert.That(response.Id, Is.EqualTo(1));
        Assert.That(response.String, Is.EqualTo("foo"));
        Assert.That(response.Contact.Id, Is.EqualTo(1));
        Assert.That(response.Contact.FirstName, Is.EqualTo("First"));
        Assert.That(response.Contacts[0].Id, Is.EqualTo(2));
        Assert.That(response.Contacts[0].FirstName, Is.EqualTo("First2"));
        Assert.That(response.Contacts[1].Id, Is.EqualTo(3));
        Assert.That(response.Contacts[1].FirstName, Is.EqualTo("First3"));
        Assert.That(response.ProfileUrl, Is.EqualTo($"/uploads/profiles/{DateTime.UtcNow:yyyy/MM/dd}/async-profile.txt"));
        Assert.That(response.UploadedFiles.Count, Is.EqualTo(2));
        Assert.That(response.UploadedFiles[0].FilePath, Is.EqualTo($"/uploads/applications/app/1/{DateTime.UtcNow:yyyy/MM/dd}/async-uploadedFiles1.txt"));
        Assert.That(response.UploadedFiles[1].FilePath, Is.EqualTo($"/uploads/applications/app/1/{DateTime.UtcNow:yyyy/MM/dd}/async-uploadedFiles2.txt"));
    }
}

#endif