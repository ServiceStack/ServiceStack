// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Html
{
    public class FormContext
    {
        private readonly Dictionary<string, FieldValidationMetadata> _fieldValidators = new Dictionary<string, FieldValidationMetadata>();
        private readonly Dictionary<string, bool> _renderedFields = new Dictionary<string, bool>();

        public IDictionary<string, FieldValidationMetadata> FieldValidators
        {
            get { return _fieldValidators; }
        }

        public string FormId { get; set; }

        public bool ReplaceValidationSummary { get; set; }

        public string ValidationSummaryId { get; set; }

        public string GetJsonValidationMetadata()
        {
            SortedDictionary<string, object> dict = new SortedDictionary<string, object>()
            {
                { "Fields", FieldValidators.Values },
                { "FormId", FormId }
            };
            if (!String.IsNullOrEmpty(ValidationSummaryId)) {
                dict["ValidationSummaryId"] = ValidationSummaryId;
            }
            dict["ReplaceValidationSummary"] = ReplaceValidationSummary;

            return JsonSerializer.SerializeToString(dict);
        }

        public FieldValidationMetadata GetValidationMetadataForField(string fieldName)
        {
            return GetValidationMetadataForField(fieldName, false /* createIfNotFound */);
        }

        public FieldValidationMetadata GetValidationMetadataForField(string fieldName, bool createIfNotFound)
        {
            if (String.IsNullOrEmpty(fieldName)) {
                throw Error.ParameterCannotBeNullOrEmpty("fieldName");
            }

            FieldValidationMetadata metadata;
            if (!FieldValidators.TryGetValue(fieldName, out metadata)) {
                if (createIfNotFound) {
                    metadata = new FieldValidationMetadata()
                    {
                        FieldName = fieldName
                    };
                    FieldValidators[fieldName] = metadata;
                }
            }
            return metadata;
        }

        public bool RenderedField(string fieldName)
        {
            bool result;
            _renderedFields.TryGetValue(fieldName, out result);
            return result;
        }

        public void RenderedField(string fieldName, bool value)
        {
            _renderedFields[fieldName] = value;
        }
    }
}
