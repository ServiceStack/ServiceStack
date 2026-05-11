using System;
using System.Collections.Generic;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ServiceStack.Azure.Storage;

internal static class AzureBlobVirtualFilesHelpers
{
    internal static string? SanitizePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;
        var sanitized = filePath[0] == '/' ? filePath.Substring(1) : filePath;
        return sanitized.Replace('\\', '/');
    }

    internal static string? GetDirPath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;
        var lastSep = filePath.LastIndexOf('/');
        return lastSep >= 0 ? filePath.Substring(0, lastSep) : null;
    }

    internal static IEnumerable<BlobItem> ListBlobs(BlobContainerClient container, string? prefix) =>
        container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None);

    internal static IEnumerable<BlobHierarchyItem> ListBlobsByHierarchy(BlobContainerClient container, string? prefix) =>
        container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", prefix, CancellationToken.None);

    // createdOn: omit for block blobs (default); pass item.Properties.CreatedOn for append blobs
    // so LastModified uses ms-precision x-ms-creation-time instead of s-precision Last-Modified.
    internal static BlobProperties MakeBlobProperties(BlobItem item, DateTimeOffset createdOn = default) =>
        BlobsModelFactory.BlobProperties(
            lastModified: item.Properties.LastModified ?? DateTimeOffset.MinValue,
            contentLength: item.Properties.ContentLength ?? 0,
            contentType: item.Properties.ContentType,
            createdOn: createdOn);
}
