using System;
using System.Diagnostics;

namespace ServiceStack.Text.Tests
{
	public abstract class TextSerializerTestBase
	{
		private readonly Stopwatch stopwatch = new Stopwatch();

		public T Serialize<T>(T model)
		{
			stopwatch.Reset();
			stopwatch.Start();
			var jsvModel = TypeSerializer.SerializeToString(model);
			stopwatch.Stop();

			Console.WriteLine("JSV  Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, jsvModel.Length, jsvModel);

			stopwatch.Reset();
			stopwatch.Start();
			var jsonModel = JsonSerializer.SerializeToString(model);
			stopwatch.Stop();

			Console.WriteLine("JSON Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, jsonModel.Length, jsonModel); 

			var toModel = TypeSerializer.DeserializeFromString<T>(jsvModel);
			return toModel;
		}
		
	}
}