using System;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Logging.Slack
{
    public class SlackLog : ILogWithException
    {
        private readonly string incomingWebHookUrl;
        private readonly bool debugEnabled;

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
        /// Debug logging is only posted in <see cref="IsDebugEnabled" /> is true.
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

        private const string NewLine = "\n";

        public SlackLog(string incomingWebHookUrl, bool debugEnabled = false)
        {
            this.incomingWebHookUrl = incomingWebHookUrl;
            this.debugEnabled = debugEnabled;
            // Init from DefaultChannel
            ErrorChannel = DefaultChannel;
            DebugChannel = DefaultChannel;
            InfoChannel = DefaultChannel;
            FatalChannel = DefaultChannel;
            WarnChannel = DefaultChannel;
        }

        private void LogMessage(SlackLoggingData message)
        {
            using (JsConfig.With(propertyConvention: PropertyConvention.Lenient,
                emitLowercaseUnderscoreNames: true,
                emitCamelCaseNames: false))
            {
                incomingWebHookUrl.PostJsonToUrlAsync(message);
            }
        }

        private SlackLoggingData BuildMessage(string text, string channel = null)
        {
            string finalChannel;
            if (!string.IsNullOrEmpty(ChannelPrefix) && (channel != null || DefaultChannel != null))
                finalChannel = ChannelPrefix + (channel ?? DefaultChannel);
            else
                finalChannel = channel ?? DefaultChannel;

            return new SlackLoggingData
            {
                Channel = finalChannel,
                IconEmoji = IconEmoji,
                Text = text,
                Username = BotUsername
            };
        }

        private void Write(object message, Exception execption, string channel = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(message);
            if (execption != null)
                sb.Append(NewLine);
            while (execption != null)
            {
                sb.Append("Message: ").Append(execption.Message).Append(NewLine)
                    .Append("Source: ").Append(execption.Source).Append(NewLine)
                    .Append("Target site: ").Append(execption.TargetSite).Append(NewLine)
                    .Append("Stack trace: ").Append(execption.StackTrace).Append(NewLine);

                // Walk the InnerException tree
                execption = execption.InnerException;
            }

            var slackMessage = BuildMessage(sb.ToString(), channel);
            LogMessage(slackMessage);
        }

        public void Debug(object message)
        {
            if (!debugEnabled)
                return;
            Write(message, null, DebugChannel);
        }

        public void Debug(object message, Exception exception)
        {
            if (!debugEnabled)
                return;
            Write(message, exception, DebugChannel);
        }

        public void DebugFormat(string format, params object[] args)
        {
            if (!debugEnabled)
                return;
            Write(string.Format(format, args), null, DebugChannel);
        }

        public void Debug(Exception exception, string format, params object[] args)
        {
            if (!debugEnabled)
                return;
            Write(string.Format(format, args), exception, DebugChannel);
        }

        public void Error(object message)
        {
            Write(message, null, ErrorChannel);
        }

        public void Error(object message, Exception exception)
        {
            Write(message, exception, ErrorChannel);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Write(string.Format(format, args), null, ErrorChannel);
        }

        public void Error(Exception exception, string format, params object[] args)
        {
            Write(string.Format(format, args), exception, ErrorChannel);
        }

        public void Fatal(object message)
        {
            Write(message, null, FatalChannel);
        }

        public void Fatal(object message, Exception exception)
        {
            Write(message, exception, FatalChannel);
        }

        public void FatalFormat(string format, params object[] args)
        {
            Write(string.Format(format, args), null, FatalChannel);
        }

        public void Fatal(Exception exception, string format, params object[] args)
        {
            Write(string.Format(format, args), exception, FatalChannel);
        }

        public void Info(object message)
        {
            Write(message, null, InfoChannel);
        }

        public void Info(object message, Exception exception)
        {
            Write(message, exception, InfoChannel);
        }

        public void InfoFormat(string format, params object[] args)
        {
            Write(string.Format(format, args), null, InfoChannel);
        }

        public void Info(Exception exception, string format, params object[] args)
        {
            Write(string.Format(format, args), exception, InfoChannel);
        }

        public void Warn(object message)
        {
            Write(message, null, WarnChannel);
        }

        public void Warn(object message, Exception exception)
        {
            Write(message, exception, WarnChannel);
        }

        public void WarnFormat(string format, params object[] args)
        {
            Write(string.Format(format, args), null, WarnChannel);
        }

        public void Warn(Exception exception, string format, params object[] args)
        {
            Write(string.Format(format, args), exception, WarnChannel);
        }

        public bool IsDebugEnabled => debugEnabled;
    }

    class SlackLoggingData
    {
        public string Channel { get; set; }
        public string Text { get; set; }
        public string Username { get; set; }
        public string IconEmoji { get; set; }
    }
}