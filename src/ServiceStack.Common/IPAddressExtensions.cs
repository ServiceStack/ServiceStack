#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ServiceStack.Logging;

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

            return new IPAddress(GetNetworkAddressBytes(ipAdressBytes, subnetMaskBytes));
        }

        public static byte[] GetNetworkAddressBytes(byte[] ipAdressBytes, byte[] subnetMaskBytes) 
        {
            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            var broadcastAddress = new byte[ipAdressBytes.Length];
            for (var i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return broadcastAddress;
        }

        public static bool IsInSameIpv6Subnet(this IPAddress address2, IPAddress address)
        {
            if (address2.AddressFamily != AddressFamily.InterNetworkV6 || address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("Both IPAddress must be IPV6 addresses");
            }
            var address1Bytes = address.GetAddressBytes();
            var address2Bytes = address2.GetAddressBytes();

            return IsInSameIpv6Subnet(address1Bytes, address2Bytes);
        }

        public static bool IsInSameIpv6Subnet(this byte[] address1Bytes, byte[] address2Bytes) 
        {
            if (address1Bytes.Length != address2Bytes.Length)
                throw new ArgumentException("Lengths of IP addresses do not match.");

            for (var i = 0; i < 8; i++)
            {
                if (address1Bytes[i] != address2Bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInSameIpv4Subnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
        {
            if (address2.AddressFamily != AddressFamily.InterNetwork || address.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException("Both IPAddress must be IPV4 addresses");
            }
            var network1 = address.GetNetworkAddress(subnetMask);
            var network2 = address2.GetNetworkAddress(subnetMask);

            return network1.Equals(network2);
        }

        public static bool IsInSameIpv4Subnet(this byte[] address1Bytes, byte[] address2Bytes, byte[] subnetMaskBytes)
        {
            if (address1Bytes.Length != address2Bytes.Length)
                throw new ArgumentException("Lengths of IP addresses do not match.");

            var network1Bytes = GetNetworkAddressBytes(address1Bytes, subnetMaskBytes);
            var network2Bytes = GetNetworkAddressBytes(address2Bytes, subnetMaskBytes);

            return network1Bytes.AreEqual(network2Bytes);
        }


        /// <summary>
        /// Gets the ipv4 addresses from all Network Interfaces that have Subnet masks.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<IPAddress, IPAddress> GetAllNetworkInterfaceIpv4Addresses()
        {
            var map = new Dictionary<IPAddress, IPAddress>();

            try
            {
#if !SILVERLIGHT 
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (var uipi in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (uipi.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                        if (uipi.IPv4Mask == null) continue; //ignore 127.0.0.1
                        map[uipi.Address] = uipi.IPv4Mask;
                    }
                }
#endif
            }
            catch /*(NotImplementedException ex)*/
            {
                //log.Warn("MONO does not support NetworkInterface.GetAllNetworkInterfaces(). Could not detect local ip subnets.", ex);
            } 
            return map;
        }

        /// <summary>
        /// Gets the ipv6 addresses from all Network Interfaces.
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetAllNetworkInterfaceIpv6Addresses()
        {
            var list = new List<IPAddress>();

            try
            {
#if !SILVERLIGHT 
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (var uipi in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (uipi.Address.AddressFamily != AddressFamily.InterNetworkV6) continue;
                        list.Add(uipi.Address);
                    }
                }
#endif
            }
            catch /*(NotImplementedException ex)*/
            {
                //log.Warn("MONO does not support NetworkInterface.GetAllNetworkInterfaces(). Could not detect local ip subnets.", ex);
            }
            
            return list;
        }

    }
}
#endif