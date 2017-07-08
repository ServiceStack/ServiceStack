using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class FilterExamples : TemplateFilter
    {
        public IAppSettings AppSettings { get; set; }

        public string appsetting(string name) => AppSettings.GetString(name);
        
        public string capitalise(string text) => text.ToPascalCase();

        public int add(int target, int value) => target + value;
    }

    public class TemplateFilterTests
    {
        [Test]
        public void Can_Scan_FilterExamples_TemplateFilter()
        {
            var context = new TemplatePagesContext
            {
                ScanTypes = { typeof(FilterExamples) }
            }.Init();
            
            Assert.That(context.TemplateFilters.Count, Is.EqualTo(1));
            Assert.That(context.TemplateFilters[0].Pages, Is.EqualTo(context.Pages));
            Assert.That(((FilterExamples)context.TemplateFilters[0]).AppSettings, Is.Null);
        }
    }
}