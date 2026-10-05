using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Python-compatible portable JSON encoding. Document hashes sort keys and retain default separators.</summary>
public static class JevJson
{
    public static string Encode(JsonNode? value,bool sorted=false,bool spaced=false)
    {
        using var document=JsonDocument.Parse(value?.ToJsonString(ChatJson.Options)??"null");
        var output=new StringBuilder();Write(document.RootElement,output,sorted,spaced);return output.ToString();
    }
    public static string Hash(JsonNode? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Encode(value,true,true)))).ToLowerInvariant();
    public static string Fingerprint(JsonNode? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Encode(value)))).ToLowerInvariant();
    static void Write(JsonElement value,StringBuilder result,bool sorted,bool spaced)
    {
        switch(value.ValueKind) {
            case JsonValueKind.Object:
                result.Append('{');var properties=value.EnumerateObject().ToArray();
                if(sorted)Array.Sort(properties,(a,b)=>CompareUnicode(a.Name,b.Name));
                for(var i=0;i<properties.Length;i++){if(i>0)result.Append(spaced?", ":",");Quote(properties[i].Name,result);result.Append(spaced?": ":":");Write(properties[i].Value,result,sorted,spaced);}result.Append('}');break;
            case JsonValueKind.Array:
                result.Append('[');var first=true;foreach(var item in value.EnumerateArray()){if(!first)result.Append(spaced?", ":",");first=false;Write(item,result,sorted,spaced);}result.Append(']');break;
            case JsonValueKind.String:Quote(value.GetString()!,result);break;
            case JsonValueKind.Number:
                var raw=value.GetRawText();
                if(!raw.Contains('.')&&!raw.Contains('e')&&!raw.Contains('E'))result.Append(raw);
                else result.Append(Float(value.GetDouble()));
                break;
            default:result.Append(value.GetRawText());break;
        }
    }
    static int CompareUnicode(string a,string b)
    {
        var x=a.EnumerateRunes().Select(r=>r.Value).ToArray();var y=b.EnumerateRunes().Select(r=>r.Value).ToArray();
        for(var i=0;i<Math.Min(x.Length,y.Length);i++)if(x[i]!=y[i])return x[i].CompareTo(y[i]);return x.Length.CompareTo(y.Length);
    }
    static string Float(double value)
    {
        if(!double.IsFinite(value))throw new JevValidationException("document","Use finite, serializable JSON values.");
        if(value==0)return double.IsNegative(value)?"-0.0":"0.0";
        var text=value.ToString("R",CultureInfo.InvariantCulture).ToLowerInvariant();var e=text.IndexOf('e');
        if(e>=0) {
            var mantissa=text[..e];var exponent=int.Parse(text[(e+1)..],CultureInfo.InvariantCulture);
            if(Math.Abs(value)>=1e-4&&Math.Abs(value)<1e16) {
                var negative=mantissa.StartsWith('-');var digits=mantissa.TrimStart('-').Replace(".","");var point=mantissa.TrimStart('-').IndexOf('.');
                if(point<0)point=mantissa.TrimStart('-').Length;point+=exponent;
                text=(negative?"-":"")+(point<=0?"0."+new string('0',-point)+digits:point>=digits.Length?digits+new string('0',point-digits.Length)+".0":digits.Insert(point,"."));
            } else text=mantissa+"e"+(exponent>=0?"+":"-")+Math.Abs(exponent).ToString("D2",CultureInfo.InvariantCulture);
        } else if(Math.Abs(value)>=1e16||Math.Abs(value)<1e-4) {
            var sign=text.StartsWith('-')?"-":"";var unsigned=text.TrimStart('-');var point=unsigned.IndexOf('.');if(point<0)point=unsigned.Length;
            var digits=unsigned.Replace(".","");var leading=digits.TakeWhile(c=>c=='0').Count();var exponent=point-leading-1;digits=digits[leading..].TrimEnd('0');
            text=sign+digits[0]+(digits.Length>1?"."+digits[1..]:"")+"e"+(exponent>=0?"+":"-")+Math.Abs(exponent).ToString("D2",CultureInfo.InvariantCulture);
        } else if(!text.Contains('.'))text+=".0";
        return text;
    }
    static void Quote(string value,StringBuilder result)
    {
        result.Append('"');foreach(var c in value) {
            switch(c) {
                case '"':result.Append((char)92).Append((char)34);break;
                case '\\':result.Append("\\\\");break;
                case '\b':result.Append("\\b");break;
                case '\f':result.Append("\\f");break;
                case '\n':result.Append("\\n");break;
                case '\r':result.Append("\\r");break;
                case '\t':result.Append("\\t");break;
                default:if(c<32)result.Append("\\u"+((int)c).ToString("x4"));else result.Append(c);break;
            }
        }result.Append('"');
    }
    public static int Length(string value)=>value.EnumerateRunes().Count();
    public static JsonNode? Copy(JsonNode? value,int maximum=DecisionRecipeValidator.MaxBytes)
    {
        try {
            var encoded=Encode(value,false,true);DecisionRecipeValidator.Require(Encoding.UTF8.GetByteCount(encoded)<=maximum,"document","Keep the document under 512 KB.");
            return JsonNode.Parse(encoded);
        }catch(Exception e) when(e is JsonException or ArgumentException or FormatException or OverflowException) {throw new JevValidationException("document","Use finite, serializable JSON values.");}
    }
}
