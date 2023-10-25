using ServiceStack;
namespace MyApp.ServiceModel;

public class AppData
{
    public string[] Currencies { get; set; }
    public List<string> AlphaValues { get; set; }
    public Dictionary<string, string> AlphaDictionary { get; set; }
    public List<KeyValuePair<string, string>> AlphaKeyValuePairs { get; set; }
}
