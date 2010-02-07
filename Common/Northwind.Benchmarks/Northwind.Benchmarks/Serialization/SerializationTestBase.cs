using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Northwind.Perf;
using NUnit.Framework;
using Platform.Text;
using ProtoBuf;
using ServiceStack.Client;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Text;

namespace Northwind.Benchmarks.Serialization
{
	public class SerializationTestBase
		: PerfTestBase
	{

		public SerializationTestBase()
		{
			this.MultipleIterations = new List<int> { 1000, 10000 };
		}

		List<SerializersBenchmarkEntry> TestResults;
		public List<List<SerializersBenchmarkEntry>> FixtureTestResults = new List<List<SerializersBenchmarkEntry>>();
		public List<SerializersBenchmarkEntry> FixtureTestResultsSummary = new List<SerializersBenchmarkEntry>();

		string ModelName { get; set; }

		public string ToHtmlReport(string htmlSummary)
		{
			if (FixtureTestResults.Count == 0)
				throw new ArgumentException("FixtureTestResults is empty");

			var sb = new StringBuilder();

			sb.AppendLine("<html>\n<head>");
			sb.AppendLine("\t<title>Serialization benchmark results</title>");
			sb.AppendLine("\t<link href='default.css' rel='stylesheet' type='text/css' />");
			sb.AppendLine("</head>\n<body>\n");

			sb.AppendFormat("<h2>Results of <span>{0}</span> benchmarks run at {1}</h2>\n",
				GetType().Name.ToEnglish(), DateTime.Now.ToShortDateString());

			if (!htmlSummary.IsNullOrEmpty())
			{
				sb.AppendFormat("<span class=\"summary\">{0}</span>", htmlSummary);
			}

			sb.AppendLine("<div id='combined'>");
			var combinedResults = GetCombinedResults(FixtureTestResults);
			WriteTable(sb, combinedResults, "<h3>Combined results of all benchmarks below</h3>");
			sb.AppendLine("</div>");

			foreach (var fixtureTestResult in FixtureTestResults)
			{
				WriteTable(sb, fixtureTestResult, 
					"<h3>Results of serializing and deserializing {0} {1} times</h3>");
			}

			sb.AppendLine("</body>\n</html>\n");
			return sb.ToString();
		}

		private static void WriteTable(
			StringBuilder sb,
			IEnumerable<SerializersBenchmarkEntry> fixtureTestResult, string benchmarkTitleHtml)
		{
			var testResultCount = 0;

			foreach (var benchmarkEntry in fixtureTestResult)
			{
				if (testResultCount++ == 0)
				{
					sb.AppendFormat(benchmarkTitleHtml, 
						benchmarkEntry.ModelName, 
						benchmarkEntry.Iterations.ToString("#,##0"));

					sb.AppendFormat("<table>\n<caption>* All times measured in ticks and payload size in bytes</caption>");
					sb.AppendFormat(
						"<thead><tr><th>{0}</th><th>{1}</th><th>{6}</th><th>{2}</th><th>{3}</th><th>{4}</th><th>{5}</th><th>{7}</th></tr></thead>",
						"Serializer",
						"Payload size",
						"Serialization",
						"Deserialization",
						"Total",
						"Avg per iteration",
						"Larger than best",
						"Slower than best"
					);
					sb.AppendLine("\n<tbody>");
				}

				var trClass = "";
				if (!benchmarkEntry.Success)
					trClass += "failed ";
				if (benchmarkEntry.TimesLargerThanBest == 1)
					trClass += "best-size ";
				if (benchmarkEntry.TimesSlowerThanBest == 1)
					trClass += "best-time ";

				sb.AppendFormat(
					"<tr class='{8}'><th class='c1'>{0}</th><td>{1}</td><th>{6}x</th><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><th>{7}x</th></tr>",
					benchmarkEntry.SerializerName,
					benchmarkEntry.SerializedBytesLength,
					benchmarkEntry.TotalSerializationTicks,
					benchmarkEntry.TotalDeserializationTicks,
					benchmarkEntry.TotalTicks,
					benchmarkEntry.AvgTicksPerIteration,
					benchmarkEntry.TimesLargerThanBest,
					benchmarkEntry.TimesSlowerThanBest,
					trClass.TrimEnd()
				);
			}
			sb.AppendLine("\n</tbody>\n</table>");
		}

		private static List<SerializersBenchmarkEntry> GetCombinedResults(
			IEnumerable<List<SerializersBenchmarkEntry>> textFixtureResults)
		{
			var combinedBenchmarksMap = new Dictionary<string, SerializersBenchmarkEntry>();
			var orderedList = new List<string>();

			foreach (var benchmarkEntries in textFixtureResults)
			{
				var skipIfOneSerializerFailed = benchmarkEntries.Any(x => !x.Success);
				if (skipIfOneSerializerFailed) continue;

				foreach (var benchmarkEntry in benchmarkEntries)
				{
					SerializersBenchmarkEntry combinedEntry;
					if (!combinedBenchmarksMap.TryGetValue(benchmarkEntry.SerializerName, out combinedEntry))
					{
						orderedList.Add(benchmarkEntry.SerializerName);
						combinedEntry = new SerializersBenchmarkEntry {
							Iterations = benchmarkEntry.Iterations,
							SerializerName = benchmarkEntry.SerializerName,
							ModelName = "All Models"
						};
						combinedBenchmarksMap[combinedEntry.SerializerName] = combinedEntry;
					}

					combinedEntry.SerializedBytesLength += benchmarkEntry.SerializedBytesLength;
					combinedEntry.TotalSerializationTicks += benchmarkEntry.TotalSerializationTicks;
					combinedEntry.TotalDeserializationTicks += benchmarkEntry.TotalDeserializationTicks;
				}
			}

			var orderedCombinedBenchmarks = new List<SerializersBenchmarkEntry>();
			foreach (var serializerName in orderedList)
			{
				orderedCombinedBenchmarks.Add(combinedBenchmarksMap[serializerName]);
			}

			CalculateBestTimes(orderedCombinedBenchmarks);
			return orderedCombinedBenchmarks;
		}

		public void LogDto(string dtoString)
		{
			Log("Len: " + dtoString.Length + ", " + dtoString);
		}

		public T With_DataContractSerializer<T>(T dto)
		{
			var dtoString = DataContractSerializer.Instance.Parse(dto);
			LogDto(dtoString);
			return DataContractDeserializer.Instance.Parse<T>(dtoString);
		}

		public T With_JsonDataContractSerializer<T>(T dto)
		{
			var dtoString = JsonDataContractSerializer.Instance.Parse(dto);
			LogDto(dtoString);
			return JsonDataContractDeserializer.Instance.Parse<T>(dtoString);
		}

		public static byte[] ProtoBufToBytes<T>(T dto)
		{
			using (var ms = new MemoryStream())
			{
				Serializer.Serialize(ms, dto);
				var bytes = ms.ToArray();
				return bytes;
			}
		}

		public static T ProtoBufFromBytes<T>(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				return Serializer.Deserialize<T>(ms);
			}
		}

