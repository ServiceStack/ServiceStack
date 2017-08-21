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

    }
}