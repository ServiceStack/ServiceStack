using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Blazor
{
    public enum Theme
    {
        Bootstrap5,
        Tailwind,
    }

    public static class BlazorConfig
    {
        public static Theme Theme { get; set; } = Theme.Bootstrap5;
    }
}
