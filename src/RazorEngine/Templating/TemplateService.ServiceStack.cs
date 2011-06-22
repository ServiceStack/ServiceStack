using System;
using System.Collections.Generic;
using ServiceStack.Common;

namespace RazorEngine.Templating
{
	public abstract partial class TemplateBase
	{
		private Dictionary<string, Action> sections;
		public Dictionary<string, Action> Sections
		{
			get { return sections ?? (sections = new Dictionary<string, Action>()); }
		}

		[ThreadStatic] private static string childBody;
		[ThreadStatic] private static IRazorTemplate childTemplate;

		public IRazorTemplate ChildTemplate
		{
			get { return childTemplate; }
			set
			{
				childTemplate = value;
				childBody = childTemplate.Result;
			}
		}

		public void WriteSection(string name, Action contents)
		{
			if (name == null || contents == null)
				return;

			Sections[name] = contents;
		}

		public string RenderBody()
		{
			return childBody;
		}

		public string RenderSection(string sectionName)
		{
			return RenderSection(sectionName, false);
		}

		public string RenderSection(string sectionName, bool required)
		{
			if (sectionName == null)
				throw new ArgumentNullException("sectionName");

			if (childTemplate == null) return null;

			Action renderSection;
			childTemplate.Sections.TryGetValue(sectionName, out renderSection);

			if (renderSection == null)
			{
				if (required)
					throw new ApplicationException("Section not defined: " + sectionName);
				return null;
			}

			renderSection();

			return null;
		}
	}

	public partial class TemplateService
	{
		/// <summary>
		/// Runs and returns the template with the specified name.
		/// </summary>
		public IRazorTemplate ExecuteTemplate<T>(T model, string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("The named of the cached template is required.");

			ITemplate instance;
			if (!templateCache.TryGetValue(name, out instance))
				throw new ArgumentException("No compiled template exists with the specified name.");

			SetService(instance, this);
			SetModel(instance, model);
			instance.Execute();

			var razorTemplate = (IRazorTemplate) instance;

			if (!razorTemplate.Layout.IsNullOrEmpty())
			{
				var layoutTemplate = GetTemplate(razorTemplate.Layout);
				if (layoutTemplate == null)
					throw new ArgumentException(
						"No template exists with the specified Layout: " + razorTemplate.Layout);

				layoutTemplate.ChildTemplate = razorTemplate;
				SetService(layoutTemplate, this);
				SetModel(layoutTemplate, model);
				layoutTemplate.Execute();

				return layoutTemplate;
			}

			return razorTemplate;
		}

		public IRazorTemplate GetTemplate(string name)
		{
			ITemplate instance;
			templateCache.TryGetValue(name, out instance);
			return instance as IRazorTemplate;
		}

	}
}