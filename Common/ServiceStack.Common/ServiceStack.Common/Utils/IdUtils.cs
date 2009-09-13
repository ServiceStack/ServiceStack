using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Common.Utils
{
	public static class IdUtils
	{
		private const string IdField = "Id";

		public static object GetId<T>(T entity)
		{
			var guidEntity = entity as IHasGuidId;
			if (guidEntity != null)
			{
				return guidEntity.Id;
			}

			var intEntity = entity as IHasIntId;
			if (intEntity != null)
			{
				return intEntity.Id;
			}

			var longEntity = entity as IHasLongId;
			if (longEntity != null)
			{
				return longEntity.Id;
			}

			var stringEntity = entity as IHasStringId;
			if (stringEntity != null)
			{
				return stringEntity.Id;
			}

			var propertyInfo = typeof (T).GetProperty(IdField);
			if (propertyInfo != null)
			{
				return propertyInfo.GetGetMethod().Invoke(entity, new object[0]);
			}

			throw new NotSupportedException("Cannot retrieve value of Id field, use IHasId<>");
		}

		public static string CreateUrn<T>(object id)
			where T : class
		{
			return string.Format("urn:{0}:{1}", typeof(T).Name.ToLower(), id);
		}

		public static string CreateUrn<T>(this T entity)
			where T : class 
		{
			var id = GetId(entity);
			return string.Format("urn:{0}:{1}", typeof(T).Name.ToLower(), id);
		}


		public static string CreateCacheKeyPath<T>(string idValue)
		{
			if (idValue.Length < 4)
			{
				idValue = idValue.PadLeft(4, '0');
			}
			idValue = idValue.Replace(" ", "-");

			var rootDir = typeof(T).Name;
			var dir1 = idValue.Substring(0, 2);
			var dir2 = idValue.Substring(2, 2);

			var path = string.Format("{1}{0}{2}{0}{3}{0}{4}", Path.DirectorySeparatorChar,
				rootDir, dir1, dir2, idValue);

			return path;
		}

	}

}