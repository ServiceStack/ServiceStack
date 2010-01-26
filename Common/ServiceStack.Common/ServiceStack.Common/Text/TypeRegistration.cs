using System;

namespace ServiceStack.Common.Text
{

#if STATIC_ONLY
	public static class TypeRegistration
	{
		private static bool DefaultTypesRegistered = false;

		/// <summary>
		/// Required for environments that require static compilation only, i.e. MonoTouch
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static void RegisterType<T>()
		{
			ParseStringArrayMethods.ParseArray<T>(null, null);
			ParseStringCollectionMethods.ParseCollection<T>(null, null, null);
			ParseStringListMethods.ParseList<T>(null, null, null);
			ToStringListMethods.WriteListValueType<T>(null, null);
			ToStringListMethods.WriteGenericIListObject<T>(null, null, null);
			ToStringListMethods.WriteArray<T>(null, null, null);

			ParseStringDictionaryMethods.ParseDictionary<string, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<bool, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<bool?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<byte, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<byte?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<sbyte, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<sbyte?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<short, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<short?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<int, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<int?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<long, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<long?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<float, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<float?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<double, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<double?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<decimal, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<decimal?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<Guid, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<Guid?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<DateTime, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<DateTime?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<TimeSpan, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<TimeSpan?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<char, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<char?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<ushort, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<ushort?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<uint, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<uint?, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<ulong, T>(null, null, null, null);
			ParseStringDictionaryMethods.ParseDictionary<ulong?, T>(null, null, null, null);

			TypeToStringMethods.CreateFunc<T, string>(null);
			TypeToStringMethods.CreateFunc<T, string>(null);
			TypeToStringMethods.CreateFunc<T, bool>(null);
			TypeToStringMethods.CreateFunc<T, bool?>(null);
			TypeToStringMethods.CreateFunc<T, byte>(null);
			TypeToStringMethods.CreateFunc<T, byte?>(null);
			TypeToStringMethods.CreateFunc<T, sbyte>(null);
			TypeToStringMethods.CreateFunc<T, sbyte?>(null);
			TypeToStringMethods.CreateFunc<T, short>(null);
			TypeToStringMethods.CreateFunc<T, short?>(null);
			TypeToStringMethods.CreateFunc<T, int>(null);
			TypeToStringMethods.CreateFunc<T, int?>(null);
			TypeToStringMethods.CreateFunc<T, long>(null);
			TypeToStringMethods.CreateFunc<T, long?>(null);
			TypeToStringMethods.CreateFunc<T, float>(null);
			TypeToStringMethods.CreateFunc<T, float?>(null);
			TypeToStringMethods.CreateFunc<T, double>(null);
			TypeToStringMethods.CreateFunc<T, double?>(null);
			TypeToStringMethods.CreateFunc<T, decimal>(null);
			TypeToStringMethods.CreateFunc<T, decimal?>(null);
			TypeToStringMethods.CreateFunc<T, Guid>(null);
			TypeToStringMethods.CreateFunc<T, Guid?>(null);
			TypeToStringMethods.CreateFunc<T, DateTime>(null);
			TypeToStringMethods.CreateFunc<T, DateTime?>(null);
			TypeToStringMethods.CreateFunc<T, TimeSpan>(null);
			TypeToStringMethods.CreateFunc<T, TimeSpan?>(null);
			TypeToStringMethods.CreateFunc<T, ushort>(null);
			TypeToStringMethods.CreateFunc<T, ushort?>(null);
			TypeToStringMethods.CreateFunc<T, uint>(null);
			TypeToStringMethods.CreateFunc<T, uint?>(null);
			TypeToStringMethods.CreateFunc<T, ulong>(null);
			TypeToStringMethods.CreateFunc<T, ulong?>(null);

			if (!DefaultTypesRegistered)
			{
				DefaultTypesRegistered = true;
			}
		}

		public static void RegisterDefaultTypes<T>()
		{
			RegisterType<string>();
			RegisterType<bool>();
			RegisterType<bool?>();
			RegisterType<byte>();
			RegisterType<byte?>();
			RegisterType<sbyte>();
			RegisterType<sbyte?>();
			RegisterType<short>();
			RegisterType<short?>();
			RegisterType<int>();
			RegisterType<int?>();
			RegisterType<long>();
			RegisterType<long?>();
			RegisterType<float>();
			RegisterType<float?>();
			RegisterType<double>();
			RegisterType<double?>();
			RegisterType<decimal>();
			RegisterType<decimal?>();
			RegisterType<Guid>();
			RegisterType<Guid?>();
			RegisterType<DateTime>();
			RegisterType<DateTime?>();
			RegisterType<TimeSpan>();
			RegisterType<TimeSpan?>();
			RegisterType<ushort>();
			RegisterType<ushort?>();
			RegisterType<uint>();
			RegisterType<uint?>();
			RegisterType<ulong>();
			RegisterType<ulong?>();
		}

	}

#endif

}