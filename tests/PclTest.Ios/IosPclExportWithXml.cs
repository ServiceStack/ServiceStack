using System;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace PclTest.Ios
{
    public class IosPclExportWithXml : IosPclExport
    {
        public static new IosPclExportWithXml Provider = new IosPclExportWithXml();

        public new static void Configure()
        {
            IosPclExportClient.Configure(); 
            Configure(Provider);
        }

        public override string ToXsdDateTimeString(DateTime dateTime)
        {
            return System.Xml.XmlConvert.ToString(dateTime.ToStableUniversalTime(), System.Xml.XmlDateTimeSerializationMode.Utc);
        }

        public override string ToLocalXsdDateTimeString(DateTime dateTime)
        {
            return System.Xml.XmlConvert.ToString(dateTime, System.Xml.XmlDateTimeSerializationMode.Local);
        }

        public override DateTime ParseXsdDateTime(string dateTimeStr)
        {
            return System.Xml.XmlConvert.ToDateTime(dateTimeStr, System.Xml.XmlDateTimeSerializationMode.Utc);
        }

        public override DateTime ParseXsdDateTimeAsUtc(string dateTimeStr)
        {
            return System.Xml.XmlConvert.ToDateTime(dateTimeStr, System.Xml.XmlDateTimeSerializationMode.Utc).Prepare(parsedAsUtc: true);
        }
    }
}
