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

using ServiceStack.Web;

namespace ServiceStack.FluentValidation {
	using System.Collections.Generic;
	using Internal;

	/// <summary>
	/// Validation context
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public partial class ValidationContext<T> : IValidationContext {
		//Migration: Needs to be injected in Clone(), CloneForChildValidator() + all new ValidationContext()
		public IRequest Request { get; set; }
	}

	/// <summary>
	/// Validation context
	/// </summary>
	public partial interface IValidationContext {

        IRequest Request { get; set; }
	}
}