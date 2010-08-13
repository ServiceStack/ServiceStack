using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	public abstract class TestBase
	{
		private readonly Stopwatch stopwatch = new Stopwatch();

		public virtual void Log(string message, params object[] args)
		{
#if DEBUG
#endif
			if (args.Length > 0)
				Console.WriteLine(message, args);
			else
				Console.WriteLine(message);
		}

		public T Serialize<T>(T model)
		{
			return Serialize(model, false);
		}

		public T SerializeAndCompare<T>(T model)
		{
			return Serialize(model, true);
		}

		private T Serialize<T>(T model, bool assertEqual)
		{
			stopwatch.Reset();
			stopwatch.Start();
			var jsv = TypeSerializer.SerializeToString(model);
			stopwatch.Stop();

			var partialJsv = jsv.Length > 100 ? jsv.Substring(0, 100) + "..." : jsv;
			Console.WriteLine("JSV  Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, jsv.Length, partialJsv);

			stopwatch.Reset();
			stopwatch.Start();
			var json = JsonSerializer.SerializeToString(model);
			stopwatch.Stop();

			var partialJson = json.Length > 100 ? json.Substring(0, 100) + "..." : json;
			Console.WriteLine("JSON Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, json.Length, partialJson);

			var fromJsvModel = TypeSerializer.DeserializeFromString<T>(jsv);
			var fromJsonModel = JsonSerializer.DeserializeFromString<T>(json);

			if (assertEqual)
			{
				var enumerableModel = model as IEnumerable;
				if (enumerableModel != null)
				{
					Assert.That(fromJsvModel, Is.EquivalentTo(enumerableModel),
						string.Format("Deserialized JSV  {0} was not equal to the original\n{1}", typeof(T), partialJsv));

					Assert.That(fromJsonModel, Is.EquivalentTo(enumerableModel),
						string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJsv));
				}
				else
				{
					Assert.That(fromJsvModel, Is.EqualTo(model),
						string.Format("Deserialized JSV  {0} was not equal to the original\n{1}", typeof(T), partialJsv));

					Assert.That(fromJsonModel, Is.EqualTo(model),
						string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJsv));
				}
			}

			return fromJsvModel;
		}
	}
}