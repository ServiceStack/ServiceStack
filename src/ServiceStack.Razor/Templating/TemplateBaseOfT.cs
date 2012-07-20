using System.Dynamic;
using ServiceStack.Razor.Compilation;

namespace ServiceStack.Razor.Templating
{
	/// <summary>
    /// Provides a base implementation of a template with a model.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public abstract class TemplateBase<TModel> : TemplateBase, ITemplate<TModel>
    {
        private object model;

        /// <summary>
        /// Initialises a new instance of <see cref="TemplateBase{TModel}"/>.
        /// </summary>
        protected TemplateBase()
        {
            HasDynamicModel = GetType()
                .IsDefined(typeof(HasDynamicModelAttribute), true);
        }

        /// <summary>
        /// Gets whether this template uses a dynamic model.
        /// </summary>
        protected bool HasDynamicModel { get; private set; }

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
                if (HasDynamicModel && !(value is DynamicObject) && !(value is ExpandoObject))
                    model = new RazorDynamicObject { Model = value };
                else
                    model = value;
            }
        }
    }
}