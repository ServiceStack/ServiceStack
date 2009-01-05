using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceModel
{
	public abstract class ServiceModelFinderBase : IServiceModelFinder
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ServiceModelFinderBase));

		protected int MinVersion { get; set; }
		protected int MaxVersion { get; set; }
		private readonly IDictionary<string, Type> typesByName;

		private static readonly Regex typeRegex =
				new Regex(@".*Version([0-9]+)\..*", RegexOptions.Compiled);

		protected ServiceModelFinderBase()
		{
			this.typesByName = new Dictionary<string, Type>();

			foreach (Type type in this.GetType().Assembly.GetTypes())
			{
				try
				{
					if (!type.Namespace.Contains("Version"))
					{
						log.WarnFormat("Ignoring DTO candidate: '{0}'", type.FullName);
						continue;
					}
					Match match = typeRegex.Match(type.FullName);
					string versionText = match.Groups[1].Value;
					int version = Convert.ToInt32(versionText);

					if (match.Success)
					{
						this.typesByName[version + ":" + type.Name] = type;
					}
				}
				catch (Exception ex)
				{
					log.ErrorFormat("Exception in ServiceModelFinderBase() parsing type: '{0}'",
						type.FullName, ex);

					throw;
				}
			}
		}

		public Type FindTypeByOperation(string operationName, int? version)
		{
			Type retval;
			version = version ?? this.MaxVersion;

			if (this.typesByName.TryGetValue(version + ":" + operationName, out retval))
			{
				return retval;
			}

			return null;
		}

		public Assembly ServiceModelAssembly
		{
			get { return this.GetType().Assembly; }
		}
	}
}
