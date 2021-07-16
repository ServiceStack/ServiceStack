using System;
using System.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Formats
{
    public class CsvFormat : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Csv;
        public void Register(IAppHost appHost)
        {
            //Register the 'text/csv' content-type and serializers (format is inferred from the last part of the content-type)
            appHost.ContentTypes.Register(MimeTypes.Csv,
                SerializeToStream, CsvSerializer.DeserializeFromStream);

            //Add a response filter to add a 'Content-Disposition' header so browsers treat it natively as a .csv file
            appHost.GlobalResponseFilters.Add((req, res, dto) =>
            {
                if (req.ResponseContentType == MimeTypes.Csv && dto is not IHttpResult) //avoid double Content-Disposition headers
                {
                    var fileName = req.OperationName + ".csv";
                    res.AddHeader(HttpHeaders.ContentDisposition, $"attachment;{HttpExt.GetDispositionFileName(fileName)}");
                }
            });
        }

        public void SerializeToStream(IRequest req, object request, Stream stream)
        {
            if (request is string str)
            {
                stream.Write(str);
            }
            else if (request is byte[] bytes)
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (request is Stream s)
            {
                s.WriteTo(stream);
            }
            else if (request is ReadOnlyMemory<char> roms)
            {
                MemoryProvider.Instance.Write(stream, roms);
            }
            else
            {
                CsvSerializer.SerializeToStream(request, stream);
            }
        }
    }
}