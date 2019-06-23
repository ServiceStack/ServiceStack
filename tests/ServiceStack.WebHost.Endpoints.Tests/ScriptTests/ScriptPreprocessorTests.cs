using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptPreprocessorTests
    {
        private const string CodeBlock = @"
<!--
title: The title
-->

# {{title}}

{{ 1 + 1 }}

```code
#each i in range(1,5)

    #if i.isOdd()
        `${i} is odd`
    else
        `${i} is even`
    /if

/each
```

```code
1 + 2 * 3
```
";

        [Test]
        public void Does_process_code_block()
        {
            var processed = ScriptPreprocessors.TransformCodeBlocks(CodeBlock);
            processed.Print();
            Assert.That(processed.Trim(), Is.EqualTo(@"
<!--
title: The title
-->

# {{title}}

{{ 1 + 1 }}

{{#each i in range(1,5)}}
{{#if i.isOdd()}}
{{`${i} is odd`}}
{{else}}
{{`${i} is even`}}
{{/if}}
{{/each}}

{{1 + 2 * 3}}".Trim()));
        }
        
        [Test]
        public void Does_preprocess_code_blocks_by_default()
        {
            var context = new ScriptContext().Init();

            var script = context.OneTimePage(CodeBlock);
            
            var output = new PageResult(script).Result;
            output.Print();
            
            Assert.That(output.Trim(), Is.EqualTo(@"
# The title

2

1 is odd
2 is even
3 is odd
4 is even
5 is odd

7".Trim()));
        }
    }
}