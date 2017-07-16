using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Templates;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateQueryFilterTests
    {
        private static TemplatePagesContext CreateContext(Dictionary<string, object> optionalArgs = null)
        {
            var context = new TemplatePagesContext
            {
                Args =
                {
                    ["numbers"] = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 },
                    ["products"] = TemplateQueryData.Products,
                    ["customers"] = TemplateQueryData.Customers,
                }
            };
            optionalArgs.Each((key, val) => context.Args[key] = val);
            return context.Init();
        }

        [Test]
        public void linq1_original()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Numbers < 5:
{{ numbers | where('it < 5') | select('{{ it }}\n') }}").NormalizeNewLines(), 
                
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
        public void linq1() // alternative with clean whitespace sensitive string argument syntax:
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
        public void linq2_original()
        {
            var context = CreateContext();
            
            Assert.That(context.EvaluateTemplate(@"
Sold out products:
{{ products 
   | where('it.UnitsInStock = 0') 
   | select('{{ it.productName | raw }} is sold out!\n')
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
        public void linq2() // alternative with clean whitespace sensitive string argument syntax:
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
        public void linq3()
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
        public void linq4()
        {
            var context = CreateContext(new Dictionary<string, object>
            {
                {TemplateConstants.DefaultDateFormat, "yyyy/MM/dd"}
            });
            
            Assert.That(context.EvaluateTemplate(@"
{{ customers 
    | where: it.Region = 'WA' 
    | assignTo: waCustomers }}
Customers from Washington and their orders:
{{ 'Customer {{ it.CustomerId }} {{ it.CompanyName | raw }}
{{ it.Orders | select(""  Order {{ it.OrderId }}: {{ it.OrderDate | dateFormat | newLine }}"") }}' 
    | forEach(waCustomers) }}
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

    }
}