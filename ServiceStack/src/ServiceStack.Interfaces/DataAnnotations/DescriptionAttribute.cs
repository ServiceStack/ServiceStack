// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Annotate any Type, Property or Enum with a textual description
    /// </summary>
    public class DescriptionAttribute : AttributeBase
    {
        public string Description { get; set; }

        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}