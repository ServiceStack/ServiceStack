#if !NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ServiceStack.Aws
{
    public static class NetCoreAwsExtensions
    {
        public static DescribeTableResponse DescribeTable(this IAmazonDynamoDB client, DescribeTableRequest request)
        {
            return client.DescribeTableAsync(request).GetResult();
        }

        public static ListTablesResponse ListTables(this IAmazonDynamoDB client, ListTablesRequest request)
        {
            return client.ListTablesAsync(request).GetResult();
        }

        public static CreateTableResponse CreateTable(this IAmazonDynamoDB client, CreateTableRequest request)
        {
            return client.CreateTableAsync(request).GetResult();
        }

        public static DeleteTableResponse DeleteTable(this IAmazonDynamoDB client, DeleteTableRequest request)
        {
            return client.DeleteTableAsync(request).GetResult();
        }

        public static GetItemResponse GetItem(this IAmazonDynamoDB client, GetItemRequest request)
        {
            return client.GetItemAsync(request).GetResult();
        }

        public static PutItemResponse PutItem(this IAmazonDynamoDB client, PutItemRequest request)
        {
            return client.PutItemAsync(request).GetResult();
        }

        public static UpdateItemResponse UpdateItem(this IAmazonDynamoDB client, UpdateItemRequest request)
        {
            return client.UpdateItemAsync(request).GetResult();
        }

        public static DeleteItemResponse DeleteItem(this IAmazonDynamoDB client, DeleteItemRequest request)
        {
            return client.DeleteItemAsync(request).GetResult();
        }

        public static BatchGetItemResponse BatchGetItem(this IAmazonDynamoDB client, BatchGetItemRequest request)
        {
            return client.BatchGetItemAsync(request).GetResult();
        }

        public static BatchWriteItemResponse BatchWriteItem(this IAmazonDynamoDB client, BatchWriteItemRequest request)
        {
            return client.BatchWriteItemAsync(request).GetResult();
        }

        public static ScanResponse Scan(this IAmazonDynamoDB client, ScanRequest request)
        {
            return client.ScanAsync(request).GetResult();
        }

        public static QueryResponse Query(this IAmazonDynamoDB client, QueryRequest request)
        {
            return client.QueryAsync(request).GetResult();
        }

        public static ListBucketsResponse ListBuckets(this IAmazonS3 client, ListBucketsRequest request)
        {
            return client.ListBucketsAsync(request).GetResult();
        }

        public static GetObjectMetadataResponse GetObjectMetadata(this IAmazonS3 client, GetObjectMetadataRequest request)
        {
            return client.GetObjectMetadataAsync(request).GetResult();
        }

        public static GetObjectResponse GetObject(this IAmazonS3 client, GetObjectRequest request)
        {
            return client.GetObjectAsync(request).GetResult();
        }

        public static PutObjectResponse PutObject(this IAmazonS3 client, PutObjectRequest request)
        {
            return client.PutObjectAsync(request).GetResult();
        }

        public static DeleteObjectResponse DeleteObject(this IAmazonS3 client, DeleteObjectRequest request)
        {
            return client.DeleteObjectAsync(request).GetResult();
        }

        public static DeleteObjectsResponse DeleteObjects(this IAmazonS3 client, DeleteObjectsRequest request)
        {
            return client.DeleteObjectsAsync(request).GetResult();
        }

        public static CopyObjectResponse CopyObject(this IAmazonS3 client, CopyObjectRequest request)
        {
            return client.CopyObjectAsync(request).GetResult();
        }

        public static ListObjectsResponse ListObjects(this IAmazonS3 client, ListObjectsRequest request)
        {
            return client.ListObjectsAsync(request).GetResult();
        }

        public static GetBucketLocationResponse GetBucketLocation(this IAmazonS3 client, GetBucketLocationRequest request)
        {
            return client.GetBucketLocationAsync(request).GetResult();
        }

        public static DeleteBucketResponse DeleteBucket(this IAmazonS3 client, DeleteBucketRequest request)
        {
            return client.DeleteBucketAsync(request).GetResult();
        }

        public static PutBucketResponse PutBucket(this IAmazonS3 client, PutBucketRequest request)
        {
            return client.PutBucketAsync(request).GetResult();
        }

        public static GetQueueUrlResponse GetQueueUrl(this IAmazonSQS client, GetQueueUrlRequest request)
        {
            return client.GetQueueUrlAsync(request).GetResult();
        }

        public static GetQueueAttributesResponse GetQueueAttributes(this IAmazonSQS client, GetQueueAttributesRequest request)
        {
            return client.GetQueueAttributesAsync(request).GetResult();
        }

        public static SetQueueAttributesResponse SetQueueAttributes(this IAmazonSQS client, SetQueueAttributesRequest request)
        {
            return client.SetQueueAttributesAsync(request).GetResult();
        }

        public static CreateQueueResponse CreateQueue(this IAmazonSQS client, CreateQueueRequest request)
        {
            return client.CreateQueueAsync(request).GetResult();
        }

        public static DeleteQueueResponse DeleteQueue(this IAmazonSQS client, DeleteQueueRequest request)
        {
            return client.DeleteQueueAsync(request).GetResult();
        }

        public static ListQueuesResponse ListQueues(this IAmazonSQS client, ListQueuesRequest request)
        {
            return client.ListQueuesAsync(request).GetResult();
        }

        public static PurgeQueueResponse PurgeQueue(this IAmazonSQS client, PurgeQueueRequest request)
        {
            return client.PurgeQueueAsync(request).GetResult();
        }

        public static SendMessageResponse SendMessage(this IAmazonSQS client, SendMessageRequest request)
        {
            return client.SendMessageAsync(request).GetResult();
        }

        public static SendMessageBatchResponse SendMessageBatch(this IAmazonSQS client, SendMessageBatchRequest request)
        {
            return client.SendMessageBatchAsync(request).GetResult();
        }

        public static ReceiveMessageResponse ReceiveMessage(this IAmazonSQS client, ReceiveMessageRequest request)
        {
            return client.ReceiveMessageAsync(request).GetResult();
        }

        public static DeleteMessageResponse DeleteMessage(this IAmazonSQS client, DeleteMessageRequest request)
        {
            return client.DeleteMessageAsync(request).GetResult();
        }

        public static DeleteMessageBatchResponse DeleteMessageBatch(this IAmazonSQS client, DeleteMessageBatchRequest request)
        {
            return client.DeleteMessageBatchAsync(request).GetResult();
        }

        public static ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(this IAmazonSQS client, ChangeMessageVisibilityBatchRequest request)
        {
            return client.ChangeMessageVisibilityBatchAsync(request).GetResult();
        }

        public static ChangeMessageVisibilityResponse ChangeMessageVisibility(this IAmazonSQS client, ChangeMessageVisibilityRequest request)
        {
            return client.ChangeMessageVisibilityAsync(request).GetResult();
        }

        public static void WriteResponseStreamToFile(this GetObjectResponse response, string filePath, bool append)
        {
            response.WriteResponseStreamToFileAsync(filePath, append, default(CancellationToken)).Wait();
        }
    }
}

#endif