using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Host;
using ServiceStack.Logging;

namespace ServiceStack.Metadata
{
	public class XsdGenerator
	{
		private readonly ILog log = LogManager.GetLogger(typeof(XsdGenerator));
		public bool OptimizeForFlash { get; set; }
		public ICollection<Type> OperationTypes { get; set; }

		private string Filter(string xsd)
		{
			return !this.OptimizeForFlash ? xsd : xsd.Replace("ser:guid", "xs:string");
		}

		public override string ToString()
		{
			if (OperationTypes == null || OperationTypes.Count == 0) return null;

            var uniqueTypes = new HashSet<Type>();
            var uniqueTypeNames = new List<string>();
            foreach (var type in OperationTypes)
            {
                foreach (var assemblyType in type.Assembly.GetTypes())
                {
                    if (assemblyType.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0)
                    {
                        var baseTypeWithSameName = XsdMetadata.GetBaseTypeWithTheSameName(assemblyType);
                        if (uniqueTypeNames.Contains(baseTypeWithSameName.GetOperationName()))
                        {
                            log.WarnFormat("Skipping duplicate type with existing name '{0}'", baseTypeWithSameName.GetOperationName());
                        }
                        uniqueTypes.Add(baseTypeWithSameName);
                    }
                }
            }

		    uniqueTypes.RemoveWhere(x => x.IsGenericTypeDefinition());

            this.OperationTypes = uniqueTypes;

			var schemaSet = XsdUtils.GetXmlSchemaSet(OperationTypes);
			var xsd = XsdUtils.GetXsd(schemaSet);
			var filteredXsd = Filter(xsd);
			return filteredXsd;
		}
	}
}