using System;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class AuthenticateAttribute : Attribute
	{
		public string Provider { get; set; }
        public ApplyTo ApplyTo { get; set; }

        public AuthenticateAttribute()
            : this(ApplyTo.All)
        {
        }

		public AuthenticateAttribute(string provider)
            : this(ApplyTo.All, provider)
		{
		}

        public AuthenticateAttribute(ApplyTo applyTo)
        {
            this.ApplyTo = applyTo;
        }

        public AuthenticateAttribute(ApplyTo applyTo, string provider)
        {
            this.Provider = provider;
            this.ApplyTo = applyTo;
        }
	}
}