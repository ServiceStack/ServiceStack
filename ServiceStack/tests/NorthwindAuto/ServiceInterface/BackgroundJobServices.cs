using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

public class BackgroundJobServices(IBackgroundJobs jobs) : Service
{
    public object Any(QueueCheckUrl request)
    {
        var options = new BackgroundJobOptions().PopulateWith(request);
        var jobRef = jobs.EnqueueCommand<CheckUrlsCommand>(new CheckUrls { Urls = [request.Url] }, options);
        return new QueueCheckUrlResponse
        {
            Id = jobRef.Id,
            RefId = jobRef.RefId,
        };
    }
}
