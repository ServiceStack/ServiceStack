using System.Data;
using Bogus;
using ServiceStack;
using ServiceStack.OrmLite;
using TalentBlazor.ServiceModel;

namespace TalentBlazor;

public static class ConfigureDbTalent
{
    /// <summary>
    /// Create non-existing Table and add Seed Data Example
    /// </summary>
    public static void SeedTalent(this IDbConnection db, string profilesDir)
    {
        var seedData = db.CreateTableIfNotExists<Contact>();
        db.CreateTableIfNotExists<Job>();
        db.CreateTableIfNotExists<JobApplication>();
        db.CreateTableIfNotExists<JobApplicationEvent>();
        db.CreateTableIfNotExists<PhoneScreen>();
        db.CreateTableIfNotExists<Interview>();
        db.CreateTableIfNotExists<JobApplicationAttachment>();
        db.CreateTableIfNotExists<JobApplicationComment>();

        if (seedData)
        {
            string[] profilePhotos = Directory.GetFiles(profilesDir);

            var now = DateTime.UtcNow;
            var jobFaker = new Faker<Job>()
                //.CustomInstantiator(f => new Job())
                .RuleFor(j => j.Description, (faker, job1) => faker.Lorem.Paragraphs(3))
                .RuleFor(j => j.Title, (faker, job1) => faker.Name.JobTitle())
                .RuleFor(j => j.Company, (faker, job1) => faker.Company.CompanyName())
                .RuleFor(j => j.SalaryRangeLower, (faker, job1) => faker.Random.Int(90, 120) * 1000)
                .RuleFor(j => j.SalaryRangeUpper, (faker, job1) => faker.Random.Int(125, 250) * 1000)
                .RuleFor(j => j.EmploymentType, (faker, job1) => faker.Random.Enum<EmploymentType>())
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
                //.CustomInstantiator(f => new Contact())
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
                .RuleFor(c => c.PreferredWorkType, (faker, contact1) => faker.Random.Enum<EmploymentType>())
                .RuleFor(c => c.SalaryExpectation, (faker, contact1) => faker.Random.Int(92, 245) * 1000)
                .RuleFor(c => c.CreatedDate, () => now)
                .RuleFor(c => c.ModifiedDate, () => now)
                .RuleFor(c => c.CreatedBy, () => "SYSTEM")
                .RuleFor(c => c.ModifiedBy, () => "SYSTEM");

            var jobAppFaker = new Faker<JobApplication>()
                //.CustomInstantiator(f => new JobApplication())
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

                contact.Id = (int)db.Insert(contact, selectIdentity: true);
                job.Id = (int)db.Insert(job, selectIdentity: true);

                contacts.Add(contact);
                jobs.Add(job);
            }

            foreach (var contact in contacts)
            {
                var faker = new Faker();
                var uniqueJobIndexes = Enumerable.Range(0, jobs.Count - 1)
                    .OrderBy(x => faker.Random.Int()).Take(8);
                var lastIndex = 0;
                foreach (var index in uniqueJobIndexes)
                {
                    var job = jobs[index];
                    var jobApplication = jobAppFaker.Generate();
                    jobApplication.JobId = job.Id;
                    jobApplication.ContactId = contact.Id;
                    jobApplication.Id = 0;

                    jobApplication.Id = (int)db.Insert(jobApplication, selectIdentity: true);
                    jobApplication.Applicant = contact;
                    jobApplication.Position = job;

                    db.PopulateJobApplicationEvents(jobApplication);
                }
            }
        }
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

    public static void PopulateJobApplicationEvents(this IDbConnection db, JobApplication jobApp)
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

        // TODO generate attachments here

        if (status >= JobApplicationStatus.InterviewCompleted)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = "Completed interview";
            appEvent.EventDate = eventDate;
            appEvent.Status = JobApplicationStatus.InterviewCompleted;
            db.Insert(appEvent);
            var interview = interviewFaker.Generate();
            interview.JobApplicationId = jobApp.Id;
            interview.BookingTime = eventDate;
            interview.AppUserId = FakerInstance.Random.Int(1, 5);
            db.Insert(interview);
        }

        if (status >= JobApplicationStatus.Interview)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = "Advanced to interview";
            appEvent.Status = JobApplicationStatus.Interview;
            appEvent.EventDate = eventDate;
            db.Insert(appEvent);
            if(status == JobApplicationStatus.Interview)
            {
                var interview = interviewFaker.Generate();
                interview.JobApplicationId = jobApp.Id;
                interview.BookingTime = eventDate;
                interview.AppUserId = FakerInstance.Random.Int(1, 5);
                db.Insert(interview);
            }
        }

        if (status >= JobApplicationStatus.PhoneScreeningCompleted)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = "Completed phone screening";
            appEvent.EventDate = eventDate;
            appEvent.Status = JobApplicationStatus.PhoneScreeningCompleted;
            db.Insert(appEvent);
            var screen = phoneScreenFaker.Generate();
            screen.JobApplicationId = jobApp.Id;
            screen.AppUserId = FakerInstance.Random.Int(1, 5);
            db.Insert(screen);
        }

        if (status >= JobApplicationStatus.PhoneScreening)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = "Advanced to phone screening";
            appEvent.Status = JobApplicationStatus.PhoneScreening;
            appEvent.EventDate = eventDate;
            db.Insert(appEvent);
            if(status == JobApplicationStatus.PhoneScreening)
            {
                var screen = phoneScreenFaker.Generate();
                screen.JobApplicationId = jobApp.Id;
                screen.AppUserId = FakerInstance.Random.Int(1, 5);
                db.Insert(screen);
            }
        }

        if (status >= JobApplicationStatus.Applied)
        {
            eventDate = eventDate - TimeSpan.FromDays(FakerInstance.Random.Int(1, 3));
            appEvent.AppUserId = FakerInstance.Random.Int(1, 5);
            appEvent.Description = "Applied";
            appEvent.Status = JobApplicationStatus.Applied;
            appEvent.EventDate = eventDate;
            db.Insert(appEvent);
        }

        var numOfComments = FakerInstance.Random.Int(1, 5);
        for (var i = 0;  i < numOfComments; i++)
        {
            var comment = commentFaker.Generate();
            comment.JobApplicationId = jobApp.Id;
            db.Insert(comment);
        }
    }


}