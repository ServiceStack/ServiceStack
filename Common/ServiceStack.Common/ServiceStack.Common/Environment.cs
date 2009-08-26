
namespace ServiceStack.Common
{
	public static class Env
	{
		public static bool IsUnix
		{
			get
			{
				var platform = (int)System.Environment.OSVersion.Platform;
				return (platform == 4) || (platform == 6) || (platform == 128);
			}
		}

	}
}