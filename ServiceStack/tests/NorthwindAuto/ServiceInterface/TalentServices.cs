using ServiceStack;
using ServiceStack.OrmLite;
using System;
using TalentBlazor.ServiceModel;

namespace TalentBlazor.ServiceInterface;

public class TalentServices : Service
{
    public IAutoQueryDb AutoQuery { get; set; }

    public void Any(StoreContacts request) {}
    
    public object Any(GetContacts request) => new GetContactsResponse
    {
        Results = Db.Select<Contact>()
    };

    JobApplicationEvent CreateEvent(JobApplicationStatus status)
    {
        var userId = this.GetSession().UserAuthId;
        return new JobApplicationEvent
        {
            ApplicationUserId = userId,
            Status = status,
            Description = status.ToDescription(),
            EventDate = DateTime.UtcNow,
        }.WithAudit(userId);
    }

    public object Post(CreatePhoneScreen request)
    {
        var jobApp = Db.SingleById<JobApplication>(request.JobApplicationId);
        jobApp.PhoneScreen = request.ConvertTo<PhoneScreen>().WithAudit(Request);
        jobApp.Events ??= new();
        if(jobApp.ApplicationStatus != request.ApplicationStatus)
            jobApp.Events.Add(CreateEvent(request.ApplicationStatus));
        jobApp.ApplicationStatus = request.ApplicationStatus;
        Db.Save(jobApp, references:true);
        return jobApp.PhoneScreen;
    }

    public object Patch(UpdatePhoneScreen request)
    {
        var jobApp = Db.LoadSingleById<JobApplication>(request.JobApplicationId);
        var jobAppStatus = request.ApplicationStatus ?? JobApplicationStatus.PhoneScreeningCompleted;
        jobApp.PhoneScreen.PopulateWithNonDefaultValues(request).WithAudit(Request);
        if (jobApp.ApplicationStatus != request.ApplicationStatus)
            jobApp.Events.Add(CreateEvent(jobAppStatus));
        jobApp.ApplicationStatus = jobAppStatus;
        Db.Save(jobApp, references:true);
        return jobApp.PhoneScreen;
    }

    public object Post(CreateInterview request)
    {
        var jobApp = Db.SingleById<JobApplication>(request.JobApplicationId);
        jobApp.Interview = request.ConvertTo<Interview>().WithAudit(Request);
        jobApp.Events ??= new();
        if (jobApp.ApplicationStatus != request.ApplicationStatus)
            jobApp.Events.Add(CreateEvent(request.ApplicationStatus));
        jobApp.ApplicationStatus = request.ApplicationStatus;
        Db.Save(jobApp, references: true);
        return jobApp.Interview;
    }

    public object Patch(UpdateInterview request)
    {
        var jobApp = Db.LoadSingleById<JobApplication>(request.JobApplicationId);
        var jobAppStatus = request.ApplicationStatus ?? JobApplicationStatus.InterviewCompleted;
        jobApp.Interview.PopulateWithNonDefaultValues(request).WithAudit(Request);
        if (jobApp.ApplicationStatus != request.ApplicationStatus)
            jobApp.Events.Add(CreateEvent(jobAppStatus));
        jobApp.ApplicationStatus = jobAppStatus;
        Db.Save(jobApp, references: true);
        return jobApp.Interview;
    }

    public object Post(CreateJobOffer request)
    {
        var jobApp = Db.SingleById<JobApplication>(request.JobApplicationId);
        jobApp.JobOffer = request.ConvertTo<JobOffer>().WithAudit(Request);
        jobApp.JobOffer.ApplicationUserId = GetSession().UserAuthId;
        jobApp.Events ??= new();
        if (jobApp.ApplicationStatus != request.ApplicationStatus)
            jobApp.Events.Add(CreateEvent(request.ApplicationStatus));
        jobApp.ApplicationStatus = request.ApplicationStatus;
        Db.Save(jobApp, references: true);
        return jobApp.JobOffer;
    }

    public object Get(TalentStats request)
    {
        var jobsCount = Db.Count<Job>();
        var contactCount = Db.Count<Contact>();
        var avgExpectedSalary = Db.Scalar<Contact, int>(x => Sql.Avg(x.SalaryExpectation));
        var avgJobSalaryRangeLower = Db.Scalar<Job, int>(x => Sql.Avg(x.SalaryRangeLower));
        var avgJobSalaryRangeUpper = Db.Scalar<Job, int>(x => Sql.Avg(x.SalaryRangeUpper));
        var preferredRemote = Db.Count<Contact>(x => x.PreferredLocation == "Remote");

        return new TalentStatsResponse
        {
            TotalJobs = jobsCount,
            TotalContacts = contactCount,
            AvgSalaryExpectation = avgExpectedSalary,
            AvgSalaryLower = avgJobSalaryRangeLower,
            AvgSalaryUpper = avgJobSalaryRangeUpper,
            PreferredRemotePercentage = Math.Round((decimal)((double)preferredRemote / (double)contactCount * 100),2)
        };
    }
}