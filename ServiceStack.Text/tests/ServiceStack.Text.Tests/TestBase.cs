using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public abstract class TestBase
    {
        protected TestBase()
        {
            //Uncomment to run tests under a different Culture 
            //System.Threading.Thread.CurrentThread.CurrentCulture =
            //    System.Globalization.CultureInfo.GetCultureInfo("sv-SE");
        }

        public virtual void Log(string message, params object[] args)
        {
#if DEBUG
#endif
            if (args.Length > 0)
                Console.WriteLine(message, args);
            else
                Console.WriteLine(message);
        }

        public T Serialize<T>(T model, bool includeXml = true)
        {
            return Serialize(model, false, includeXml);
        }

        public T JsonSerialize<T>(T model)
        {
            return JsonSerialize(model, false);
        }

        public T JsonSerializeAndCompare<T>(T model)
        {
            return JsonSerialize(model, true);
        }

        public T SerializeAndCompare<T>(T model, bool includeXml = true)
        {
            return Serialize(model, true, includeXml);
        }

        private T Serialize<T>(T model, bool assertEqual, bool includeXml)
        {
            var stopwatch = Stopwatch.StartNew();
            var jsv = TypeSerializer.SerializeToString(model);
            stopwatch.Stop();

            var partialJsv = jsv.Length > 100 ? jsv.Substring(0, 100) + "..." : jsv;
            Console.WriteLine("JSV  Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, jsv.Length, partialJsv);

            stopwatch = Stopwatch.StartNew();
            var json = JsonSerializer.SerializeToString(model);
            stopwatch.Stop();

            var partialJson = json.Length > 100 ? json.Substring(0, 100) + "..." : json;
            Console.WriteLine("JSON Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, json.Length, partialJson);

            if (includeXml)
            {
                using (var xmlWriter = new System.IO.StringWriter())
                {
                    stopwatch = Stopwatch.StartNew();
                    XmlSerializer.SerializeToWriter((object)model, xmlWriter);
                    var xml = xmlWriter.ToString();
                    stopwatch.Stop();
                    var partialXml = xml.Length > 100 ? xml.Substring(0, 100) + "..." : xml;
                    Console.WriteLine("XML Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, xml.Length, partialXml);
                }
            }

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
                        string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJson));
                }
                else
                {
                    Assert.That(fromJsvModel, Is.EqualTo(model),
                        string.Format("Deserialized JSV  {0} was not equal to the original\n{1}", typeof(T), partialJsv));

                    Assert.That(fromJsonModel, Is.EqualTo(model),
                        string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJson));
                }
            }

            return fromJsonModel;
        }

        private T JsonSerialize<T>(T model, bool assertEqual)
        {
            var stopwatch = Stopwatch.StartNew();
            var json = JsonSerializer.SerializeToString(model);
            stopwatch.Stop();

            var partialJson = json.Length > 100 ? json.Substring(0, 100) + "..." : json;
            Console.WriteLine("JSON Time: {0} ticks, Len: {1}: {2}", stopwatch.ElapsedTicks, json.Length, partialJson);

            var fromJsonModel = JsonSerializer.DeserializeFromString<T>(json);

            if (assertEqual)
            {
                var enumerableModel = model as IEnumerable;
                if (enumerableModel != null)
                {
                    Assert.That(fromJsonModel, Is.EquivalentTo(enumerableModel),
                        string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJson));
                }
                else
                {
                    Assert.That(fromJsonModel, Is.EqualTo(model),
                        string.Format("Deserialized JSON {0} was not equal to the original\n{1}", typeof(T), partialJson));
                }
            }

            return fromJsonModel;
        }
    }
}