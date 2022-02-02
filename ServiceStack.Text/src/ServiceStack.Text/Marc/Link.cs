using System.Threading;

//Not using it here, but @marcgravell's stuff is too good not to include
namespace ServiceStack.Text.Marc
{
	/// <summary>
	/// Pretty Thread-Safe cache class from:
	/// http://code.google.com/p/dapper-dot-net/source/browse/Dapper/SqlMapper.cs
	/// 
	/// This is a micro-cache; suitable when the number of terms is controllable (a few hundred, for example),
	/// and strictly append-only; you cannot change existing values. All key matches are on **REFERENCE**
	/// equality. The type is fully thread-safe.
	/// </summary>
	class Link<TKey, TValue> where TKey : class
	{
		public static bool TryGet(Link<TKey, TValue> link, TKey key, out TValue value)
		{
			while (link != null)
			{
				if ((object)key == (object)link.Key)
				{
					value = link.Value;
					return true;
				}
				link = link.Tail;
			}
			value = default(TValue);
			return false;
		}

		public static bool TryAdd(ref Link<TKey, TValue> head, TKey key, ref TValue value)
		{
			bool tryAgain;
			do
			{
				var snapshot = Interlocked.CompareExchange(ref head, null, null);
				TValue found;
				if (TryGet(snapshot, key, out found))
				{ // existing match; report the existing value instead
					value = found;
					return false;
				}
				var newNode = new Link<TKey, TValue>(key, value, snapshot);
				// did somebody move our cheese?
				tryAgain = Interlocked.CompareExchange(ref head, newNode, snapshot) != snapshot;
			} while (tryAgain);
			return true;
		}

		private Link(TKey key, TValue value, Link<TKey, TValue> tail)
		{
			Key = key;
			Value = value;
			Tail = tail;
		}
		
		public TKey Key { get; private set; }
		public TValue Value { get; private set; }
		public Link<TKey, TValue> Tail { get; private set; }
	}
}