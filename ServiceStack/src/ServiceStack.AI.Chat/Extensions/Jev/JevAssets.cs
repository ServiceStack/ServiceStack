using System.Reflection;
using System.Text;
namespace ServiceStack.AI;
public static class JevAssets
{
    public static string? Read(string path)
    {
        var assembly=typeof(JevAssets).Assembly;var suffix=".chat."+path.Replace('/','.');
        var name=assembly.GetManifestResourceNames().FirstOrDefault(n=>n.EndsWith(suffix,StringComparison.Ordinal));
        if(name==null)return null;using var stream=assembly.GetManifestResourceStream(name)!;using var reader=new StreamReader(stream,Encoding.UTF8);return reader.ReadToEnd();
    }
}
