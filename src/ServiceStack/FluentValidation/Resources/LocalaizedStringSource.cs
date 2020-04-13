using System;
using System.Linq.Expressions;
using ServiceStack.FluentValidation.Internal;

namespace ServiceStack.FluentValidation.Resources
{
    public partial class LocalizedStringSource
    {
        /// <summary>
        /// Creates a LocalizedStringSource from an expression: () => MyResources.SomeResourceName
        /// </summary>
        /// <param name="expression">The expression </param>
        /// <param name="resourceProviderSelectionStrategy">Strategy used to construct the resource accessor</param>
        /// <returns>Error message source</returns>
        [Obsolete]
        public static IStringSource CreateFromExpression(Expression<Func<string>> expression) {
            if (expression.Body is ConstantExpression constant) {
                return new StaticStringSource((string)constant.Value);
            }

            var member = expression.GetMember();

            if (member == null) {
                throw new InvalidOperationException("Only MemberExpressions an be passed to BuildResourceAccessor, eg () => Messages.MyResource");
            }

            var resourceType = member.DeclaringType;
            var resourceName = member.Name;

            return new LocalizedStringSource(resourceType, resourceName);
        }        
    }
}