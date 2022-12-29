using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack;

namespace NetCore.Console.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var obj = new { name = "Hello ServiceStack!" };
            var json = obj.ToJson();
            System.Console.WriteLine(json);
        }
    }
}
