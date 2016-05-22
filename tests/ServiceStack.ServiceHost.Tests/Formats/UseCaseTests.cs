using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost.Tests.Formats
{
    public class Page
    {
        public Page()
        {
            this.Tags = new List<string>();
        }

        public string Name { get; set; }
        public string Slug { get; set; }
        public string Src { get; set; }
        public string FilePath { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<string> Tags { get; set; }

        public string AbsoluteUrl
        {
            get { return "http://path.com/to/" + this.Slug; }
        }
    }

    public class SearchResponse : IHasResponseStatus
    {
        public SearchResponse()
        {
            this.Results = new List<Page>();
        }

        public string Query { get; set; }

        public List<Page> Results { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }


    [TestFixture]
    public class UseCaseTests : MarkdownTestBase
    {
        private List<Page> Pages;
        private SearchResponse SearchResponse;

        string websiteTemplate =
@"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
        <div id=""header"">
            <a href=""/"">Home</a>
        </div>
        
        <div id=""body"">
            <!--@Body-->
        </div>
    </body>
</html>".NormalizeNewLines();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var jsonPages = File.ReadAllText("~/AppData/Pages.json".MapProjectPath());

            Pages = JsonSerializer.DeserializeFromString<List<Page>>(jsonPages);

            SearchResponse = new SearchResponse
            {
                Query = "OrmLite",
                Results = Pages.Take(5).ToList(),
            };
        }

        [Test]
        public void Can_display_search_results_basic()
        {
            var pageTemplate = @"@var Title = ""Search results for "" + Model.Query

@if (Model.Results.Count == 0) {

#### Your search did not match any documents.

## Suggestions:

  - Make sure all words are spelled correctly.
  - Try different keywords.
  - Try more general keywords.
  - Try fewer keywords.
}

@if (Model.Results.Count > 0) {
#### Showing Results 1 - @Model.Results.Count
}

<div id=""searchresults"">
@foreach page in Model.Results {
### @page.Category &gt; [@page.Name](@page.AbsoluteUrl)
@page.Content
}
</div>";
            var expectedHtml = @"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
        <div id=""header"">
            <a href=""/"">Home</a>
        </div>
        
        <div id=""body"">
            <h4>Showing Results 1 - 5</h4>
<div id=""searchresults"">
<h3>Markdown &gt; <a href=""http://path.com/to/about"">About Docs</a></h3>
<h3>Markdown &gt; <a href=""http://path.com/to/markdown-features"">Markdown Features</a></h3>
<h3>Markdown &gt; <a href=""http://path.com/to/markdown-razor"">Markdown Razor</a></h3>
<h3>Framework &gt; <a href=""http://path.com/to/home"">Home</a></h3>
<h3>Framework &gt; <a href=""http://path.com/to/overview"">Overview</a></h3>
</div>
        </div>
    </body>
</html>".NormalizeNewLines();

            var markdownFormat = Create(websiteTemplate, pageTemplate);

            var html = markdownFormat.RenderDynamicPageHtml(PageName, SearchResponse);

            Console.WriteLine(html);

            Assert.That(html.NormalizeNewLines(), Is.EqualTo(expectedHtml));
        }


        [Test]
        public void Can_display_search_results()
        {
            var pageTemplate = @"@var Title = ""Search results for "" + Model.Query

@if (Model.Results.Count == 0) {

#### Your search did not match any documents.

## Suggestions:

  - Make sure all words are spelled correctly.
  - Try different keywords.
  - Try more general keywords.
  - Try fewer keywords.

} else {

#### Showing Results 1 - @Model.Results.Count

^<div id=""searchresults"">

@foreach page in Model.Results {

### @page.Category &gt; [@page.Name](@page.AbsoluteUrl)
@page.Content

}

^</div>

}";
            var expectedHtml = @"<!DOCTYPE html>
<html>
    <head>
        <title>Simple Site</title>
    </head>
    <body>
        <div id=""header"">
            <a href=""/"">Home</a>
        </div>
        
        <div id=""body"">
            <h4>Showing Results 1 - 5</h4>
<div id=""searchresults"">
<h3>Markdown &gt; <a href=""http://path.com/to/about"">About Docs</a></h3>
<h3>Markdown &gt; <a href=""http://path.com/to/markdown-features"">Markdown Features</a></h3>
<h3>Markdown &gt; <a href=""http://path.com/to/markdown-razor"">Markdown Razor</a></h3>
<h3>Framework &gt; <a href=""http://path.com/to/home"">Home</a></h3>
<h3>Framework &gt; <a href=""http://path.com/to/overview"">Overview</a></h3>
</div>

        </div>
    </body>
</html>".NormalizeNewLines();

            var markdownFormat = Create(websiteTemplate, pageTemplate);

            var html = markdownFormat.RenderDynamicPageHtml(PageName, SearchResponse);

            Console.WriteLine(html);

            Assert.That(html, Is.EqualTo(expectedHtml));
        }
    }

}


