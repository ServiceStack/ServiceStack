using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MarkdownSharp;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.WebHost.EndPoints.Formats
{
	public class MarkdownFormat
	{
		public static string TemplateName = "default.htm";
		public static string TemplatePlaceHolder = "<!--@Response-->";

		public static MarkdownFormat Instance = new MarkdownFormat();

		public Dictionary<string, MarkdownPage> Pages = new Dictionary<string, MarkdownPage>(
			StringComparer.CurrentCultureIgnoreCase);

		public Dictionary<string, MarkdownTemplate> PageTemplates = new Dictionary<string, MarkdownTemplate>(
			StringComparer.CurrentCultureIgnoreCase);

		private readonly Markdown markdown;

		public MarkdownFormat()
		{
			markdown = new Markdown();
		}

		public void Register(IAppHost appHost)
		{
			RegisterMarkdownPages("~".MapHostAbsolutePath());

			HtmlFormat.ContentResolvers.Add((requestContext, dto, stream) => {
				var pageName = dto.GetType().Name;

				MarkdownPage markdownPage;
				if (!Pages.TryGetValue(pageName, out markdownPage))
					return false;

				var html = RenderStaticPage(markdownPage);
				var htmlBytes = html.ToUtf8Bytes();
				stream.Write(htmlBytes, 0, htmlBytes.Length);
				return true;
			});
		}

		public void RegisterMarkdownPages(string dirPath)
		{
			var di = new DirectoryInfo(dirPath);
			var markDownFiles = di.GetMatchingFiles("*.md");

			foreach (var markDownFile in markDownFiles)
			{
				var fileInfo = new FileInfo(markDownFile);
				var pageName = fileInfo.Name.SplitOnFirst('.')[0];
				var pageContents = File.ReadAllText(markDownFile);

				AddPage(new MarkdownPage(markDownFile, pageName, pageContents));
			}
		}

		private void AddPage(MarkdownPage page)
		{
			Pages.Add(page.Name, page);

			var templatePath = page.GetTemplatePath();

			if (!PageTemplates.ContainsKey(templatePath))
			{
				var templateFile = new FileInfo(templatePath);

				if (!templateFile.Exists)
				{
					PageTemplates.Add(templateFile.FullName, null);
					return;
				}

				var pageContents = File.ReadAllText(templatePath);
				var template = new MarkdownTemplate(
					templatePath, templateFile.Name, pageContents);

				PageTemplates.Add(template.Path, template);
			}
		}

		public string RenderStaticPage(string pageName)
		{
			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new KeyNotFoundException(pageName);

			return RenderStaticPage(markdownPage);
		}

		private string RenderStaticPage(MarkdownPage markdownPage)
		{
			var pageHtml = markdown.Transform(markdownPage.Contents);
			var templatePath = markdownPage.GetTemplatePath();
			
			MarkdownTemplate markdownTemplate;
			PageTemplates.TryGetValue(templatePath, out markdownTemplate);
			if (markdownTemplate == null) return pageHtml;
			
			var htmlPage = markdownTemplate.Contents.ReplaceFirst(
				TemplatePlaceHolder, pageHtml);
			
			return htmlPage;
		}

		public string Transform(string markdownText)
		{
			var pageHtml = markdown.Transform(markdownText);
			return pageHtml;
		}

		public string RenderDynamicPage(string pageName, object model)
		{
			throw new NotImplementedException();
		}

		static Func<object, string> CompileDataBinder(Type type, string expr)
		{
			var param = Expression.Parameter(typeof(object), "model");
			Expression body = Expression.Convert(param, type);
			var members = expr.Split('.');
			for (int i = 0; i < members.Length; i++)
			{
				body = Expression.PropertyOrField(body, members[i]);
			}
			var method = typeof(Convert).GetMethod("ToString", BindingFlags.Static | BindingFlags.Public,
				null, new Type[] { body.Type }, null);
			if (method == null)
			{
				method = typeof(Convert).GetMethod("ToString", BindingFlags.Static | BindingFlags.Public,
					null, new Type[] { typeof(object) }, null);
				body = Expression.Call(method, Expression.Convert(body, typeof(object)));
			}
			else
			{
				body = Expression.Call(method, body);
			}

			return Expression.Lambda<Func<object, string>>(body, param).Compile();
		}

		static Func<TModel, TProp> CompileDataBinder<TModel, TProp>(string expression)
		{
			var propNames = expression.Split('.');

			var model = Expression.Parameter(typeof(TModel), "model");

			Expression body = model;
			foreach (string propName in propNames.Skip(1))
				body = Expression.Property(body, propName);
			//Debug.WriteLine(prop);

			if (body.Type != typeof(TProp))
				body = Expression.Convert(body, typeof(TProp));

			Func<TModel, TProp> func = Expression.Lambda<Func<TModel, TProp>>(body, model).Compile();
			//TODO: cache funcs
			return func;
		}
	}
}