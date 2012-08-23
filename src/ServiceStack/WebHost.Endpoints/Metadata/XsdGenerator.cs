using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class XsdGenerator
	{
		private readonly ILog log = LogManager.GetLogger(typeof(XsdGenerator));
		public bool OptimizeForFlash { get; set; }
		public ICollection<Type> OperationTypes { get; set; }
		public bool IncludeAllTypesInAssembly { get; set; }

		private string Filter(string xsd)
		{
			return !this.OptimizeForFlash ? xsd : xsd.Replace("ser:guid", "xs:string");
		}

		public override string ToString()
		{
			if (OperationTypes == null || OperationTypes.Count == 0) return null;
			
			if (IncludeAllTypesInAssembly)
			{
				var uniqueTypes = new List<Type>();
				var uniqueTypeNames = new List<string>();
				foreach (var type in OperationTypes)
				{
					foreach (var assemblyType in type.Assembly.GetTypes())
					{
						if (assemblyType.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0)
						{
							var baseTypeWithSameName = ServiceOperations.GetBaseTypeWithTheSameName(assemblyType);
							if (uniqueTypeNames.Contains(baseTypeWithSameName.Name))
							{
								log.WarnFormat("Skipping duplicate type with existing name '{0}'", baseTypeWithSameName.Name);
							}
							uniqueTypes.Add(baseTypeWithSameName);
						}
					}
				}
				this.OperationTypes = uniqueTypes;
			}

			var schemaSet = XsdUtils.GetXmlSchemaSet(OperationTypes);
			var xsd = XsdUtils.GetXsd(schemaSet);
			var filteredXsd = Filter(xsd);
			return filteredXsd;
		}
	}
}