using System;
using System.Collections.Generic;
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
            foreach (var opType in OperationTypes)
            {
                var refTypes = opType.GetReferencedTypes();
                foreach (var type in refTypes)
                {
                    if (type.IsDto())
                    {
                        var baseTypeWithSameName = XsdMetadata.GetBaseTypeWithTheSameName(type);
                        if (uniqueTypeNames.Contains(baseTypeWithSameName.GetOperationName()))
                        {
                            log.WarnFormat("Skipping duplicate type with existing name '{0}'", baseTypeWithSameName.GetOperationName());
                        }

                        if (HostContext.AppHost.ExportSoapType(baseTypeWithSameName))
                        {
                            uniqueTypes.Add(baseTypeWithSameName);
                        }
                    }
                }
            }

            this.OperationTypes = uniqueTypes;

            var schemaSet = XsdUtils.GetXmlSchemaSet(OperationTypes);
            var xsd = XsdUtils.GetXsd(schemaSet);
            var filteredXsd = Filter(xsd);
            return filteredXsd;
        }
    }
}