		public T With_ProtoBuf<T>(T dto)
		{
			var bytes = ProtoBufToBytes(dto);

			Log("Len: " + bytes.Length + ", {protobuf bytes}");

			return ProtoBufFromBytes<T>(bytes);
		}

		public T With_JsonNet<T>(T dto)
		{
			var dtoString = JsonConvert.SerializeObject(dto);
			LogDto(dtoString);
			return JsonConvert.DeserializeObject<T>(dtoString);
		}

		public T With_TypeSerializer<T>(T dto)
		{
			var dtoString = TypeSerializer.SerializeToString(dto);
			LogDto(dtoString);
			return TypeSerializer.DeserializeFromString<T>(dtoString);
		}

		public T With_TextSerializer<T>(T dto)
		{
			var dtoString = TextSerializer.SerializeToString(dto);
			LogDto(dtoString);
			return TextSerializer.DeserializeFromString<T>(dtoString);
		}

		protected void AssertEqual<T>(T dto, T originalDto)
		{
			Assert.That(originalDto.Equals(dto));
		}

		protected void AssertAllAreEqual<T>(T dto)
		{
			AssertEqual(With_DataContractSerializer(dto), dto);
			AssertEqual(With_JsonDataContractSerializer(dto), dto);
			try
			{
				AssertEqual(With_ProtoBuf(dto), dto);
			}
			catch (Exception ex)
			{
				Log("AssertEqual Error in ProtoBuf: {0}", ex);
			}
			AssertEqual(With_JsonNet(dto), dto);
			AssertEqual(With_TypeSerializer(dto), dto);
			try
			{
				AssertEqual(With_TextSerializer(dto), dto);
			}
			catch (Exception ex)
			{
				Log("AssertEqual Error in TextSerializer: {0}", ex);
			}
		}

		protected void RecordRunResults(string serializerName, object serialziedDto,
			Action serializeFn, Action deSerializeFn)
		{
			var dtoString = serialziedDto as string;
			var dtoBytes = serialziedDto as byte[];

			var totalSerializationTicks = GetTotalTicksTakenForAllIterations(
				serializeFn, serializerName + " Serializing");

			var totalDeserializationTicks = GetTotalTicksTakenForAllIterations(
				deSerializeFn, serializerName + " Deserializing");

			var result = new SerializersBenchmarkEntry {
				Iterations = this.MultipleIterations.Sum(),
				ModelName = this.ModelName,
				SerializerName = serializerName,
				SerializedBytesLength = dtoString != null
					? Encoding.UTF8.GetBytes(dtoString).Length
					: dtoBytes.Length,
				TotalSerializationTicks = totalSerializationTicks,
				TotalDeserializationTicks = totalDeserializationTicks,
			};
			TestResults.Add(result);

			Log("Len: " + result.SerializedBytesLength);
			Log("Total: " + result.AvgTicksPerIteration);
		}

