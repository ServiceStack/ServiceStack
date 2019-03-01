using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    /* Usages:
     
    {{#ul {class:'nav'}}} <li>item</li> {{/ul}}
    {{#ul {each:items, class:'nav'}}} <li>{{it}}</li> {{/ul}}
    {{#ul {each:numbers, it:'num', class:'nav'}}} <li>{{num}}</li> {{/ul}}

    {{#ul {if:hasAccess, each:items, where:'Age > 27', 
           class:['nav', !disclaimerAccepted ? 'blur' : ''], 
           id:`ul-${id}`, selected:true} }}
        {{#li {class: {alt:isOdd(index), active:Name==highlight} }}
            {{Name}}
        {{/li}}
    {{else}}
        <div>no items</div>
    {{/ul}}

    // Equivalent to:

    {{#if hasAccess}}
        {{ items | where => it.Age > 27 | assignTo: items }}
        {{#if !isEmpty(items)}}
            <ul {{ ['nav', !disclaimerAccepted ? 'blur' : ''] | htmlClass }} id="menu-{{id}}">
            {{#each items}}
                <li {{ {alt:isOdd(index), active:Name==highlight} | htmlClass }}>{{Name}}</li>
            {{/each}}
            </ul>
        {{else}}
            <div>no items</div>
        {{/if}}
    {{/if}}

    // Razor:

    @{
        var persons = (items as IEnumerable<Person>)?.Where(x => x.Age > 27);
    }
    @if (hasAccess)
    {
        if (persons?.Any() == true)
        {
            <ul id="menu-@id" class="nav @(!disclaimerAccepted ? "hide" : "")">
                @{
                    var index = 0;
                }
                @foreach (var person in persons)
                {
                    <li class="@(index++ % 2 == 1 ? "alt " : "" )@(person.Name == activeName ? "active" : "")">
                        @person.Name
                    </li>
                }
            </ul>
        }
        else
        {
            <div>no items</div>
        }
    }
    */
    public class ScriptUlBlock : ScriptHtmlBlock
    {
        public override string Tag => "ul";
    }
    public class ScriptOlBlock : ScriptHtmlBlock
    {
        public override string Tag => "ol";
    }
    public class ScriptLiBlock : ScriptHtmlBlock
    {
        public override string Tag => "li";
    }
    public class ScriptDivBlock : ScriptHtmlBlock
    {
        public override string Tag => "div";
    }
    public class ScriptPBlock : ScriptHtmlBlock
    {
        public override string Tag => "p";
    }
    public class ScriptFormBlock : ScriptHtmlBlock
    {
        public override string Tag => "form";
    }
    public class ScriptInputBlock : ScriptHtmlBlock
    {
        public override string Tag => "input";
    }
    public class ScriptSelectBlock : ScriptHtmlBlock
    {
        public override string Tag => "select";
    }
    public class ScriptOptionBlock : ScriptHtmlBlock
    {
        public override string Tag => "option";
    }
    public class ScriptTextAreaBlock : ScriptHtmlBlock
    {
        public override string Tag => "textarea";
    }
    public class ScriptButtonBlock : ScriptHtmlBlock
    {
        public override string Tag => "button";
    }
    public class ScriptTableBlock : ScriptHtmlBlock
    {
        public override string Tag => "table";
    }
    public class ScriptTrBlock : ScriptHtmlBlock
    {
        public override string Tag => "tr";
    }
    public class ScriptTdBlock : ScriptHtmlBlock
    {
        public override string Tag => "td";
    }
    public class ScriptTHeadBlock : ScriptHtmlBlock
    {
        public override string Tag => "thead";
    }
    public class ScriptTBodyBlock : ScriptHtmlBlock
    {
        public override string Tag => "tbody";
    }
    public class ScriptTFootBlock : ScriptHtmlBlock
    {
        public override string Tag => "tfoot";
    }
    public class ScriptDlBlock : ScriptHtmlBlock
    {
        public override string Tag => "dl";
    }
    public class ScriptDtBlock : ScriptHtmlBlock
    {
        public override string Tag => "dt";
    }
    public class ScriptDdBlock : ScriptHtmlBlock
    {
        public override string Tag => "dd";
    }
    
    //Don't emit new line on in-line elements
    public class ScriptSpanBlock : ScriptHtmlBlock
    {
        public override string Tag => "span";
        public override string Suffix => "";
    }
    public class ScriptABlock : ScriptHtmlBlock
    {
        public override string Tag => "a";
        public override string Suffix => "";
    }
    public class ScriptImgBlock : ScriptHtmlBlock
    {
        public override string Tag => "img";
        public override string Suffix => "";
    }
    public class ScriptEmBlock : ScriptHtmlBlock
    {
        public override string Tag => "em";
        public override string Suffix => "";
    }
    public class ScriptBBlock : ScriptHtmlBlock
    {
        public override string Tag => "b";
        public override string Suffix => "";
    }
    public class ScriptIBlock : ScriptHtmlBlock
    {
        public override string Tag => "i";
        public override string Suffix => "";
    }
    public class ScriptStrongBlock : ScriptHtmlBlock
    {
        public override string Tag => "strong";
        public override string Suffix => "";
    }

    public abstract class ScriptHtmlBlock : ScriptBlock
    {
        public override string Name => Tag;
            
        public abstract string Tag { get; }

        public virtual string Suffix { get; } = Environment.NewLine; 
        
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            var htmlAttrs = block.Argument.GetJsExpressionAndEvaluate(scope) as Dictionary<string, object>;
            var hasEach = false;
            IEnumerable each = null;
            var binding = "it";
            var hasExplicitBinding = false;
            JsToken where = null;

            if (htmlAttrs != null)
            {
                if (htmlAttrs.TryGetValue("if", out var oIf))
                {
                    if (Script.DefaultScripts.isFalsy(oIf))
                        return;
                    htmlAttrs.Remove("if");
                }

                if (htmlAttrs.TryGetValue(nameof(where), out var oWhere))
                {
                    if (!(oWhere is string whereExpr))
                        throw new NotSupportedException($"'where' should be a string expression but instead found '{oWhere.GetType().Name}'");

                    where = whereExpr.GetCachedJsExpression(scope);
                    htmlAttrs.Remove(nameof(where));
                }
                
                if (htmlAttrs.TryGetValue(nameof(each), out var oEach))
                {
                    hasEach = true;
                    htmlAttrs.Remove(nameof(each));
                }
                each = oEach as IEnumerable;

                if (htmlAttrs.TryGetValue("it", out var oIt) && oIt is string it)
                {
                    binding = it;
                    hasExplicitBinding = true;
                    htmlAttrs.Remove("it");
                }

                if (htmlAttrs.TryGetValue("class", out var oClass))
                {
                    var cls = scope.Context.HtmlMethods.htmlClassList(oClass);
                    if (string.IsNullOrEmpty(cls))
                        htmlAttrs.Remove("class");
                    else
                        htmlAttrs["class"] = cls;
                }
            }

            var attrString = scope.Context.HtmlMethods.htmlAttrsList(htmlAttrs);

            if (HtmlScripts.VoidElements.Contains(Tag)) //e.g. img, input, br, etc
            {
                await scope.OutputStream.WriteAsync($"<{Tag}{attrString}>{Suffix}", token);
            }
            else
            {
                if (hasEach)
                {
                    var hasElements = each != null && each.GetEnumerator().MoveNext();
                    if (hasElements)
                    {
                        await scope.OutputStream.WriteAsync($"<{Tag}{attrString}>{Suffix}", token);

                        var index = 0;
                        var whereIndex = 0;
                        foreach (var element in each)
                        {
                            // Add all properties into scope if called without explicit in argument 
                            var scopeArgs = !hasExplicitBinding && CanExportScopeArgs(element)
                                ? element.ToObjectDictionary()
                                : new Dictionary<string, object>();

                            scopeArgs[binding] = element;
                            scopeArgs[nameof(index)] = AssertWithinMaxQuota(whereIndex++);
                            var itemScope = scope.ScopeWithParams(scopeArgs);

                            if (where != null)
                            {
                                var result = where.EvaluateToBool(itemScope);
                                if (!result)
                                    continue;
                            }

                            itemScope.ScopedParams[nameof(index)] = AssertWithinMaxQuota(index++);

                            await WriteBodyAsync(itemScope, block, token);
                        }

                        await scope.OutputStream.WriteAsync($"</{Tag}>{Suffix}", token);
                    }
                    else
                    {
                        await WriteElseAsync(scope, block.ElseBlocks, token);
                    }
                }
                else
                {
                    await scope.OutputStream.WriteAsync($"<{Tag}{attrString}>{Suffix}", token);
                    await WriteBodyAsync(scope, block, token);
                    await scope.OutputStream.WriteAsync($"</{Tag}>{Suffix}", token);
                }
            }
        }
    }
    
    public class HtmlScriptBlocks : IScriptPlugin
    {
        /// <summary>
        /// Usages: {{#ul {each:items, class:'nav'} }} <li>{{it}}</li> {{/ul}}
        /// </summary>
        
        public void Register(ScriptContext context)
        {
            context.ScriptBlocks.AddRange(new ScriptBlock[] {
                new ScriptUlBlock(),
                new ScriptOlBlock(),
                new ScriptLiBlock(),
                new ScriptDivBlock(),
                new ScriptPBlock(), 
                new ScriptFormBlock(), 
                new ScriptInputBlock(), 
                new ScriptSelectBlock(), 
                new ScriptOptionBlock(),
                new ScriptTextAreaBlock(), 
                new ScriptButtonBlock(), 
                new ScriptTableBlock(),
                new ScriptTrBlock(),
                new ScriptTdBlock(),
                new ScriptTHeadBlock(),
                new ScriptTBodyBlock(),
                new ScriptTFootBlock(),
                new ScriptDlBlock(), 
                new ScriptDtBlock(), 
                new ScriptDdBlock(), 
                new ScriptSpanBlock(),
                new ScriptABlock(),
                new ScriptImgBlock(), 
                new ScriptEmBlock(), 
                new ScriptBBlock(), 
                new ScriptIBlock(), 
                new ScriptStrongBlock(), 
            });
        }
    }

}