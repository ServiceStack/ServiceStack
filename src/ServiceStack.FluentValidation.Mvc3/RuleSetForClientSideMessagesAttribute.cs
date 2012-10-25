namespace FluentValidation.Mvc {
	using System.Web;
	using System.Web.Mvc;

	/// <summary>
	/// Specifies which ruleset should be used when deciding which validators should be used to generate client-side messages.
	/// </summary>
	public class RuleSetForClientSideMessagesAttribute : ActionFilterAttribute {
		private const string key = "_FV_ClientSideRuleSet";
		string[] ruleSets;

		public RuleSetForClientSideMessagesAttribute(params string[] ruleSets) {
			this.ruleSets = ruleSets;
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			filterContext.HttpContext.Items[key] = ruleSets;
		}

		public static string[] GetRuleSetsForClientValidation(HttpContextBase context) {
			return context.Items[key] as string[] ?? new string[] { null };
		}
	}
}