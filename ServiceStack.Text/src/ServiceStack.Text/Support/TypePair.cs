//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;

namespace ServiceStack.Text.Support
{
	public class TypePair
	{
		public Type[] Args1 { get; set; }
		public Type[] Arg2 { get; set; }

		public TypePair(Type[] arg1, Type[] arg2)
		{
			Args1 = arg1;
			Arg2 = arg2;
		}

		public bool Equals(TypePair other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Args1, Args1) && Equals(other.Arg2, Arg2);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (TypePair)) return false;
			return Equals((TypePair) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Args1 != null ? Args1.GetHashCode() : 0)*397) ^ (Arg2 != null ? Arg2.GetHashCode() : 0);
			}
		}
	}
}