﻿namespace ServiceStack
{
    public interface IMarkdownTransformer
    {
        string Transform(string markdown);
    }
    
    public static class MarkdownConfig
    {
        public static IMarkdownTransformer Transformer { get; set; } = new MarkdownDeep.Markdown();

        public static string Transform(string html) => Transformer.Transform(html);
    }
}