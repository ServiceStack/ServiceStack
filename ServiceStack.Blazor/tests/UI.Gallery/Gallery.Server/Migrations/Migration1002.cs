using Bogus;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using System.Data;

namespace MyApp.Migrations;

[Description("Add Talent Blazor")]
public class Migration1002 : MigrationBase
{
    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Computed]
        public string DisplayName => FirstName + " " + LastName;
        public string ProfileUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Format(FormatMethods.Currency)]
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

        [Reference, Ref(Model = nameof(JobOffer), RefId = nameof(Id))]
        public JobOffer JobOffer { get; set; }
    }

    public class JobApplicationEvent : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(JobApplication))]
        public int JobApplicationId { get; set; }

        [References(typeof(AppUser))]
        public int AppUserId { get; set; }

        [Reference]
        public AppUser AppUser { get; set; }
        public string Description { get; set; }
        public JobApplicationStatus? Status { get; set; }
        public DateTime EventDate { get; set; }
    }

    public class PhoneScreen : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        [Reference]
        public AppUser AppUser { get; set; }
        [References(typeof(JobApplication))]
        public int JobApplicationId { get; set; }
        [ReferenceField(typeof(JobApplication), nameof(JobApplicationId))]
        public JobApplicationStatus? ApplicationStatus { get; set; }
        public string Notes { get; set; }
    }

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
        public string Notes { get; set; }
    }

    public class JobOffer : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int SalaryOffer { get; set; }
        [References(typeof(JobApplication))]
        public int JobApplicationId { get; set; }
        [References(typeof(AppUser))]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
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


    public override void Up()
    {
        var profilesDir = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../../../wwwroot", "profiles"));
        var sourceDir = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../../../App_Data"));

        Db.CreateTable<Contact>();
        Db.CreateTable<Job>();
        Db.CreateTable<JobApplication>();
        Db.CreateTable<JobApplicationEvent>();
        Db.CreateTable<PhoneScreen>();
        Db.CreateTable<Interview>();
        Db.CreateTable<JobOffer>();
        Db.CreateTable<JobApplicationAttachment>();
        Db.CreateTable<JobApplicationComment>();

        SeedTalent(profilesDir);
        SeedAttachments(sourceDir);
        
        Db.CreateTable<FileSystemItem>();
        Db.CreateTable<FileSystemFile>();
    }

    public override void Down()
    {
        Db.DropTable<Contact>();
        Db.DropTable<Job>();
        Db.DropTable<JobApplication>();
        Db.DropTable<JobApplicationEvent>();
        Db.DropTable<PhoneScreen>();
        Db.DropTable<Interview>();
        Db.DropTable<JobApplicationAttachment>();
        Db.DropTable<JobApplicationComment>();

        Db.DropTable<FileSystemItem>();
        Db.DropTable<FileSystemFile>();
    }

    /// <summary>
    /// Create non-existing Table and add Seed Data Example
    /// </summary>
    public void SeedTalent(string profilesDir)
    {
        string[] profilePhotos = Directory.GetFiles(profilesDir);

        var now = DateTime.UtcNow;
        var jobFaker = new Faker<Job>()
            .RuleFor(j => j.Description, (faker, job1) => faker.Lorem.Paragraphs(3))
            .RuleFor(j => j.Title, (faker, job1) => faker.Name.JobTitle())
            .RuleFor(j => j.Company, (faker, job1) => faker.Company.CompanyName())
            .RuleFor(j => j.SalaryRangeLower, (faker, job1) => faker.Random.Int(90, 120) * 1000)
            .RuleFor(j => j.SalaryRangeUpper, (faker, job1) => faker.Random.Int(125, 250) * 1000)
            .RuleFor(j => j.EmploymentType, (faker, job1) =>
            {
                var rand = faker.Random.Int(0, 12);
                var empType = rand < 8 ? EmploymentType.FullTime : rand < 10 ? EmploymentType.Contract : rand < 11 ? EmploymentType.PartTime : EmploymentType.Casual;
                return empType;
            })
            .RuleFor(j => j.Location,
                (faker, job1) => faker.Random.Int(1, 3) == 1
                    ? "Remote"
                    : ($"{faker.Address.City()},{faker.Address.Country()}"))
            .RuleFor(j => j.Closing, (faker, job1) => faker.Date.Soon(21))
            .RuleFor(j => j.CreatedDate, () => now)
            .RuleFor(j => j.ModifiedDate, () => now)
            .RuleFor(j => j.CreatedBy, () => "SYSTEM")
            .RuleFor(j => j.ModifiedBy, () => "SYSTEM");

        var contactFaker = new Faker<Contact>()
            .RuleFor(c => c.FirstName, ((faker, contact1) => faker.Name.FirstName()))
            .RuleFor(c => c.LastName, ((faker, contact1) => faker.Name.LastName()))
            .RuleFor(c => c.Email,
                ((faker, contact1) => faker.Internet.Email(contact1.FirstName, contact1.LastName)))
            .RuleFor(c => c.About, (faker, contact1) => faker.Lorem.Paragraphs(3))
            .RuleFor(c => c.Phone, (faker, contact1) => faker.Phone.PhoneNumber())
            .RuleFor(c => c.JobType, (faker, contact1) => faker.Name.JobType())
            .RuleFor(c => c.AvailabilityWeeks, (faker, contact1) => faker.Random.Int(2, 12))
            .RuleFor(c => c.PreferredLocation,
                (faker, contact1) => faker.Random.Int(1, 2) == 1
                    ? "Remote"
                    : ($"{faker.Address.City()},{faker.Address.Country()}"))
            .RuleFor(c => c.PreferredWorkType, (faker, contact1) =>
            {
                var rand = faker.Random.Int(0, 12);
                var empType = rand < 8 ? EmploymentType.FullTime : rand < 10 ? EmploymentType.Contract : rand < 11 ? EmploymentType.PartTime : EmploymentType.Casual;
                return empType;
            })
            .RuleFor(c => c.SalaryExpectation, (faker, contact1) => faker.Random.Int(92, 245) * 1000);

        var jobAppFaker = new Faker<JobApplication>()
            .RuleFor(j => j.ApplicationStatus,
                ((faker, application) => faker.Random.Enum<JobApplicationStatus>()))
            .RuleFor(j => j.AppliedDate, (faker, application) => faker.Date.Recent(21))
            .RuleFor(j => j.CreatedDate, () => now)
            .RuleFor(j => j.ModifiedDate, () => now)
            .RuleFor(j => j.CreatedBy, () => "SYSTEM")
            .RuleFor(j => j.ModifiedBy, () => "SYSTEM");

        var contacts = new List<Contact>();
        var jobs = new List<Job>();

        foreach (var profilePath in profilePhotos)
        {
            var contact = contactFaker.Generate();
            contact.ProfileUrl = "/profiles".CombineWith(Path.GetRelativePath(profilesDir, profilePath).Replace("\\", "/"));
            contact.Id = 0;

            var job = jobFaker.Generate();
            job.Id = 0;

            contact.Id = (int)Db.Insert(contact, selectIdentity: true);
            job.Id = (int)Db.Insert(job, selectIdentity: true);

            contacts.Add(contact);
            jobs.Add(job);
        }

        foreach (var contact in contacts)
        {
            var faker = new Faker();
            var uniqueJobIndexes = Enumerable.Range(0, jobs.Count - 1)
                .OrderBy(x => faker.Random.Int()).Take(8);

            foreach (var index in uniqueJobIndexes)
            {
                var job = jobs[index];
                var jobApplication = jobAppFaker.Generate();
                jobApplication.JobId = job.Id;
                jobApplication.ContactId = contact.Id;
                jobApplication.Id = 0;

                jobApplication.Id = (int)Db.Insert(jobApplication, selectIdentity: true);
                jobApplication.Applicant = contact;
                jobApplication.Position = job;

                PopulateJobApplicationEvents(jobApplication);
            }
        }
    }

    public void SeedAttachments(string sourceDir)
    {
        sourceDir.AssertDir();
        var resumeFileInfo = new FileInfo(Path.Join(sourceDir, "sample_resume.pdf"));
        var coverFileInfo = new FileInfo(Path.Join(sourceDir, "sample_coverletter.pdf"));

        var jobApps = Db.LoadSelect<JobApplication>();
        foreach (var jobApp in jobApps)
        {
            Db.Save(CreatePdfAttachment(sourceDir, jobApp, resumeFileInfo));
            Db.Save(CreatePdfAttachment(sourceDir, jobApp, coverFileInfo));
        }
    }

    static JobApplicationAttachment CreatePdfAttachment(string sourceDir, JobApplication jobApp, FileInfo fileInfo)
    {
        var newName = $"{fileInfo.Name.WithoutExtension().Replace("sample_", "")}_{jobApp.Position.Title.ToLower().Replace(" ", "_")}.pdf";
        var relativePath = $"applications/app/{jobApp.JobId}/{DateTime.UtcNow:yyyy/MM/dd}/{newName}";
        var attachment = new JobApplicationAttachment
        {
            FilePath = $"/uploads/{relativePath}",
            FileName = newName,
            ContentLength = fileInfo.Length,
            ContentType = "application/pdf",
            JobApplicationId = jobApp.Id
        };
        var destFile = new FileInfo(Path.Join(sourceDir, relativePath));
        if (!destFile.Exists)
        {
            destFile.DirectoryName.AssertDir();
            File.Copy(fileInfo.FullName, destFile.FullName);
        }
        return attachment;
    }

    private static Faker<PhoneScreen> phoneScreenFaker = new Faker<PhoneScreen>()
        .RuleFor(p => p.Id, () => 0)
        .RuleFor(p => p.AppUserId, (faker, screen) => faker.Random.Int(1, 5))
        .RuleFor(p => p.Notes, (faker, screen) => faker.Lorem.Paragraph())
        .RuleFor(p => p.CreatedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.ModifiedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.CreatedBy, () => "SYSTEM")
        .RuleFor(p => p.ModifiedBy, () => "SYSTEM");

    private static Faker<Interview> interviewFaker = new Faker<Interview>()
        .RuleFor(p => p.Id, () => 0)
        .RuleFor(p => p.AppUserId, (faker, screen) => faker.Random.Int(1, 5))
        .RuleFor(p => p.Notes, (faker, screen) => faker.Lorem.Paragraph())
        .RuleFor(p => p.CreatedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.ModifiedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.CreatedBy, () => "SYSTEM")
        .RuleFor(p => p.ModifiedBy, () => "SYSTEM");

    private static Faker<JobOffer> jobOfferFaker = new Faker<JobOffer>()
        .RuleFor(p => p.Id, () => 0)
        .RuleFor(p => p.AppUserId, (faker, screen) => faker.Random.Int(1, 5))
        .RuleFor(p => p.Notes, (faker, screen) => faker.Lorem.Paragraph())
        .RuleFor(p => p.CreatedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.ModifiedDate, () => DateTime.UtcNow)
        .RuleFor(p => p.CreatedBy, () => "SYSTEM")
        .RuleFor(p => p.ModifiedBy, () => "SYSTEM");

    private static Faker<JobApplicationEvent> eventFaker = new Faker<JobApplicationEvent>()
        .RuleFor(e => e.Id, () => 0)
        .RuleFor(e => e.AppUserId, (f) => f.Random.Int(1, 5))
        .RuleFor(e => e.CreatedDate, () => DateTime.UtcNow)
        .RuleFor(e => e.ModifiedDate, () => DateTime.UtcNow)
        .RuleFor(e => e.CreatedBy, () => "SYSTEM")
        .RuleFor(e => e.ModifiedBy, () => "SYSTEM");

    private static Faker<JobApplicationComment> commentFaker = new Faker<JobApplicationComment>()
        .RuleFor(c => c.Id, () => 0)
        .RuleFor(c => c.AppUserId, (f) => f.Random.Int(1, 5))
        .RuleFor(c => c.Comment, (f) => f.Lorem.Paragraph())
        .RuleFor(c => c.CreatedDate, (f) => f.Date.Recent(5))
        .RuleFor(c => c.ModifiedDate, () => DateTime.UtcNow)
        .RuleFor(c => c.CreatedBy, () => "SYSTEM")
        .RuleFor(c => c.ModifiedBy, () => "SYSTEM");

    private static readonly Faker FakerInstance = new();

    public void PopulateJobApplicationEvents(JobApplication jobApp)
    {
        var disqualifiedStep =
            FakerInstance.Random.Enum(JobApplicationStatus.Applied, JobApplicationStatus.Disqualified);

        var status = jobApp.ApplicationStatus == JobApplicationStatus.Disqualified
            ? disqualifiedStep
            : jobApp.ApplicationStatus;

        var appEvent = eventFaker.Generate();
        appEvent.JobApplicationId = jobApp.Id;

        var now = DateTime.UtcNow;
        var baseDate = (new DateTime(now.Year, now.Month, now.Day)) - TimeSpan.FromDays(3);
        var eventDate = baseDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));

        if (status >= JobApplicationStatus.Offer)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.Offer.ToDescription();
            appEvent.EventDate = eventDate;
            appEvent.Status = JobApplicationStatus.Offer;
            Db.Insert(appEvent);
            var offer = jobOfferFaker.Generate();
            offer.JobApplicationId = jobApp.Id;
            offer.SalaryOffer = FakerInstance.Random.Int(jobApp.Position.SalaryRangeLower, jobApp.Position.SalaryRangeUpper);
            offer.AppUserId = FakerInstance.Random.Int(1, 5);
            Db.Insert(offer);
        }

        if (status >= JobApplicationStatus.InterviewCompleted)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.InterviewCompleted.ToDescription();
            appEvent.EventDate = eventDate;
            appEvent.Status = JobApplicationStatus.InterviewCompleted;
            Db.Insert(appEvent);
            var interview = interviewFaker.Generate();
            interview.JobApplicationId = jobApp.Id;
            interview.BookingTime = eventDate;
            interview.AppUserId = FakerInstance.Random.Int(1, 5);
            Db.Insert(interview);
        }

        if (status >= JobApplicationStatus.Interview)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.Interview.ToDescription();
            appEvent.Status = JobApplicationStatus.Interview;
            appEvent.EventDate = eventDate;
            Db.Insert(appEvent);
            if (status == JobApplicationStatus.Interview)
            {
                var interview = interviewFaker.Generate();
                interview.JobApplicationId = jobApp.Id;
                interview.BookingTime = eventDate;
                interview.AppUserId = FakerInstance.Random.Int(1, 5);
                Db.Insert(interview);
            }
        }

        if (status >= JobApplicationStatus.PhoneScreeningCompleted)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.PhoneScreeningCompleted.ToDescription();
            appEvent.EventDate = eventDate;
            appEvent.Status = JobApplicationStatus.PhoneScreeningCompleted;
            Db.Insert(appEvent);
            var screen = phoneScreenFaker.Generate();
            screen.JobApplicationId = jobApp.Id;
            screen.AppUserId = FakerInstance.Random.Int(1, 5);
            Db.Insert(screen);
        }

        if (status >= JobApplicationStatus.PhoneScreening)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.PhoneScreening.ToDescription();
            appEvent.Status = JobApplicationStatus.PhoneScreening;
            appEvent.EventDate = eventDate;
            Db.Insert(appEvent);
            if (status == JobApplicationStatus.PhoneScreening)
            {
                var screen = phoneScreenFaker.Generate();
                screen.JobApplicationId = jobApp.Id;
                screen.AppUserId = FakerInstance.Random.Int(1, 5);
                Db.Insert(screen);
            }
        }

        if (status >= JobApplicationStatus.Applied)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = JobApplicationStatus.Applied.ToDescription();
            appEvent.Status = JobApplicationStatus.Applied;
            appEvent.EventDate = eventDate;
            Db.Insert(appEvent);
        }

        var numOfComments = FakerInstance.Random.Int(1, 5);
        for (var i = 0; i < numOfComments; i++)
        {
            var comment = commentFaker.Generate();
            comment.JobApplicationId = jobApp.Id;
            Db.Insert(comment);
        }
    }
}

