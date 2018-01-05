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
using ServiceStack.Web;

namespace ServiceStack.FluentValidation
{
    /// <summary>
    /// Base class for entity validator classes.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated</typeparam>
    public abstract partial class AbstractValidator<T> : IRequiresRequest
    {
        public virtual IRequest Request { get; set; }

        public virtual IServiceGateway Gateway => HostContext.AppHost.GetServiceGateway(Request);

        /// <summary>
        /// Defines a RuleSet that can be used to provide specific validation rules for specific HTTP methods (GET, POST...)
        /// </summary>
        /// <param name="appliesTo">The HTTP methods where this rule set should be used.</param>
        /// <param name="action">Action that encapuslates the rules in the ruleset.</param>
        public void RuleSet(ApplyTo appliesTo, Action action)
        {
            var httpMethods = appliesTo.ToString().Split(',')
                .Map(x => x.Trim().ToUpper());

            foreach (var httpMethod in httpMethods)
            {
                RuleSet(httpMethod, action);
            }
        }
    }
}
