namespace ServiceStack.Razor.Compilation
{
    public interface IRazorHost
    {
        string DefaultNamespace { get; set; }

        bool EnableLinePragmas { get; set; }

        //event EventHandler<GeneratorErrorEventArgs> Error;

        //event EventHandler<ProgressEventArgs> Progress;

        //string GenerateCode();
    }
}
