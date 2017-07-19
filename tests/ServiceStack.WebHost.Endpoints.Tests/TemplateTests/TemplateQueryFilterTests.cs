using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;
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
                    [TemplateConstants.DefaultDateFormat] = "yyyy/MM/dd",
                    ["numbers"] = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 },
                    ["products"] = TemplateQueryData.Products,
                    ["customers"] = TemplateQueryData.Customers,
                    ["digits"] = new[]{ "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
                    ["strings"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
                    ["words"] = new[]{"cherry", "apple", "blueberry"},
                    ["doubles"] = new[]{ 1.7, 2.3, 1.9, 4.1, 2.9 },
                    ["anagrams"] = new[]{ "from   ", " salt", " earn ", "  last   ", " near ", " form  " },
                    ["comparer"] = new CaseInsensitiveComparer(),
                    ["anagramComparer"] = new AnagramEqualityComparer(),
                }
            };
            optionalArgs.Each((key, val) => context.Args[key] = val);
            return context.Init();
        }

        [SetUp]
        public void Setup() => context = CreateContext();
        private TemplateContext context;

        [Test]
        public void Linq01() // alternative with clean whitespace sensitive string argument syntax:
        {
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
            Assert.That(context.EvaluateTemplate(@"
{{ ['aPPLE', 'BlUeBeRrY', 'cHeRry'] | assignTo: words }}
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
            Assert.That(context.EvaluateTemplate(@"
{{ [0, 2, 4, 5, 6, 8, 9] | assignTo: numbersA }}
{{ [1, 3, 5, 7, 8] | assignTo: numbersB }}
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
        
        [Test]
        public void Linq16()
        {
            Assert.That(context.EvaluateTemplate(@"
{{ customers | zip: it.Orders
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: o.OrderDate >= '1998-01-01' 
   | select: ({ c.CustomerId }, { o.OrderId }, { o.OrderDate })\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
(ALFKI, 10835, 1/15/1998 12:00:00 AM)
(ALFKI, 10952, 3/16/1998 12:00:00 AM)
(ALFKI, 11011, 4/9/1998 12:00:00 AM)
(ANATR, 10926, 3/4/1998 12:00:00 AM)
(ANTON, 10856, 1/28/1998 12:00:00 AM)
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq17()
        {
            Assert.That(context.EvaluateTemplate(@"
{{ customers | zip: it.Orders
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: o.Total >= 2000 
   | select: ({ c.CustomerId }, { o.OrderId }, { o.Total | format('0.0#') })\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
(ANTON, 10573, 2082.0)
(AROUT, 10558, 2142.9)
(AROUT, 10953, 4441.25)
(BERGS, 10384, 2222.4)
(BERGS, 10524, 3192.65)
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq18()
        {
            var template = @"
{{ '1997-01-01' | assignTo: cutoffDate }}
{{ customers 
   | where: it.Region = 'WA'
   | zip: it.Orders
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: o.OrderDate  >= cutoffDate 
   | select: ({ c.CustomerId }, { o.OrderId })\n }}
";
            Assert.That(context.EvaluateTemplate(template.NormalizeNewLines()).NormalizeNewLines(),
                
                Does.StartWith(@"
(LAZYK, 10482)
(LAZYK, 10545)
(TRAIH, 10574)
(TRAIH, 10577)
(TRAIH, 10822)
(WHITC, 10469)
(WHITC, 10483)
(WHITC, 10504)
(WHITC, 10596)
(WHITC, 10693)
(WHITC, 10696)
(WHITC, 10723)
(WHITC, 10740)
(WHITC, 10861)
(WHITC, 10904)
(WHITC, 11032)
(WHITC, 11066)
".NormalizeNewLines()));
        }

        [Test]
        public void Linq19()
        {
            Assert.That(context.EvaluateTemplate(@"
{{ customers 
   | let({ cust: 'it', custIndex: 'index' })
   | zip: cust.Orders
   | let({ o: 'it[1]' })
   | select: Customer #{ custIndex | incr } has an order with OrderID { o.OrderId }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Customer #1 has an order with OrderID 10643
Customer #1 has an order with OrderID 10692
Customer #1 has an order with OrderID 10702
Customer #1 has an order with OrderID 10835
Customer #1 has an order with OrderID 10952
Customer #1 has an order with OrderID 11011
Customer #2 has an order with OrderID 10308
Customer #2 has an order with OrderID 10625
Customer #2 has an order with OrderID 10759
Customer #2 has an order with OrderID 10926
".NormalizeNewLines()));
        }

        [Test]
        public void Linq20()
        {
            Assert.That(context.EvaluateTemplate(@"
First 3 numbers:
{{ numbers | take(3) | select: { it }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
First 3 numbers:
5
4
1
".NormalizeNewLines()));
        }

        [Test]
        public void Linq21()
        {
            Assert.That(context.EvaluateTemplate(@"
First 3 orders in WA:
{{ customers | zip: it.Orders 
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: c.Region = 'WA'
   | select: { [c.CustomerId, o.OrderId, o.OrderDate] | jsv }\n 
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
        public void Linq22()
        {
            Assert.That(context.EvaluateTemplate(@"
All but first 4 numbers:
{{ numbers | skip(4) | select: { it }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
All but first 4 numbers:
9
8
6
7
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq23()
        {
            Assert.That(context.EvaluateTemplate(@"
All but first 2 orders in WA:
{{ customers | zip: it.Orders
   | let({ c: 'it[0]', o: 'it[1]' })
   | where: c.Region = 'WA'
   | skip(2)
   | select: { [c.CustomerId, o.OrderId, o.OrderDate] | jsv }\n 
}}
").NormalizeNewLines(),
                
                Does.StartWith(@"
All but first 2 orders in WA:
[TRAIH,10574,1997-06-19]
[TRAIH,10577,1997-06-23]
[TRAIH,10822,1998-01-08]
[WHITC,10269,1996-07-31]
[WHITC,10344,1996-11-01]
[WHITC,10469,1997-03-10]
[WHITC,10483,1997-03-24]
[WHITC,10504,1997-04-11]
[WHITC,10596,1997-07-11]
[WHITC,10693,1997-10-06]
[WHITC,10696,1997-10-08]
[WHITC,10723,1997-10-30]
[WHITC,10740,1997-11-13]
[WHITC,10861,1998-01-30]
[WHITC,10904,1998-02-24]
[WHITC,11032,1998-04-17]
[WHITC,11066,1998-05-01]
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq24()
        { 
            Assert.That(context.EvaluateTemplate(@"
First numbers less than 6:
{{ numbers 
   | takeWhile: it < 6 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
First numbers less than 6:
5
4
1
3
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq25()
        { 
            Assert.That(context.EvaluateTemplate(@"
First numbers not less than their position:
{{ numbers 
   | takeWhile: it >= index 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
First numbers not less than their position:
5
4
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq26()
        { 
            Assert.That(context.EvaluateTemplate(@"
All elements starting from first element divisible by 3:
{{ numbers 
   | skipWhile: mod(it,3) != 0 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
All elements starting from first element divisible by 3:
3
9
8
6
7
2
0
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq27()
        { 
            Assert.That(context.EvaluateTemplate(@"
All elements starting from first element less than its position:
{{ numbers 
   | skipWhile: it >= index 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
All elements starting from first element less than its position:
1
3
9
8
6
7
2
0
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq28()
        { 
            Assert.That(context.EvaluateTemplate(@"
The sorted list of words:
{{ words 
   | orderBy: it 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
The sorted list of words:
apple
blueberry
cherry
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq29()
        { 
            Assert.That(context.EvaluateTemplate(@"
The sorted list of words (by length):
{{ words 
   | orderBy: it.Length 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
The sorted list of words (by length):
apple
cherry
blueberry
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq30()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products 
   | orderBy: it.ProductName 
   | select: { it | jsv }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
{ProductId:17,ProductName:Alice Mutton,Category:Meat/Poultry,UnitPrice:39,UnitsInStock:0}
{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13}
{ProductId:40,ProductName:Boston Crab Meat,Category:Seafood,UnitPrice:18.4,UnitsInStock:123}
{ProductId:60,ProductName:Camembert Pierrot,Category:Dairy Products,UnitPrice:34,UnitsInStock:19}
{ProductId:18,ProductName:Carnarvon Tigers,Category:Seafood,UnitPrice:62.5,UnitsInStock:42}
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq31()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] | assignTo: words }}
{{ words 
   | orderBy('it', { comparer }) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
AbAcUs
aPPLE
BlUeBeRrY
bRaNcH
cHeRry
ClOvEr
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq32()
        { 
            Assert.That(context.EvaluateTemplate(@"
The doubles from highest to lowest:
{{ doubles 
   | orderByDescending: it 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
The doubles from highest to lowest:
4.1
2.9
2.3
1.9
1.7
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq33()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products 
   | orderByDescending: it.UnitsInStock
   | select: { it | jsv }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
{ProductId:75,ProductName:Rhönbräu Klosterbier,Category:Beverages,UnitPrice:7.75,UnitsInStock:125}
{ProductId:40,ProductName:Boston Crab Meat,Category:Seafood,UnitPrice:18.4,UnitsInStock:123}
{ProductId:6,ProductName:Grandma's Boysenberry Spread,Category:Condiments,UnitPrice:25,UnitsInStock:120}
{ProductId:55,ProductName:Pâté chinois,Category:Meat/Poultry,UnitPrice:24,UnitsInStock:115}
{ProductId:61,ProductName:Sirop d'érable,Category:Condiments,UnitPrice:28.5,UnitsInStock:113}
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq34()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] | assignTo: words }}
{{ words 
   | orderByDescending('it', { comparer }) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
ClOvEr
cHeRry
bRaNcH
BlUeBeRrY
aPPLE
AbAcUs
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq35()
        { 
            Assert.That(context.EvaluateTemplate(@"
Sorted digits:
{{ digits 
   | orderBy: it.length
   | thenBy: it
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Sorted digits:
one
six
two
five
four
nine
zero
eight
seven
three
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq36()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] | assignTo: words }}
{{ words 
   | orderBy: it.length
   | thenBy('it', { comparer }) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
aPPLE
AbAcUs
bRaNcH
cHeRry
ClOvEr
BlUeBeRrY
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq37()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products 
   | orderBy: it.Category
   | thenByDescending: it.UnitPrice
   | select: { it | jsv }\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17}
{ProductId:43,ProductName:Ipoh Coffee,Category:Beverages,UnitPrice:46,UnitsInStock:17}
{ProductId:2,ProductName:Chang,Category:Beverages,UnitPrice:19,UnitsInStock:17}
{ProductId:1,ProductName:Chai,Category:Beverages,UnitPrice:18,UnitsInStock:39}
{ProductId:35,ProductName:Steeleye Stout,Category:Beverages,UnitPrice:18,UnitsInStock:20}
{ProductId:39,ProductName:Chartreuse verte,Category:Beverages,UnitPrice:18,UnitsInStock:69}
{ProductId:76,ProductName:Lakkalikööri,Category:Beverages,UnitPrice:18,UnitsInStock:57}
{ProductId:70,ProductName:Outback Lager,Category:Beverages,UnitPrice:15,UnitsInStock:15}
{ProductId:34,ProductName:Sasquatch Ale,Category:Beverages,UnitPrice:14,UnitsInStock:111}
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq38()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] | assignTo: words }}
{{ words 
   | orderBy: it.length
   | thenByDescending('it', { comparer }) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
aPPLE
ClOvEr
cHeRry
bRaNcH
AbAcUs
BlUeBeRrY
".NormalizeNewLines()));
        }
         
        [Test]
        public void Linq39()
        { 
            Assert.That(context.EvaluateTemplate(@"
A backwards list of the digits with a second character of 'i':
{{ digits 
   | where: it[1] = 'i'
   | reverse
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
A backwards list of the digits with a second character of 'i':
nine
eight
six
five
".NormalizeNewLines()));
        }

        [Test]
        public void Linq40()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ numbers 
   | groupBy: mod(it,5)
   | let({ remainder: 'it.Key', numbers: 'it' })
   | select: Numbers with a remainder of { remainder } when divided by 5:\n{ numbers | select('{it}\n') } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Numbers with a remainder of 0 when divided by 5:
5
0
Numbers with a remainder of 4 when divided by 5:
4
9
Numbers with a remainder of 1 when divided by 5:
1
6
Numbers with a remainder of 3 when divided by 5:
3
8
Numbers with a remainder of 2 when divided by 5:
7
2
".NormalizeNewLines()));
        }

        [Test]
        public void Linq41()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['blueberry', 'chimpanzee', 'abacus', 'banana', 'apple', 'cheese'] | assignTo: words }}
{{ words 
   | groupBy: it[0]
   | let({ firstLetter: 'it.Key', words: 'it' })
   | select: Words that start with the letter '{firstLetter}':\n{ words | select('{it}\n') } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Words that start with the letter 'b':
blueberry
banana
Words that start with the letter 'c':
chimpanzee
cheese
Words that start with the letter 'a':
abacus
apple
".NormalizeNewLines()));
        }

        [Test]
        public void Linq42()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products 
   | groupBy: it.Category
   | let({ category: 'it.Key', products: 'it' })
   | select: {category}:\n{ products | select('{it | jsv}\n') } }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages:
{ProductId:1,ProductName:Chai,Category:Beverages,UnitPrice:18,UnitsInStock:39}
{ProductId:2,ProductName:Chang,Category:Beverages,UnitPrice:19,UnitsInStock:17}
{ProductId:24,ProductName:Guaraná Fantástica,Category:Beverages,UnitPrice:4.5,UnitsInStock:20}
{ProductId:34,ProductName:Sasquatch Ale,Category:Beverages,UnitPrice:14,UnitsInStock:111}
{ProductId:35,ProductName:Steeleye Stout,Category:Beverages,UnitPrice:18,UnitsInStock:20}
{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17}
{ProductId:39,ProductName:Chartreuse verte,Category:Beverages,UnitPrice:18,UnitsInStock:69}
{ProductId:43,ProductName:Ipoh Coffee,Category:Beverages,UnitPrice:46,UnitsInStock:17}
{ProductId:67,ProductName:Laughing Lumberjack Lager,Category:Beverages,UnitPrice:14,UnitsInStock:52}
{ProductId:70,ProductName:Outback Lager,Category:Beverages,UnitPrice:15,UnitsInStock:15}
{ProductId:75,ProductName:Rhönbräu Klosterbier,Category:Beverages,UnitPrice:7.75,UnitsInStock:125}
{ProductId:76,ProductName:Lakkalikööri,Category:Beverages,UnitPrice:18,UnitsInStock:57}
Condiments:
{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13}
{ProductId:4,ProductName:Chef Anton's Cajun Seasoning,Category:Condiments,UnitPrice:22,UnitsInStock:53}
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq43()
        { 
            context.VirtualFiles.WriteFile("month-orders.html", @"
{{ year }}
{{ monthGroups | select: { indent }{ month }\n{ 2 | indents }{ orders | jsv }\n }}");
            
            Assert.That(context.EvaluateTemplate(@"
{{ customers 
   | let({ 
        companyName: 'it.CompanyName', 
        yearGroups: ""map (
                        groupBy(it.Orders, 'it.OrderDate.Year'),
                        '{ 
                            year: it.Key, 
                            monthGroups: map (
                                groupBy(it, `it.OrderDate.Month`),
                                `{ month: it.Key, orders: it }`
                            ) 
                        }'
                    )"" 
     })
   | select: \n# { companyName | raw }{ yearGroups | selectPartial('month-orders') } 
}}
").NormalizeNewLines(),
                
                Does.StartWith(@"
# Alfreds Futterkiste
1997
	8
		[{OrderId:10643,OrderDate:1997-08-25,Total:814.5}]
	10
		[{OrderId:10692,OrderDate:1997-10-03,Total:878},{OrderId:10702,OrderDate:1997-10-13,Total:330}]

1998
	1
		[{OrderId:10835,OrderDate:1998-01-15,Total:845.8}]
	3
		[{OrderId:10952,OrderDate:1998-03-16,Total:471.2}]
	4
		[{OrderId:11011,OrderDate:1998-04-09,Total:933.5}]

# Ana Trujillo Emparedados y helados
1996
	9
		[{OrderId:10308,OrderDate:1996-09-18,Total:88.8}]
".NormalizeNewLines()));
        }
 
        public class AnagramEqualityComparer : IEqualityComparer<string> 
        {
            public bool Equals(string x, string y) => GetCanonicalString(x) == GetCanonicalString(y);
            public int GetHashCode(string obj) => GetCanonicalString(obj).GetHashCode();
            private string GetCanonicalString(string word) 
            {
                var wordChars = word.ToCharArray();
                Array.Sort(wordChars);
                return new string(wordChars);
            }
        }
        
        [Test]
        public void Linq44()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ anagrams 
   | groupBy('trim(it)', { comparer: anagramComparer })
   | select: { it | json }\n 
}}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
[""from   "","" form  ""]
["" salt"",""  last   ""]
["" earn "","" near ""]
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq45()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ anagrams 
   | groupBy('trim(it)', { map: 'upper(it)', comparer: anagramComparer })
   | select: { it | json }\n 
}}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
[""FROM   "","" FORM  ""]
["" SALT"",""  LAST   ""]
["" EARN "","" NEAR ""]
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq46()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [2, 2, 3, 5, 5] | assignTo: factorsOf300 }}
Prime factors of 300:
{{ factorsOf300 | distinct | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Prime factors of 300:
2
3
5
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq47()
        { 
            Assert.That(context.EvaluateTemplate(@"
Category names:
{{ products 
   | map: it.Category 
   | distinct
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Category names:
Beverages
Condiments
Produce
Meat/Poultry
Seafood
Dairy Products
Confections
Grains/Cereals
".NormalizeNewLines()));
        }
         
        [Test]
        public void Linq48()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] | assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] | assignTo: numbersB }}
Unique numbers from both arrays:
{{ numbersA | union(numbersB) | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Unique numbers from both arrays:
0
2
4
5
6
8
9
1
3
7
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq49()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products  
   | map: it.ProductName[0] 
   | assignTo: productFirstChars }}
{{ customers 
   | map: it.CompanyName[0] 
   | assignTo: customerFirstChars }}
Unique first letters from Product names and Customer names:
{{ productFirstChars 
   | union(customerFirstChars) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Unique first letters from Product names and Customer names:
C
A
G
U
N
M
I
Q
K
T
P
S
R
B
J
Z
V
F
E
W
L
O
D
H
".NormalizeNewLines()));
        }

        [Test]
        public void Linq50()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] | assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] | assignTo: numbersB }}
Common numbers shared by both arrays:
{{ numbersA | intersect(numbersB) | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Common numbers shared by both arrays:
5
8
".NormalizeNewLines()));
        }
        
        [Test]
        public void Linq51()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products  
   | map: it.ProductName[0] 
   | assignTo: productFirstChars }}
{{ customers 
   | map: it.CompanyName[0] 
   | assignTo: customerFirstChars }}
Common first letters from Product names and Customer names:
{{ productFirstChars 
   | intersect(customerFirstChars) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Common first letters from Product names and Customer names:
C
A
G
N
M
I
Q
K
T
P
S
R
B
V
F
E
W
L
O
".NormalizeNewLines()));
        }
 
        [Test]
        public void Linq52()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] | assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] | assignTo: numbersB }}
Numbers in first array but not second array:
{{ numbersA | except(numbersB) | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Numbers in first array but not second array:
0
2
4
6
9
".NormalizeNewLines()));
        }
         
        [Test]
        public void Linq53()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products  
   | map: it.ProductName[0] 
   | assignTo: productFirstChars }}
{{ customers 
   | map: it.CompanyName[0] 
   | assignTo: customerFirstChars }}
First letters from Product names, but not from Customer names:
{{ productFirstChars 
   | except(customerFirstChars) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
First letters from Product names, but not from Customer names:
U
J
Z
".NormalizeNewLines()));
        }
 
        [Test]
        public void Linq54()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 1.7, 2.3, 1.9, 4.1, 2.9 ] | assignTo: doubles }}
Every other double from highest to lowest:
{{ doubles 
   | orderByDescending: it
   | step({ by: 2 }) 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Every other double from highest to lowest:
4.1
2.3
1.7
".NormalizeNewLines()));
        }
 
        [Test]
        public void Linq55()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 'cherry', 'apple', 'blueberry' ] | assignTo: words }}
The sorted word list:
{{ words
   | orderBy: it 
   | toList 
   | select: { it }\n }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
The sorted word list:
apple
blueberry
cherry
".NormalizeNewLines()));
        }
 
        [Test]
        public void Linq56()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [{name:'Alice', score:50}, {name: 'Bob', score:40}, {name:'Cathy', score:45}] | assignTo: scoreRecords }}
Bob's score: 
{{ scoreRecords 
   | toDictionary: it.name
   | map: it['Bob']
   | select: { it['name'] } = { it['score'] }
}} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Bob's score: 
Bob = 40
".NormalizeNewLines()));
        }
 
        [Test]
        public void Linq57()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [null, 1.0, 'two', 3, 'four', 5, 'six', 7.0] | assignTo: numbers }}
Numbers stored as doubles:
{{ numbers 
   | of({ type: 'Double' })
   | select: { it | format('#.0') }\n }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Numbers stored as doubles:
1.0
7.0
".NormalizeNewLines()));
        }
  
        [Test]
        public void Linq58()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products
   | where: it.ProductId = 12 
   | first
   | select: { it | jsv } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
{ProductId:12,ProductName:Queso Manchego La Pastora,Category:Dairy Products,UnitPrice:38,UnitsInStock:86}
".NormalizeNewLines()));
        }
  
        [Test]
        public void Linq59()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] | assignTo: strings }}
{{ strings
   | first: it[0] = 'o'
   | select: A string starting with 'o': { it } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
A string starting with 'o': one
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq61()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [] | assignTo: numbers }}
{{ numbers | first | select: { it | otherwise('null') } }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
null
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq62()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ products 
   | first: it.ProductId = 789 
   | select: Product 789 exists: { it | otherwise('false') } }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Product 789 exists: false
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq64()
        { 
            Assert.That(context.EvaluateTemplate(@"
{{ [ 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 ] | assignTo: numbers }} 
{{ numbers
   | where: it > 5
   | elementAt(1) 
   | select: Second number > 5: { it } }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Second number > 5: 8
".NormalizeNewLines()));
        }
    }
}