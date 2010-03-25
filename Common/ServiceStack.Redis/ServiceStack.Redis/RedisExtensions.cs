using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ServiceStack.Common.Web;
using ServiceStack.DesignPatterns.Model;

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
	}

}