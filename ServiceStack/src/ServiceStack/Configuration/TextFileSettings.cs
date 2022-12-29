using System.IO;

namespace ServiceStack.Configuration
{
    public class TextFileSettings : DictionarySettings
    {
        public TextFileSettings(string filePath, string delimiter=" ") 
            : base(File.ReadAllText(filePath).ParseKeyValueText(delimiter)) {}
    }
}