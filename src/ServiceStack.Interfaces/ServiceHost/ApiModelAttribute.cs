using System;

namespace ServiceStack.ServiceHost
{
    public class ApiModelAttribute : Attribute
    {
        public ApiModelAttribute()
        {
        }

        public ApiModelAttribute(bool isRequired)
        {
            this.IsRequired = isRequired;
        }

        public ApiModelAttribute(string description)
        {
            Description = description;
        }

        public ApiModelAttribute(bool isRequired, string description)
        {
            this.IsRequired = isRequired;
            Description = description;
        }

        public bool OverrideRequired()
        {
            return overrideRequired;
        }

        // overrideRequired convention since we can't use nullable properties on attributes
        public bool IsRequired
        {
            get
            {
                return isRequired;
            }

            set
            {
                overrideRequired = true;
                isRequired = value;
            }
        }

        public string Description { get; set; }

        private bool isRequired;
        private bool overrideRequired;
    }
}
