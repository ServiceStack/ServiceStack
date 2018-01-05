// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;

namespace ServiceStack
{
    public class AttributeBase : Attribute
    {
        public AttributeBase()
        {
            this.typeId = Guid.NewGuid();
        }

        protected readonly Guid typeId; //Hack required to give Attributes unique identity
    }
}