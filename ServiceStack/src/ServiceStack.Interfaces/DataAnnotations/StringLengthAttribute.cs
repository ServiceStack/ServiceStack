// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


namespace ServiceStack.DataAnnotations
{
    public class StringLengthAttribute : AttributeBase
    {
        public const int MaxText = int.MaxValue;
        public int MinimumLength { get; set; }
        public int MaximumLength { get; set; }

        public StringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        public StringLengthAttribute(int minimumLength, int maximumLength)
        {
            MinimumLength = minimumLength;
            MaximumLength = maximumLength;
        }
    }
}