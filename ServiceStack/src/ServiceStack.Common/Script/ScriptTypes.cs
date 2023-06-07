namespace ServiceStack.Script;

public class RawString : IRawString
{
    public static RawString Empty = new RawString("");
        
    private readonly string value;
    public RawString(string value) => this.value = value;
    public string ToRawString() => value;
}