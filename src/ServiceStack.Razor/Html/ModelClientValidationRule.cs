// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServiceStack.Html
{
#if NET_4_0
    [TypeForwardedFrom("System.Web.Mvc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
#endif
    public class ModelClientValidationRule
    {
        private readonly Dictionary<string, object> _validationParameters = new Dictionary<string, object>();
        private string _validationType;

        public string ErrorMessage { get; set; }

        public IDictionary<string, object> ValidationParameters
        {
            get { return _validationParameters; }
        }

        public string ValidationType
        {
            get { return _validationType ?? String.Empty; }
            set { _validationType = value; }
        }
    }
}
