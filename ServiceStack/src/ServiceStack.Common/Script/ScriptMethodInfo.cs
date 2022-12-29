using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class ScriptMethodInfo
    {
        private readonly MethodInfo methodInfo;
        private readonly ParameterInfo[] @params;
        public MethodInfo GetMethodInfo() => methodInfo;

        public string Name => methodInfo.Name;
        public string FirstParam => @params.FirstOrDefault()?.Name;
        public string FirstParamType => @params.FirstOrDefault()?.ParameterType.Name;
        public string ReturnType => methodInfo.ReturnType?.Name;
        public int ParamCount => @params.Length;

        public string[] RemainingParams => @params.Length > 1
            ? @params.Skip(1).Select(x => x.Name).ToArray()
            : TypeConstants.EmptyStringArray;
        public string[] ParamNames => @params.Select(x => x.Name).ToArray();
        public string[] ParamTypes => @params.Select(x => x.ParameterType.Name.ToString()).ToArray();

        public ScriptMethodInfo(MethodInfo methodInfo, ParameterInfo[] @params)
        {
            this.methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            this.@params = @params ?? throw new ArgumentNullException(nameof(@params));
        }

        public static List<ScriptMethodInfo> GetScriptMethods(Type scriptMethodsType, Func<MethodInfo,bool> where=null)
        {
            var filters = scriptMethodsType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var to = filters
                .OrderBy(x => x.Name)
                .ThenBy(x => x.GetParameters().Length)
                .Where(x => x.DeclaringType != typeof(ScriptMethods) && x.DeclaringType != typeof(object))
                .Where(m => !m.IsSpecialName);

            if (where != null)
                to = to.Where(where);
                
            return to.Select(Create).ToList();
        }

        public static ScriptMethodInfo Create(MethodInfo mi)
        {
            var pis = mi.GetParameters()
                .Where(x => x.ParameterType != typeof(ScriptScopeContext)).ToArray();
            
            return new ScriptMethodInfo(mi, pis);
        }

        public string Return => ReturnType != null && ReturnType != nameof(StopExecution) ? " -> " + ReturnType : "";

        public string Body => ParamCount == 0
            ? $"{Name}"
            : ParamCount == 1
                ? $"|> {Name}"
                : $"|> {Name}(" + string.Join(", ", RemainingParams) + $")";

        public string ScriptSignature => ParamCount == 0
            ? $"{Name}{Return}"
            : ParamCount == 1
                ? $"{FirstParam} |> {Name}{Return}"
                : $"{FirstParam} |> {Name}(" + string.Join(", ", RemainingParams) + $"){Return}";

        private string signature;
        public string Signature
        {
            get
            {
                if (signature != null)
                    return signature;
                
                var sb = StringBuilderCache.Allocate()
                    .Append(Name);

                if (@params.Length > 0)
                {
                    sb.Append(" (");
                    for (var i = 0; i < @params.Length; i++)
                    {
                        sb.Append(i > 0 ? ", " : "")
                            .Append(@params[i].ParameterType.Name)
                            .Append(" ")
                            .Append(@params[i].Name);
                    }
                    sb.Append(")");
                }
                sb.Append(Return);

                return signature = StringBuilderCache.ReturnAndFree(sb);
            }
        }

        public override string ToString() => Signature;
    }

}
