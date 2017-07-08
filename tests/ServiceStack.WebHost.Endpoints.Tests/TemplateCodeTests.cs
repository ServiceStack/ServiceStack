using ServiceStack.Configuration;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Page("/code-layout")]
    [PageArg("title", "Code Layout Title")]
    public class LayoutTemplateCode : TemplateCode
    {
        string render(string title, string content) => $@"
<h1>{title}</h1>
<p>
    {content}
</p>
";
    }

    [Page("/foreach-code")]
    public class ForEachCodeExample : TemplateCode
    {
        public IAppSettings AppSettings { get; set; }

        string render(string title, string[] items) => $@"
<h1>{title}</h1>
<ul>
    {items.Map(x => $"<li>{x}</li>").Join("")}        
</ul>
";            
    }

    public class TemplateCodeTests
    {
    }
}