using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Authentication;
using System.Text;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public class RedisEndpoint : IEndpoint
    {
        public RedisEndpoint()
        {
            Host = RedisConfig.DefaultHost;
            Port = RedisConfig.DefaultPort;
            Db = RedisConfig.DefaultDb;

            ConnectTimeout = RedisConfig.DefaultConnectTimeout;
            SendTimeout = RedisConfig.DefaultSendTimeout;
            ReceiveTimeout = RedisConfig.DefaultReceiveTimeout;
            RetryTimeout = RedisConfig.DefaultRetryTimeout;
            IdleTimeOutSecs = RedisConfig.DefaultIdleTimeOutSecs;
        }

        public RedisEndpoint(string host, int port, string password = null, long db = RedisConfig.DefaultDb)
            : this()
        {
            this.Host = host;
            this.Port = port;
            this.Password = password;
            this.Db = db;
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public bool Ssl { get; set; }
        public SslProtocols? SslProtocols {get; set;}
        public int ConnectTimeout { get; set; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public int RetryTimeout { get; set; }
        public int IdleTimeOutSecs { get; set; }
        public long Db { get; set; }
        public string Client { get; set; }
        public string Password { get; set; }
        public bool RequiresAuth { get { return !string.IsNullOrEmpty(Password); } }
        public string NamespacePrefix { get; set; }

        public override string ToString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.AppendFormat("{0}:{1}", Host, Port);

            var args = new List<string>();
            if (Client != null)
                args.Add("Client=" + Client);
            if (Password != null)
                args.Add("Password=" + Password.UrlEncode());
            if (Db != RedisConfig.DefaultDb)
                args.Add("Db=" + Db);
            if (Ssl)
                args.Add("Ssl=true");
            if (SslProtocols != null)
                args.Add("SslProtocols=" + SslProtocols.ToString());
            if (ConnectTimeout != RedisConfig.DefaultConnectTimeout)
                args.Add("ConnectTimeout=" + ConnectTimeout);
            if (SendTimeout != RedisConfig.DefaultSendTimeout)
                args.Add("SendTimeout=" + SendTimeout);
            if (ReceiveTimeout != RedisConfig.DefaultReceiveTimeout)
                args.Add("ReceiveTimeout=" + ReceiveTimeout);
            if (RetryTimeout != RedisConfig.DefaultRetryTimeout)
                args.Add("RetryTimeout=" + RetryTimeout);
            if (IdleTimeOutSecs != RedisConfig.DefaultIdleTimeOutSecs)
                args.Add("IdleTimeOutSecs=" + IdleTimeOutSecs);
            if (NamespacePrefix != null)
                args.Add("NamespacePrefix=" + NamespacePrefix.UrlEncode());

            if (args.Count > 0)
                sb.Append("?").Append(string.Join("&", args));
            
            return StringBuilderCache.ReturnAndFree(sb);
        }

        protected bool Equals(RedisEndpoint other)
        {
            return string.Equals(Host, other.Host) 
                && Port == other.Port 
                && Ssl.Equals(other.Ssl) 
                && SslProtocols.Equals(other.SslProtocols)
                && ConnectTimeout == other.ConnectTimeout 
                && SendTimeout == other.SendTimeout 
                && ReceiveTimeout == other.ReceiveTimeout 
                && RetryTimeout == other.RetryTimeout
                && IdleTimeOutSecs == other.IdleTimeOutSecs 
                && Db == other.Db 
                && string.Equals(Client, other.Client) 
                && string.Equals(Password, other.Password) 
                && string.Equals(NamespacePrefix, other.NamespacePrefix);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RedisEndpoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Host != null ? Host.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Port;
                hashCode = (hashCode * 397) ^ Ssl.GetHashCode();
                hashCode = (hashCode * 397) ^ SslProtocols.GetHashCode();
                hashCode = (hashCode * 397) ^ ConnectTimeout;
                hashCode = (hashCode * 397) ^ SendTimeout;
                hashCode = (hashCode * 397) ^ ReceiveTimeout;
                hashCode = (hashCode * 397) ^ RetryTimeout;
                hashCode = (hashCode * 397) ^ IdleTimeOutSecs;
                hashCode = (hashCode * 397) ^ Db.GetHashCode();
                hashCode = (hashCode * 397) ^ (Client != null ? Client.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NamespacePrefix != null ? NamespacePrefix.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}