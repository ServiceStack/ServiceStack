using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Pipeline
{
    internal partial class QueuedRedisOperation
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(QueuedRedisOperation));

        public Action VoidReadCommand { get; set; }
        public Func<int> IntReadCommand { get; set; }
        public Func<long> LongReadCommand { get; set; }
        public Func<bool> BoolReadCommand { get; set; }
        public Func<byte[]> BytesReadCommand { get; set; }
        public Func<byte[][]> MultiBytesReadCommand { get; set; }
        public Func<string> StringReadCommand { get; set; }
        public Func<List<string>> MultiStringReadCommand { get; set; }
        public Func<Dictionary<string, string>> DictionaryStringReadCommand { get; set; }
        public Func<double> DoubleReadCommand { get; set; }
        public Func<RedisData> RedisDataReadCommand { get; set; }

        public Action OnSuccessVoidCallback { get; set; }
        public Action<int> OnSuccessIntCallback { get; set; }
        public Action<long> OnSuccessLongCallback { get; set; }
        public Action<bool> OnSuccessBoolCallback { get; set; }
        public Action<byte[]> OnSuccessBytesCallback { get; set; }
        public Action<byte[][]> OnSuccessMultiBytesCallback { get; set; }
        public Action<string> OnSuccessStringCallback { get; set; }
        public Action<List<string>> OnSuccessMultiStringCallback { get; set; }
        public Action<Dictionary<string, string>> OnSuccessDictionaryStringCallback { get; set; }
        public Action<RedisData> OnSuccessRedisDataCallback { get; set; }
        public Action<RedisText> OnSuccessRedisTextCallback { get; set; }
        public Action<double> OnSuccessDoubleCallback { get; set; }

        public Action<string> OnSuccessTypeCallback { get; set; }
        public Action<List<string>> OnSuccessMultiTypeCallback { get; set; }

        public Action<Exception> OnErrorCallback { get; set; }

        public virtual void Execute(IRedisClient client)
        {

        }

        public void ProcessResult()
        {
            try
            {
                if (VoidReadCommand != null)
                {
                    VoidReadCommand();
                    OnSuccessVoidCallback?.Invoke();
                }
                else if (IntReadCommand != null)
                {
                    var result = IntReadCommand();
                    OnSuccessIntCallback?.Invoke(result);
                    OnSuccessLongCallback?.Invoke(result);
                    OnSuccessBoolCallback?.Invoke(result == RedisNativeClient.Success);
                    OnSuccessVoidCallback?.Invoke();
                }
                else if (LongReadCommand != null)
                {
                    var result = LongReadCommand();
                    OnSuccessIntCallback?.Invoke((int)result);
                    OnSuccessLongCallback?.Invoke(result);
                    OnSuccessBoolCallback?.Invoke(result == RedisNativeClient.Success);
                    OnSuccessVoidCallback?.Invoke();
                }
                else if (DoubleReadCommand != null)
                {
                    var result = DoubleReadCommand();
                    OnSuccessDoubleCallback?.Invoke(result);
                }
                else if (BytesReadCommand != null)
                {
                    var result = BytesReadCommand();
                    if (result != null && result.Length == 0)
                        result = null;

                    OnSuccessBytesCallback?.Invoke(result);
                    OnSuccessStringCallback?.Invoke(result != null ? Encoding.UTF8.GetString(result) : null);
                    OnSuccessTypeCallback?.Invoke(result != null ? Encoding.UTF8.GetString(result) : null);
                    OnSuccessIntCallback?.Invoke(result != null ? int.Parse(Encoding.UTF8.GetString(result)) : 0);
                    OnSuccessBoolCallback?.Invoke(result != null && Encoding.UTF8.GetString(result) == "OK");
                }
                else if (StringReadCommand != null)
                {
                    var result = StringReadCommand();
                    OnSuccessStringCallback?.Invoke(result);
                    OnSuccessTypeCallback?.Invoke(result);
                }
                else if (MultiBytesReadCommand != null)
                {
                    var result = MultiBytesReadCommand();
                    OnSuccessMultiBytesCallback?.Invoke(result);
                    OnSuccessMultiStringCallback?.Invoke(result != null ? result.ToStringList() : null);
                    OnSuccessMultiTypeCallback?.Invoke(result.ToStringList());
                    OnSuccessDictionaryStringCallback?.Invoke(result.ToStringDictionary());
                }
                else if (MultiStringReadCommand != null)
                {
                    var result = MultiStringReadCommand();
                    OnSuccessMultiStringCallback?.Invoke(result);
                }
                else if (RedisDataReadCommand != null)
                {
                    var data = RedisDataReadCommand();
                    OnSuccessRedisTextCallback?.Invoke(data.ToRedisText());
                    OnSuccessRedisDataCallback?.Invoke(data);
                }
                else if (BoolReadCommand != null)
                {
                    var result = BoolReadCommand();
                    OnSuccessBoolCallback?.Invoke(result);
                }
                else if (DictionaryStringReadCommand != null)
                {
                    var result = DictionaryStringReadCommand();
                    OnSuccessDictionaryStringCallback?.Invoke(result);
                }
                else
                {
                    ProcessResultThrowIfAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (OnErrorCallback != null)
                {
                    OnErrorCallback(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        protected void ProcessResultThrowIfAsync() => OnProcessResultThrowIfAsync();
        partial void OnProcessResultThrowIfAsync();
    }
}