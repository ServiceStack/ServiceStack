using ServiceStack.Messaging;
using MyApp.ServiceInterface;
using MyApp.ServiceModel;
using Microsoft.AspNetCore.Identity;
using MyApp.Data;

[assembly: HostingStartup(typeof(MyApp.ConfigureMq))]

namespace MyApp;

/**
 * Register ServiceStack Services you want to be able to invoke in a managed Background Thread
 * https://docs.servicestack.net/background-mq
*/
public class ConfigureMq : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            var smtpConfig = context.Configuration.GetSection(nameof(SmtpConfig))?.Get<SmtpConfig>();
            if (smtpConfig is not null)
            {
                services.AddSingleton(smtpConfig);
            }
            services.AddSingleton<IMessageService>(c => new BackgroundMqService());
        })
        .ConfigureAppHost(afterAppHostInit: appHost => {
            appHost.Resolve<IMessageService>().Start();
            var mqService = appHost.Resolve<IMessageService>();

            //Register ServiceStack APIs you want to be able to invoke via MQ
            mqService.RegisterHandler<SendEmail>(appHost.ExecuteMessage);
        });
}

/// <summary>
/// Sends emails by publishing a message to the Background MQ Server where it's processed in the background
/// </summary>
public class EmailSender(IMessageService messageService) : IEmailSender<ApplicationUser>
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var mqClient = messageService.CreateMessageProducer();
        mqClient.Publish(new SendEmail
        {
            To = email,
            Subject = subject,
            BodyHtml = htmlMessage,
        });

        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}
