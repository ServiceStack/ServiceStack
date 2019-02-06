using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class TemplateUseCaseTests
    {
        [Test]
        public void Does_execute_live_document()
        {
            var context = new TemplateContext().Init();

            var template = @"{{ 11200 | assignTo: balance }}
{{ 3     | assignTo: projectedMonths }}
{{'
Salary:        4000
App Royalties: 200
'| trim | parseKeyValueText(':') | assignTo: monthlyRevenues }}
{{'
Rent      1000
Internet  50
Mobile    50
Food      400
Misc      200
'| trim | parseKeyValueText | assignTo: monthlyExpenses }}
{{ monthlyRevenues | values | sum | assignTo: totalRevenues }}
{{ monthlyExpenses | values | sum | assignTo: totalExpenses }}
{{ subtract(totalRevenues, totalExpenses) | assignTo: totalSavings }}

Current Balance: <b>{{ balance | currency }}</b>

Monthly Revenues:
{{ monthlyRevenues | toList | select: { it.Key | padRight(17) }{ it.Value | currency }\n }}
Total            <b>{{ totalRevenues | currency }}</b> 

Monthly Expenses:
{{ monthlyExpenses | toList | select: { it.Key | padRight(17) }{ it.Value | currency }\n }}
Total            <b>{{ totalExpenses | currency }}</b>

Monthly Savings: <b>{{ totalSavings | currency }}</b>
{{ htmlErrorDebug }}";

            var output = context.EvaluateTemplate(template);
            
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

        class FilterInfoFilters : TemplateFilter
        {
            Type GetFilterType(string name)
            {
                switch(name)
                {
                    case nameof(TemplateDefaultFilters):
                        return typeof(TemplateDefaultFilters);
                    case nameof(TemplateHtmlFilters):
                        return typeof(TemplateHtmlFilters);
                    case nameof(TemplateProtectedFilters):
                        return typeof(TemplateProtectedFilters);
                    case nameof(TemplateInfoFilters):
                        return typeof(TemplateInfoFilters);
                    case nameof(TemplateServiceStackFilters):
                        return typeof(TemplateServiceStackFilters);
                    case nameof(TemplateAutoQueryFilters):
                        return typeof(TemplateAutoQueryFilters);
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
                    .Where(x => x.DeclaringType != typeof(TemplateFilter) && x.DeclaringType != typeof(object))
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
                    .Where(x => x.ParameterType != typeof(TemplateScopeContext))
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
                    ? $"| {Name}"
                    : $"| {Name}(" + string.Join(", ", RemainingParams) + $")";

            public string Display => ParamCount == 0
                ? $"{Name}{Return}"
                : ParamCount == 1
                    ? $"{FirstParam} | {Name}{Return}"
                    : $"{FirstParam} | {Name}(" + string.Join(", ", RemainingParams) + $"){Return}";
        }
        
        [Test]
        public void Can_query_filters()
        {
            var context = new TemplateContext
            {
                TemplateFilters = { new FilterInfoFilters() }
            }.Init();

            var results = context.EvaluateTemplate(@"{{ 'TemplateDefaultFilters' | assignTo: filter }}
{{ filter | filtersAvailable | where => contains(lower(it.Name), lower(nameContains ?? ''))  
          | assignTo: filters }}
{{#each filters}}
{{Body}}
{{/each}}", new Dictionary<string, object> { ["nameContains"] = "atan" });
            
            Assert.That(results.NormalizeNewLines(), Is.EqualTo(@"
| atan
| atan2(x)".NormalizeNewLines()));
        }
    }
}