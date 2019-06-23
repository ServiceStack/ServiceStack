using ServiceStack.Text;

namespace ServiceStack.Script
{
    public static class ScriptPreprocessors
    {
        public static string TransformCodeBlocks(string script)
        {
            var hadCodeBlocks = false;
            var processed = StringBuilderCache.Allocate();
            var inCodeBlock = false;
            var inMultiLineBlock = false;
            foreach (var line in script.ReadLines())
            {
                if (line == "```code")
                {
                    hadCodeBlocks = true;
                    inCodeBlock = true;
                    continue;
                }
                if (inCodeBlock)
                {
                    if (line == "```")
                    {
                        inCodeBlock = false;
                        continue;
                    }
                    
                    var codeOnly = line.Trim();
                    if (string.IsNullOrEmpty(codeOnly))
                        continue;

                    if (codeOnly.StartsWith("{{") && !codeOnly.EndsWith("}}"))
                    {
                        inMultiLineBlock = true;
                        processed.AppendLine(codeOnly);
                        continue;
                    }
                    if (inMultiLineBlock)
                    {
                        if (codeOnly.EndsWith("}}"))
                        {
                            inMultiLineBlock = false;
                        }
                        processed.AppendLine(codeOnly);
                        continue;
                    }

                    var codify = "{{" + codeOnly + "}}";
                    processed.AppendLine(codify);
                    continue;
                }
                processed.AppendLine(line);
            }

            if (inMultiLineBlock)
                throw new SyntaxErrorException("Unterminated '}}' multi-line block not found");
            if (inCodeBlock)
                throw new SyntaxErrorException("Unterminated '```code' block, line with '```' not found");

            if (hadCodeBlocks)
                return StringBuilderCache.ReturnAndFree(processed);
            
            // return original script if there were no code blocks
            StringBuilderCache.Free(processed);
            return script;
        }
    }
}