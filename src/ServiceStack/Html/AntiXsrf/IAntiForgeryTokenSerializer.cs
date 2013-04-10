// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace ServiceStack.Html.AntiXsrf
{
    // Abstracts out the serialization process for an anti-forgery token
    internal interface IAntiForgeryTokenSerializer
    {
        AntiForgeryToken Deserialize(string serializedToken);
        string Serialize(AntiForgeryToken token);
    }
}
