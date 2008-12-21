using System;
using TimeSpan=System.TimeSpan;

namespace ServiceStack.Common.Services.Tests.Support.DataContracts
{
    public class User
    {
        public string Id { get; set; }
        public Guid GlobalId { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }
}