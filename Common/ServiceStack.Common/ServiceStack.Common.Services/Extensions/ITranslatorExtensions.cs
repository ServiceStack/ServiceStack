using System.Collections.Generic;
using ServiceStack.Common.DesignPatterns.Translator;

namespace ServiceStack.Common.Services.Extensions
{
    public static class ITranslatorExtensions
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