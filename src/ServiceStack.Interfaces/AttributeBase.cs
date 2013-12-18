// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;

namespace ServiceStack
{
    public class AttributeBase : Attribute
    {
#if !(NETFX_CORE || WINDOWS_PHONE || SILVERLIGHT || PCL)
        /// <summary>
        /// Required when using a TypeDescriptor to make it unique
        /// </summary>
        public override object TypeId
        {
            get { return this; }
        }
#endif
        
    }
}