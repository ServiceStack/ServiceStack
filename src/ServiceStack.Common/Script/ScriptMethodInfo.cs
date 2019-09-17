using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class ScriptMethodInfo
    {
        public string Name { get; set; }
        public string FirstParam { get; set; }
        public string ReturnType { get; set; }
        public int ParamCount { get; set; }
        public string[] RemainingParams { get; set; }
        
        public ParameterInfo[] Params { get; set; }
        
        public static List<ScriptMethodInfo> GetScriptMethods(Type scriptMethodsType)
        {
            var filters = scriptMethodsType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var to = filters
                .OrderBy(x => x.Name)
                .ThenBy(x => x.GetParameters().Length)
                .Where(x => x.DeclaringType != typeof(ScriptMethods) && x.DeclaringType != typeof(object))
                .Where(m => !m.IsSpecialName)                
                .Select(Create);
            return to.ToList();
        }

        public static ScriptMethodInfo Create(MethodInfo mi)
        {
            var pis = mi.GetParameters()
                .Where(x => x.ParameterType != typeof(ScriptScopeContext)).ToArray();
            
            var paramNames = pis
                .Select(x => x.Name)
                .ToArray();

            var to = new ScriptMethodInfo {
                Name = mi.Name,
                Params = pis,
                FirstParam = paramNames.FirstOrDefault(),
                ParamCount = paramNames.Length,
                RemainingParams = paramNames.Length > 1 ? paramNames.Skip(1).ToArray() : new string[]{},
                ReturnType = mi.ReturnType?.Name,
            };

            return to;
        }

        public string Return => ReturnType != null && ReturnType != nameof(StopExecution) ? " -> " + ReturnType : "";

        public string Body => ParamCount == 0
            ? $"{Name}"
            : ParamCount == 1
                ? $"| {Name}"
                : $"| {Name}(" + string.Join(", ", RemainingParams) + $")";

        public string Display => ParamCount == 0
            ? $"{Name}{Return}"
            : ParamCount == 1
                ? $"{FirstParam} | {Name}{Return}"
                : $"{FirstParam} | {Name}(" + string.Join(", ", RemainingParams) + $"){Return}";

        public string DisplaySignature
        {
            get
            {
                var sb = StringBuilderCache.Allocate()
                    .Append(Name);

                if (Params.Length > 0)
                {
                    sb.Append(" (");
                    for (var i = 0; i < Params.Length; i++)
                    {
                        sb.Append(i > 0 ? ", " : "")
                            .Append(Params[i].ParameterType.Name)
                            .Append(" ")
                            .Append(Params[i].Name);
                    }
                    sb.Append(")");
                }
                sb.Append(Return);

                return StringBuilderCache.ReturnAndFree(sb);
            }
        }
    }
}
