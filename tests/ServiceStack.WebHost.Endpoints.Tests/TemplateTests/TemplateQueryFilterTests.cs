using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateQueryFilterTests
    {
        private static TemplateContext CreateContext(Dictionary<string, object> optionalArgs = null)
        {
            var context = new TemplateContext
            {
                Args =
                {
                    ["numbers"] = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 },
                    ["products"] = TemplateQueryData.Products,
                    ["customers"] = TemplateQueryData.Customers,
                    ["digits"] = new[]{ "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
                    ["strings"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
                }
            };
            optionalArgs.Each((key, val) => context.Args[key] = val);
            return context.Init();
        }

        [Test]
        public void Linq01() // alternative with clean whitespace sensitive string argument syntax:
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Numbers < 5:
{{ numbers | where('it < 5') | select: { it }\n }}").NormalizeNewLines(), 
                
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
        public void Linq02() // alternative with clean whitespace sensitive string argument syntax:
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Sold out products:
{{ products 
    | where: it.UnitsInStock = 0 
    | select: { it.productName | raw } is sold out!\n }}
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
        public void Linq03()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
In-stock products that cost more than 3.00:
{{ products 
    | where: it.UnitsInStock > 0 and it.UnitPrice > 3 
    | select: { it.productName | raw } is in stock and costs more than 3.00.\n 
}}
").NormalizeNewLines(),
                
                Does.StartWith(@"
In-stock products that cost more than 3.00:
Chai is in stock and costs more than 3.00.
Chang is in stock and costs more than 3.00.
Aniseed Syrup is in stock and costs more than 3.00.
".NormalizeNewLines()));
        }

        [Test]
        public void Linq04()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {TemplateConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });
            
            context.VirtualFiles.WriteFile("customer.html", @"
Customer {{ it.CustomerId }} {{ it.CompanyName | raw }}
{{ it.Orders | selectPartial: order }}");

            context.VirtualFiles.WriteFile("order.html", "  Order {{ it.OrderId }}: {{ it.OrderDate | dateFormat }}\n");
            
            Assert.That(context.EvaluateTemplate(@"
{{ customers 
   | where: it.Region = 'WA' 
   | assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers | selectPartial: customer }}
").NormalizeNewLines(),
                
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
        public void Linq05()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Short digits:
{{ digits 
   | where: it.Length < index
   | select: The word {it} is shorter than its value.\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Short digits:
The word five is shorter than its value.
The word six is shorter than its value.
The word seven is shorter than its value.
The word eight is shorter than its value.
The word nine is shorter than its value.
".NormalizeNewLines()));
        }

        [Test]
        public void Linq06()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Numbers + 1:
{{ numbers | select: { it | incr }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Numbers + 1:
6
5
2
4
10
9
7
8
3
1
".NormalizeNewLines()));
        }

        [Test]
        public void Linq07()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Product Names:
{{ products | select: { it.ProductName | raw }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Product Names:
Chai
Chang
Aniseed Syrup
Chef Anton's Cajun Seasoning
Chef Anton's Gumbo Mix
".NormalizeNewLines()));
        }

        [Test]
        public void Linq08()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Number strings:
{{ numbers | select: { strings[it] }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Number strings:
five
four
one
three
nine
eight
six
seven
two
zero
".NormalizeNewLines()));
        }

        [Test]
        public void Linq09()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {"words", new[]{ "aPPLE", "BlUeBeRrY", "cHeRry" }}
            });
            
            Assert.That(context.EvaluateTemplate(@"
{{ words | select: Uppercase: { it | upper }, Lowercase: { it | lower }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Uppercase: APPLE, Lowercase: apple
Uppercase: BLUEBERRY, Lowercase: blueberry
Uppercase: CHERRY, Lowercase: cherry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq10()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
{{ numbers | select: The digit { strings[it] } is { 'even' | if (isEven(it)) | otherwise('odd') }.\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
The digit five is odd.
The digit four is even.
The digit one is odd.
The digit three is odd.
The digit nine is odd.
The digit eight is even.
The digit six is even.
The digit seven is odd.
The digit two is even.
The digit zero is even.
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Product Info:
{{ products | select: { it.ProductName | raw } is in the category { it.Category } and costs { it.UnitPrice | currency } per unit.\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs $18.00 per unit.
Chang is in the category Beverages and costs $19.00 per unit.
Aniseed Syrup is in the category Condiments and costs $10.00 per unit.
".NormalizeNewLines()));
        }

        [Test]
        public void Linq12()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Number: In-place?
{{ numbers | select: { it }: { it | equals(index) | lower }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Number: In-place?
5: false
4: false
1: false
3: true
9: false
8: false
6: true
7: true
2: false
0: false
".NormalizeNewLines()));
        }

        [Test]
        public void Linq13()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Numbers < 5:
{{ numbers
   | where: it < 5 
   | select: { digits[it] }\n 
}}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Numbers < 5:
four
one
three
two
zero
".NormalizeNewLines()));
        }

        [Test]
        public void Linq14()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {"numbersA", new[]{ 0, 2, 4, 5, 6, 8, 9 }},
                {"numbersB", new[]{ 1, 3, 5, 7, 8 }},
            });
            
            Assert.That(context.EvaluateTemplate(@"
Pairs where a < b:
{{ numbersA | zip(numbersB)
   | let({ a: 'it[0]', b: 'it[1]' })  
   | where: a < b 
   | select: { a } is less than { b }\n 
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
        public void Linq15()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
{{ customers | zip: it.Orders
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: o.Total < 500
   | select: ({ c.CustomerId }, { o.OrderId }, { o.Total | format('0.0#') })\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
(ALFKI, 10702, 330.0)
(ALFKI, 10952, 471.2)
(ANATR, 10308, 88.8)
(ANATR, 10625, 479.75)
".NormalizeNewLines()));
            
        }
    }
}