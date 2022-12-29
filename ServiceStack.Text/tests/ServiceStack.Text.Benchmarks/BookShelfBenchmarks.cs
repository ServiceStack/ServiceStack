using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace ServiceStack.Text.Benchmarks
{
    [DataContract]
    public class BookShelf
    {
        [DataMember]
        public List<Book> Books
        {
            get;
            set;
        }

        [DataMember]
        private string Secret;

        public BookShelf(string secret)
        {
            Secret = secret;
        }

        public BookShelf() // Parameterless ctor is needed for every protocol buffer class during deserialization
        { }
    }

    [DataContract]
    public class Book
    {
        [DataMember]
        public string Title;

        [DataMember]
        public int Id;
    }
    
    public static class BookUtils
    {
        public static BookShelf Data(int nToCreate)
        {
            var lret = new BookShelf("private member value")
            {
                Books = Enumerable.Range(1, nToCreate).Select(i => new Book { Id = i, Title = $"Book {i}" }).ToList()
            };
            return lret;
        }
    }

    public class BookShelfooksBenchmarksBase
    {
        protected BookShelf data;
        protected MemoryStream ssStream;
        protected ReadOnlyMemory<char> ssSpan;
        protected string ssJson;

        protected MemoryStream jnStream;
        protected string jnJson;

        protected void Init(int count)
        {
            data = BookUtils.Data(10000);
            
            ssStream = new MemoryStream();            
            ServiceStack.Text.JsonSerializer.SerializeToStream(data, ssStream);
            ssJson = ssStream.ReadToEnd();
            ssSpan = ssJson.AsMemory();
            
            jnStream = new MemoryStream();
            var writer = new StreamWriter(jnStream, Encoding.UTF8, 1024, leaveOpen: true);
            var jsonWriter = new JsonTextWriter(writer);
            var serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Serialize(jsonWriter, data);
            jsonWriter.Flush();
            
            jnJson = jnStream.ReadToEnd();
            $"DATA ServiceStack length = {ssJson.Length}, JSON.NET length = {jnJson.Length}".Print();
        }
    }

/*
                       Method |     N |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
----------------------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
        DeserializeFromStream | 10000 | 7.069 ms | 0.0331 ms | 0.0277 ms | 226.5625 | 109.3750 |  39.0625 |   1.25 MB |
 DeserializeFromStreamJsonNet | 10000 | 8.432 ms | 0.1628 ms | 0.2059 ms | 218.7500 | 109.3750 |  31.2500 |   1.25 MB |
            SerializeToString | 10000 | 2.649 ms | 0.0200 ms | 0.0167 ms | 304.6875 | 156.2500 | 156.2500 |   1.21 MB |
     SerializeToStringJsonNet | 10000 | 4.632 ms | 0.0664 ms | 0.0621 ms | 304.6875 | 257.8125 | 156.2500 |   1.45 MB |

 */
    [MemoryDiagnoser]
    public class BookShelf10000BooksBenchmarks : BookShelfooksBenchmarksBase
    {
        [Params(10000)] public int N;

        [GlobalSetup]
        public void Setup()
        {
            Init(10000);
        }
        
        [Benchmark]
        public object DeserializeFromStream() => JsonSerializer.DeserializeFromStream<BookShelf>(ssStream);

        [Benchmark]
        public object DeserializeFromStreamJsonNet()
        {
            jnStream.Position = 0;
            var serializer = new Newtonsoft.Json.JsonSerializer();
            var sr = new StreamReader(jnStream);
            var jsonTextReader = new JsonTextReader(sr);
            return serializer.Deserialize<BookShelf>(jsonTextReader);
        }

//        [Benchmark]
//        public Task<BookShelf> DeserializeFromStreamAsync() => JsonSerializer.DeserializeFromStreamAsync<BookShelf>(ssStream);

//        [Benchmark]
//        public object DeserializeFromSpan() => JsonSerializer.DeserializeFromSpan<BookShelf>(ssSpan.Span);
//
//        [Benchmark]
//        public object DeserializeFromString() => JsonSerializer.DeserializeFromString<BookShelf>(ssJson);

//        [Benchmark]
//        public object DeserializeFromStringJsonNet() => JsonConvert.DeserializeObject<BookShelf>(jnJson);

        [Benchmark]
        public string SerializeToString() => ServiceStack.Text.JsonSerializer.SerializeToString(data);

        [Benchmark]
        public string SerializeToStringJsonNet() => Newtonsoft.Json.JsonConvert.SerializeObject(data);
    }
    
}