using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class QueryFilterAlternativeTests
    {
        private static ScriptContext CreateContext(Dictionary<string, object> optionalArgs = null)
        {
            var context = new ScriptContext
            {
                Args =
                {
                    ["numbers"] = new[] {5, 4, 1, 3, 9, 8, 6, 7, 2, 0},
                    ["products"] = QueryData.Products,
                    ["customers"] = QueryData.Customers,
                }
            };
            optionalArgs.Each((key, val) => context.Args[key] = val);
            return context.Init();
        }

        [Test]
        public void linq1_original()
        {
            var context = CreateContext();

            Assert.That(context.EvaluateScript(@"
Numbers < 5:
{{ numbers |> where('it < 5') |> select('{{ it }}\n') }}").NormalizeNewLines(),

                Is.EqualTo(@"
Numbers < 5:
4
1
3
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void linq2_original()
        {
            var context = CreateContext();

            Assert.That(context.EvaluateScript(@"
Sold out products:
{{ products 
   |> where('it.UnitsInStock == 0') 
   |> select('{{ it.productName |> raw }} is sold out!\n')
}}
").NormalizeNewLines(),

                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out!
Alice Mutton is sold out!
Thüringer Rostbratwurst is sold out!
Gorgonzola Telino is sold out!
Perth Pasties is sold out!
".NormalizeNewLines()));
        }

        [Test]
        public void linq2_original_with_custom_item_binding()
        {
            var context = CreateContext();

            Assert.That(context.EvaluateScript(@"
Sold out products:
{{ products 
   |> where('product.UnitsInStock == 0', { it: 'product' }) 
   |> select('{{ product.productName |> raw }} is sold out!\n', { it: 'product' })
}}
").NormalizeNewLines(),

                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out!
Alice Mutton is sold out!
Thüringer Rostbratwurst is sold out!
Gorgonzola Telino is sold out!
Perth Pasties is sold out!
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {ScriptConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });

            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  |> where: it.Region == 'WA' 
  |> assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers |> selectPartial('customer') }}");

            context.VirtualFiles.WriteFile("customer.html", @"Customer {{ it.CustomerId }} {{ it.CompanyName |> raw }}
{{ it.Orders |> select(""  Order {{ it.OrderId }}: {{ it.OrderDate |> dateFormat |> newLine }}"") }}");

            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial_nested()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {ScriptConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });

            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  |> where: it.Region == 'WA' 
  |> assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers |> selectPartial: customer }}");

            context.VirtualFiles.WriteFile("customer.html",
                @"Customer {{ it.CustomerId }} {{ it.CompanyName |> raw }}
{{ it.Orders |> selectPartial: order }}");

            context.VirtualFiles.WriteFile("order.html", @"  Order {{ it.OrderId }}: {{ it.OrderDate |> dateFormat}}
");

            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }

        [Test]
        public void linq4_selectPartial_nested_with_custom_item_binding()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {ScriptConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });

            context.VirtualFiles.WriteFile("page.html", @"{{ 
  customers 
  |> where: it.Region == 'WA' 
  |> assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers |> selectPartial: customer }}");

            context.VirtualFiles.WriteFile("customer.html",
                @"
<!--
it: cust
-->

Customer {{ cust.CustomerId }} {{ cust.CompanyName |> raw }}
{{ cust.Orders |> selectPartial('order', { it: 'order' })  }}");

            context.VirtualFiles.WriteFile("order.html",
                "  Order {{ order.OrderId }}: {{ order.OrderDate |> dateFormat}}\n");

            Assert.That(new PageResult(context.GetPage("page")).Result.NormalizeNewLines(),
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK Lazy K Kountry Store
  Order 10482: 1997/03/21
  Order 10545: 1997/05/22
Customer TRAIH Trail's Head Gourmet Provisioners
  Order 10574: 1997/06/19
  Order 10577: 1997/06/23
  Order 10822: 1998/01/08
".NormalizeNewLines()));
        }

        [Test]
        public void Linq14_original()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {"numbersA", new[] {0, 2, 4, 5, 6, 8, 9}},
                {"numbersB", new[] {1, 3, 5, 7, 8}},
            });

            Assert.That(context.EvaluateScript(@"
Pairs where a < b:
{{ numbersA |> zip(numbersB)
   |> where: it[0] < it[1] 
   |> select: { it[0] } is less than { it[1] }\n 
}}
").NormalizeNewLines(),

                Does.StartWith(@"
Pairs where a < b:
0 is less than 1
0 is less than 3
0 is less than 5
0 is less than 7
0 is less than 8
2 is less than 3
2 is less than 5
2 is less than 7
2 is less than 8
4 is less than 5
4 is less than 7
4 is less than 8
5 is less than 7
5 is less than 8
6 is less than 7
6 is less than 8
".NormalizeNewLines()));
        }

        [Test]
        public void Linq14_bindings()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {"numbersA", new[] {0, 2, 4, 5, 6, 8, 9}},
                {"numbersB", new[] {1, 3, 5, 7, 8}},
            });

            Assert.That(context.EvaluateScript(@"
Pairs where a < b:
{{ numbersA |> zip(numbersB)
   |> let({ a: 'it[0]', b: 'it[1]' })  
   |> where: a < b 
   |> select: { a } is less than { b }\n 
}}
").NormalizeNewLines(),

                Does.StartWith(@"
Pairs where a < b:
0 is less than 1
0 is less than 3
0 is less than 5
0 is less than 7
0 is less than 8
2 is less than 3
2 is less than 5
2 is less than 7
2 is less than 8
4 is less than 5
4 is less than 7
4 is less than 8
5 is less than 7
5 is less than 8
6 is less than 7
6 is less than 8
".NormalizeNewLines()));
        }

        [Test]
        public void Linq18_whitespace_test()
        {
            var context = CreateContext();

            var template = @"
{{ '1997-01-01' |> assignTo: cutoffDate }}
{{ customers 
   |> where: it.Region == 'WA'
   |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: o.OrderDate  >= cutoffDate 
   |> select: ({ c.CustomerId }, { o.OrderId })\n }}
".NormalizeNewLines();
            Assert.That(context.EvaluateScript(template).NormalizeNewLines(),

                Does.StartWith(@"
(LAZYK, 10482)
(LAZYK, 10545)
(TRAIH, 10574)
".NormalizeNewLines()));
        }

        [Test]
        public void Linq21_jsv()
        {
            var context = CreateContext();

            Assert.That(context.EvaluateScript(@"
First 3 orders in WA:
{{ customers |> zip: it.Orders 
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: c.Region == 'WA'
   |> select: { [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }\n 
}}
").NormalizeNewLines(),

                Does.StartWith(@"
First 3 orders in WA:
[LAZYK,10482,1997-03-21]
[LAZYK,10545,1997-05-22]
[TRAIH,10574,1997-06-19]
".NormalizeNewLines()));
        }

        [Test]
        public void Linq21_json_with_config()
        {
            var context = CreateContext();

            Assert.That(context.EvaluateScript(@"
First 3 orders in WA:
{{ customers |> zip: it.Orders 
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: c.Region == 'WA'
   |> select: { [c.CustomerId, o.OrderId, o.OrderDate] |> json('DateHandler:ISO8601DateOnly') }\n 
}}
").NormalizeNewLines(),

                Does.StartWith(@"
First 3 orders in WA:
[""LAZYK"",10482,""1997-03-21""]
[""LAZYK"",10545,""1997-05-22""]
[""TRAIH"",10574,""1997-06-19""]
".NormalizeNewLines()));
        }
    }
}