		protected void SerializeDto<T>(T dto)
		{
			TestResults = new List<SerializersBenchmarkEntry>();
			FixtureTestResults.Add(TestResults);
			this.ModelName = typeof(T).IsGenericType
				&& typeof(T).GetGenericTypeDefinition() == typeof(List<>)
				? typeof(T).GetGenericArguments()[0].Name
				: typeof(T).Name;

			var dtoXml = DataContractSerializer.Instance.Parse(dto);
			RecordRunResults("Microsoft DataContractSerializer", dtoXml,
				() => DataContractSerializer.Instance.Parse(dto),
				() => DataContractDeserializer.Instance.Parse<T>(dtoXml)
			);

			var dtoJson = JsonDataContractSerializer.Instance.Parse(dto);
			RecordRunResults("Microsoft JsonDataContractSerializer", dtoJson,
				() => JsonDataContractSerializer.Instance.Parse(dto),
				() => JsonDataContractDeserializer.Instance.Parse<T>(dtoJson)
			);

			if (this.MultipleIterations.Sum() <= 10)
			{
				//To slow to include, up to 280x slower than ProtoBuf
				var js = new JavaScriptSerializer();
				var dtoJs = js.Serialize(dto);
				RecordRunResults("Microsoft JavaScriptSerializer", dtoJs,
					() => js.Serialize(dto),
					() => js.Deserialize<T>(dtoJs)
				);

				//Can't import complex types, e.g. Lists, etc
				//var jayRockString = Jayrock.Json.Conversion.JsonConvert.ExportToString(dto);
				//RecordRunResults("JayRock JsonConvert", jayRockString,
				//    () => Jayrock.Json.Conversion.JsonConvert.ExportToString(dto),
				//    () => Jayrock.Json.Conversion.JsonConvert.Import(typeof(T), jayRockString)
				//);
			}

			var msBytes = BinaryFormatterSerializer.Instance.Serialize(dto);
			RecordRunResults("Microsoft BinaryFormatter", msBytes,
				() => BinaryFormatterSerializer.Instance.Serialize(dto),
				() => BinaryFormatterDeserializer.Instance.Deserialize<T>(msBytes)
			);

			var dtoJsonNet = JsonConvert.SerializeObject(dto);
			RecordRunResults("NewtonSoft.Json", dtoJsonNet,
				() => JsonConvert.SerializeObject(dto),
				() => JsonConvert.DeserializeObject<T>(dtoJsonNet)
			);

			var dtoProtoBuf = ProtoBufToBytes(dto);
			RecordRunResults("ProtoBuf.net", dtoProtoBuf,
				() => ProtoBufToBytes(dto),
				() => ProtoBufFromBytes<T>(dtoProtoBuf)
			);

			var dtoString = TypeSerializer.SerializeToString(dto);
			RecordRunResults("ServiceStack TypeSerializer", dtoString,
				() => TypeSerializer.SerializeToString(dto),
				() => TypeSerializer.DeserializeFromString<T>(dtoString)
			);

			//Propietary library, not freely available.
			//var dtoPlatformText = TextSerializer.SerializeToString(dto);
			//RecordRunResults("Platform TextSerializer", dtoPlatformText,
			//    () => TextSerializer.SerializeToString(dto),
			//    () => TextSerializer.DeserializeFromString<T>(dtoPlatformText)
			//);

			CalculateBestTimes(TestResults);
		}

		private static void CalculateBestTimes(IEnumerable<SerializersBenchmarkEntry> testResults)
		{
			try
			{
				//omit serializer scores that fail and to serialize
				var modelsWithAtLeastOneFailedToSerialise = testResults.Any(x => !x.Success);

				if (modelsWithAtLeastOneFailedToSerialise) return;

				var smallestTime = testResults.ConvertAll(x => x.TotalTicks).Min();

				var smallestSize = testResults.ConvertAll(x => x.SerializedBytesLength).Min();

				testResults.ForEach(x =>
					x.TimesSlowerThanBest = Math.Round(x.TotalTicks / (decimal)smallestTime, 2));

				testResults.ForEach(x =>
					x.TimesLargerThanBest = Math.Round(x.SerializedBytesLength / (decimal)smallestSize, 2));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error trying to calculate the best times: {0}\n{1}", ex.Message, ex);
			}
		}

		public void GenerateHtmlReport(string htmlSummary)
		{
			var path = "~/_Results/Serialization/".MapAbsolutePath()
				+ string.Format("{0}.{1}-times.{2:yyyy-MM-dd}.html",
					GetType().Name, this.MultipleIterations.Sum(), DateTime.Now);

			File.WriteAllText(path, this.ToHtmlReport(htmlSummary));
		}
	}
}