﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;

namespace ServiceStack.Html
{
    public class SelectList : MultiSelectList
    {
        public SelectList(IEnumerable items)
            : this(items, null /* selectedValue */)
        {
        }

        public SelectList(IEnumerable items, object selectedValue)
            : this(items, null /* dataValuefield */, null /* dataTextField */, selectedValue)
        {
        }

        public SelectList(IEnumerable items, string dataValueField, string dataTextField)
            : this(items, dataValueField, dataTextField, null /* selectedValue */)
        {
        }

        public SelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue)
            : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue))
        {
            SelectedValue = selectedValue;
        }

        public object SelectedValue { get; private set; }

        private static IEnumerable ToEnumerable(object selectedValue)
        {
            return (selectedValue != null) ? new object[] { selectedValue } : null;
        }
    }
}
