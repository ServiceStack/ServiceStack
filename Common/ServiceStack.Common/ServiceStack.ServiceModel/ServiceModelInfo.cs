using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Logging;

namespace ServiceStack.ServiceModel
{
	public abstract class ServiceModelInfo
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ServiceModelInfo));

		protected int MinVersion { get; set; }
		protected int MaxVersion { get; set; }
		private readonly IDictionary<string, Type> typesByName;

		private static readonly Regex typeRegex =
				new Regex(@".*Version([0-9]+)\..*", RegexOptions.Compiled);

		protected ServiceModelInfo()
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
					log.ErrorFormat("Exception in ServiceModelInfo() parsing type: '{0}'",
						type.FullName, ex);

					throw;
				}
			}
		}

		public virtual Type GetDtoTypeFromOperation(string operationName, int version)
		{
			Type retval;

			if (this.typesByName.TryGetValue(version + ":" + operationName, out retval))
			{
				return retval;
			}

			return null;
		}

		public virtual Type GetDtoTypeFromOperation(string operationName)
		{
			return GetDtoTypeFromOperation(operationName, this.MaxVersion);
		}
	}
}
