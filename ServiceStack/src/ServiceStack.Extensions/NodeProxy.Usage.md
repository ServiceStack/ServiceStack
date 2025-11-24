# NodeProxy Cache Configuration

## Overview

The `NodeProxy` class now includes intelligent in-memory caching with configurable size limits to optimize performance when proxying to Node.js applications.

## Default Settings

- **MaxFileSizeBytes**: 5 MB (individual file limit)
- **MaxCacheSizeBytes**: 100 MB (total cache size limit)
- **LRU Eviction**: Automatically evicts least recently used entries when cache is full

## Configuration Examples

### Basic Usage (Default Settings)

```csharp
var proxy = new NodeProxy("http://localhost:3000");
// Uses default: 5 MB per file, 100 MB total cache
```

### Custom Size Limits

```csharp
var proxy = new NodeProxy("http://localhost:3000")
{
    MaxFileSizeBytes = 10 * 1024 * 1024,    // 10 MB per file
    MaxCacheSizeBytes = 200 * 1024 * 1024,  // 200 MB total cache
    Verbose = true                           // Enable cache logging
};
```

### Conservative Settings (Low Memory)

```csharp
var proxy = new NodeProxy("http://localhost:3000")
{
    MaxFileSizeBytes = 1 * 1024 * 1024,     // 1 MB per file
    MaxCacheSizeBytes = 25 * 1024 * 1024,   // 25 MB total cache
};
```

### Disable Caching for Large Files

```csharp
var proxy = new NodeProxy("http://localhost:3000")
{
    MaxFileSizeBytes = 512 * 1024,          // 512 KB per file (skip large bundles)
    MaxCacheSizeBytes = 50 * 1024 * 1024,   // 50 MB total cache
};
```

## Cache Statistics

Monitor cache performance:

```csharp
var stats = proxy.GetCacheStats();
Console.WriteLine($"Cache Hits: {stats.hits}");
Console.WriteLine($"Cache Misses: {stats.misses}");
Console.WriteLine($"Hit Rate: {stats.hitRate:P2}");
Console.WriteLine($"Entries: {stats.entryCount}");
Console.WriteLine($"Total Size: {stats.totalSize / 1024 / 1024} MB");
```

## Cache Management

### Clear All Cache

```csharp
proxy.ClearCache();
```

### Remove Specific Entry

```csharp
proxy.RemoveCacheEntry("/static/bundle.js");
```

### Clear via HTTP Request

```
GET /?clear=all          // Clear entire cache
GET /path/to/file?clear  // Clear specific file
```

## How It Works

1. **Size Check**: Files larger than `MaxFileSizeBytes` are never cached
2. **LRU Eviction**: When adding a new entry would exceed `MaxCacheSizeBytes`, the least recently used entries are evicted
3. **Access Tracking**: Each cache hit updates the `LastAccessTime` for LRU tracking
4. **Thread-Safe**: All cache operations are thread-safe using locks and interlocked operations

## Recommended Settings by Scenario

### Production (High Traffic)
- MaxFileSizeBytes: 5-10 MB
- MaxCacheSizeBytes: 200-500 MB

### Development
- MaxFileSizeBytes: 5 MB
- MaxCacheSizeBytes: 100 MB

### Low Memory Environments
- MaxFileSizeBytes: 1 MB
- MaxCacheSizeBytes: 25-50 MB

### Disable Caching
```csharp
var proxy = new NodeProxy("http://localhost:3000")
{
    ShouldCache = (context) => false  // Never cache
};
```

