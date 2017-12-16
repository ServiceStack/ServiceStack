using System;
using ServiceStack.Configuration;

namespace ServiceStack.Logging.Slack
{
    public class SlackLogFactory : ILogFactory
    {
        public bool DebugEnabled { get; set; }
        private readonly string incomingWebHookUrl;

        /// <summary>
        /// Slack Incoming Webhook URL.
        /// </summary>
        public string Url => incomingWebHookUrl;

        /// <summary>
        /// Default channel override.
        /// This will override the channel name specified when the Incoming Webhook was created.
        /// </summary>
        public string DefaultChannel { get; set; }
        
        /// <summary>
        /// Icon emoji override for the bot. Eg, ":ghost:".
        /// </summary>
        public string IconEmoji { get; set; }

        /// <summary>
        /// Channel override specifically for logging errors.
        /// Falls back to <see cref="DefaultChannel"/> if not specified.
        /// If ErrorChannel and DefaultChannel null, falls back to Slack configued default channel.
        /// </summary>
        public string ErrorChannel { get; set; }

        /// <summary>
        /// Channel override specifically for debug logging.
        /// Debug logging is only posted in <see cref="DebugEnabled" /> is true.
        /// Falls back to <see cref="DefaultChannel"/> if not specified.
        /// If DebugChannel and DefaultChannel null, falls back to Slack configued default channel.
        /// </summary>
        public string DebugChannel { get; set; }

        /// <summary>
        /// Channel override specifically for info logging.
        /// Falls back to <see cref="DefaultChannel"/> if not specified.
        /// If InfoChannel and DefaultChannel null, falls back to Slack configued default channel.
        /// </summary>
        public string InfoChannel { get; set; }

        /// <summary>
        /// Channel override specifically for fatal error logging.
        /// Falls back to <see cref="DefaultChannel"/> if not specified.
        /// If FatalChannel and DefaultChannel null, falls back to Slack configued default channel.
        /// </summary>
        public string FatalChannel { get; set; }

        /// <summary>
        /// Channel override specifically for fatal warning logging.
        /// Falls back to <see cref="DefaultChannel"/> if not specified.
        /// If WarnChannel and DefaultChannel null, falls back to Slack configued default channel.
        /// </summary>
        public string WarnChannel { get; set; }

        /// <summary>
        /// Bot username override. 
        /// Falls back to Slack configured default if not specified.
        /// </summary>
        public string BotUsername { get; set; }

        public string ChannelPrefix { get; set; }

        private const string ConfigKeyFmt = "ServiceStack.Logging.Slack.{0}";

        /// <summary>
        /// Initializes a new instance of the <see cref="SlackLogFactory"/> class.
        /// </summary>
        /// <param name="incomingWebHookUrl">Private URL create by Slack when setting up an Incoming WebHook integration.</param>
        /// <param name="debugEnabled">By default, Debug logs are not posted to Slack.</param>
        public SlackLogFactory(string incomingWebHookUrl, bool debugEnabled = false)
        {
            DebugEnabled = debugEnabled;
            this.incomingWebHookUrl = incomingWebHookUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlackLogFactory"/> class.
        /// Configuration from IAppSettings looking at keys with prefix "ServiceStack.Logging.Slack.".
        /// Eg, &lt;add key="ServiceStack.Logging.Slack.DefaultChannel" value="LoggingChannel" /&gt;
        /// </summary>
        /// <param name="incomingWebHookUrl"></param>
        /// <param name="appSettings">Further config provided by an IAppSettings instance.</param>
        public SlackLogFactory(string incomingWebHookUrl, IAppSettings appSettings)
            : this(appSettings)
        {
            this.incomingWebHookUrl = incomingWebHookUrl;
        }

        /// <summary>
        /// Configuration from IAppSettings looking at keys with prefix "ServiceStack.Logging.Slack.".
        /// Eg, &lt;add key="ServiceStack.Logging.Slack.Url" value="{SlackIncomingWebhookUrl}" /&gt;
        /// </summary>
        /// <param name="appSettings"></param>
        public SlackLogFactory(IAppSettings appSettings)
        {
            if (appSettings == null)
                throw new ArgumentNullException(nameof(appSettings));

            if(incomingWebHookUrl == null)
                incomingWebHookUrl = appSettings.GetString(ConfigKeyFmt.Fmt("Url"));

            DebugEnabled = appSettings.Get<bool>(ConfigKeyFmt.Fmt("DebugEnabled"));
            DefaultChannel = appSettings.GetString(ConfigKeyFmt.Fmt("DefaultChannel"));
            IconEmoji = appSettings.GetString(ConfigKeyFmt.Fmt("IconEmoji"));
            ErrorChannel = appSettings.GetString(ConfigKeyFmt.Fmt("ErrorChannel"));
            DebugChannel = appSettings.GetString(ConfigKeyFmt.Fmt("DebugChannel"));
            InfoChannel = appSettings.GetString(ConfigKeyFmt.Fmt("InfoChannel"));
            FatalChannel = appSettings.GetString(ConfigKeyFmt.Fmt("FatalChannel"));
            WarnChannel = appSettings.GetString(ConfigKeyFmt.Fmt("WarnChannel"));
            BotUsername = appSettings.GetString(ConfigKeyFmt.Fmt("BotUsername"));
            ChannelPrefix = appSettings.GetString(ConfigKeyFmt.Fmt("ChannelPrefix"));
        }

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.ToString());
        }

        public ILog GetLogger(string typeName)
        {
            return new SlackLog(incomingWebHookUrl, DebugEnabled)
            {
                DebugChannel = DebugChannel,
                DefaultChannel = DefaultChannel,
                IconEmoji = IconEmoji,
                InfoChannel = InfoChannel,
                FatalChannel = FatalChannel,
                WarnChannel = WarnChannel,
                ErrorChannel = ErrorChannel,
                BotUsername = BotUsername,
                ChannelPrefix = ChannelPrefix
            };
        }
    }
}
