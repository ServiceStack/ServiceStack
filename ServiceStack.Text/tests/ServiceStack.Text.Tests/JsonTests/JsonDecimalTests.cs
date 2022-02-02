using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class JsonDecimalTests
    {
        public class MyOrderBook
        {
            public decimal[][] Bids { get; set; }
        }

        [Test]
        public void Does_parse_float_exp_notation_into_decimal()
        {
            var json = @"{""bids"": [[""0.01985141"", 7.67e-6],[""0.01985141"", 7.67e-6]] }";
            var response = json.FromJson<MyOrderBook>();
            Assert.That(response.Bids, Is.Not.Null);
            Assert.That(response.Bids[0][1], Is.EqualTo(0.00000767m));
            Assert.That(response.Bids[1][0], Is.EqualTo(0.01985141m));
        }
    }
}