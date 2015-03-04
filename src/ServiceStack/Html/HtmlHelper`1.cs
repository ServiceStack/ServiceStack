
namespace ServiceStack.Html
{
	public class HtmlHelper<TModel> : HtmlHelper
	{
		// TODO: ServiceStack.Html private viewData is never used; this can probably be removed since base.ViewData is used by public property.
		//private ViewDataDictionary<TModel> viewData;
		public new ViewDataDictionary<TModel> ViewData
		{
			get 
			{ 
				return base.ViewData as ViewDataDictionary<TModel> 
					?? new ViewDataDictionary<TModel>((TModel)base.ViewData.Model); 
			}
		}
	}
}
