using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Used to decorate Request DTO's to alter the behaviour of a service.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ServiceAttribute
		: Attribute
	{
		/// <summary>
		/// Sets a single access restriction
		/// </summary>
		/// <value>The restrict access to.</value>
		public EndpointAttributes RestrictAccessTo
		{
			get
			{
				return this.RestrictAccessToScenarios.Length == 0
				       	? EndpointAttributes.All
				       	: this.RestrictAccessToScenarios[0];
			}
			set
			{
				this.RestrictAccessToScenarios = new[] { value };
			}
		}

		/// <summary>
		/// Set multiple access scenarios
		/// </summary>
		/// <value>The restrict access to scenarios.</value>
		public EndpointAttributes[] RestrictAccessToScenarios { get; private set; }

		public int? Version { get; set; }

		public ServiceAttribute()
		{
			this.RestrictAccessToScenarios = new EndpointAttributes[0];
		}

		public ServiceAttribute(params EndpointAttributes[] restrictAccessToScenarios)
			: this()
		{
			if (restrictAccessToScenarios.Length == 0)
			{
				this.RestrictAccessTo = EndpointAttributes.All;
				return;
			}

			var scenarios = new List<EndpointAttributes>();
			foreach (var restrictAccessToScenario in restrictAccessToScenarios)
			{
				var restrictAccessTo = EndpointAttributes.None;

				//Network access
				if (!HasAnyRestrictionsOf(restrictAccessToScenario, EndpointAttributes.AllNetworkAccessTypes))
				{
					restrictAccessTo |= EndpointAttributes.AllNetworkAccessTypes;
				}
				else
				{
					restrictAccessTo |= (restrictAccessToScenario & EndpointAttributes.AllNetworkAccessTypes);
				}

				//Security
				if (!HasAnyRestrictionsOf(restrictAccessToScenario, EndpointAttributes.AllSecurityModes))
				{
					restrictAccessTo |= EndpointAttributes.AllSecurityModes;
				}
				else
				{
					restrictAccessTo |= (restrictAccessToScenario & EndpointAttributes.AllSecurityModes);
				}

				//Http Method
				if (!HasAnyRestrictionsOf(restrictAccessToScenario, EndpointAttributes.AllHttpMethods))
				{
					restrictAccessTo |= EndpointAttributes.AllHttpMethods;
				}
				else
				{
					restrictAccessTo |= (restrictAccessToScenario & EndpointAttributes.AllHttpMethods);
				}

				//Call style
				if (!HasAnyRestrictionsOf(restrictAccessToScenario, EndpointAttributes.AllCallStyles))
				{
					restrictAccessTo |= EndpointAttributes.AllCallStyles;
				}
				else
				{
					restrictAccessTo |= (restrictAccessToScenario & EndpointAttributes.AllCallStyles);
				}

				//Endpoint
				if (!HasAnyRestrictionsOf(restrictAccessToScenario, EndpointAttributes.AllEndpointTypes))
				{
					restrictAccessTo |= EndpointAttributes.AllEndpointTypes;
				}
				else
				{
					restrictAccessTo |= (restrictAccessToScenario & EndpointAttributes.AllEndpointTypes);
				}

				scenarios.Add(restrictAccessTo);
			}

			this.RestrictAccessToScenarios = scenarios.ToArray();
		}

		static bool HasAnyRestrictionsOf(EndpointAttributes allRestrictions, EndpointAttributes restrictions)
		{
			return (allRestrictions & restrictions) != 0;
		}

		public ServiceAttribute(int version, params EndpointAttributes[] restrictAccessScenarios)
			: this(restrictAccessScenarios)
		{
			this.Version = version;
		}

		public bool HasNoAccessRestrictions
		{
			get
			{
				return this.RestrictAccessTo == EndpointAttributes.All;
			}
		}
	}
}