using System;
using System.Collections.Generic;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ConvertAll")]
    public static class TranslatorExtensions
    {
        // Methods
        public static List<To> ParseAll<To, From>(this ITranslator<To, From> translator, IEnumerable<From> from)
        {
            var list = new List<To>();
            if (from != null)
            {
                foreach (var local in from)
                {
                    list.Add(translator.Parse(local));
                }
            }
            return list;
        }
    }


}