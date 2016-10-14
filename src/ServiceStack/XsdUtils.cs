#if !NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Schema;
using ServiceStack.Text;

namespace ServiceStack
{
    public class XsdUtils
    {
        public static XmlSchemaSet GetXmlSchemaSet(ICollection<Type> operationTypes)
        {
            var exporter = new XsdDataContractExporter();

            exporter.Export(operationTypes);
            exporter.Schemas.Compile();
            return exporter.Schemas;
        }

        public static string GetXsd(XmlSchemaSet schemaSet)
        {
            var sb = StringBuilderCache.Allocate();
            using (var sw = new StringWriter(sb))
            {
                foreach (XmlSchema schema in schemaSet.Schemas())
                {
                    if (schema.SchemaTypes.Count == 0) continue; //remove blank schemas
                    schema.Write(sw);
                }
            }
            sb = sb.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", ""); //remove xml declaration
            return StringBuilderCache.ReturnAndFree(sb).Trim();
        }

        public static string GetXsd(Type operationType)
        {
            if (operationType == null) return null;
            var sb = StringBuilderCache.Allocate();
            var exporter = new XsdDataContractExporter();
            if (exporter.CanExport(operationType))
            {
                exporter.Export(operationType);
                var mySchemas = exporter.Schemas;

                var qualifiedName = exporter.GetRootElementName(operationType);
                if (qualifiedName == null) return null;
                foreach (XmlSchema schema in mySchemas.Schemas(qualifiedName.Namespace))
                {
                    schema.Write(new StringWriter(sb));
                }
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }
    }
}

#endif
