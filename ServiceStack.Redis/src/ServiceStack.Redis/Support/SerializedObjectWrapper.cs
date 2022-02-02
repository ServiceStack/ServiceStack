using System;

namespace ServiceStack.Redis.Support
{
    /// <summary>
    /// wraps a serialized representation of an object
    /// </summary>
    ///
    [Serializable]
	public struct SerializedObjectWrapper
	{
		private ArraySegment<byte> data;
		private ushort flags;

		/// <summary>
        /// Initializes a new instance of <see cref="SerializedObjectWrapper"/>.
		/// </summary>
		/// <param name="flags">Custom item data.</param>
		/// <param name="data">The serialized item.</param>
		public SerializedObjectWrapper(ushort flags, ArraySegment<byte> data)
		{
			this.data = data;
			this.flags = flags;
		}

		/// <summary>
		/// The data representing the item being stored/retireved.
		/// </summary>
		public ArraySegment<byte> Data
		{
			get => data;
		    set => data = value;
		}

		/// <summary>
		/// Flags set for this instance.
		/// </summary>
		public ushort Flags
		{
			get => flags;
		    set => flags = value;
		}
	}
}

