using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging.Slack;

namespace ServiceStack.Logging.Tests.UseCases
{
    [TestFixture]
    public class UseCaseSlack : UseCaseBase
    {
        public void SlackLogUseCase()
        {
            LogManager.LogFactory = new SlackLogFactory("{GeneratedSlackUrlFromCreatingIncomingWebhook}", debugEnabled:true)
            {
                //Alternate default channel than one specified when creating Incoming Webhook.
                DefaultChannel = "other-default-channel",
                //Custom channel for Fatal logs. Warn, Info etc will fallback to DefaultChannel or 
                //channel specified when Incoming Webhook was created.
                FatalChannel = "more-grog-logs",
                //Custom bot username other than default
                BotUsername = "Guybrush Threepwood",
                //Custom channel prefix can be provided to help filter logs from different users or environments. 
                ChannelPrefix = System.Security.Principal.WindowsIdentity.GetCurrent().Name
            };
            ILog log = LogManager.GetLogger(GetType());

            log.Debug("Start Logging...");
        }

        public void SlackFromAppConfig()
        {
            IAppSettings appSettings = null; // Get from ServiceStack core library.
            // AppSettings is loaded from App/Web.config files and can populate all of the settings for the SlackLogFactory
            // Keys prefix from app.config and web.config appSettings is "ServiceStack.Logging.Slack.{0}".
            LogManager.LogFactory = new SlackLogFactory(appSettings);
            ILog log = LogManager.GetLogger(GetType());

            log.Debug("Start Logging...");
        }
    }
}
