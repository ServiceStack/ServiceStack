using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Common.Host.Utils;

namespace ServiceStack.Common.Host.Endpoints
{
	public class XsdGenerator
	{
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
				OperationTypes = OperationTypes.Select(x => x.Assembly).Distinct()
					.SelectMany(x => x.GetTypes())
					.Where(x => x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0).ToList();
			}

			var schemaSet = XsdUtils.GetXmlSchemaSet(OperationTypes);
			var xsd = XsdUtils.GetXsd(schemaSet);
			var filteredXsd = Filter(xsd);
			return filteredXsd;
		}
	}
}