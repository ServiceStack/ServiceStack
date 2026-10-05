using System.Text;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public static class JevPaths
{
    static bool Unsafe(string value)=>Regex.IsMatch(value,@"[<>:""/\\|?*\x00-\x1f]")||Regex.IsMatch(value,@"\A(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?\z",RegexOptions.IgnoreCase);
    public static string Reference(string? value)
    {
        if(string.IsNullOrWhiteSpace(value)||value!=value.Trim()||Encoding.UTF8.GetByteCount(value)>240||value is "." or ".."||value.EndsWith('.')||value.Equals("_drafts",StringComparison.OrdinalIgnoreCase)||Unsafe(value))
            throw new JevValidationException("filename","Use a portable recipe filename stem, such as sentiment.");return value;
    }
    public static string Filename(string? value)
    {
        if(value==null||!value.EndsWith(".json",StringComparison.OrdinalIgnoreCase)||Encoding.UTF8.GetByteCount(value)>245||value!=value.Trim()||Unsafe(value))
            throw new JevValidationException("filename","Use a recipe JSON filename, such as sentiment.json.");Reference(value[..^5]);return value;
    }
    public static string DefaultFilename(string name)
    {
        var stem=Regex.Replace(name,@"[<>:""/\\|?*\x00-\x1f]","-").Trim().TrimEnd('.');if(stem.Length==0)stem="recipe";
        while(Encoding.UTF8.GetByteCount(stem)>240)stem=stem[..^1];try{return Filename(stem+".json");}catch(JevValidationException){return "recipe.json";}
    }
    public static string RunId(string? value)
    {
        if(value!=null&&Regex.IsMatch(value,@"\A[A-Za-z0-9_-]{1,100}\z"))return value;
        var index=value?.LastIndexOf('-')??-1;
        if(value==null||index<1||!Regex.IsMatch(value[(index+1)..],@"\A[0-9]{5,}\z")||Encoding.UTF8.GetByteCount(value+".md")>255)
            throw new JevValidationException("id","Invalid decision ID.");
        if(value[..index]!="_drafts")Reference(value[..index]);return value;
    }
}
