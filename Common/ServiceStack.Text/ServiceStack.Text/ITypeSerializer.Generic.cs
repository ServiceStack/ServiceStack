//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.IO;

namespace ServiceStack.Text
{
	public interface ITypeSerializer<T>
	{
		/// <summary>
		/// Determines whether this serializer can create the specified type from a string.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// 	<c>true</c> if this instance [can create from string] the specified type; otherwise, <c>false</c>.
		/// </returns>
		bool CanCreateFromString(Type type);

		/// <summary>
		/// Parses the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		T DeserializeFromString(string value);

		/// <summary>
		/// Deserializes from reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
		T DeserializeFromReader(TextReader reader);

		/// <summary>
		/// Serializes to string.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		string SerializeToString(T value);

		/// <summary>
		/// Serializes to writer.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="writer">The writer.</param>
		void SerializeToWriter(T value, TextWriter writer);
	}
}