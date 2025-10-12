namespace ServiceStack.Aws.Sqs
{
    public class SqsQueueDefinition
    {
        public const int MaxWaitTimeSeconds = 20;
        public const int DefaultWaitTimeSeconds = MaxWaitTimeSeconds;
        public const int MaxVisibilityTimeoutSeconds = 43200;
        public const int DefaultVisibilityTimeoutSeconds = 300;

        public const int MaxBatchReceiveItems = 10;
        public const int MaxBatchDeleteItems = 10;
        public const int MaxBatchSendItems = 10;
        public const int MaxBatchCvItems = 10;
        public const int DefaultTempQueueRetentionSeconds = 60 * 60 * 6;
        public const int DefaultPermanentQueueRetentionSeconds = 60 * 60 * 24 * 5;

        private static readonly char[] validNonAlphaNumericChars = ['-', '_'];

        public SqsQueueDefinition()
        {
            SendBufferSize = MaxBatchSendItems;
            ReceiveBufferSize = MaxBatchReceiveItems;
            DeleteBufferSize = MaxBatchDeleteItems;
            ChangeVisibilityBufferSize = MaxBatchCvItems;
            ReceiveWaitTime = DefaultWaitTimeSeconds;
            VisibilityTimeout = DefaultVisibilityTimeoutSeconds;
        }

        public string QueueName => SqsQueueName.QueueName;

        public string AwsQueueName => SqsQueueName.AwsQueueName;

        public SqsQueueName SqsQueueName { get; set; }

        public string QueueUrl { get; set; }
        public int VisibilityTimeout { get; set; }
        public int ReceiveWaitTime { get; set; }
        public long CreatedTimestamp { get; set; }
        public SqsRedrivePolicy RedrivePolicy { get; set; }
        public bool DisableBuffering { get; set; }

        private int sendBufferSize = 1;
        public int SendBufferSize
        {
            get => sendBufferSize;
            set => sendBufferSize = value > 0
                ? value
                : 1;
        }

        private int receiveBufferSize = 1;
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => receiveBufferSize = value > 0
                ? value
                : 1;
        }

        private int deleteBufferSize = 1;
        public int DeleteBufferSize
        {
            get => deleteBufferSize;
            set => deleteBufferSize = value > 0
                ? value
                : 1;
        }

        private int cvBufferSize = 1;
        public int ChangeVisibilityBufferSize
        {
            get => cvBufferSize;
            set => cvBufferSize = value > 0
                ? value
                : 1;
        }

        public long ApproximateNumberOfMessages { get; set; }
        public string QueueArn { get; set; }

        public static char[] ValidNonAlphaNumericChars => validNonAlphaNumericChars;

        public static int GetValidQueueWaitTime(int value)
        {
            return value is >= 0 and <= MaxWaitTimeSeconds
                ? value
                : value < 0
                        ? 0
                        : 20;
        }

        public static int GetValidVisibilityTimeout(int value)
        {
            return value is >= 0 and <= MaxVisibilityTimeoutSeconds
                ? value
                : value < 0
                        ? 0
                        : 43200;
        }
    }
}
