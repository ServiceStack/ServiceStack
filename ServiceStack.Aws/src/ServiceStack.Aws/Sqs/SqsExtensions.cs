using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;
using ServiceStack.Text;
using Message = Amazon.SQS.Model.Message;

namespace ServiceStack.Aws.Sqs
{
    public static class SqsExtensions
    {
        public static Exception ToException(this BatchResultErrorEntry entry, [CallerMemberName] string methodOperation = null)
        {
            return entry == null
                ? null
                : new Exception(
                    $"Batch Entry exception for operation [{methodOperation}]. Id [{entry.Id}], Code [{entry.Code}], Is Sender Fault [{entry.SenderFault}]. Message [{entry.Message}].");
        }

        public static string ToValidQueueName(this string queueName)
        {
            if (IsValidQueueName(queueName))
                return queueName;

            var validQueueName = Regex.Replace(queueName, @"([^\d\w-_])", "-");
            return validQueueName;
        }

        public static bool IsValidQueueName(this string queueName)
        {
            return queueName.All(c => char.IsLetterOrDigit(c) ||
                SqsQueueDefinition.ValidNonAlphaNumericChars.Contains(c));
        }

        public static SetQueueAttributesRequest ToSetAttributesRequest(this CreateQueueRequest request, string queueUrl)
        {
            return new SetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                Attributes = request.Attributes
            };
        }

        public static SqsQueueDefinition ToQueueDefinition(this Dictionary<string, string> attributes, SqsQueueName queueName,
                                                           string queueUrl, bool disableBuffering)
        {
            var attrToUse = attributes ?? new Dictionary<string, string>();

            Guard.AgainstNullArgument(queueName, "queueName");
            Guard.AgainstNullArgument(queueUrl, "queueUrl");

            return new SqsQueueDefinition
            {
                SqsQueueName = queueName,
                QueueUrl = queueUrl,
                VisibilityTimeout = attrToUse.ContainsKey(QueueAttributeName.VisibilityTimeout)
                    ? attrToUse[QueueAttributeName.VisibilityTimeout].ToInt(SqsQueueDefinition.DefaultVisibilityTimeoutSeconds)
                    : SqsQueueDefinition.DefaultVisibilityTimeoutSeconds,
                ReceiveWaitTime = attrToUse.ContainsKey(QueueAttributeName.ReceiveMessageWaitTimeSeconds)
                    ? attrToUse[QueueAttributeName.ReceiveMessageWaitTimeSeconds].ToInt(SqsQueueDefinition.DefaultWaitTimeSeconds)
                    : SqsQueueDefinition.DefaultWaitTimeSeconds,
                CreatedTimestamp = attrToUse.ContainsKey(QueueAttributeName.CreatedTimestamp)
                    ? attrToUse[QueueAttributeName.CreatedTimestamp].ToInt64(DateTime.UtcNow.ToUnixTime())
                    : DateTime.UtcNow.ToUnixTime(),
                DisableBuffering = disableBuffering,
                ApproximateNumberOfMessages = attrToUse.ContainsKey(QueueAttributeName.ApproximateNumberOfMessages)
                    ? attrToUse[QueueAttributeName.ApproximateNumberOfMessages].ToInt64(0)
                    : 0,
                QueueArn = attrToUse.ContainsKey(QueueAttributeName.QueueArn)
                    ? attrToUse[QueueAttributeName.QueueArn]
                    : null,
                RedrivePolicy = attrToUse.ContainsKey(QueueAttributeName.RedrivePolicy)
                    ? attrToUse[QueueAttributeName.RedrivePolicy].FromJson<SqsRedrivePolicy>()
                    : null
            };
        }

        public static Message<T> FromSqsMessage<T>(this Message sqsMessage, string queueName)
        {
            if (sqsMessage == null)
                return null;

            Guard.AgainstNullArgument(queueName, "queueName");

            var body = sqsMessage.Body.FromJson<T>();
            var message = new Message<T>(body)
            {
                Tag = SqsMessageTag.CreateTag(queueName, sqsMessage.ReceiptHandle)
            };

            if (sqsMessage.MessageAttributes != null)
            {
                foreach (var entry in sqsMessage.MessageAttributes)
                {
                    if (string.IsNullOrEmpty(entry.Value?.StringValue))
                        continue;

                    var strValue = entry.Value.StringValue;

                    switch (entry.Key)
                    {
                        case "CreatedDate":
                            message.CreatedDate = strValue.FromJson<DateTime>();
                            break;
                        case "Options":
                            message.Options = int.Parse(strValue);
                            break;
                        case "Priority":
                            message.Priority = long.Parse(strValue);
                            break;
                        case "RetryAttempts":
                            message.RetryAttempts = int.Parse(strValue);
                            break;
                        case "Error":
                            message.Error = strValue.FromJson<ResponseStatus>();
                            break;
                        case "ReplyId":
                            message.ReplyId = strValue.FromJson<Guid>();
                            break;
                        case "ReplyTo":
                            message.ReplyTo = strValue;
                            break;
                        case "Meta":
                            message.Meta = strValue.FromJson<Dictionary<string, string>>();
                            break;
                    }
                }
            }

            return message;
        }

        public static SendMessageRequest ToSqsSendMessageRequest(this IMessage message, SqsQueueDefinition queueDefinition)
        {
            var to = new SendMessageRequest
            {
                QueueUrl = queueDefinition.QueueUrl,
                MessageBody = message.Body.ToJson(),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "CreatedDate", message.CreatedDate.AsMessageAttributeValue() },
                    { "Options", message.Options.AsMessageAttributeValue() },
                    { "Priority", message.Priority.AsMessageAttributeValue() },
                    { "RetryAttempts", message.RetryAttempts.AsMessageAttributeValue() },
                }
            };

            if (message.Tag != null)
                to.MessageAttributes["Tag"] = message.Tag.AsMessageAttributeValue();
            if (message.Error != null)
                to.MessageAttributes["Error"] = message.Error.AsMessageAttributeValue();
            if (message.ReplyId != null)
                to.MessageAttributes["ReplyId"] = message.ReplyId.AsMessageAttributeValue();
            if (message.ReplyTo != null)
                to.MessageAttributes["ReplyTo"] = message.ReplyTo.AsMessageAttributeValue();
            if (message.Meta != null)
                to.MessageAttributes["Meta"] = message.Meta.AsMessageAttributeValue();

            return to;
        }

        internal static MessageAttributeValue AsMessageAttributeValue<T>(this T value)
        {
            return new MessageAttributeValue
            {
                DataType = "String",
                StringValue = value.ToJson()
            };
        }

        internal static MessageAttributeValue AsMessageAttributeValue(this string strValue)
        {
            return new MessageAttributeValue
            {
                DataType = "String",
                StringValue = strValue
            };
        }

        internal static MessageAttributeValue AsMessageAttributeValue(this int numValue)
        {
            return new MessageAttributeValue
            {
                DataType = "Number",
                StringValue = numValue.ToString(CultureInfo.InvariantCulture),
            };
        }

        internal static MessageAttributeValue AsMessageAttributeValue(this long numValue)
        {
            return new MessageAttributeValue
            {
                DataType = "Number",
                StringValue = numValue.ToString(CultureInfo.InvariantCulture),
            };
        }

    }
}