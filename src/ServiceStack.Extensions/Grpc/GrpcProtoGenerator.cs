using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ProtoBuf.Meta;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Grpc
{
    public class GrpcProtoGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        readonly GrpcFeature grpc;
        public static string Package { get; set; }

        public GrpcProtoGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.AssertPlugin<NativeTypesFeature>();
            grpc = HostContext.AssertPlugin<GrpcFeature>();
        }

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            metadata.RemoveIgnoredTypesForNet(Config);

            string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sbInner = StringBuilderCache.Allocate();
            var sb = new StringBuilderWrapper(sbInner);
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T"," ")));
            sb.AppendLine("Version: {0}".Fmt(Env.ServiceStackVersion));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}Package: {1}".Fmt(defaultValue("Package"), Config.Package));
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
//            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
//            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));

            sb.AppendLine("*/");
            sb.AppendLine();

            var methods = grpc.GrpcServicesType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetParameters().Length > 0)
                .OrderBy(x => x.GetParameters()[0].ParameterType.Name);

            var types = new HashSet<Type>();
            var services = new List<(Type, string)>();
            foreach (var method in methods)
            {
                if (!method.ReturnType.IsGenericType || method.ReturnType.DeclaringType == typeof(object))
                    continue;
                    
                var resGenericDef = method.ReturnType.GetGenericTypeDefinition();
                var isTask = resGenericDef == typeof(Task<>) || resGenericDef == typeof(ValueTask<>);
                var isStream = resGenericDef == typeof(IAsyncEnumerable<>);
                if (isTask || isStream)
                {
                    var reqType = method.GetParameters()[0].ParameterType;
                    var resType = method.ReturnType.GetGenericArguments()[0];
                    ServiceMetadata.AddReferencedTypes(types, reqType);
                    ServiceMetadata.AddReferencedTypes(types, resType);
                    if (isTask)
                    {
                        services.Add((reqType, $"rpc {method.Name}({reqType.Name}) returns ({resType.Name}) {{}}"));
                    }
                    else
                    {
                        services.Add((reqType, $"rpc {method.Name}({reqType.Name}) returns (stream {resType.Name}) {{}}"));
                    }
                }
            }

            var orderedTypes = types.ToList()
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name)
                .ToList();

            var addedRpcServices = false;
            //https://github.com/protobuf-net/protobuf-net/blob/master/src/Tools/bcl.proto
            var proto = GrpcUtils.TypeModel.GetSchema(null /*all types*/, ProtoSyntax.Proto3);
            foreach (var line in proto.ReadLines())
            {
                if (line.StartsWith("package "))
                {
                    var globalNs = Config.GlobalNamespace ?? orderedTypes[0].Namespace; 
                    sb.AppendLine($"package {Config.Package ?? globalNs.ToLowercaseUnderscore()};");
                    sb.AppendLine($"option csharp_namespace = \"{globalNs}\";");
                    continue;
                }

                sb.AppendLine(line);

                if (!addedRpcServices && string.IsNullOrEmpty(line))
                {
                    addedRpcServices = true;
                    
                    sb.AppendLine($"services {grpc.GrpcServicesType.Name} {{");
                    sb = sb.Indent();

                    foreach (var service in services)
                    {
                        sb.AppendLine();
                        if (Config.AddDescriptionAsComments)
                        {
                            var desc = service.Item1.FirstAttribute<DescriptionAttribute>() ?.Description
                                       ?? service.Item1.FirstAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                            if (desc != null)
                            {
                                foreach (var lineDesc in desc.ReadLines())
                                {
                                    sb.AppendLine("// " + lineDesc);
                                }
                            }
                        }
                        sb.AppendLine(service.Item2);
                    }
            
                    sb = sb.UnIndent();
                    sb.AppendLine("}");
            
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine(proto);

            return StringBuilderCache.ReturnAndFree(sbInner);
        }

    }
}