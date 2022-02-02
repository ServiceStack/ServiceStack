using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public struct UserStat
    {
        public Guid UserId { get; set; }

        public int TimesRecommended { get; set; }

        public int TimesPurchased { get; set; }

        public int TimesFlowed { get; set; }

        public int TimesPreviewed { get; set; }

        public int GetWeightedValue()
        {
            return (this.TimesRecommended * 10)
                   + (this.TimesPurchased * 3)
                   + (this.TimesFlowed * 2)
                   + this.TimesPreviewed;
        }

        public void Add(UserStat userStat)
        {
            this.TimesRecommended += userStat.TimesRecommended;
            this.TimesFlowed += userStat.TimesFlowed;
            this.TimesPreviewed += userStat.TimesPreviewed;
            this.TimesPurchased += userStat.TimesPurchased;
        }

        public static UserStat Parse(string userStatString)
        {
            var parts = userStatString.Split(':');
            if (parts.Length != 6)
                throw new ArgumentException("userStatString must have 6 parts");

            var i = 0;
            var userStat = new UserStat
            {
                UserId = new Guid(parts[i++]),
                TimesRecommended = int.Parse(parts[i++]),
                TimesPurchased = int.Parse(parts[i++]),
                TimesFlowed = int.Parse(parts[i++]),
                TimesPreviewed = int.Parse(parts[i++]),
            };
            return userStat;
        }

        public override string ToString() => 
            $"{this.UserId:n}:{TimesRecommended}:{TimesPurchased}:{TimesRecommended}:{TimesPreviewed}:{GetWeightedValue()}";
    }

    [TestFixture]
    public class CustomStructTests
        : TestBase
    {
        private static UserStat CreateUserStat(Guid userId, int score)
        {
            return new UserStat
            {
                UserId = userId,
                TimesRecommended = score,
                TimesPurchased = score,
                TimesFlowed = score,
                TimesPreviewed = score
            };
        }

        [Test]
        public void Can_serialize_empty_UserStat()
        {
            var userStat = new UserStat();
            var dtoStr = TypeSerializer.SerializeToString(userStat);

            Assert.That(dtoStr, Is.EqualTo("\"00000000000000000000000000000000:0:0:0:0:0\""));

            SerializeAndCompare(userStat);
        }

        [Test]
        public void Can_serialize_UserStat()
        {
            var userId = new Guid("96d7a49f7a0f46918661217995c5e4cc");
            var userStat = CreateUserStat(userId, 1);
            var dtoStr = TypeSerializer.SerializeToString(userStat);

            Assert.That(dtoStr, Is.EqualTo("\"96d7a49f7a0f46918661217995c5e4cc:1:1:1:1:16\""));

            SerializeAndCompare(userStat);
        }
#if !IOS
        [Test]
        public void Can_serialize_UserStats_list()
        {
            var guidValues = new[] {
                  new Guid("6203A3AF-1738-4CDF-A3AD-0F578AD198F0"),
                  new Guid("C7C87DF5-4821-400D-B9F7-D8EEE23C5842"),
                  new Guid("33EB45D4-21A0-41CC-A07D-43BFAB4B3E92"),
                  new Guid("ED041F82-572A-41CB-90D3-E227786BE9EB"),
                  new Guid("D703F00C-613A-44A9-AC2B-C46ED0F23D3C"),
              };

            var userStats = 5.Times(i => CreateUserStat(guidValues[i], i));
            var dtoStr = TypeSerializer.SerializeToString(userStats);

            Assert.That(dtoStr, Is.EqualTo(
                "[\"6203a3af17384cdfa3ad0f578ad198f0:0:0:0:0:0\",\"c7c87df54821400db9f7d8eee23c5842:1:1:1:1:16\",\"33eb45d421a041cca07d43bfab4b3e92:2:2:2:2:32\",\"ed041f82572a41cb90d3e227786be9eb:3:3:3:3:48\",\"d703f00c613a44a9ac2bc46ed0f23d3c:4:4:4:4:64\"]"));

            SerializeAndCompare(userStats);
        }
#endif
    }
}