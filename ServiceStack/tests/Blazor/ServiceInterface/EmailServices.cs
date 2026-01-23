using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

/// <summary>
/// Configuration for sending emails using SMTP servers in EmailServices
/// E.g. for managed services like Amazon (SES): https://aws.amazon.com/ses/ or https://mailtrap.io
/// </summary>
public class SmtpConfig
{
    /// <summary>
    /// Username of the SMTP Server account
    /// </summary>
    public string Username { get; set; }
    /// <summary>
    /// Password of the SMTP Server account
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Hostname of the SMTP Server
    /// </summary>
    public string Host { get; set; }
    /// <summary>
    /// Port of the SMTP Server
    /// </summary>
    public int Port { get; set; } = 587;
    /// <summary>
    /// Which email address to send emails from
    /// </summary>
    public string FromEmail { get; set; }
    /// <summary>
    /// The name of the Email Sender
    /// </summary>
    public string? FromName { get; set; }
    /// <summary>
    /// Prevent emails from being sent to real users during development by sending to this Dev email instead
    /// </summary>
    public string? DevToEmail { get; set; }
    /// <summary>
    /// Keep a copy of all emails sent by BCC'ing a copy to this email address
    /// </summary>
    public string? Bcc { get; set; }
}

public class SendEmail
{
    public string To { get; set; }
    public string? ToName { get; set; }
    public string Subject { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
}

[Worker("smtp")]
public class SendEmailCommand(ILogger<SendEmailCommand> logger, IBackgroundJobs jobs, SmtpConfig config) 
    : SyncCommand<SendEmail>
{
    private static long count = 0;
    protected override void Run(SendEmail request)
    {
        Interlocked.Increment(ref count);
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Sending {Count} email to {Email} with subject {Subject}", 
            count, request.To, request.Subject);

        using var client = new SmtpClient(config.Host, config.Port);
        client.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
        client.EnableSsl = true;

        // If DevToEmail is set, send all emails to that address instead
        var emailTo = config.DevToEmail != null
            ? new MailAddress(config.DevToEmail)
            : new MailAddress(request.To, request.ToName);

        var emailFrom = new MailAddress(config.FromEmail, config.FromName);

        var msg = new MailMessage(emailFrom, emailTo)
        {
            Subject = request.Subject,
            Body = request.BodyHtml ?? request.BodyText,
            IsBodyHtml = request.BodyHtml != null,
        };

        if (config.Bcc != null)
        {
            msg.Bcc.Add(new MailAddress(config.Bcc));
        }

        client.Send(msg);
    }
}
