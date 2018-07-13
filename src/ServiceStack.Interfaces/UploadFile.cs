using System.IO;

namespace ServiceStack
{
    public class UploadFile
    {
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public string FieldName { get; set; }
        public string ContentType { get; set; }

        public UploadFile(Stream stream)
            : this(null, stream, null, null) {}

        public UploadFile(string fileName, Stream stream)
            : this(fileName, stream, null, null) {}

        public UploadFile(string fileName, Stream stream, string fieldName)
            : this(fileName, stream, fieldName, null) {}

        public UploadFile(string fileName, Stream stream, string fieldName, string contentType)
        {
            FileName = fileName;
            Stream = stream;
            FieldName = fieldName;
            ContentType = contentType;
        }
    }
}