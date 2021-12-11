using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Blazor
{
    public enum BlazorTheme
    {
        Bootstrap5,
        Tailwind,
    }

    public static class BlazorConfig
    {
        public static BlazorTheme BlazorTheme { get; set; } = BlazorTheme.Bootstrap5;
    }
}
