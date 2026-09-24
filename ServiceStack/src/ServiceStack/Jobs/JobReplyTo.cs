#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Jobs;

/// <summary>
/// The completed Job and its result being delivered to the Job's ReplyTo address
/// </summary>
public class JobReplyToContext(IBackgroundJobs jobs, BackgroundJobBase job, string replyTo, object? response)
{
    public IBackgroundJobs Jobs { get; } = jobs;
    public BackgroundJobBase Job { get; } = job;
    /// <summary>MQ Queue Name, or an http:// or https:// URL</summary>
    public string ReplyTo { get; } = replyTo;
    /// <summary>The Job's Response DTO, or null when the Job didn't return a result</summary>
    public object? Response { get; } = response;
    public CancellationToken Token { get; init; }
}

/// <summary>
/// Default delivery of a completed Job's result to its ReplyTo address.
/// Replaced by configuring BackgroundsJobFeature/DatabaseJobFeature OnJobReplyTo.
/// </summary>
public static class JobReplyTo
{
    /// <summary>
    /// Sends the Job's result to an http:// or https:// URL with an HTTP POST, otherwise treats
    /// ReplyTo as an MQ Queue Name and publishes the result to it.
    /// </summary>
    public static async Task SendAsync(JobReplyToContext ctx)
    {
        var replyTo = ctx.ReplyTo;
        if (IsUrl(replyTo))
        {
            await PostToUrlAsync(ctx).ConfigAwait();
        }
        else
        {
            PublishToMq(ctx);
        }
    }

    public static bool IsUrl(string replyTo) =>
        replyTo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        replyTo.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A ValidateReplyTo that only allows http:// and https:// ReplyTo URLs starting with one of
    /// these prefixes, e.g. "https://hooks.example.org/". MQ Queue Names are allowed unless
    /// allowMqQueues is false.
    /// </summary>
    public static Func<BackgroundJobBase, bool> AllowUrlPrefixes(string[] urlPrefixes, bool allowMqQueues = true)
    {
        var allowed = new List<Uri>();
        foreach (var prefix in urlPrefixes)
        {
            if (!Uri.TryCreate(prefix, UriKind.Absolute, out var uri) || !IsUrl(prefix))
                throw new ArgumentException($"'{prefix}' is not an absolute http:// or https:// URL", nameof(urlPrefixes));
            allowed.Add(uri);
        }
        return job => job.ReplyTo == null || (IsUrl(job.ReplyTo)
            ? IsAllowedUrl(job.ReplyTo, allowed)
            : allowMqQueues);
    }

    /// <summary>
    /// Compares the parsed scheme, host and port rather than the raw string, so an allowed prefix of
    /// https://hooks.example.org can't be satisfied by https://hooks.example.org.evil.com
    /// </summary>
    private static bool IsAllowedUrl(string url, List<Uri> allowed)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo))
            return false;
        foreach (var prefix in allowed)
        {
            if (string.Equals(uri.Scheme, prefix.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.Host, prefix.Host, StringComparison.OrdinalIgnoreCase)
                && uri.Port == prefix.Port
                && uri.AbsolutePath.StartsWith(prefix.AbsolutePath, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Headers sent with an HTTP ReplyTo, identifying which Job the result belongs to so the
    /// receiver can correlate it without having to parse the body.
    /// </summary>
    public static Dictionary<string, string> GetHeaders(JobReplyToContext ctx)
    {
        var to = new Dictionary<string, string> {
            ["X-Job-Id"] = ctx.Job.Id.ToString(),
            ["X-Job-State"] = ctx.Job.State.ToString(),
        };
        if (ctx.Job.RefId != null)
            to["X-Job-RefId"] = ctx.Job.RefId;
        if (ctx.Job.BatchId != null)
            to["X-Job-BatchId"] = ctx.Job.BatchId;
        if (ctx.Job.Tag != null)
            to["X-Job-Tag"] = ctx.Job.Tag;
        return to;
    }

    private static async Task PostToUrlAsync(JobReplyToContext ctx)
    {
        var headers = GetHeaders(ctx);
        // A Job with no result still notifies, so the receiver learns the Job finished
        var json = ctx.Response != null
            ? ClientConfig.ToJson(ctx.Response)
            : "{}";
        await ctx.ReplyTo.PostJsonToUrlAsync(json, requestFilter: req => {
            foreach (var (name, value) in headers)
            {
                req.Headers.TryAddWithoutValidation(name, value);
            }
        }).ConfigAwait();
    }

    private static void PublishToMq(JobReplyToContext ctx)
    {
        var msgFactory = HostContext.TryResolve<IMessageFactory>()
            ?? throw new NotSupportedException(
                $"No IMessageFactory is registered to publish Job {ctx.Job.Id} to ReplyTo '{ctx.ReplyTo}'. " +
                $"Register an MQ Server or configure OnJobReplyTo to handle it.");

        // The Response DTO is what a caller awaiting this Job wants, but a Job without one still
        // publishes the Request DTO so the receiver knows which work completed.
        var body = ctx.Response ?? ctx.Jobs.CreateRequest(ctx.Job);
        var message = MessageFactory.Create(body);
        message.ReplyTo = ctx.ReplyTo;
        message.Tag = ctx.Job.RefId;

        using var mqClient = msgFactory.CreateMessageQueueClient();
        mqClient.Publish(ctx.ReplyTo, message);
    }
}
#endif
