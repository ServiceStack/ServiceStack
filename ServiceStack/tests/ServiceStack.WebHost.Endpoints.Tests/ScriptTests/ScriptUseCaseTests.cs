using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptUseCaseTests
    {
        [Test]
        public void Does_execute_live_document()
        {
            var context = new ScriptContext().Init();

            var template = @"{{ 11200 |> assignTo: balance }}
{{ 3     |> assignTo: projectedMonths }}
{{'
Salary:        4000
App Royalties: 200
'|> trim |> parseKeyValueText(':') |> assignTo: monthlyRevenues }}
{{'
Rent      1000
Internet  50
Mobile    50
Food      400
Misc      200
'|> trim |> parseKeyValueText |> assignTo: monthlyExpenses }}
{{ monthlyRevenues |> values |> sum |> assignTo: totalRevenues }}
{{ monthlyExpenses |> values |> sum |> assignTo: totalExpenses }}
{{ subtract(totalRevenues, totalExpenses) |> assignTo: totalSavings }}

Current Balance: <b>{{ balance |> currency }}</b>

Monthly Revenues:
{{ monthlyRevenues |> toList |> select: { it.Key |> padRight(17) }{ it.Value |> currency }\n }}
Total            <b>{{ totalRevenues |> currency }}</b> 

Monthly Expenses:
{{ monthlyExpenses |> toList |> select: { it.Key |> padRight(17) }{ it.Value |> currency }\n }}
Total            <b>{{ totalExpenses |> currency }}</b>

Monthly Savings: <b>{{ totalSavings |> currency }}</b>
{{ htmlErrorDebug }}";

            var output = context.EvaluateScript(template);
            
            Assert.That(output.NormalizeNewLines(), Is.EqualTo(@"
Current Balance: <b>$11,200.00</b>

Monthly Revenues:
Salary           $4,000.00
App Royalties    $200.00

Total            <b>$4,200.00</b> 

Monthly Expenses:
Rent             $1,000.00
Internet         $50.00
Mobile           $50.00
Food             $400.00
Misc             $200.00

Total            <b>$1,700.00</b>

Monthly Savings: <b>$2,500.00</b>".NormalizeNewLines()));
        }

        class FilterInfoFilters : ScriptMethods
        {
            Type GetFilterType(string name)
            {
                switch(name)
                {
                    case nameof(DefaultScripts):
                        return typeof(DefaultScripts);
                    case nameof(HtmlScripts):
                        return typeof(HtmlScripts);
                    case nameof(ProtectedScripts):
                        return typeof(ProtectedScripts);
                    case nameof(InfoScripts):
                        return typeof(InfoScripts);
                    case nameof(ServiceStackScripts):
                        return typeof(ServiceStackScripts);
                    case nameof(AutoQueryScripts):
                        return typeof(AutoQueryScripts);
                }

                throw new NotSupportedException("Unknown Filter: " + name);
            }

            public FilterInfo[] filtersAvailable(string name)
            {
                var filterType = GetFilterType(name);
                var filters = filterType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var to = filters
                    .OrderBy(x => x.Name)
                    .ThenBy(x => x.GetParameters().Count())
                    .Where(x => x.DeclaringType != typeof(ScriptMethods) && x.DeclaringType != typeof(object))
                    .Where(m => !m.IsSpecialName)                
                    .Select(x => FilterInfo.Create(x));

                return to.ToArray();
            }            
        }
        
        public class FilterInfo
        {
            public string Name { get; set; }
            public string FirstParam { get; set; }
            public string ReturnType { get; set; }
            public int ParamCount { get; set; }
            public string[] RemainingParams { get; set; }

            public static FilterInfo Create(MethodInfo mi)
            {
                var paramNames = mi.GetParameters()
                    .Where(x => x.ParameterType != typeof(ScriptScopeContext))
                    .Select(x => x.Name)
                    .ToArray();

                var to = new FilterInfo {
                    Name = mi.Name,
                    FirstParam = paramNames.FirstOrDefault(),
                    ParamCount = paramNames.Length,
                    RemainingParams = paramNames.Length > 1 ? paramNames.Skip(1).ToArray() : new string[]{},
                    ReturnType = mi.ReturnType?.Name,
                };

                return to;
            }

            public string Return => ReturnType != null && ReturnType != nameof(StopExecution) ? " -> " + ReturnType : "";

            public string Body => ParamCount == 0
                ? $"{Name}"
                : ParamCount == 1
                    ? $"|> {Name}"
                    : $"|> {Name}(" + string.Join(", ", RemainingParams) + $")";

            public string Display => ParamCount == 0
                ? $"{Name}{Return}"
                : ParamCount == 1
                    ? $"{FirstParam} |> {Name}{Return}"
                    : $"{FirstParam} |> {Name}(" + string.Join(", ", RemainingParams) + $"){Return}";
        }
        
        [Test]
        public void Can_query_filters()
        {
            var context = new ScriptContext
            {
                ScriptMethods = { new FilterInfoFilters() }
            }.Init();

            var results = context.EvaluateScript(@"{{ 'DefaultScripts' |> assignTo: filter }}
{{ filter |> filtersAvailable |> where => contains(lower(it.Name), lower(nameContains ?? ''))  
          |> assignTo: filters }}
{{#each filters}}
{{Body |> raw}}
{{/each}}", new Dictionary<string, object> { ["nameContains"] = "atan" });
            
            Assert.That(results.NormalizeNewLines(), Is.EqualTo(@"
|> atan
|> atan2(x)".NormalizeNewLines()));
        }
        

        [Test]
        public void Can_convert_dbScript_Results_to_Customer_Poco()
        {
            void AssertProduct(Product actual, Product expected)
            {
                Assert.That(actual.ProductId, Is.EqualTo(expected.ProductId));
                Assert.That(actual.ProductName, Is.EqualTo(expected.ProductName));
                Assert.That(actual.Category, Is.EqualTo(expected.Category));
                Assert.That(actual.UnitPrice, Is.EqualTo(expected.UnitPrice));
                Assert.That(actual.UnitsInStock, Is.EqualTo(expected.UnitsInStock));
            }

            var product1 = QueryData.Products[0];
            var product2 = QueryData.Products[1];
            var context = new ScriptContext
            {
                ScriptMethods = { new DbScriptsAsync() },
                Args = {
                    ["id"] = product1.ProductId,
                }
            };
            
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteOrmLiteDialectProvider.Instance);
            context.Container.AddSingleton<IDbConnectionFactory>(() => dbFactory);
            context.Init();
            
            using (var db = context.Container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Product>();
                db.Insert(product1);
                db.Insert(product2);
            }
                

            var result = context.Evaluate<Product>("{{ `select * from product where productId=@id` |> dbSingle({ id }) |> return }}");

            result.TextDump().Print();
            
            AssertProduct(result, product1);

            var results = context.Evaluate<Product[]>("{{ `select * from product where productId IN (@ids) order by productId` |> dbSelect({ ids }) |> return }}", 
                new ObjectDictionary {
                    ["ids"] = new[]{ product1.ProductId, product2.ProductId },
                });

            results.TextDump().Print();
            
            Assert.That(results.Length, Is.EqualTo(2));

            AssertProduct(results[0], product1);
            AssertProduct(results[1], product2);
        }

        private static ScriptContext CreateDbContext()
        {
            var context = new ScriptContext {
                ScriptMethods = {new DbScriptsAsync()},
            };

            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteOrmLiteDialectProvider.Instance);
            context.Container.AddSingleton<IDbConnectionFactory>(() => dbFactory);
            context.Init();
            return context;
        }

        [Test]
        public void Can_use_GetTableNames_with_textDump()
        {
            var context = CreateDbContext();

            using (var db = context.Container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Customer>();
                db.DropAndCreateTable<Product>();
                
                QueryData.Customers.Take(1).Each(x => db.Insert(x));
                QueryData.Products.Take(3).Each(x => db.Insert(x));
            }

            var output = context.EvaluateScript("{{ dbTableNames |> textDump({ caption:'Tables' }) }}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("| Tables   |\n|----------|\n| Customer |\n| Product  |"));
            
            output = context.EvaluateScript("{{ dbTableNamesWithRowCounts |> textDump({ caption:'Tables' }) }}");
            Assert.That(output.NormalizeNewLines(), Is.EqualTo("| Tables      ||\n|----------|---|\n| Product  | 3 |\n| Customer | 1 |"));
        }

        [Test]
        public void Can_catch_dbSelect_exceptions()
        {
            var context = CreateDbContext();

            var output = context.EvaluateScript("{{ `SELECT * FROM Unknown` |> dbSelect(null, { ifErrorReturn: 'No Table' }) }}");
            Assert.That(output, Is.EqualTo("No Table"));
        }

    }
}