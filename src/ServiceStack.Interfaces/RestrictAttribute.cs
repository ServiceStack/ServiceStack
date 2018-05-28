//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack
{
    /// <summary>
    /// Decorate on Request DTO's to alter the accessibility of a service and its visibility on /metadata pages
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RestrictAttribute : AttributeBase
    {
        /// <summary>
        /// Allow access but hide from metadata to requests from Localhost only
        /// </summary>
        public bool VisibleInternalOnly
        {
            get => CanShowTo(RequestAttributes.InternalNetworkAccess);
            set
            {
                if (value == false)
                    throw new Exception("Only true allowed");

                VisibilityTo = RequestAttributes.InternalNetworkAccess.ToAllowedFlagsSet();
            }
        }

        /// <summary>
        /// Allow access but hide from metadata to requests from Localhost and Local Intranet only
        /// </summary>
        public bool VisibleLocalhostOnly
        {
            get => CanShowTo(RequestAttributes.Localhost);
            set
            {
                if (value == false)
                    throw new Exception("Only true allowed");

                VisibilityTo = RequestAttributes.Localhost.ToAllowedFlagsSet();
            }
        }

        /// <summary>
        /// Restrict access and hide from metadata to requests from Localhost only
        /// </summary>
        public bool LocalhostOnly
        {
            get => HasAccessTo(RequestAttributes.Localhost) && CanShowTo(RequestAttributes.Localhost);
            set
            {
                if (value == false)
                    throw new Exception("Only true allowed");

                AccessTo = RequestAttributes.Localhost.ToAllowedFlagsSet();
                VisibilityTo = RequestAttributes.Localhost.ToAllowedFlagsSet();
            }
        }

        /// <summary>
        /// Restrict access and hide from metadata to requests from Localhost and Local Intranet only
        /// </summary>
        public bool InternalOnly
        {
            get => HasAccessTo(RequestAttributes.InternalNetworkAccess) && CanShowTo(RequestAttributes.InternalNetworkAccess);
            set
            {
                if (value == false)
                    throw new Exception("Only true allowed");

                AccessTo = RequestAttributes.InternalNetworkAccess.ToAllowedFlagsSet();
                VisibilityTo = RequestAttributes.InternalNetworkAccess.ToAllowedFlagsSet();
            }
        }

        /// <summary>
        /// Restrict access and hide from metadata to requests from External only
        /// </summary>
        public bool ExternalOnly
        {
            get => HasAccessTo(RequestAttributes.External) && CanShowTo(RequestAttributes.External);
            set
            {
                if (value == false)
                    throw new Exception("Only true allowed");

                AccessTo = RequestAttributes.External.ToAllowedFlagsSet();
                VisibilityTo = RequestAttributes.External.ToAllowedFlagsSet();
            }
        }

        /// <summary>
        /// Sets a single access restriction
        /// </summary>
        /// <value>Restrict Access to.</value>
        public RequestAttributes AccessTo
        {
            get => this.AccessibleToAny.Length == 0
                ? RequestAttributes.Any
                : this.AccessibleToAny[0];

            set => this.AccessibleToAny = new[] { value };
        }

        /// <summary>
        /// Restrict access to any of the specified access scenarios
        /// </summary>
        /// <value>Access restrictions</value>
        public RequestAttributes[] AccessibleToAny { get; private set; }

        /// <summary>
        /// Sets a single metadata Visibility restriction
        /// </summary>
        /// <value>Restrict metadata Visibility to.</value>
        public RequestAttributes VisibilityTo
        {
            get => this.VisibleToAny.Length == 0
                ? RequestAttributes.Any
                : this.VisibleToAny[0];

            set => this.VisibleToAny = new[] { value };
        }

        /// <summary>
        /// Restrict metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>Visibility restrictions</value>
        public RequestAttributes[] VisibleToAny { get; private set; }

        public RestrictAttribute()
        {
            this.AccessTo = RequestAttributes.Any;
            this.VisibilityTo = RequestAttributes.Any;
        }

        /// <summary>
        /// Restrict access and metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>The restrict access to scenarios.</value>
        public RestrictAttribute(params RequestAttributes[] restrictAccessAndVisibilityToScenarios)
        {
            this.AccessibleToAny = ToAllowedFlagsSet(restrictAccessAndVisibilityToScenarios);
            this.VisibleToAny = ToAllowedFlagsSet(restrictAccessAndVisibilityToScenarios);
        }

        /// <summary>
        /// Restrict access and metadata visibility to any of the specified access scenarios
        /// </summary>
        /// <value>The restrict access to scenarios.</value>
        public RestrictAttribute(RequestAttributes[] allowedAccessScenarios, RequestAttributes[] visibleToScenarios)
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
        private static RequestAttributes[] ToAllowedFlagsSet(RequestAttributes[] restrictToAny)
        {
            if (restrictToAny.Length == 0)
                return new[] { RequestAttributes.Any };

            var scenarios = new List<RequestAttributes>();
            foreach (var restrictToScenario in restrictToAny)
            {
                var restrictTo = restrictToScenario.ToAllowedFlagsSet();

                scenarios.Add(restrictTo);
            }

            return scenarios.ToArray();
        }

        public bool CanShowTo(RequestAttributes restrictions)
        {
            return this.VisibleToAny.Any(scenario => (restrictions & scenario) == restrictions);
        }

        public bool HasAccessTo(RequestAttributes restrictions)
        {
            return this.AccessibleToAny.Any(scenario => (restrictions & scenario) == restrictions);
        }

        public bool HasNoAccessRestrictions => this.AccessTo == RequestAttributes.Any;

        public bool HasNoVisibilityRestrictions => this.VisibilityTo == RequestAttributes.Any;
    }

    public static class RestrictExtensions
    {
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
        public static RequestAttributes ToAllowedFlagsSet(this RequestAttributes restrictTo)
        {
            if (restrictTo == RequestAttributes.Any)
                return RequestAttributes.Any;

            var allowedAttrs = RequestAttributes.None;

            //Network access
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnyNetworkAccessType))
                allowedAttrs |= RequestAttributes.AnyNetworkAccessType;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnyNetworkAccessType);

            //Security
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnySecurityMode))
                allowedAttrs |= RequestAttributes.AnySecurityMode;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnySecurityMode);

            //Http Method
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnyHttpMethod))
                allowedAttrs |= RequestAttributes.AnyHttpMethod;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnyHttpMethod);

            //Call Style
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnyCallStyle))
                allowedAttrs |= RequestAttributes.AnyCallStyle;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnyCallStyle);

            //Format
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnyFormat))
                allowedAttrs |= RequestAttributes.AnyFormat;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnyFormat);

            //Endpoint
            if (!HasAnyRestrictionsOf(restrictTo, RequestAttributes.AnyEndpoint))
                allowedAttrs |= RequestAttributes.AnyEndpoint;
            else
                allowedAttrs |= (restrictTo & RequestAttributes.AnyEndpoint);

            return allowedAttrs;
        }

        public static bool HasAnyRestrictionsOf(RequestAttributes allRestrictions, RequestAttributes restrictions)
        {
            return (allRestrictions & restrictions) != 0;
        }
    }
}