using System;
using System.Diagnostics;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisUtilTests
    {
        [Test]
        public void Can_Calculate_Lexical_Score()
        {
            const string minScore = "AAAA";
            const string nextMinScore = "AAAB";
            const string maxScore = "ZZZZ";

            Assert.That(RedisClient.GetLexicalScore(minScore),
                Is.LessThan(RedisClient.GetLexicalScore(nextMinScore)));

            Assert.That(RedisClient.GetLexicalScore(nextMinScore),
                Is.LessThan(RedisClient.GetLexicalScore(maxScore)));

            Debug.WriteLine(String.Format("Lexical Score of '{0}' is: {1}", minScore, RedisClient.GetLexicalScore(minScore)));
            Debug.WriteLine(String.Format("Lexical Score of '{0}' is: {1}", nextMinScore, RedisClient.GetLexicalScore(nextMinScore)));
            Debug.WriteLine(String.Format("Lexical Score of '{0}' is: {1}", maxScore, RedisClient.GetLexicalScore(maxScore)));
        }
    }

}