namespace ServiceStack.DesignPatterns.Translator
{
    public interface ITranslator<To, From>
    {
        To Parse(From from);
    }
}