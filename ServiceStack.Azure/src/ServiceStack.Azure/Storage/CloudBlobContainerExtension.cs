using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServiceStack.Azure.Storage;

public static class CloudBlobContainerExtension
{
#if !NETFRAMEWORK
    public static IEnumerable<IListBlobItem> ListBlobs(this CloudBlobContainer container, string? prefix = null,
        bool useFlatBlobListing = false)
    {
        BlobContinuationToken? continuationToken = null;
        List<IListBlobItem> blobs = [];
        do
        {
            var blobResults = container.ListBlobsSegmentedAsync(prefix, useFlatBlobListing,
                    BlobListingDetails.None, 100, continuationToken, null, null)
                .Result;
            continuationToken = blobResults.ContinuationToken;

            blobs.AddRange(blobResults.Results);
        } while (continuationToken != null);

        return blobs;
    }

    public static void CreateIfNotExists(this CloudBlobContainer container)
    {
        container.CreateIfNotExistsAsync().Wait();
    }

    public static void DeleteIfExists(this CloudBlobContainer container)
    {
        container.DeleteIfExistsAsync().Wait();
    }

    public static void CreateOrReplace(this CloudAppendBlob blob, AccessCondition? condition, BlobRequestOptions? options, OperationContext? operationContext)
    {
        blob.CreateOrReplaceAsync(condition, options, operationContext).Wait();
    }

    public static void Delete(this ICloudBlob blob)
    {
        blob.DeleteAsync().Wait();
    }

    public static void DeleteIfExists(this ICloudBlob blob)
    {
        blob.DeleteIfExistsAsync().Wait();
    }

    public static void UploadText(this CloudBlockBlob blob, string content)
    {
        blob.UploadTextAsync(content).Wait();
    }

    public static void UploadFromStream(this CloudBlockBlob blob, Stream stream)
    {
        blob.UploadFromStreamAsync(stream).Wait();
    }

    public static void AppendText(this CloudAppendBlob blob, string content)
    {
        blob.AppendTextAsync(content).Wait();
    }

    public static void AppendFromStream(this CloudAppendBlob blob, Stream stream)
    {
        blob.AppendFromStreamAsync(stream).Wait();
    }

    public static Stream OpenRead(this CloudBlob blob)
    {
        return blob.OpenReadAsync().Result;
    }

    public static bool Exists(this CloudBlob blob)
    {
        return blob.ExistsAsync().GetResult();
    }

    public static TableResult Execute(this CloudTable table, TableOperation op)
    {
        return table.ExecuteAsync(op).GetResult();
    }

    public static bool CreateIfNotExists(this CloudTable table)
    {
        return table.CreateIfNotExistsAsync().GetResult();
    }

    public static IEnumerable<TElement> ExecuteQuery<TElement>(this CloudTable table, TableQuery<TElement> query) where TElement : ITableEntity, new()
    {
        TableContinuationToken continuationToken = null;
        var elements = new List<TElement>();

        do
        {
            var result = table.ExecuteQuerySegmentedAsync(query, continuationToken).GetResult();
            continuationToken = result.ContinuationToken;
            elements.AddRange(result.Results);
        } while (continuationToken != null);

        return elements;
    }
#endif
        
    public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, CancellationToken token = default) 
        where T : ITableEntity, new()
    {
        var runningQuery = new TableQuery<T> {
            FilterString = query.FilterString,
            SelectColumns = query.SelectColumns
        };

        var items = new List<T>();
        TableContinuationToken? tct = null;

        do
        {
            runningQuery.TakeCount = query.TakeCount - items.Count;

            // ReSharper disable once MethodSupportsCancellation
            var seg = await table.ExecuteQuerySegmentedAsync(runningQuery, tct);
            tct = seg.ContinuationToken;
            items.AddRange(seg);

        } while (tct != null && !token.IsCancellationRequested && (query.TakeCount == null || items.Count < query.TakeCount.Value));

        return items;
    }

    public static Task<TableResult> ExecuteAsync(this CloudTable table, TableOperation operation, CancellationToken token)
    {
        return table.ExecuteAsync(operation, requestOptions:null, operationContext:null, token);
    }

    public static bool HasStatus(this StorageException ex, HttpStatusCode code)
    {
        if (ex.RequestInformation != null)
        {
            return ex.RequestInformation.HttpStatusCode == (int) code;
        }
        return (ex.InnerException ?? ex).HasStatus(code);
    }
        
}