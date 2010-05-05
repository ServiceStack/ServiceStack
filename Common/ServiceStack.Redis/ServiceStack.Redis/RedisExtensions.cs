//
// http://code.google.com/p/servicestack/wiki/ServiceStackRedis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	internal static class RedisExtensions
	{
		public static List<EndPoint> ToIpEndPoints(this IEnumerable<string> hosts)
		{
			if (hosts == null) return new List<EndPoint>();

			const int hostOrIpAddressIndex = 0;
			const int portIndex = 1;

			var ipEndpoints = new List<EndPoint>();
			foreach (var host in hosts)
			{
				var hostParts = host.Split(':');
				if (hostParts.Length == 0)
					throw new ArgumentException("'{0}' is not a valid Host or IP Address: e.g. '127.0.0.0[:11211]'");

				var port = (hostParts.Length == 1)
					? RedisNativeClient.DefaultPort : int.Parse(hostParts[portIndex]);

				var endpoint = new EndPoint(hostParts[hostOrIpAddressIndex], port);
				ipEndpoints.Add(endpoint);
			}
			return ipEndpoints;
		}

		public static bool IsConnected(this Socket socket)
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException) { return false; }
		}


		public static string[] GetIds(this IHasStringId[] itemsWithId)
		{
			var ids = new string[itemsWithId.Length];
			for (var i = 0; i < itemsWithId.Length; i++)
			{
				ids[i] = itemsWithId[i].Id;
			}
			return ids;
		}

		public static List<string> ToStringList(this byte[][] multiDataList)
		{
			if (multiDataList == null)
				return new List<string>();

			var results = new List<string>();
			foreach (var multiData in multiDataList)
			{
				results.Add(multiData.FromUtf8Bytes());
			}
			return results;
		}
	}

}