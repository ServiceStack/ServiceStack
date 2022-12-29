// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using Amazon.DynamoDBv2;

namespace ServiceStack.Aws.DynamoDb
{
    public interface IPocoDynamoFactory
    {
        IPocoDynamo GetClient();
    }

    public class PocoDynamoFactory : IPocoDynamoFactory
    {
        private readonly Func<IAmazonDynamoDB> factory;

        public PocoDynamoFactory(Func<IAmazonDynamoDB> factory)
        {
            this.factory = factory;
        }

        public IPocoDynamo GetClient()
        {
            return new PocoDynamo(factory());
        }
    }
}