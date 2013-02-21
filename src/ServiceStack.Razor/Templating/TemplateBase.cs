using System.Collections.Generic;
using System.Dynamic;
using System.Web;
using ServiceStack.Html;
using System;
using System.IO;
using System.Text;
using ServiceStack.Razor.Compilation;

namespace ServiceStack.Razor.Templating
{
    /// <summary>
    /// Defines an attribute that marks the presence of a dynamic model in a template.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HasDynamicModelAttribute : Attribute {}

    public abstract partial class TemplateBase
    {
        [ThreadStatic]
        public static dynamic ViewBag;

        private Dictionary<string, Action> sections;
        public Dictionary<string, Action> Sections
        {
            get { return sections ?? (sections = new Dictionary<string, Action>()); }
        }

        private static string childBody;
        private static IRazorTemplate childTemplate;

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

        public MvcHtmlString RenderBody()
        {
            return MvcHtmlString.Create(childBody);
        }

        public MvcHtmlString RenderSection(string sectionName)
        {
            return RenderSection(sectionName, false);
        }

        public MvcHtmlString RenderSection(string sectionName, bool required)
        {
            if (sectionName == null)
                throw new ArgumentNullException("sectionName");

            Action renderSection;
            this.Sections.TryGetValue(sectionName, out renderSection);

            if (renderSection == null)
            {
                if (childTemplate == null) return null;

                childTemplate.Sections.TryGetValue(sectionName, out renderSection);

                if (renderSection == null)
                {
                    if (required)
                        throw new ApplicationException("Section not defined: " + sectionName);
                    return null;
                }
            }

            renderSection();

            return null;
        }
    }

    /// <summary>
    /// Provides a base implementation of a template with a model.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public abstract class TemplateBase<TModel> : TemplateBase, ITemplate<TModel>
    {
        private object model;

        /// <summary>
        /// Gets whether this template uses a dynamic model.
        /// </summary>
        protected bool HasDynamicModel { get; private set; }
        
        protected TemplateBase()
        {
            HasDynamicModel = GetType()
                .IsDefined(typeof(HasDynamicModelAttribute), true);
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public TModel Model
        {
            get
            {
                if (HasDynamicModel
                    && !typeof(TModel).IsAssignableFrom(typeof(DynamicObject))
                    && (model is DynamicObject || model is ExpandoObject))
                {
                    TModel m = (dynamic)model;
                    return m;
                }
                return model != null ? (TModel)model : default(TModel);
            }
            set
            {
                if (HasDynamicModel 
                    && !(value is DynamicObject) 
                    && !(value is ExpandoObject) 
                    && !(typeof(TModel).IsInterface))
                {
                    model = new RazorDynamicObject { Model = value };
                }
                else
                {
                    model = value;                    
                }
            }
        }

        public override void SetModel(object model)
        {
            if (model is TModel)
            {
                this.Model = (TModel)model;
            }
        }
    }

	/// <summary>
    /// Provides a base implementation of a template.
    /// </summary>
	public abstract partial class TemplateBase : ITemplate
    {
        public virtual HtmlHelper HtmlHelper { get { return null; } }
        public virtual void SetState(HtmlHelper htmlHelper){}
	    public abstract void SetModel(object model);
        
        [ThreadStatic]
        private static StringBuilder builder;

        /// <summary>
        /// Gets the string builder used to output the template result.
        /// </summary>
        public StringBuilder Builder
        {
            get { return builder ?? (builder = new StringBuilder()); }
        }

	    /// <summary>
        /// Gets the last result of the template.
        /// </summary>
        public string Result { get { return Builder.ToString(); } }

        /// <summary>
        /// Gets or sets the template service.
        /// </summary>
        public TemplateService Service { get; set; }

        /// <summary>
        /// Clears the last result of the template.
        /// </summary>
        public void Clear()
        {
            Builder.Clear();
        }

        /// <summary>
        /// Executes the compiled template.
        /// </summary>
        public virtual void Execute() { }

        /// <summary>
        /// Includes the template with the specified name.
        /// </summary>
        /// <param name="name">The template name.</param>
        /// <returns>The result of the template with the specified name.</returns>
        public virtual string Include(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("The name of the template to include is required.");

            if (Service == null)
                throw new InvalidOperationException("No template service has been set of this template.");

            return Service.ResolveTemplate(name);
        }

        /// <summary>
        /// Includes the template with the specified name.
        /// </summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="name">The template name.</param>
        /// <param name="model">The model required by the template.</param>
        /// <returns>The result of the template with the specified name.</returns>
        public virtual string Include<T>(string name, T model)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("The name of the template to include is required.");

            if (Service == null)
                throw new InvalidOperationException("No template service has been set of this template.");

            return Service.ResolveTemplate(name, model);
        }

        /// <summary>
        /// Writes the specified object to the template result.
        /// </summary>
        /// <param name="object">The object to write.</param>
        public void Write(object @object)
        {
            if (@object == null)
                return;

            if (@object is MvcHtmlString)
            {
                Builder.Append(@object);
            }
            else
            {
                var strValue = Convert.ToString(@object);
                Builder.Append(HttpUtility.HtmlEncode(strValue));
            }
        }

        /// <summary>
        /// Writes the specified string to the template result.
        /// </summary>
        /// <param name="string">The string to write.</param>
        public void WriteLiteral(string @string)
        {
            if (@string == null)
                return;

            Builder.Append(@string);
        }

        /// <summary>
        /// Writes a string literal to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="literal">The literal to be written.</param>
        public static void WriteLiteralTo(TextWriter writer, string literal)
        {
            if (literal == null)
                return;

            writer.Write(literal);
        }

        /// <summary>
        /// Writes the specified object to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="obj">The object to be written.</param>
        public static void WriteTo(TextWriter writer, object obj)
        {
            if (obj == null)
                return;

            writer.Write(obj);
        }

        public int Counter { get; set; }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }

    public static class TemplateExtensions
    {
        public static ITemplate CloneTemplate(this ITemplate template)
        {
            if (template == null) 
                return null;
            
            var clone = (ITemplate) template.Clone();
            return clone;
        }
    }
}