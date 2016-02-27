using System.IO;

namespace ServiceStack
{
    public class UploadFile
    {
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public string FieldName { get; set; }

        public UploadFile(Stream stream)
        {
            Stream = stream;
        }

        public UploadFile(string fileName, Stream stream)
        {
            FileName = fileName;
            Stream = stream;
        }

        public UploadFile(string fileName, Stream stream, string fieldName)
        {
            FileName = fileName;
            Stream = stream;
            FieldName = fieldName;
        }
    }
}