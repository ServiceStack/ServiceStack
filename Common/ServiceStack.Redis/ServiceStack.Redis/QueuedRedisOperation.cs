using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
	internal class QueuedRedisOperation
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(QueuedRedisOperation));

		public Action VoidReadCommand { get; set; }
		public Func<int> IntReadCommand { get; set; }
		public Func<bool> BoolReadCommand { get; set; }
		public Func<byte[]> BytesReadCommand { get; set; }
		public Func<byte[][]> MultiBytesReadCommand { get; set; }
		public Func<string> StringReadCommand { get; set; }
		public Func<List<string>> MultiStringReadCommand { get; set; }
		public Func<double> DoubleReadCommand { get; set; }

		public Action OnSuccessVoidCallback { get; set; }
		public Action<int> OnSuccessIntCallback { get; set; }
		public Action<bool> OnSuccessBoolCallback { get; set; }
		public Action<byte[]> OnSuccessBytesCallback { get; set; }
		public Action<byte[][]> OnSuccessMultiBytesCallback { get; set; }
		public Action<string> OnSuccessStringCallback { get; set; }
		public Action<List<string>> OnSuccessMultiStringCallback { get; set; }
		public Action<double> OnSuccessDoubleCallback { get; set; }
		
		public Action<string> OnSuccessTypeCallback { get; set; }
		public Action<List<string>> OnSuccessMultiTypeCallback { get; set; }

		public Action<Exception> OnErrorCallback { get; set; }

		public void ProcessResult()
		{
			try
			{
				if (VoidReadCommand != null)
				{
					VoidReadCommand();
					if (OnSuccessVoidCallback != null)
					{
						OnSuccessVoidCallback();
					}
				}
				else if (IntReadCommand != null)
				{
					var result = IntReadCommand();
					if (OnSuccessIntCallback != null)
					{
						OnSuccessIntCallback(result);
					}
					if (OnSuccessBoolCallback != null)
					{
						var success = result == RedisNativeClient.Success;
						OnSuccessBoolCallback(success);
					}
				}
				else if (DoubleReadCommand != null)
				{
					var result = DoubleReadCommand();
					if (OnSuccessDoubleCallback != null)
					{
						OnSuccessDoubleCallback(result);
					}
				}
				else if (BytesReadCommand != null)
				{
					var result = BytesReadCommand();
					if (OnSuccessBytesCallback != null)
					{
						OnSuccessBytesCallback(result);
					}
					if (OnSuccessStringCallback != null)
					{
						OnSuccessStringCallback(Encoding.UTF8.GetString(result));
					}
					if (OnSuccessTypeCallback != null)
					{
						OnSuccessTypeCallback(Encoding.UTF8.GetString(result));
					}
				}
				else if (StringReadCommand != null)
				{
					var result = StringReadCommand();
					if (OnSuccessStringCallback != null)
					{
						OnSuccessStringCallback(result);
					}
					if (OnSuccessTypeCallback != null)
					{
						OnSuccessTypeCallback(result);
					}
				}
				else if (MultiBytesReadCommand != null)
				{
					var result = MultiBytesReadCommand();
					if (OnSuccessBytesCallback != null)
					{
						OnSuccessMultiBytesCallback(result);
					}
					if (OnSuccessMultiStringCallback != null)
					{
						OnSuccessMultiStringCallback(result.ToStringList());
					}
					if (OnSuccessMultiTypeCallback != null)
					{
						OnSuccessMultiTypeCallback(result.ToStringList());
					}
				}
				else if (MultiStringReadCommand != null)
				{
					var result = MultiStringReadCommand();
					if (OnSuccessMultiStringCallback != null)
					{
						OnSuccessMultiStringCallback(result);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);

				if (OnErrorCallback != null)
				{
					OnErrorCallback(ex);
				}
			}
		}

	}
}