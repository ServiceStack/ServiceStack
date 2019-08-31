using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    /// <summary>
    /// Parse csv contents into a string dictionary List and assign to identifier
    /// Usage: {{#csv list}}
    ///          Item,Qty
    ///          Apples,2
    ///          Oranges,3
    ///        {{/csv}}
    /// </summary>
    public class CsvScriptBlock : ScriptBlock
    {
        public override string Name => "csv";
        public override BodyStyle ParseBody => BodyStyle.Verbatim;

        public override Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken ct)
        {
            var literal = block.Argument.ParseVarName(out var name);
            
            var strFragment = (PageStringFragment)block.Body[0];
            var trimmedBody = StringBuilderCache.Allocate();
            foreach (var line in strFragment.ValueString.ReadLines())
            {
                trimmedBody.AppendLine(line.Trim());
            }
            var strList = trimmedBody.ToString().FromCsv<List<List<string>>>();
            scope.PageResult.Args[name.ToString()] = strList;

            return TypeConstants.EmptyTask;
        }
    }
}