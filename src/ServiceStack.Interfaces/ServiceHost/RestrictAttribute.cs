using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Decorate on Request DTO's to alter the accessibility of a service and its visibility on /metadata pages
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class RestrictAttribute
		: Attribute
	{
        /// <summary>
        /// Sets a single access restriction
        /// </summary>
        /// <value>Restrict Access to.</value>
        public EndpointAttributes AccessTo
        {
            get
            {
                return this.AccessibleToAny.Length == 0
                    ? EndpointAttributes.Any
                    : this.AccessibleToAny[0];
            }
            set
            {
                this.AccessibleToAny = new[] { value };
            }
        }

        /// <summary>
        /// Restrict access to any of the specified access scenarios
        /// </summary>
        /// <value>Access restrictions</value>
        public EndpointAttributes[] AccessibleToAny { get; private set; }

        /// <summary>
        /// Sets a single metadata Visibility restriction
        /// </summary>
        /// <value>Restrict metadata Visibility to.</value>
        public EndpointAttributes VisibilityTo
        {
            get
            {
                return this.VisibleToAny.Length == 0
                    ? EndpointAttributes.Any
                    : this.VisibleToAny[0];
            }
            set
            {
                this.VisibleToAny = new[] { value };
            }
        }

        /// <summary>
        /// Restrict metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>Visibility restrictions</value>
        public EndpointAttributes[] VisibleToAny { get; private set; }

        /// <summary>
        /// Restrict access and metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>The restrict access to scenarios.</value>
        public RestrictAttribute(params EndpointAttributes[] restrictAccessAndVisibilityToScenarios)
        {
            this.AccessibleToAny = ToAllowedFlagsSet(restrictAccessAndVisibilityToScenarios);
            this.VisibleToAny = ToAllowedFlagsSet(restrictAccessAndVisibilityToScenarios);
        }

        /// <summary>
        /// Restrict access and metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>The restrict access to scenarios.</value>
        public RestrictAttribute(EndpointAttributes[] allowedAccessScenarios, EndpointAttributes[] visibleToScenarios)
            : this()
        {
            this.AccessibleToAny = ToAllowedFlagsSet(allowedAccessScenarios);
            this.VisibleToAny = ToAllowedFlagsSet(visibleToScenarios);
        }

        /// <summary>
        /// Returns the allowed set of scenarios based on the user-specified restrictions
        /// </summary>
        /// <param name="restrictToAny"></param>
        /// <returns></returns>
        private static EndpointAttributes[] ToAllowedFlagsSet(EndpointAttributes[] restrictToAny)
	    {
	        if (restrictToAny.Length == 0)
                return new[] { EndpointAttributes.Any };

	        var scenarios = new List<EndpointAttributes>();
	        foreach (var restrictToScenario in restrictToAny)
	        {
	            var restrictTo = ToAllowedFlagsSet(restrictToScenario);

	            scenarios.Add(restrictTo);
	        }

            return scenarios.ToArray();
	    }

        /// <summary>
        /// Converts from a User intended restriction to a flag with all the allowed attribute flags set, e.g:
        /// 
        /// If No Network restrictions were specified all Network access types are allowed, e.g:
        ///     restrict EndpointAttributes.None => ... 111
        /// 
        /// If a Network restriction was specified, only it will be allowed, e.g:
        ///     restrict EndpointAttributes.LocalSubnet => ... 010
        /// 
        /// The returned Enum will have a flag with all the allowed attributes set
        /// </summary>
        /// <param name="restrictTo"></param>
        /// <returns></returns>
	    public static EndpointAttributes ToAllowedFlagsSet(EndpointAttributes restrictTo)
	    {
	        var allowedAttrs = EndpointAttributes.None;

	        //Network access
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnyNetworkAccessType))
	            allowedAttrs |= EndpointAttributes.AnyNetworkAccessType;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnyNetworkAccessType);

	        //Security
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnySecurityMode))
	            allowedAttrs |= EndpointAttributes.AnySecurityMode;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnySecurityMode);

	        //Http Method
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnyHttpMethod))
	            allowedAttrs |= EndpointAttributes.AnyHttpMethod;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnyHttpMethod);

	        //Call Style
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnyCallStyle))
	            allowedAttrs |= EndpointAttributes.AnyCallStyle;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnyCallStyle);

	        //Format
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnyFormat))
	            allowedAttrs |= EndpointAttributes.AnyFormat;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnyFormat);

	        //Endpoint
	        if (!HasAnyRestrictionsOf(restrictTo, EndpointAttributes.AnyEndpoint))
	            allowedAttrs |= EndpointAttributes.AnyEndpoint;
	        else
	            allowedAttrs |= (restrictTo & EndpointAttributes.AnyEndpoint);

	        return allowedAttrs;
	    }

	    static bool HasAnyRestrictionsOf(EndpointAttributes allRestrictions, EndpointAttributes restrictions)
		{
			return (allRestrictions & restrictions) != 0;
		}

        public bool HasNoAccessRestrictions
        {
            get
            {
                return this.AccessTo == EndpointAttributes.Any;
            }
        }

        public bool HasNoVisibilityRestrictions
        {
            get
            {
                return this.VisibilityTo == EndpointAttributes.Any;
            }
        }
    }
}