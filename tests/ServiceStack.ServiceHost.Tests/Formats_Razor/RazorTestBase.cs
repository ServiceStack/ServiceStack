using System;
using System.Collections.Generic;
using ServiceStack.Razor;
using ServiceStack.Razor.Managers;
using ServiceStack.VirtualPath;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
    public static class RazorFormatExtensions
    {
        public static RazorPage AddFileAndPage(this RazorFormat razorFormat, string filePath, string contents)
        {
            razorFormat.VirtualFileSources.WriteFile(filePath, contents);
            return razorFormat.AddPage(filePath);
        }
    }

    public class RazorTestBase
    {
        public const string TemplateName = "Template";
        protected const string PageName = "Page";
        protected RazorFormat RazorFormat;
    }
}
