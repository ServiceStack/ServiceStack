#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at https://github.com/jeremyskinner/FluentValidation
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.FluentValidation.Internal;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.FluentValidation
{
    public interface IDefaultValidator {}
    public class DefaultValidator<T> : AbstractValidator<T>, IDefaultValidator {}

    public interface IServiceStackValidator
    {
        void RemovePropertyRules(Func<PropertyRule, bool> where);
    }
    
    /// <summary>
    /// Base class for entity validator classes.
    /// </summary>
    public abstract partial class AbstractValidator<T> : IRequiresRequest, IHasTypeValidators, IServiceStackValidator
    {
        /// <summary>
        /// Validators are auto-wired transient instances
        /// </summary>
        protected AbstractValidator()
        {
            var appHost = HostContext.AppHost;
            if (appHost == null) //Unit tests or stand-alone usage
                return;
            
            if (ServiceStack.Validators.TypePropertyRulesMap.TryGetValue(typeof(T), out var dtoRules))
            {
                foreach (var rule in dtoRules)
                {
                    Rules.Add(rule);
                }
            }

            var source = appHost.TryResolve<IValidationSource>();
            if (source != null)
            {
                var sourceRules = source.GetValidationRules(typeof(T)).ToList();
                if (!sourceRules.IsEmpty())
                {
                    var typeRules = new List<IValidationRule>();
                    foreach (var entry in sourceRules)
                    {
                        var isTypeValidator = entry.Key == null;
                        if (isTypeValidator)
                        {
                            ServiceStack.Validators.AddTypeValidator(TypeValidators, entry.Value);
                            continue;
                        }
                        
                        var pi = typeof(T).GetProperty(entry.Key);
                        if (pi != null)
                        {
                            var propRule = ServiceStack.Validators.CreatePropertyRule(typeof(T), pi);
                            typeRules.Add(propRule);
                            var propValidators = (List<IPropertyValidator>) propRule.Validators;
                            propValidators.AddRule(pi, entry.Value);
                        }
                    }

                    foreach (var propertyValidator in typeRules)
                    {
                        Rules.Add(propertyValidator);
                    }
                }
            }
        }

        public List<ITypeValidator> TypeValidators { get; } = new List<ITypeValidator>();

        public virtual IRequest Request { get; set; }

        public virtual IServiceGateway Gateway => HostContext.AppHost.GetServiceGateway(Request);

        /// <summary>
        /// Defines a RuleSet that can be used to provide specific validation rules for specific HTTP methods (GET, POST...)
        /// </summary>
        /// <param name="appliesTo">The HTTP methods where this rule set should be used.</param>
        /// <param name="action">Action that encapsulates the rules in the ruleset.</param>
        public void RuleSet(ApplyTo appliesTo, Action action)
        {
            var httpMethods = appliesTo.ToString().Split(',')
                .Map(x => x.Trim().ToUpper());

            foreach (var httpMethod in httpMethods)
            {
                RuleSet(httpMethod, action);
            }
        }

        //TODO: [SYNC] Call from AbstractValidator.Validate/ValidateAsync(context)
        private void Init(IValidationContext context)
        {
            if (this.Request == null)
                this.Request = context.Request;
        }

        public void RemovePropertyRules(Func<PropertyRule, bool> where)
        {
            var rulesToRemove = Rules.OfType<PropertyRule>().Where(where).ToList();
            foreach (var validationRule in rulesToRemove)
            {
                Rules.Remove(validationRule);
            }
        }
    }
}
