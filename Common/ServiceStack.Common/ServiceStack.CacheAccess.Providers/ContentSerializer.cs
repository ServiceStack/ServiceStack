using System;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.CacheAccess.Providers
{
	/// <summary>
	/// Encapsulates the behaviour of serving compressed or uncompressed 
	/// serialized results for a particular MimeType and CompressionType.
	/// </summary>
	public sealed class ContentSerializer<T>
		where T : class
	{
		public Func<T> FactoryFn { get; private set; }
		public string CompressionType { get; private set; }
		public string MimeType { get; private set; }

		public ContentSerializer(Func<T> factoryFn, string mimeType)
			: this(factoryFn, mimeType, null)
		{
		}

		public ContentSerializer(Func<T> factoryFn, string mimeType, string compressionType)
		{
			factoryFn.ThrowIfNull("FactoryFn");
			mimeType.ThrowIfNull("MimeType");

			this.FactoryFn = factoryFn;
			this.MimeType = mimeType;
			this.CompressionType = compressionType;
		}

		public bool DoCompress
		{
			get
			{
				return !CompressionType.IsNullOrEmpty();
			}
		}

		public object ToSerializedResult()
		{
			return ToSerializedString(FactoryFn(), MimeType);
		}

		public string ToSerializedString()
		{
			return ToSerializedString(FactoryFn(), MimeType);
		}

		public byte[] ToCompressedResult()
		{
			return ToCompressedResult(FactoryFn(), MimeType, CompressionType);
		}

		public static byte[] ToCompressedResult(object result, string mimeType, string compressionType)
		{
			result.ThrowIfNull("result");
			mimeType.ThrowIfNull("MimeType");

			return ToCompressedResult(ToSerializedString(result, mimeType), compressionType);
		}

		public static byte[] ToCompressedResult(string serializedResult, string compressionType)
		{
			if (serializedResult == null) return null;

			var compressedResult = serializedResult.Compress(compressionType);

			return compressedResult;
		}

		public static string ToSerializedString(object result, string mimeType)
		{
			if (result == null) return null;

			switch (mimeType)
			{
				case MimeTypes.Xml:
					return DataContractSerializer.Instance.Parse(result);

				case MimeTypes.Json:
					return JsonDataContractSerializer.Instance.Parse(result);

				case MimeTypes.Jsv:
					return TypeSerializer.SerializeToString(result);

				default:
					throw new NotSupportedException(mimeType);
			}
		}

	}
}