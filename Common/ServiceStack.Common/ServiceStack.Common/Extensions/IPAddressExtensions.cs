using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ServiceStack.Common.Extensions
{
	/// <summary>
	/// Useful IPAddressExtensions from: 
	/// http://blogs.msdn.com/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
	/// 
	/// </summary>
	public static class IPAddressExtensions
	{
		public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
		{
			var ipAdressBytes = address.GetAddressBytes();
			var subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			var broadcastAddress = new byte[ipAdressBytes.Length];
			for (var i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
			}
			return new IPAddress(broadcastAddress);
		}

		public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
		{
			var ipAdressBytes = address.GetAddressBytes();
			var subnetMaskBytes = subnetMask.GetAddressBytes();

			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

			var broadcastAddress = new byte[ipAdressBytes.Length];
			for (var i = 0; i < broadcastAddress.Length; i++)
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress(broadcastAddress);
		}

		public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
		{
			var network1 = address.GetNetworkAddress(subnetMask);
			var network2 = address2.GetNetworkAddress(subnetMask);

			return network1.Equals(network2);
		}


		/// <summary>
		/// Gets the ipv4 addresses from all Network Interfaces that have Subnet masks.
		/// </summary>
		/// <returns></returns>
		public static Dictionary<IPAddress, IPAddress> GetAllNetworkInterfaceIpv4Addresses()
		{
			var map = new Dictionary<IPAddress, IPAddress>();

			foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				foreach (var uipi in ni.GetIPProperties().UnicastAddresses)
				{
					if (uipi.Address.AddressFamily != AddressFamily.InterNetwork) continue;

					if (uipi.IPv4Mask == null) continue; //ignore 127.0.0.1
					map[uipi.Address] = uipi.IPv4Mask;
				}
			}
			return map;
		}

	}
}