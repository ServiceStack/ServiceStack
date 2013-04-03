using System;
using System.Text;
using ServiceStack.Razor.Templating;
using ServiceStack.Text;

namespace ServiceStack.Razor
{
    //The type or namespace name 'Html' does not exist in the namespace 'ServiceStack.Markdown' (are you missing an assembly reference?)
    public class ErrorViewPage : ViewPageRef
    {
        public static string DefaultPageName = "Error";
        public static string DefaultContents = @"﻿<!DOCTYPE HTML>
<html>
	<head>
		<title>Error</title>
        <style>
        BODY {{
            background-color:white;
            color:#000000;
            font-family:Verdana;
            margin: 0;
        }}
        H1 {{
            background-color: #036;
            color: #FFF;
            font-family: Tahoma;
            font-size: 26px;
            font-weight: normal;
            margin: 0;
            padding: 10px 0 3px 15px;
        }}
        </style>
	</head>
	<body>
	    <h1>Oops something went wrong!</h1>
        <div style=""padding:10px;"">
            <h2>{0}</h2>
            <div>{1}</div>
            <h3>Stack Trace</h3>
	        <pre>{2}</pre>
        </div>
	</body>
</html>";

        private Exception ex;
        public ErrorViewPage(RazorFormat razorFormat, Exception ex)
            : base(razorFormat, null, DefaultPageName, ErrorPage(ex, DefaultContents))
        {
            this.ex = ex;
        }

        public static string ErrorPage(Exception ex, string template)
        {
            var details = "";
            var tempEx = ex as TemplateCompilationException;
            if (tempEx != null)
            {
                var sb = new StringBuilder("<h3>Compile Errors</h3>");
                foreach (var error in tempEx.Errors)
                {
                    sb.AppendFormat("<p>{0}</p>\n", error.Dump());
                }
                details = sb.ToString();
            }
            return template.Fmt(ex.Message + " (" + ex.GetType().Name + ")", details, ex.StackTrace);
        }
    }
}