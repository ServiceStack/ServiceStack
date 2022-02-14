using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Script;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class AnagramEqualityComparer : IEqualityComparer<string>, IEqualityComparer<object>
    {
        public bool Equals(string x, string y) => GetCanonicalString(x) == GetCanonicalString(y);
        public int GetHashCode(string obj) => GetCanonicalString(obj).GetHashCode();
        private string GetCanonicalString(string word) 
        {
            var wordChars = word.ToCharArray();
            Array.Sort(wordChars);
            return new string(wordChars);
        }

        bool IEqualityComparer<object>.Equals(object x, object y) => Equals((string) x, (string) y);

        public int GetHashCode(object obj) => GetHashCode((string)obj);
    }
        
    public class QueryFilterTests
    {
        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext
            {
                Args =
                {
                    [ScriptConstants.DefaultDateFormat] = "yyyy/MM/dd",
                    ["products"] = QueryData.Products,
                    ["customers"] = QueryData.Customers,
                    ["comparer"] = new CaseInsensitiveComparer(),
                    ["anagramComparer"] = new AnagramEqualityComparer(),
                }
            };
            return context.Init();
        }

        [SetUp]
        public void Setup() => context = CreateContext();
        private ScriptContext context;

        [Test]
        public void Linq01() // alternative with clean whitespace sensitive string argument syntax:
        {
            Assert.That(context.EvaluateScript(@"
Numbers < 5:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> where: it < 5 
   |> select: { it }\n 
}}").NormalizeNewLines(), 
                
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
            Assert.That(context.EvaluateScript(@"
Sold out products:
{{ products 
    |> where: it.UnitsInStock == 0 
    |> select: { it.productName |> raw } is sold out!\n }}
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
            Assert.That(context.EvaluateScript(@"
In-stock products that cost more than 3.00:
{{ products 
    |> where: it.UnitsInStock > 0 and it.UnitPrice > 3 
    |> select: { it.productName |> raw } is in stock and costs more than 3.00.\n 
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
Customer {{ it.CustomerId }} {{ it.CompanyName |> raw }}
{{ it.Orders |> selectPartial: order }}");

            context.VirtualFiles.WriteFile("order.html", "  Order {{ it.OrderId }}: {{ it.OrderDate |> dateFormat }}\n");
            
            Assert.That(context.EvaluateScript(@"
{{ customers 
   |> where: it.Region == 'WA' 
   |> assignTo: waCustomers 
}}
Customers from Washington and their orders:
{{ waCustomers |> selectPartial: customer }}
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
            Assert.That(context.EvaluateScript(@"
Short digits:
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: digits }}
{{ digits 
   |> where: it.Length < index
   |> select: The word {it} is shorter than its value.\n }}
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
            Assert.That(context.EvaluateScript(@"
Numbers + 1:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> select: { it |> incr }\n }}
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
            Assert.That(context.EvaluateScript(@"
Product Names:
{{ products |> select: { it.ProductName |> raw }\n }}
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
            Assert.That(context.EvaluateScript(@"
Number strings:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: strings }}
{{ numbers |> select: { strings[it] }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['aPPLE', 'BlUeBeRrY', 'cHeRry'] |> assignTo: words }}
{{ words |> select: Uppercase: { it |> upper }, Lowercase: { it |> lower }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: strings }}
{{ numbers |> select: The digit { strings[it] } is { it.isEven() ? 'even' : 'odd' }.\n }}
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
            Assert.That(context.EvaluateScript(@"
Product Info:
{{ products |> select: { it.ProductName |> raw } is in the category { it.Category } and costs { it.UnitPrice |> currency } per unit.\n }}
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
            Assert.That(context.EvaluateScript(@"
Number: In-place?
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> select: { it }: { it |> equals(index) |> lower }\n }}
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
            Assert.That(context.EvaluateScript(@"
Numbers < 5:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: digits }}
{{ numbers
   |> where: it < 5 
   |> select: { digits[it] }\n 
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
            Assert.That(context.EvaluateScript(@"
{{ [0, 2, 4, 5, 6, 8, 9] |> assignTo: numbersA }}
{{ [1, 3, 5, 7, 8] |> assignTo: numbersB }}
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
        public void Linq15()
        {
            Assert.That(context.EvaluateScript(@"
{{ customers |> zip => it.Orders
   |> let => { c: it[0], o: it[1] }
   |> where => o.Total < 500
   |> select: ({ c.CustomerId }, { o.OrderId }, { o.Total |> format('0.0#') })\n }}
").NormalizeNewLines(),
                
                Does.StartWith(@"
(ALFKI, 10702, 330.0)
(ALFKI, 10952, 471.2)
(ANATR, 10308, 88.8)
(ANATR, 10625, 479.75)
".NormalizeNewLines()));
        }

        [Test]
        public void Linq15_literal()
        {
            Assert.That(context.EvaluateScript(@"
{{ customers |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: o.Total < 500
   |> select: ({ c.CustomerId }, { o.OrderId }, { o.Total |> format('0.0#') })\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ customers |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: o.OrderDate >= '1998-01-01' 
   |> select: ({ c.CustomerId }, { o.OrderId }, { o.OrderDate })\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ customers |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: o.Total >= 2000 
   |> select: ({ c.CustomerId }, { o.OrderId }, { o.Total |> format('0.0#') })\n }}
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
{{ '1997-01-01' |> assignTo: cutoffDate }}
{{ customers 
   |> where: it.Region == 'WA'
   |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: o.OrderDate  >= cutoffDate 
   |> select: ({ c.CustomerId }, { o.OrderId })\n }}
";
            Assert.That(context.EvaluateScript(template.NormalizeNewLines()).NormalizeNewLines(),
                
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
            Assert.That(context.EvaluateScript(@"
{{ customers 
   |> let({ cust: 'it', custIndex: 'index' })
   |> zip: cust.Orders
   |> let({ o: 'it[1]' })
   |> select: Customer #{ custIndex |> incr } has an order with OrderID { o.OrderId }\n }}
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
            Assert.That(context.EvaluateScript(@"
First 3 numbers:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> take(3) |> select: { it }\n }}
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
        public void Linq22()
        {
            Assert.That(context.EvaluateScript(@"
All but first 4 numbers:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> skip(4) |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
All but first 2 orders in WA:
{{ customers |> zip: it.Orders
   |> let({ c: 'it[0]', o: 'it[1]' })
   |> where: c.Region == 'WA'
   |> skip(2)
   |> select: { [c.CustomerId, o.OrderId, o.OrderDate] |> jsv }\n 
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
            Assert.That(context.EvaluateScript(@"
First numbers less than 6:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> to => numbers }}
{{ numbers 
   |> takeWhile: it < 6 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
First numbers not less than their position:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> takeWhile: it >= index 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
All elements starting from first element divisible by 3:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> skipWhile: mod(it,3) != 0 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
All elements starting from first element less than its position:
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> skipWhile: it >= index 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
The sorted list of words:
{{ ['cherry', 'apple', 'blueberry'] |> assignTo: words }}
{{ words 
   |> orderBy: it 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
The sorted list of words (by length):
{{ ['cherry', 'apple', 'blueberry'] |> assignTo: words }}
{{ words 
   |> orderBy: it.Length 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> orderBy: it.ProductName 
   |> select: { it |> jsv }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] |> assignTo: words }}
{{ words 
   |> orderBy('it', { comparer }) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
The doubles from highest to lowest:
{{ [1.7, 2.3, 1.9, 4.1, 2.9] |> assignTo: doubles }}
{{ doubles 
   |> orderByDescending: it 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> orderByDescending: it.UnitsInStock
   |> select: { it |> jsv }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] |> assignTo: words }}
{{ words 
   |> orderByDescending('it', { comparer }) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
Sorted digits:
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: digits }}
{{ digits 
   |> orderBy: it.length
   |> thenBy: it
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] |> assignTo: words }}
{{ words 
   |> orderBy: it.length
   |> thenBy('it', { comparer }) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> orderBy: it.Category
   |> thenByDescending: it.UnitPrice
   |> select: { it |> jsv }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['aPPLE', 'AbAcUs', 'bRaNcH', 'BlUeBeRrY', 'ClOvEr', 'cHeRry'] |> assignTo: words }}
{{ words 
   |> orderBy: it.length
   |> thenByDescending('it', { comparer }) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
A backwards list of the digits with a second character of 'i':
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: digits }}
{{ digits 
   |> where: it[1] == 'i'
   |> reverse
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> groupBy: mod(it,5)
   |> let({ remainder: 'it.Key', numbers: 'it' })
   |> select: Numbers with a remainder of { remainder } when divided by 5:\n{ numbers |> select('{it}\n') } }}
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
            Assert.That(context.EvaluateScript(@"
{{ ['blueberry', 'chimpanzee', 'abacus', 'banana', 'apple', 'cheese'] |> assignTo: words }}
{{ words 
   |> groupBy: it[0]
   |> let({ firstLetter: 'it.Key', words: 'it' })
   |> select: Words that start with the letter '{firstLetter}':\n{ words |> select('{it}\n') } }}
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
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', products: 'it' })
   |> select: {category}:\n{ products |> select('{it |> jsv}\n') } }}
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
{{ monthGroups |> scopeVars |> select: { indent }{ month }\n{ 2 |> indents }{ orders |> jsv }\n }}");
            
            Assert.That(context.EvaluateScript(@"
{{ customers 
   |> let({ 
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
   |> select: \n# { companyName |> raw }{ yearGroups |> scopeVars |> selectPartial('month-orders') } 
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

        [Test]
        public void Linq43_alt()
        { 
            context.VirtualFiles.WriteFile("month-orders.html", @"
{{ year }}
{{ monthGroups |> scopeVars |> select: { indent }{ month }\n{ 2 |> indents }{ orders |> jsv }\n }}");
            
            Assert.That(context.EvaluateScript(@"
{{ customers 
   |> map => { 
        companyName: it.CompanyName, 
        yearGroups: map (
            groupBy(it.Orders, it => it.OrderDate.Year),
            yg => { 
                year: yg.Key,
                monthGroups: map (
                    groupBy(yg, o => o.OrderDate.Month),
                    mg => { month: mg.Key, orders: mg }
                ) 
            }
        ) 
     }
   |> select: \n# { it.companyName |> raw }{ it.yearGroups |> scopeVars |> selectPartial('month-orders') } 
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
 
        [Test]
        public void Linq44()
        { 
            Assert.That(context.EvaluateScript(@"
{{ ['from   ', ' salt', ' earn ', '  last   ', ' near ', ' form  '] |> assignTo: anagrams }}
{{ anagrams 
   |> groupBy('trim(it)', { comparer: anagramComparer })
   |> select: { it |> json }\n 
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
            Assert.That(context.EvaluateScript(@"
{{ ['from   ', ' salt', ' earn ', '  last   ', ' near ', ' form  '] |> assignTo: anagrams }}
{{ anagrams 
   |> groupBy('trim(it)', { map: 'upper(it)', comparer: anagramComparer })
   |> select: { it |> json }\n 
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
            Assert.That(context.EvaluateScript(@"
Prime factors of 300:
{{ [2, 2, 3, 5, 5] |> assignTo: factorsOf300 }}
{{ factorsOf300 |> distinct |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
Category names:
{{ products 
   |> map: it.Category 
   |> distinct
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] |> assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] |> assignTo: numbersB }}
Unique numbers from both arrays:
{{ numbersA |> union(numbersB) |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products  
   |> map: it.ProductName[0] 
   |> assignTo: productFirstChars }}
{{ customers 
   |> map: it.CompanyName[0] 
   |> assignTo: customerFirstChars }}
Unique first letters from Product names and Customer names:
{{ productFirstChars 
   |> union(customerFirstChars) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] |> assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] |> assignTo: numbersB }}
Common numbers shared by both arrays:
{{ numbersA |> intersect(numbersB) |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products  
   |> map: it.ProductName[0] 
   |> assignTo: productFirstChars }}
{{ customers 
   |> map: it.CompanyName[0] 
   |> assignTo: customerFirstChars }}
Common first letters from Product names and Customer names:
{{ productFirstChars 
   |> intersect(customerFirstChars) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [ 0, 2, 4, 5, 6, 8, 9 ] |> assignTo: numbersA }}
{{ [ 1, 3, 5, 7, 8 ] |> assignTo: numbersB }}
Numbers in first array but not second array:
{{ numbersA |> except(numbersB) |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ products  
   |> map: it.ProductName[0] 
   |> assignTo: productFirstChars }}
{{ customers 
   |> map: it.CompanyName[0] 
   |> assignTo: customerFirstChars }}
First letters from Product names, but not from Customer names:
{{ productFirstChars 
   |> except(customerFirstChars) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [ 1.7, 2.3, 1.9, 4.1, 2.9 ] |> assignTo: doubles }}
Every other double from highest to lowest:
{{ doubles 
   |> orderByDescending: it
   |> step({ by: 2 }) 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: words }}
The sorted word list:
{{ words
   |> orderBy: it 
   |> toList 
   |> select: { it }\n }}
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
            Assert.That(context.EvaluateScript(@"
{{ [{name:'Alice', score:50}, {name: 'Bob', score:40}, {name:'Cathy', score:45}] |> assignTo: scoreRecords }}
Bob's score: 
{{ scoreRecords 
   |> toDictionary: it.name
   |> get: Bob
   |> select: { it['name'] } = { it['score'] }
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
            Assert.That(context.EvaluateScript(@"
{{ [null, 1.0, 'two', 3, 'four', 5, 'six', 7.0] |> assignTo: numbers }}
Numbers stored as doubles:
{{ numbers 
   |> of({ type: 'Double' })
   |> select: { it |> format('#.0') }\n }} 
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
            Assert.That(context.EvaluateScript(@"
{{ products
   |> where: it.ProductId == 12 
   |> first
   |> select: { it |> jsv } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
{ProductId:12,ProductName:Queso Manchego La Pastora,Category:Dairy Products,UnitPrice:38,UnitsInStock:86}
".NormalizeNewLines()));
        }
  
        [Test]
        public void Linq59()
        { 
            Assert.That(context.EvaluateScript(@"
{{ ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'] |> assignTo: strings }}
{{ strings
   |> first: it[0] == 'o'
   |> select: A string starting with 'o': { it } }}
").NormalizeNewLines(),
                
                Is.EqualTo(@"
A string starting with 'o': one
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq61()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [] |> assignTo: numbers }}
{{ numbers |> first |> otherwise('null') }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
null
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq62()
        { 
            Assert.That(context.EvaluateScript(@"
Product 789 exists: {{ products 
   |> first: it.ProductId == 789 
   |> isNotNull }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Product 789 exists: False
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq64()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 ] |> assignTo: numbers }} 
{{ numbers
   |> where: it > 5
   |> elementAt(1) 
   |> select: Second number > 5: { it } }} 
").NormalizeNewLines(),
                
                Is.EqualTo(@"
Second number > 5: 8
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq65()
        { 
            Assert.That(context.EvaluateScript(@"
{{ range(100,50)
   |> select: The number {it} is { it.isEven() ? 'even' : 'odd' }.\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The number 100 is even.
The number 101 is odd.
The number 102 is even.
The number 103 is odd.
The number 104 is even.
The number 105 is odd.
The number 106 is even.
The number 107 is odd.
The number 108 is even.
The number 109 is odd.
The number 110 is even.
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq66()
        { 
            Assert.That(context.EvaluateScript(@"
{{ 10 |> itemsOf(7) |> select: {it}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
7
7
7
7
7
7
7
7
7
7
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq67()
        { 
            Assert.That(context.EvaluateScript(@"
{{ ['believe', 'relief', 'receipt', 'field'] |> assignTo: words }}
{{ words 
   |> any: contains(it, 'ei')
   |> select: There is a word that contains in the list that contains 'ei': { it |> lower } }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
There is a word that contains in the list that contains 'ei': true".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq69()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> where: any(it, 'it.UnitsInStock == 0')
   |> let({ category: 'it.Key', products: 'it' }) 
   |> select: { category }\n{ products |> jsv }\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Condiments
[{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13},{ProductId:4,ProductName:Chef Anton's Cajun Seasoning,Category:Condiments,UnitPrice:22,UnitsInStock:53}
".NormalizeNewLines()));
        }
    
        [Test]
        public void Linq70()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [1, 11, 3, 19, 41, 65, 19] |> assignTo: numbers }}
{{ numbers 
   |> all: isOdd(it)
   |> select: The list contains only odd numbers: { it |> lower } }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The list contains only odd numbers: true".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq72()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> where: all(it, 'it.UnitsInStock > 0')
   |> let({ category: 'it.Key', products: 'it' }) 
   |> select: { category }\n{ products |> jsv }\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages
[{ProductId:1,ProductName:Chai,Category:Beverages,UnitPrice:18,UnitsInStock:39},{ProductId:2,ProductName:Chang,Category:Beverages,UnitPrice:19,UnitsInStock:17}
".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq73()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [2, 2, 3, 5, 5] |> assignTo: factorsOf300 }}
{{ factorsOf300 |> distinct |> count |> select: There are {it} unique factors of 300. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
There are 3 unique factors of 300.".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq74()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> count: isOdd(it) 
   |> select: There are {it} odd numbers in the list. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
There are 5 odd numbers in the list.".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq76()
        {
            Assert.That(context.EvaluateScript(@"
{{ customers 
   |> let({ customerId: 'it.CustomerId', ordersCount: 'count(it.Orders)' }) 
   |> select: {customerId}, {ordersCount}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
ALFKI, 6
ANATR, 4
ANTON, 7
AROUT, 13
BERGS, 18
BLAUS, 7
BLONP, 11
".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq77()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', productCount: 'count(it)' }) 
   |> select: {category}, {productCount}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages, 12
Condiments, 12
Produce, 5
Meat/Poultry, 6
Seafood, 12
Dairy Products, 10
Confections, 13
Grains/Cereals, 7
".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq78()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> sum |> select: The sum of the numbers is {it}. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The sum of the numbers is 45.".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq79()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry'] |> assignTo: words }}
{{ words
   |> sum: it.Length 
   |> select: There are a total of {it} characters in these words. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
There are a total of 20 characters in these words.".NormalizeNewLines()));
        }
     
        [Test]
        public void Linq80()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', totalUnitsInStock: 'sum(it, `it.UnitsInStock`)' }) 
   |> select: {category}, {totalUnitsInStock}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages, 559
Condiments, 507
Produce, 100
Meat/Poultry, 165
Seafood, 701
Dairy Products, 393
Confections, 386
Grains/Cereals, 308
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq81()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> min |> select: The minimum number is {it}. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The minimum number is 0.".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq82()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: words }}
{{ words
   |> min: it.Length 
   |> select: The shortest word is {it} characters long. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The shortest word is 5 characters long.".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq83()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', cheapestPrice: 'min(it, `it.UnitPrice`)' }) 
   |> select: {category}, {cheapestPrice}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages, 4.5
Condiments, 10
Produce, 10
Meat/Poultry, 7.45
Seafood, 6
Dairy Products, 2.5
Confections, 9.2
Grains/Cereals, 7
".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq84()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ 
        g: 'it',
        minPrice: 'min(g, `it.UnitPrice`)', 
        category: 'g.Key', 
        cheapestProducts: 'where(g, `it.UnitPrice == minPrice`)' 
     })
   |> select: { category }\n{ cheapestProducts |> jsv }\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages
[{ProductId:24,ProductName:Guaraná Fantástica,Category:Beverages,UnitPrice:4.5,UnitsInStock:20}]
Condiments
[{ProductId:3,ProductName:Aniseed Syrup,Category:Condiments,UnitPrice:10,UnitsInStock:13}]
Produce
[{ProductId:74,ProductName:Longlife Tofu,Category:Produce,UnitPrice:10,UnitsInStock:4}]
Meat/Poultry
[{ProductId:54,ProductName:Tourtière,Category:Meat/Poultry,UnitPrice:7.45,UnitsInStock:21}]
Seafood
[{ProductId:13,ProductName:Konbu,Category:Seafood,UnitPrice:6,UnitsInStock:24}]
Dairy Products
[{ProductId:33,ProductName:Geitost,Category:Dairy Products,UnitPrice:2.5,UnitsInStock:112}]
Confections
[{ProductId:19,ProductName:Teatime Chocolate Biscuits,Category:Confections,UnitPrice:9.2,UnitsInStock:25}]
Grains/Cereals
[{ProductId:52,ProductName:Filo Mix,Category:Grains/Cereals,UnitPrice:7,UnitsInStock:38}]
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq85()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> max |> select: The maximum number is {it}. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The maximum number is 9.".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq86()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: words }}
{{ words
   |> max: it.Length 
   |> select: The longest word is {it} characters long. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The longest word is 9 characters long.".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq87()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', mostExpensivePrice: 'max(it, `it.UnitPrice`)' }) 
   |> select: Category: {category}, MaximumPrice: {mostExpensivePrice}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Category: Beverages, MaximumPrice: 263.5
Category: Condiments, MaximumPrice: 43.9
Category: Produce, MaximumPrice: 53
Category: Meat/Poultry, MaximumPrice: 123.79
Category: Seafood, MaximumPrice: 62.5
Category: Dairy Products, MaximumPrice: 55
Category: Confections, MaximumPrice: 81
Category: Grains/Cereals, MaximumPrice: 38
".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq88()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ 
        g: 'it',
        maxPrice: 'max(g, `it.UnitPrice`)', 
        category: 'g.Key', 
        mostExpensiveProducts: 'where(g, `it.UnitPrice == maxPrice`)' 
     })
   |> select: { category }\n{ mostExpensiveProducts |> jsv }\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Beverages
[{ProductId:38,ProductName:Côte de Blaye,Category:Beverages,UnitPrice:263.5,UnitsInStock:17}]
Condiments
[{ProductId:63,ProductName:Vegie-spread,Category:Condiments,UnitPrice:43.9,UnitsInStock:24}]
Produce
[{ProductId:51,ProductName:Manjimup Dried Apples,Category:Produce,UnitPrice:53,UnitsInStock:20}]
Meat/Poultry
[{ProductId:29,ProductName:Thüringer Rostbratwurst,Category:Meat/Poultry,UnitPrice:123.79,UnitsInStock:0}]
Seafood
[{ProductId:18,ProductName:Carnarvon Tigers,Category:Seafood,UnitPrice:62.5,UnitsInStock:42}]
Dairy Products
[{ProductId:59,ProductName:Raclette Courdavault,Category:Dairy Products,UnitPrice:55,UnitsInStock:79}]
Confections
[{ProductId:20,ProductName:Sir Rodney's Marmalade,Category:Confections,UnitPrice:81,UnitsInStock:40}]
Grains/Cereals
[{ProductId:56,ProductName:Gnocchi di nonna Alice,Category:Grains/Cereals,UnitPrice:38,UnitsInStock:21}]
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq89()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers |> average |> select: The average number is {it}. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The average number is 4.5.".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq90()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: words }}
{{ words
   |> average: it.Length 
   |> select: The average word length is {it} characters. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The average word length is 6.6666666666666".NormalizeNewLines()));
        }
      
        [Test]
        public void Linq91()
        {
            Assert.That(context.EvaluateScript(@"
{{ products 
   |> groupBy: it.Category
   |> let({ category: 'it.Key', averagePrice: 'average(it, `it.UnitPrice`)' }) 
   |> select: Category: {category}, AveragePrice: {averagePrice}\n }} 
").NormalizeNewLines() 
    .Replace("37.979166666666664","37.9791666666667") //.NET Core 3.1
    .Replace("54.00666666666667","54.0066666666667"),
                
                Does.StartWith(@"
Category: Beverages, AveragePrice: 37.9791666666667
Category: Condiments, AveragePrice: 23.0625
Category: Produce, AveragePrice: 32.37
Category: Meat/Poultry, AveragePrice: 54.0066666666667
Category: Seafood, AveragePrice: 20.6825
Category: Dairy Products, AveragePrice: 28.73
Category: Confections, AveragePrice: 25.16
Category: Grains/Cereals, AveragePrice: 20.25
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq92()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [1.7, 2.3, 1.9, 4.1, 2.9] |> assignTo: doubles }}
{{ doubles 
   |> reduce((accumulator,it) => accumulator * it,1)
   |> select: Total product of all numbers: { it |> format('#.####') }. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Total product of all numbers: 88.3308".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq93()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [20, 10, 40, 50, 10, 70, 30] |> assignTo: attemptedWithdrawals }}
{{ attemptedWithdrawals 
   |> reduce((balance, nextWithdrawal) => ((nextWithdrawal <= balance) ? (balance - nextWithdrawal) : balance), 
            { initialValue: 100.0, })
   |> select: Ending balance: { it }. }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Ending balance: 20".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq94()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [0, 2, 4, 5, 6, 8, 9] |> assignTo: numbersA }}
{{ [1, 3, 5, 7, 8] |> assignTo: numbersB }}
All numbers from both arrays:
{{ numbersA |> concat(numbersB) |> select: {it}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
All numbers from both arrays:
0
2
4
5
6
8
9
1
3
5
7
8
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq95()
        { 
            Assert.That(context.EvaluateScript(@"
{{ customers |> map('it.CompanyName') |> assignTo: customerNames }}
{{ products |> map('it.ProductName') |> assignTo: productNames }}
Customer and product names:
{{ customerNames |> concat(productNames) |> select: { it |> raw }\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
Customer and product names:
Alfreds Futterkiste
Ana Trujillo Emparedados y helados
Antonio Moreno Taquería
Around the Horn
Berglunds snabbköp
Blauer See Delikatessen
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq96()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: wordsA }}
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: wordsB }}
{{ wordsA |> equivalentTo(wordsB) |> select: The sequences match: { it |> lower } }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The sequences match: true".NormalizeNewLines()));
        }
       
        [Test]
        public void linq97()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [ 'cherry', 'apple', 'blueberry' ] |> assignTo: wordsA }}
{{ [ 'apple', 'blueberry', 'cherry' ] |> assignTo: wordsB }}
{{ wordsA |> equivalentTo(wordsB) |> select: The sequences match: { it |> lower } }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
The sequences match: false".NormalizeNewLines()));
        }

        [Test]
        public void Linq99()
        { 
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ 0 |> assignTo: i }}
{{ numbers |> let({ i: 'incr(i)' }) |> select: v = {index |> incr}, i = {i}\n }} 
").NormalizeNewLines(),
                
                Does.StartWith(@"
v = 1, i = 1
v = 2, i = 2
v = 3, i = 3
v = 4, i = 4
v = 5, i = 5
v = 6, i = 6
v = 7, i = 7
v = 8, i = 8
v = 9, i = 9
v = 10, i = 10
".NormalizeNewLines()));
        }
       
        [Test]
        public void Linq100()
        {
            // lowNumbers is assigned the result not a reusable query
            Assert.That(context.EvaluateScript(@"
{{ [5, 4, 1, 3, 9, 8, 6, 7, 2, 0] |> assignTo: numbers }}
{{ numbers 
   |> where: it <= 3
   |> assignTo: lowNumbers }}
First run numbers <= 3:
{{ lowNumbers |> select: {it}\n }}
{{ 10 |> times |> do: assign('numbers[index]', -numbers[index]) }}
Second run numbers <= 3:
{{ lowNumbers |> select: {it}\n }}
Contents of numbers:
{{ numbers |> select: {it}\n }}
").NormalizeNewLines(),

                Does.StartWith(@"
First run numbers <= 3:
1
3
2
0

Second run numbers <= 3:
1
3
2
0

Contents of numbers:
-5
-4
-1
-3
-9
-8
-6
-7
-2
0
".NormalizeNewLines()));
        }
    }
}