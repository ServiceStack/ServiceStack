//
// ServiceStack: Open Source .NET and Mono Web Services framework
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Common.Reflection
{
	/// <summary>
	/// Type serializer cache of all the property accessors required when
	/// serializing/deserializing types.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public static class TypeSerializerPropertyAccessor<TEntity>
	{
		const string DataContract = "DataContract";
		const string DataMember = "DataMember";
		private static readonly PropertyAccessor<TEntity>[] PropertyAccessors;

		static TypeSerializerPropertyAccessor()
		{
			var pis = GetPropertyInfos();
			PropertyAccessors = new PropertyAccessor<TEntity>[pis.Count];
			for (var i=0; i < pis.Count; i++)
			{
				var pi = pis[i];
				PropertyAccessors[i] = new PropertyAccessor<TEntity>(pi.Name);
			}
		}

		/// <summary>
		/// Gets the properties that should be de/serialized.
		/// 
		/// If TEntity is a [DataContract] only properties attributed with 
		/// [DataMember] will be included.
		/// 
		/// Otherwise all public properties will be included.
		/// </summary>
		private static List<PropertyInfo> GetPropertyInfos()
		{
			var attrs = typeof(TEntity).GetCustomAttributes(false);
			var isDataContract = attrs.ToList().Any(x => x.GetType().Name == DataContract);
			if (isDataContract)
			{
				return typeof (TEntity).GetProperties(BindingFlags.Instance)
					.Where(x =>
					       x.GetCustomAttributes(false).ToList()
					       	.Any(attr => attr.GetType().Name == DataMember)).ToList();
			}

			return typeof (TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.ToList();
		}
	}

}