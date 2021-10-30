using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming
    
    public class ProtectedScripts : ScriptMethods
    {
        public static readonly ProtectedScripts Instance = new ProtectedScripts();

        public object resolve(ScriptScopeContext scope, object type)
        {
            if (type == null)
                return null;
            var t = type as Type ?? (type is string s
                        ? @typeof(s)
                        : throw new NotSupportedException($"{nameof(resolve)} requires a Type or Type Name, received '{type.GetType().Name}'"));

            var instance = scope.Context.Container.Resolve(t);
            return instance;
        }
        
        public object @default(string typeName)
        {
            var type = assertTypeOf(typeName);
            return type.GetDefaultValue();
        }

        public object @new(string typeName)
        {
            var type = @typeof(typeName);
            return type != null 
                ? createInstance(type) 
                : null;
        }

        public object @new(string typeName, List<object> constructorArgs)
        {
            var type = @typeof(typeName);
            return type != null 
                ? createInstance(type, constructorArgs) 
                : null;
        }

        public object set(object instance, Dictionary<string, object> args)
        {
            args.PopulateInstance(instance);
            return instance;
        }

        private Type[] typeGenericTypes(string typeName)
        {
            return typeGenericTypes(typeGenericArgs(typeName));
        }

        private Type[] typeGenericTypes(List<string> genericArgs)
        {
            var genericTypes = new List<Type>();
            foreach (var genericArg in genericArgs)
            {
                var genericType = @typeof(genericArg);
                genericTypes.Add(genericType);
            }

            return genericTypes.ToArray();
        }

        private static List<string> typeGenericArgs(string typeName)
        {
            var argList = typeName.RightPart('<');
            argList = argList.Substring(0, argList.Length - 1);
            var splitArgs = StringUtils.SplitGenericArgs(argList);
            return splitArgs;
        }

        public object createInstance(Type type) => AssertCanCreateType(type).CreateInstance();

        public object createInstance(Type type, List<object> constructorArgs)
        {
            var key = callKey(AssertCanCreateType(type), "<new>", constructorArgs);

            var activator = (ObjectActivator) Context.Cache.GetOrAdd(key, k => {
                
                var args = constructorArgs;
                var argTypes = args?.Select(x => x?.GetType()).ToArray() ?? TypeConstants.EmptyTypeArray;
                
                var ctorInfo = ResolveConstructor(type, argTypes);
                return ctorInfo.GetActivator();
            });

            return activator(constructorArgs?.ToArray() ?? TypeConstants.EmptyObjectArray);
        }

        private Type AssertCanCreateType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsPublic && !Context.AllowScriptingOfAllTypes)
                throw new NotSupportedException(
                    "Can only create instances of non public Types when AllowScriptingOfAllTypes=true");
            return type;
        }

        private ConstructorInfo ResolveConstructor(Type type, Type[] argTypes)
        {
            var argsCount = argTypes.Length;
            var ctors = type.GetConstructors()
                .Where(x => x.GetParameters().Length == argsCount).ToArray();

            if (ctors.Length == 0)
            {
                var argTypesList = string.Join(",", argTypes.Select(x => x?.Name ?? "null"));
                throw new NotSupportedException(
                    $"Constructor {typeQualifiedName(type)}({argTypesList}) does not exist");
            }

            ConstructorInfo targetCtor = null;
            if (ctors.Length > 1)
            {
                var candidates = 0;
                foreach (var ctor in ctors)
                {
                    var match = true;

                    var ctorParams = ctor.GetParameters();
                    for (var i = 0; i < argTypes.Length; i++)
                    {
                        var argType = argTypes[i];
                        if (argType == null)
                            continue;

                        match = ctorParams[i].ParameterType == argType;
                        if (!match)
                            break;
                    }

                    if (match)
                    {
                        targetCtor = ctor;
                        candidates++;
                    }
                }

                if (targetCtor == null || candidates != 1)
                {
                    var argTypesList = string.Join(",", argTypes.Select(x => x?.Name ?? "null"));
                    throw new NotSupportedException(
                        $"Could not resolve ambiguous constructor {typeQualifiedName(type)}({argTypesList})");
                }
            }
            else targetCtor = ctors[0];

            return targetCtor;
        }

        public Type getType(object instance) => instance?.GetType();

        public string typeQualifiedName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var sb = StringBuilderCache.Allocate();
            sb.Append(type.Namespace).Append('.');
            
            if (type.GenericTypeArguments.Length > 0)
            {
                sb.Append(type.Name.LeftPart('`'))
                    .Append('<');

                var i = 0;
                foreach (var arg in type.GenericTypeArguments)
                {
                    if (i++ > 0)
                        sb.Append(',');
                    
                    sb.Append(typeQualifiedName(arg));
                }
                sb.Append('>');
            }
            else
            {
                sb.Append(type.Name);
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        public static string TypeNotFoundErrorMessage(string typeName) => $"Could not resolve Type '{typeName}'. " +
            $"Use ScriptContext.ScriptAssemblies or ScriptContext.AllowScriptingOfAllTypes + ScriptNamespaces to increase Type resolution";
        
        public Type assertTypeOf(string name)
        {
            var type = @typeof(name);
            if (type == null)
                throw new NotSupportedException(TypeNotFoundErrorMessage(name));
            return type;
        }

        /// <summary>
        /// Returns Type from type name syntax of .NET's typeof() 
        /// </summary>
        public Type @typeof(string typeName)
        {
            typeName = typeName?.Trim();
            
            if (string.IsNullOrEmpty(typeName))
                return null;
            
            var key = "type:" + typeName;

            Type cookType(Type type, List<string> genericArgs, bool isArray, bool isNullable)
            {
                if (type.IsGenericType)
                {
                    var isGenericDefinition = genericArgs != null && genericArgs.All(x => x == "");
                    if (!isGenericDefinition)
                    {
                        var genericTypes = typeGenericTypes(genericArgs);
                        type = type.MakeGenericType(genericTypes);
                    }
                }

                if (isArray)
                {
                    type = type.MakeArrayType();
                }
            
                return isNullable
                    ? typeof(Nullable<>).MakeGenericType(type)
                    : type;
            }
            
            Type onlyTypeOf(string _typeName)
            {
                var isArray = _typeName.EndsWith("[]");
                if (isArray)
                {
                    _typeName = _typeName.Substring(0, _typeName.Length - 2);
                }

                var isGeneric = _typeName.IndexOf('<') >= 0;
                List<string> genericArgs = null;

                if (isGeneric)
                {
                    genericArgs = typeGenericArgs(_typeName);
                    _typeName = _typeName.LeftPart('<') + '`' + Math.Max(genericArgs.Count, 1);
                }
                var isNullable = _typeName.EndsWith("?");
                if (isNullable)
                    _typeName = _typeName.Substring(0, _typeName.Length - 1);
                
                if (_typeName.IndexOf('.') >= 0)
                {
                    if (Context.ScriptTypeQualifiedNameMap.TryGetValue(_typeName, out var type))
                        return cookType(type, genericArgs, isArray, isNullable);

                    if (Context.AllowScriptingOfAllTypes)
                    {
                        type = AssemblyUtils.FindType(_typeName);
                        if (type != null)
                            return cookType(type, genericArgs, isArray, isNullable);
                    }
                }
                else
                {
                    var ret = _typeName switch {
                        "int" => !isArray ? typeof(int) : typeof(int[]),
                        "long" => !isArray ? typeof(long) : typeof(long[]),
                        "bool" => !isArray ? typeof(bool) : typeof(bool[]),
                        "char" => !isArray ? typeof(char) : typeof(char[]),
                        "double" => !isArray ? typeof(double) : typeof(double[]),
                        "float" => !isArray ? typeof(float) : typeof(float[]),
                        "decimal" => !isArray ? typeof(decimal) : typeof(decimal[]),
                        "byte" => !isArray ? typeof(byte) : typeof(byte[]),
                        "sbyte" => !isArray ? typeof(sbyte) : typeof(sbyte[]),
                        "uint" => !isArray ? typeof(uint) : typeof(uint[]),
                        "ulong" => !isArray ? typeof(ulong) : typeof(ulong[]),
                        "object" => !isArray ? typeof(object) : typeof(object[]),
                        "short" => !isArray ? typeof(short) : typeof(short[]),
                        "ushort" => !isArray ? typeof(ushort) : typeof(ushort[]),
                        "string" => !isArray ? typeof(string) : typeof(string[]),
                        "Guid" => !isArray ? typeof(Guid) : typeof(Guid[]),
                        "TimeSpan" => !isArray ? typeof(TimeSpan) : typeof(TimeSpan[]),
                        "DateTime" => !isArray ? typeof(DateTime) : typeof(DateTime[]),
                        "DateTimeOffset" => !isArray ? typeof(DateTimeOffset) : typeof(DateTimeOffset[]),
                        _ => null,
                    };
                    if (ret != null)
                    {
                        return isNullable
                            ? typeof(Nullable<>).MakeGenericType(ret)
                            : ret;
                    }

                    if (Context.ScriptTypeNameMap.TryGetValue(_typeName, out var type))
                        return cookType(type, genericArgs, isArray, isNullable);
                }

                foreach (var ns in Context.ScriptNamespaces)
                {
                    var lookupType = ns + "." + _typeName;
                    if (Context.ScriptTypeQualifiedNameMap.TryGetValue(lookupType, out var type))
                        return cookType(type, genericArgs, isArray, isNullable);
                    
                    if (Context.AllowScriptingOfAllTypes)
                    {
                        type = AssemblyUtils.FindType(lookupType);
                        if (type != null)
                            return cookType(type, genericArgs, isArray, isNullable);
                    }
                }

                return null;
            }

            var resolvedType = (Type) Context.Cache.GetOrAdd(key, k => {

                var type = onlyTypeOf(typeName);
                if (type != null)
                    return type;

                var parts = typeName.Split('.');
                if (parts.Length > 1)
                {
                    var nameBuilder = "";
                    for (var i = 0; i < parts.Length; i++)
                    {
                        try
                        {
                            if (i > 0)
                                nameBuilder += '.';
                            
                            nameBuilder += parts[i];
                            var parentType = onlyTypeOf(nameBuilder);
                            if (parentType != null)
                            {
                                var nestedTypeName = parts[++i];
                                var nestedType = parentType.GetNestedType(nestedTypeName);
                                i++;
                                while (i < parts.Length)
                                {
                                    nestedTypeName = parts[i++];
                                    nestedType = nestedType.GetNestedType(nestedTypeName);
                                }
                                return nestedType;
                            }
                        }
                        catch { }
                    }
                }

                return null;
            });

            return resolvedType;
        }
        
        public Type typeofProgId(string name) => Env.IsWindows
            ? Type.GetTypeFromProgID(name) // .NET Core throws TargetInvocationException CoreCLR_REMOVED -- Unmanaged activation removed
            : null;

        public object call(object instance, string name) => call(instance, name, null);

        internal string callKey(Type type, string name, List<object> args)
        {
            var sb = StringBuilderCache.Allocate()
                .Append("call:")
                .Append(type.Namespace)
                .Append('.')
                .Append(type.Name)
                .Append('.')
                .Append(name);

            if (type.GenericTypeArguments.Length > 0)
            {
                sb.Append('<');
                for (var i = 0; i < type.GenericTypeArguments.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    var genericArg = type.GenericTypeArguments[i];
                    sb.Append(typeQualifiedName(genericArg));
                }
                sb.Append('>');
            }
            
            appendArgTypes(sb, args);
            return StringBuilderCache.ReturnAndFree(sb);
        }

        static string argTypesString(List<object> args)
        {
            var sb = StringBuilderCache.Allocate();
            appendArgTypes(sb, args);
            return StringBuilderCache.ReturnAndFree(sb);
        }

        private static void appendArgTypes(StringBuilder sb, List<object> args)
        {
            sb.Append('(');

            if (args != null)
            {
                for (var i = 0; i < args.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    var argType = args[i]?.GetType();
                    sb.Append(argType == null ? "null" : argType.Namespace + '.' + argType.Name);
                }
            }

            sb.Append(')');
        }

        public object call(object instance, string name, List<object> args)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            var type = instance.GetType();

            var key = callKey(type, name, args);

            var invoker = (Delegate)Context.Cache.GetOrAdd(key, k => {
                var argTypes = args?.Select(x => x?.GetType()).ToArray();
                var targetMethod = ResolveMethod(type, name, argTypes, argTypes?.Length ?? 0, out var fn);
                if (targetMethod != null && targetMethod.IsStatic)
                    throw new NotSupportedException($"Cannot call static method {instance.GetType().Name}.{targetMethod.Name}");
                
                return fn ?? targetMethod.GetInvokerDelegate();
            });

            if (invoker is MethodInvoker methodInvoker)
            {
                var ret = methodInvoker(instance, args?.ToArray() ?? TypeConstants.EmptyObjectArray);
                return ret;
            }
            if (invoker is ActionInvoker actionInvoker)
            {
                actionInvoker(instance, args?.ToArray() ?? TypeConstants.EmptyObjectArray);
                return IgnoreResult.Value;
            }

            throw new NotSupportedException($"Cannot call {invoker.GetType().Name} methods");
        }

        private MethodInfo ResolveMethod(Type type, string methodName, Type[] argTypes, int? argsCount, out Delegate invokerDelegate)
        {
            invokerDelegate = null;
            var isGeneric = methodName.IndexOf('<') >= 0;
            var name = isGeneric ? methodName.LeftPart('<') : methodName;

            var genericArgs = isGeneric
                ? typeGenericArgs(methodName)
                : TypeConstants.EmptyStringList;
            var genericArgsCount = genericArgs.Count;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.Name == name && (argsCount == null || x.GetParameters().Length == argsCount.Value) 
                    && ((genericArgs.Count == 0 && !x.IsGenericMethod) || (x.IsGenericMethod && x.GetGenericArguments().Length == genericArgsCount)))
                .ToArray();

            MethodInfo targetMethod = null;
            if (methods.Length == 0)
            {
                if ((argTypes?.Length ?? 0) == 0)
                {
                    var prop = type.GetProperty(name,BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                    if (prop != null)
                    {
                        targetMethod = prop.GetMethod;
                        if (targetMethod == null)
                        {
                            throw new NotSupportedException(
                                $"Property {typeQualifiedName(type)}.{name} does not have a getter");
                        }
                    }
                    else
                    {
                        var field = type.GetField(name,BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                        if (field != null)
                        {
                            if (field.IsStatic)
                            {
                                invokerDelegate = (StaticMethodInvoker) ((args) => field.GetValue(null));
                                return null;
                            }
                            else
                            {
                                invokerDelegate = (MethodInvoker) ((instance, args) => field.GetValue(instance));
                                return null;
                            }
                        }
                    }
                }

                if (targetMethod == null)
                {
                    throw new NotSupportedException(
                        $"Method {typeQualifiedName(type)}.{name} does not exist");
                }
            }

            if (targetMethod == null)
            {
                if (methods.Length > 1)
                {
                    var candidates = 0;
                    foreach (var method in methods)
                    {
                        var match = true;

                        var methodParams = method.GetParameters();
                        if (argTypes != null)
                        {
                            for (var i = 0; i < argTypes.Length; i++)
                            {
                                var argType = argTypes[i];
                                if (argType == null)
                                    continue;

                                match = methodParams[i].ParameterType == argType;
                                if (!match)
                                    break;
                            }
                        }

                        if (match)
                        {
                            targetMethod = method;
                            candidates++;
                        }
                    }

                    if (targetMethod == null || candidates != 1)
                    {
                        var argTypesList = argTypes != null ? string.Join(",", argTypes.Select(x => x?.Name ?? "null")) : "";
                        throw new NotSupportedException(
                            $"Could not resolve ambiguous method {typeQualifiedName(type)}.{name}({argTypesList})");
                    }
                }
                else targetMethod = methods[0];
            }

            if (targetMethod.IsGenericMethod)
            {
                var genericTypes = typeGenericTypes(methodName);
                targetMethod = targetMethod.MakeGenericMethod(genericTypes);
            }

            if (targetMethod == null)
                throw new NotSupportedException(MethodNotExists($"{type.Name}.{name}"));

            return targetMethod;
        }

        /// <summary>
        /// Qualified Constructor Name Examples:
        ///  - Type()
        ///  - Type(string)
        ///  - GenericType&lt;string&lt;(System.Int32)
        ///  - Namespace.Type()
        /// </summary>
        public ObjectActivator Constructor(string qualifiedConstructorName)
        {
            if (qualifiedConstructorName.IndexOf('(') == -1)
                throw new NotSupportedException($"Invalid Constructor Name '{qualifiedConstructorName}', " +
                    $"format: <type>(<arg-types>), e.g. Uri(String), see: https://sharpscript.net/docs/script-net");
            
            var name = qualifiedConstructorName;

            var activator = (ObjectActivator) Context.Cache.GetOrAdd(nameof(Constructor) + ":" + name, k => {
                var argList = name.LastRightPart('(');
                argList = argList?.Substring(0, argList.Length - 1);
                var argTypes = typeGenericTypes(StringUtils.SplitGenericArgs(argList));

                name = name.LastLeftPart('(');

                var type = assertTypeOf(name);

                var ctor = ResolveConstructor(type, argTypes);

                return ctor.GetActivator();
            });
            
            return activator;
        }

        /// <summary>
        /// Shorter Alias for Constructor
        /// </summary>
        /// <returns></returns>
        public Delegate C(string qualifiedMethodName) => Constructor(qualifiedMethodName);

        /// <summary>
        /// Shorter Alias for Function
        /// </summary>
        /// <returns></returns>
        public Delegate F(string qualifiedMethodName) => Function(qualifiedMethodName);

        /// <summary>
        /// Shorter Alias for Function(name,args)
        /// </summary>
        /// <returns></returns>
        public Delegate F(string qualifiedMethodName, List<object> args) => Function(qualifiedMethodName, args);

        /// <summary>
        /// Qualified Method Name Examples:
        ///  - Console.WriteLine(string)
        ///  - Type.StaticMethod
        ///  - Type.InstanceMethod
        ///  - GenericType&lt;string&lt;.Method
        ///  - GenericType&lt;string&lt;.GenericMethod&lt;System.Int32&lt;
        ///  - Namespace.Type.Method
        /// </summary>
        public Delegate Function(string qualifiedMethodName)
        {
            if (qualifiedMethodName.IndexOf('.') == -1)
                throw new NotSupportedException($"Invalid Function Name '{qualifiedMethodName}', " +
                    $"format: <type>.<method>(<arg-types>), e.g. Console.WriteLine(string), see: https://sharpscript.net/docs/script-net");

            var invoker = (Delegate) Context.Cache.GetOrAdd(nameof(Function) + ":" + qualifiedMethodName, k => 
                ResolveFunction(qualifiedMethodName));

            return invoker;
        }

        /// <summary>
        /// Resolve Function from qualified type name, when args Type list are unspecified fallback to use args to resolve ambiguous methods
        /// 
        /// Qualified Method Name Examples:
        ///  - Console.WriteLine ['string']
        ///  - Type.StaticMethod
        ///  - Type.InstanceMethod
        ///  - GenericType&lt;string&lt;.Method
        ///  - GenericType&lt;string&lt;.GenericMethod&lt;System.Int32&lt;
        ///  - Namespace.Type.Method
        /// </summary>
        public Delegate Function(string qualifiedMethodName, List<object> args)
        {
            if (qualifiedMethodName.IndexOf('.') == -1)
                throw new NotSupportedException($"Invalid Function Name '{qualifiedMethodName}', " +
                                                $"format: <type>.<method>(<arg-types>), e.g. Console.WriteLine(string), see: https://sharpscript.net/docs/script-net");

            var key = nameof(Function) + ":" + qualifiedMethodName + argTypesString(args);
            var invoker = (Delegate)Context.Cache.GetOrAdd(key, k => 
                ResolveFunction(qualifiedMethodName, args?.Select(x => x?.GetType()).ToArray()));

            return invoker;
        }

        private Delegate ResolveFunction(string name, Type[] argTypes=null)
        {
            var hasArgsList = name.IndexOf('(') >= 0;
            var argList = hasArgsList
                ? name.LastRightPart('(')
                : null;
            argList = argList?.Substring(0, argList.Length - 1);

            name = name.LastLeftPart('(');

            var lastGenericPos = name.LastIndexOf('>');
            var lastSepPos = name.LastIndexOf('.');

            int pos = -1;
            if (lastSepPos > lastGenericPos)
            {
                pos = lastSepPos;
            }
            else
            {
                var genericPos = name.IndexOf('<');
                pos = genericPos >= 0
                    ? name.LastIndexOf('.', genericPos)
                    : name.LastIndexOf('.');

                if (pos == -1)
                    pos = name.IndexOf(">.", StringComparison.Ordinal) + 1;
            }

            if (pos == -1)
                throw new NotSupportedException($"Could not parse Function Name '{name}', " +
                                                $"format: <type>.<method>(<arg-types>), e.g. Console.WriteLine(string)");


            var typeName = name.Substring(0, pos);
            var methodName = name.Substring(pos + 1);

            if (hasArgsList)
            {
                var splitArgs = StringUtils.SplitGenericArgs(argList);
                argTypes = typeGenericTypes(splitArgs);
                for (var i = 0; i < argTypes.Length; i++)
                {
                    if (argTypes[i] == null)
                        throw new NotSupportedException($"Could not resolve Argument Type '{splitArgs[i]}' for '{name}'");
                }
            }

            var type = assertTypeOf(typeName);

            var method = ResolveMethod(type, methodName, argTypes, argTypes?.Length, out var fn);
            return fn ?? method.GetInvokerDelegate();
        }

        static string MethodNotExists(string methodName) => $"Method {methodName} does not exist"; 

        public MemoryVirtualFiles vfsMemory() => new();

        public FileSystemVirtualFiles vfsFileSystem(string dirPath) => new(dirPath);
        
        public GistVirtualFiles vfsGist(string gistId) => new(gistId);
        public GistVirtualFiles vfsGist(string gistId, string accessToken) => new(gistId, accessToken);

        public string osPaths(string path) => Env.IsWindows
            ? path.Replace('/', '\\')
            : path.Replace('\\', '/');

        public IVirtualFile resolveFile(ScriptScopeContext scope, string virtualPath) =>
            ResolveFile(scope.Context.VirtualFiles, scope.PageResult.VirtualPath, virtualPath);

        public IVirtualFile ResolveFile(string filterName, ScriptScopeContext scope, string virtualPath)
        {
            var file = ResolveFile(scope.Context.VirtualFiles, scope.PageResult.VirtualPath, virtualPath);
            if (file == null)
                throw new FileNotFoundException($"{filterName} '{virtualPath}' in page '{scope.Page.VirtualPath}' was not found");

            return file;
        }

        public IVirtualFile ResolveFile(IVirtualPathProvider virtualFiles, string fromVirtualPath, string virtualPath)
        {
            IVirtualFile file = null;

            var pathMapKey = nameof(ResolveFile) + ">" + fromVirtualPath;
            var pathMapping = Context.GetPathMapping(pathMapKey, virtualPath);
            if (pathMapping != null)
            {
                file = virtualFiles.GetFile(pathMapping);
                if (file != null)
                    return file;                    
                Context.RemovePathMapping(pathMapKey, pathMapping);
            }

            var tryExactMatch = virtualPath.IndexOf('/') >= 0; //if nested path specified, look for an exact match first
            if (tryExactMatch)
            {
                file = virtualFiles.GetFile(virtualPath);
                if (file != null)
                {
                    Context.SetPathMapping(pathMapKey, virtualPath, virtualPath);
                    return file;
                }
            }

            var parentPath = fromVirtualPath.IndexOf('/') >= 0
                ? fromVirtualPath.LastLeftPart('/')
                : "";

            do
            {
                var seekPath = parentPath.CombineWith(virtualPath);
                file = virtualFiles.GetFile(seekPath);
                if (file != null)
                {
                    Context.SetPathMapping(pathMapKey, virtualPath, seekPath);
                    return file;
                }

                if (parentPath == "")
                    break;

                parentPath = parentPath.IndexOf('/') >= 0
                    ? parentPath.LastLeftPart('/')
                    : "";
            } while (true);

            return null;
        }

        public async Task includeFile(ScriptScopeContext scope, string virtualPath)
        {
            var file = ResolveFile(nameof(includeFile), scope, virtualPath);
            using var reader = file.OpenRead();
            await reader.CopyToAsync(scope.OutputStream).ConfigAwait();
        }

        public async Task ifDebugIncludeScript(ScriptScopeContext scope, string virtualPath)
        {
            if (scope.Context.DebugMode)
            {
                await scope.OutputStream.WriteAsync("<script>").ConfigAwait();
                await includeFile(scope, virtualPath).ConfigAwait();
                await scope.OutputStream.WriteAsync("</script>").ConfigAwait();
            }
        }

        IVirtualPathProvider VirtualFiles => Context.VirtualFiles;
        
        // Old Aliases for Backwards compatibility
        [Alias("allFiles")]
        public IEnumerable<IVirtualFile> vfsAllFiles() => allFiles(VirtualFiles);
        [Alias("allRootFiles")]
        public IEnumerable<IVirtualFile> vfsAllRootFiles() => allRootFiles(VirtualFiles);
        [Alias("allRootDirectories")]
        public IEnumerable<IVirtualDirectory> vfsAllRootDirectories() => allRootDirectories(VirtualFiles);
        [Alias("combinePath")]
        public string vfsCombinePath(string basePath, string relativePath) => combinePath(VirtualFiles, basePath, relativePath);
        [Alias("findFilesInDirectory")]
        public IEnumerable<IVirtualFile> dirFilesFind(string dirPath, string globPattern) => findFilesInDirectory(VirtualFiles,dirPath,globPattern);
        [Alias("findFiles")]
        public IEnumerable<IVirtualFile> filesFind(string globPattern) => findFiles(VirtualFiles,globPattern);
        [Alias("writeFile")]
        public string fileWrite(string virtualPath, object contents) => writeFile(VirtualFiles, virtualPath, contents);
        [Alias("appendToFile")]
        public string fileAppend(string virtualPath, object contents) => appendToFile(VirtualFiles, virtualPath, contents);
        [Alias("deleteFile")]
        public string fileDelete(string virtualPath) => deleteFile(VirtualFiles, virtualPath);
        [Alias("deleteFile")]
        public string dirDelete(string virtualPath) => deleteFile(VirtualFiles, virtualPath);
        [Alias("fileTextContents")]
        public string fileReadAll(string virtualPath) => fileTextContents(VirtualFiles,virtualPath);
        [Alias("fileBytesContent")]
        public byte[] fileReadAllBytes(string virtualPath) => fileBytesContent(VirtualFiles, virtualPath);
        
        public IEnumerable<IVirtualFile> allFiles() => allFiles(VirtualFiles);
        public IEnumerable<IVirtualFile> allFiles(IVirtualPathProvider vfs) => vfs.GetAllFiles();

        public IEnumerable<IVirtualFile> allRootFiles() => allRootFiles(VirtualFiles);
        public IEnumerable<IVirtualFile> allRootFiles(IVirtualPathProvider vfs) => vfs.GetRootFiles();
        public IEnumerable<IVirtualDirectory> allRootDirectories() => allRootDirectories(VirtualFiles);
        public IEnumerable<IVirtualDirectory> allRootDirectories(IVirtualPathProvider vfs) => vfs.GetRootDirectories();
        public string combinePath(string basePath, string relativePath) => combinePath(VirtualFiles, basePath, relativePath);
        public string combinePath(IVirtualPathProvider vfs, string basePath, string relativePath) => vfs.CombineVirtualPath(basePath, relativePath);

        public IVirtualDirectory dir(string virtualPath) => dir(VirtualFiles,virtualPath);
        public IVirtualDirectory dir(IVirtualPathProvider vfs, string virtualPath) => vfs.GetDirectory(virtualPath);
        public bool dirExists(string virtualPath) => VirtualFiles.DirectoryExists(virtualPath);
        public bool dirExists(IVirtualPathProvider vfs, string virtualPath) => vfs.DirectoryExists(virtualPath);
        public IVirtualFile dirFile(string dirPath, string fileName) => dirFile(VirtualFiles,dirPath,fileName);
        public IVirtualFile dirFile(IVirtualPathProvider vfs, string dirPath, string fileName) => vfs.GetDirectory(dirPath)?.GetFile(fileName);
        public IEnumerable<IVirtualFile> dirFiles(string dirPath) => dirFiles(VirtualFiles,dirPath);
        public IEnumerable<IVirtualFile> dirFiles(IVirtualPathProvider vfs, string dirPath) => vfs.GetDirectory(dirPath)?.GetFiles() ?? new List<IVirtualFile>();
        public IVirtualDirectory dirDirectory(string dirPath, string dirName) => dirDirectory(VirtualFiles,dirPath,dirName);
        public IVirtualDirectory dirDirectory(IVirtualPathProvider vfs, string dirPath, string dirName) => vfs.GetDirectory(dirPath)?.GetDirectory(dirName);
        public IEnumerable<IVirtualDirectory> dirDirectories(string dirPath) => dirDirectories(VirtualFiles,dirPath);
        public IEnumerable<IVirtualDirectory> dirDirectories(IVirtualPathProvider vfs, string dirPath) => vfs.GetDirectory(dirPath)?.GetDirectories() ?? new List<IVirtualDirectory>();
        public IEnumerable<IVirtualFile> findFilesInDirectory(string dirPath, string globPattern) => findFilesInDirectory(VirtualFiles,dirPath,globPattern);
        public IEnumerable<IVirtualFile> findFilesInDirectory(IVirtualPathProvider vfs, string dirPath, string globPattern) => vfs.GetDirectory(dirPath)?.GetAllMatchingFiles(globPattern);

        public IEnumerable<IVirtualFile> findFiles(string globPattern) => findFiles(VirtualFiles,globPattern);

        public IEnumerable<IVirtualFile> dirFindFiles(IVirtualDirectory dir, string globPattern) => dir.GetAllMatchingFiles(globPattern);
        public IEnumerable<IVirtualFile> dirFindFiles(IVirtualDirectory dir, string globPattern, int maxDepth) => dir.GetAllMatchingFiles(globPattern, maxDepth);
        public IEnumerable<IVirtualFile> findFiles(IVirtualPathProvider vfs, string globPattern) => vfs.GetAllMatchingFiles(globPattern);
        public IEnumerable<IVirtualFile> findFiles(IVirtualPathProvider vfs, string globPattern, int maxDepth) => vfs.GetAllMatchingFiles(globPattern, maxDepth);
        
        public bool fileExists(string virtualPath) => fileExists(VirtualFiles,virtualPath);
        public bool fileExists(IVirtualPathProvider vfs, string virtualPath) => vfs.FileExists(virtualPath);
        public IVirtualFile file(string virtualPath) => file(VirtualFiles,virtualPath);
        public IVirtualFile file(IVirtualPathProvider vfs, string virtualPath) => vfs.GetFile(virtualPath);
        public string writeFile(string virtualPath, object contents) => writeFile(VirtualFiles, virtualPath, contents);
        public string writeFile(IVirtualPathProvider vfs, string virtualPath, object contents)
        {
            vfs.WriteFile(virtualPath, contents);
            return virtualPath;
        }

        public object writeFiles(IVirtualPathProvider vfs, Dictionary<string,object> files)
        {
            vfs.WriteFiles(files);
            return IgnoreResult.Value;
        }

        public object writeTextFiles(IVirtualPathProvider vfs, Dictionary<string,string> textFiles)
        {
            vfs.WriteFiles(textFiles);
            return IgnoreResult.Value;
        }

        public string appendToFile(string virtualPath, object contents) => appendToFile(VirtualFiles, virtualPath, contents);
        public string appendToFile(IVirtualPathProvider vfs, string virtualPath, object contents)
        {
            vfs.AppendFile(virtualPath, contents);

            return virtualPath;
        }

        public string deleteFile(string virtualPath) => deleteFile(VirtualFiles, virtualPath);
        public string deleteFile(IVirtualPathProvider vfs, string virtualPath)
        {
            vfs.DeleteFile(virtualPath);
            return virtualPath;
        }

        public string deleteDirectory(string virtualPath) => deleteFile(VirtualFiles, virtualPath);
        public string deleteDirectory(IVirtualPathProvider vfs, string virtualPath)
        {
            vfs.DeleteFolder(virtualPath);
            return virtualPath;
        }

        public string fileTextContents(string virtualPath) => fileTextContents(VirtualFiles,virtualPath);
        public string fileTextContents(IVirtualPathProvider vfs, string virtualPath) => vfs.GetFile(virtualPath)?.ReadAllText();

        public object fileContents(IVirtualPathProvider vfs, string virtualPath) =>
            vfs.GetFile(virtualPath).GetContents();

        // string virtual filePath or IVirtualFile 
        public object fileContents(object file) => file is null
            ? null
            : file is string path
                ? fileContents(VirtualFiles, path)
                : file is IVirtualFile ifile
                    ? ifile.GetContents()
                : throw new NotSupportedException(nameof(fileContents) + " expects string virtualPath or IVirtualFile but was " + file.GetType().Name);
        
        public string textContents(IVirtualFile file) => file?.ReadAllText();
        public byte[] fileBytesContent(string virtualPath) => fileBytesContent(VirtualFiles, virtualPath);
        public byte[] fileBytesContent(IVirtualPathProvider vfs, string virtualPath) => vfs.GetFile(virtualPath)?.ReadAllBytes();
        public byte[] bytesContent(IVirtualFile file) => file?.ReadAllBytes();
        public string fileHash(string virtualPath) => fileHash(VirtualFiles,virtualPath);
        public string fileHash(IVirtualPathProvider vfs, string virtualPath) => vfs.GetFileHash(virtualPath);
        public string fileHash(IVirtualFile file) => file?.GetFileHash();
        public bool fileIsBinary(IVirtualFile file) => MimeTypes.IsBinary(MimeTypes.GetMimeType(file.Extension));
        public string fileContentType(IVirtualFile file) => MimeTypes.GetMimeType(file.Extension);

        //alias
        public Task urlContents(ScriptScopeContext scope, string url) => includeUrl(scope, url, null);
        public Task urlContents(ScriptScopeContext scope, string url, object options) => includeUrl(scope, url, options);

        public Task includeUrl(ScriptScopeContext scope, string url) => includeUrl(scope, url, null);
        public async Task includeUrl(ScriptScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);

            var webReq = initWebRequest(url, scopedParams);

            if (scopedParams.TryRemove("data", out object data))
            {
                if (webReq.ContentType == null)
                    webReq.ContentType = MimeTypes.FormUrlEncoded;

                var body = ConvertDataToString(data, webReq.ContentType);
                using var stream = await webReq.GetRequestStreamAsync();
                await stream.WriteAsync(body);
            }

            using var webRes = await webReq.GetResponseAsync();
            {
                using var stream = webRes.GetResponseStream();
                await stream.CopyToAsync(scope.OutputStream);
            }
        }

        private static HttpWebRequest initWebRequest(string url, Dictionary<string, object> scopedParams)
        {
            var webReq = (HttpWebRequest) WebRequest.Create(url);
            var dataType = scopedParams.TryGetValue("dataType", out object value)
                ? ConvertDataTypeToContentType((string) value)
                : null;

            if (scopedParams.TryGetValue("method", out value))
                webReq.Method = (string) value;
            if (scopedParams.TryGetValue("contentType", out value) || dataType != null)
                webReq.ContentType = (string) value ?? dataType;
            if (scopedParams.TryGetValue("accept", out value) || dataType != null)
                webReq.Accept = (string) value ?? dataType;
            if (scopedParams.TryGetValue("userAgent", out value))
                PclExport.Instance.SetUserAgent(webReq, (string) value);
            return webReq;
        }

        private static HttpWebRequest postWebRequestSync(string url, Dictionary<string, object> scopedParams)
        {
            var webReq = initWebRequest(url, scopedParams);

            if (scopedParams.TryRemove("data", out object data))
            {
                if (webReq.ContentType == null)
                    webReq.ContentType = MimeTypes.FormUrlEncoded;

                var body = ConvertDataToString(data, webReq.ContentType);
                using var stream = webReq.GetRequestStream();
                var utf8 = MemoryProvider.Instance.ToUtf8(body.AsSpan()).ToArray();
                stream.Write(utf8, 0, utf8.Length);
            }

            return webReq;
        }

        public string urlTextContents(ScriptScopeContext scope, string url) =>
            urlTextContents(scope, url, new Dictionary<string, object> {
                ["method"] = HttpMethods.Get
            });

        public string urlTextContents(ScriptScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(urlTextContents), options);
            var webReq = postWebRequestSync(url, scopedParams);

            using var webRes = webReq.GetResponse();
            using var stream = webRes.GetResponseStream();
            var ret = stream.ReadToEnd();
            return ret;
        }

        public ReadOnlyMemory<byte> urlBytesContents(ScriptScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(urlTextContents), options);
            var webReq = postWebRequestSync(url, scopedParams);

            using var webRes = webReq.GetResponse();
            using var stream = webRes.GetResponseStream();
            var ret = stream.ReadFullyAsMemory();
            return ret;
        }

        private static string ConvertDataTypeToContentType(string dataType)
        {
            switch (dataType)
            {
                case "json":
                    return MimeTypes.Json;
                case "jsv":
                    return MimeTypes.Jsv;
                case "csv":
                    return MimeTypes.Csv;
                case "xml":
                    return MimeTypes.Xml;
                case "text":
                    return MimeTypes.PlainText;
                case "form":
                    return MimeTypes.FormUrlEncoded;
            }
            
            throw new NotSupportedException($"Unknown dataType '{dataType}'");
        }

        private static string ConvertDataToString(object data, string contentType)
        {
            if (data is string s)
                return s;
            switch (contentType)
            {
                case MimeTypes.PlainText:
                    return data.ToString();
                case MimeTypes.Json:
                    return data.ToJson();
                case MimeTypes.Csv:
                    return data.ToCsv();
                case MimeTypes.Jsv:
                    return data.ToJsv();
                case MimeTypes.Xml:
                    return data.ToXml();
                case MimeTypes.FormUrlEncoded:
                    WriteComplexTypeDelegate holdQsStrategy = QueryStringStrategy.FormUrlEncoded;
                    QueryStringSerializer.ComplexTypeStrategy = QueryStringStrategy.FormUrlEncoded;
                    var urlEncodedBody = QueryStringSerializer.SerializeToString(data);
                    QueryStringSerializer.ComplexTypeStrategy = holdQsStrategy;
                    return urlEncodedBody;
            }

            throw new NotSupportedException($"Can not serialize to unknown Content-Type '{contentType}'");
        }

        public static string CreateCacheKey(string url, Dictionary<string,object> options=null)
        {
            var sb = StringBuilderCache.Allocate()
                .Append(url);
            
            if (options != null)
            {
                foreach (var entry in options)
                {
                    sb.Append(entry.Key)
                      .Append('=')
                      .Append(entry.Value);
                }
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }
        
        //alias
        public Task fileContentsWithCache(ScriptScopeContext scope, string virtualPath) => includeFileWithCache(scope, virtualPath, null);
        public Task fileContentsWithCache(ScriptScopeContext scope, string virtualPath, object options) => includeFileWithCache(scope, virtualPath, options);

        public Task includeFileWithCache(ScriptScopeContext scope, string virtualPath) => includeFileWithCache(scope, virtualPath, null);
        public async Task includeFileWithCache(ScriptScopeContext scope, string virtualPath, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);
            var expireIn = scopedParams.TryGetValue("expireInSecs", out object value)
                ? TimeSpan.FromSeconds(value.ConvertTo<int>())
                : (TimeSpan)scope.Context.Args[ScriptConstants.DefaultFileCacheExpiry];
            
            var cacheKey = CreateCacheKey("file:" + scope.PageResult.VirtualPath + ">" + virtualPath, scopedParams);
            if (Context.ExpiringCache.TryGetValue(cacheKey, out Tuple<DateTime, object> cacheEntry))
            {
                if (cacheEntry.Item1 > DateTime.UtcNow && cacheEntry.Item2 is byte[] bytes)
                {
                    await scope.OutputStream.WriteAsync(bytes);
                    return;
                }
            }

            var file = ResolveFile(nameof(includeFileWithCache), scope, virtualPath);
            var ms = MemoryStreamFactory.GetStream();
            using (ms)
            {
                using (var reader = file.OpenRead())
                {
                    await reader.CopyToAsync(ms);
                }

                ms.Position = 0;
                var bytes = ms.ToArray();
                Context.ExpiringCache[cacheKey] = Tuple.Create(DateTime.UtcNow.Add(expireIn),(object)bytes);
                await scope.OutputStream.WriteAsync(bytes);
            }
        }

        //alias
        public Task urlContentsWithCache(ScriptScopeContext scope, string url) => includeUrlWithCache(scope, url, null);
        public Task urlContentsWithCache(ScriptScopeContext scope, string url, object options) => includeUrlWithCache(scope, url, options);
        
        public Task includeUrlWithCache(ScriptScopeContext scope, string url) => includeUrlWithCache(scope, url, null);
        public async Task includeUrlWithCache(ScriptScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);
            var expireIn = scopedParams.TryGetValue("expireInSecs", out object value)
                ? TimeSpan.FromSeconds(value.ConvertTo<int>())
                : (TimeSpan)scope.Context.Args[ScriptConstants.DefaultUrlCacheExpiry];

            var cacheKey = CreateCacheKey("url:" + url, scopedParams);
            if (Context.ExpiringCache.TryGetValue(cacheKey, out Tuple<DateTime, object> cacheEntry))
            {
                if (cacheEntry.Item1 > DateTime.UtcNow && cacheEntry.Item2 is byte[] bytes)
                {
                    await scope.OutputStream.WriteAsync(bytes);
                    return;
                }
            }

            var dataType = scopedParams.TryGetValue("dataType", out value)
                ? ConvertDataTypeToContentType((string)value)
                : null;

            if (scopedParams.TryGetValue("method", out value) && !((string)value).EqualsIgnoreCase("GET"))
                throw new NotSupportedException($"Only GET requests can be used in {nameof(includeUrlWithCache)} filters in page '{scope.Page.VirtualPath}'");
            if (scopedParams.TryGetValue("data", out value))
                throw new NotSupportedException($"'data' is not supported in {nameof(includeUrlWithCache)} filters in page '{scope.Page.VirtualPath}'");

            var ms = MemoryStreamFactory.GetStream();
            using (ms)
            {
                var captureScope = scope.ScopeWithStream(ms);
                await includeUrl(captureScope, url, options);

                ms.Position = 0;
                var expireAt = DateTime.UtcNow.Add(expireIn);

                var bytes = ms.ToArray();
                Context.ExpiringCache[cacheKey] = cacheEntry = Tuple.Create(expireAt,(object)bytes);
                await scope.OutputStream.WriteAsync(bytes);
            }
        }

        public List<ScriptMethodInfo> scriptMethods(ScriptScopeContext scope)
        {
            var methods = scope.Context.ScriptMethods.SelectMany(x => ScriptMethodInfo.GetScriptMethods(x.GetType()))
                .OrderBy(x => x.Name)
                .ThenBy(x => x.ParamCount)
                .ToList();
            return methods;
        }

        public List<string> scriptMethodNames(ScriptScopeContext scope) => scriptMethods(scope)
            .Map(x => x.Name);

        public List<string> scriptMethodSignatures(ScriptScopeContext scope) => scriptMethods(scope)
            .Map(x => x.Signature);

        private ScriptMethodInfo[] filterMethods(MethodInfo[] methodInfos) =>
            methodInfos.Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
                .Select(ScriptMethodInfo.Create).ToArray();
        public List<string> methods(object o)
        {
            if (o == null)
                return TypeConstants.EmptyStringList;

            var mis = methodTypes(o);
            return mis.Map(x => x.Name).OrderBy(x => x).ToList();
        }

        public ScriptMethodInfo[] methodTypes(object o)
        {
            if (o == null)
                return TypeConstants<ScriptMethodInfo>.EmptyArray;
            
            var type = o is Type t
                ? t
                : o.GetType();

            return filterMethods(type.GetInstanceMethods());
        }
        
        public List<string> staticMethods(object o)
        {
            if (o == null)
                return TypeConstants.EmptyStringList;

            var mis = staticMethodTypes(o);
            return mis.Map(x => x.Name).OrderBy(x => x).ToList();
        }

        public ScriptMethodInfo[] staticMethodTypes(object o)
        {
            if (o == null)
                return TypeConstants<ScriptMethodInfo>.EmptyArray;
            
            var type = o is Type t
                ? t
                : o.GetType();

            return filterMethods(type.GetMethods(BindingFlags.Static | BindingFlags.Public));
        }

        public ScriptMethodInfo[] allMethodTypes(object o)
        {
            if (o == null)
                return TypeConstants<ScriptMethodInfo>.EmptyArray;
            
            var type = o is Type t
                ? t
                : o.GetType();

            return type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(ScriptMethodInfo.Create).ToArray();
        }

        public MemberInfo[] allMemberInfos(object o)
        {
            if (o == null)
                return TypeConstants<MemberInfo>.EmptyArray;
            
            var type = o is Type t
                ? t
                : o.GetType();

            return type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        
        static readonly string[] AllCacheNames = {
            nameof(ScriptContext.Cache),
            nameof(ScriptContext.CacheMemory),
            nameof(ScriptContext.ExpiringCache),
            nameof(ScriptTemplateUtils.BinderCache),
            nameof(ScriptContext.JsTokenCache),
            nameof(ScriptContext.AssignExpressionCache),
            nameof(ScriptContext.PathMappings),
        };

        internal IDictionary GetCache(string cacheName)
        {
            switch (cacheName)
            {
                case nameof(ScriptContext.Cache):
                    return Context.Cache;
                case nameof(ScriptContext.CacheMemory):
                    return Context.CacheMemory;
                case nameof(ScriptContext.ExpiringCache):
                    return Context.ExpiringCache;
                case nameof(ScriptTemplateUtils.BinderCache):
                    return ScriptTemplateUtils.BinderCache;
                case nameof(ScriptContext.JsTokenCache):
                    return Context.JsTokenCache;
                case nameof(ScriptContext.AssignExpressionCache):
                    return Context.AssignExpressionCache;
                case nameof(ScriptContext.PathMappings):
                    return Context.PathMappings;
            }
            return null;
        }

        public object cacheClear(ScriptScopeContext scope, object cacheNames)
        {
            IEnumerable<string> caches;
            if (cacheNames is string strName)
            {
                caches = strName.EqualsIgnoreCase("all")
                    ? AllCacheNames
                    : new[]{ strName };
            }
            else if (cacheNames is IEnumerable<string> nameList)
            {
                caches = nameList;
            }
            else throw new NotSupportedException(nameof(cacheClear) + 
                 " expects a cache name or list of cache names but received: " + (cacheNames.GetType()?.Name ?? "null"));

            int entriesRemoved = 0;
            foreach (var cacheName in caches)
            {
                var cache = GetCache(cacheName);
                if (cache == null)
                    throw new NotSupportedException(nameof(cacheClear) + $": Unknown cache '{cacheName}'");

                entriesRemoved += cache.Count;
                cache.Clear();
            }

            return entriesRemoved;
        }

        public object invalidateAllCaches(ScriptScopeContext scope)
        {
            cacheClear(scope, "all");
            return scope.Context.InvalidateCachesBefore = DateTime.UtcNow;
        }

        public string sh(ScriptScopeContext scope, string arguments) => sh(scope, arguments, null);
        public string sh(ScriptScopeContext scope, string arguments, Dictionary<string, object> options)
        {
            if (string.IsNullOrEmpty(arguments))
                return null;
            
            if (options == null)
                options = new Dictionary<string, object>();

            if (Env.IsWindows)
            {
                options["arguments"] = "/C " + arguments;
                return proc(scope, "cmd.exe", options);
            }
            else
            {
                var escapedArgs = arguments.Replace("\"", "\\\"");
                options["arguments"] = $"-c \"{escapedArgs}\"";
                return proc(scope, "/bin/bash", options);
            }
        }
        
        public string proc(ScriptScopeContext scope, string fileName) => proc(scope, fileName, null);
        public string proc(ScriptScopeContext scope, string fileName, Dictionary<string, object> options)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };

            if (options.TryGetValue("arguments", out var oArguments))
                process.StartInfo.Arguments = oArguments.AsString();
            
            if (options.TryGetValue("dir", out var oWorkDir))
                process.StartInfo.WorkingDirectory = oWorkDir.AsString();

            try 
            { 
                using (process)
                {
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();
                    process.Close();

                    if (!string.IsNullOrEmpty(error))
                        throw new Exception($"`{fileName} {process.StartInfo.Arguments}` command failed: " + error);

                    return output;
                }            
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public string exePath(string exeName)
        {
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        FileName = Env.IsWindows 
                            ? "where"  //Win 7/Server 2003+
                            : "which", //macOS / Linux
                        Arguments = exeName,
                        RedirectStandardOutput = true
                    }
                };
                p.Start();
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    // just return first match
                    var fullPath = output.Substring(0, output.IndexOf(Environment.NewLine, StringComparison.Ordinal));
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch {}               
            return null;
        }

        public StopExecution exit(int exitCode)
        {
            Environment.Exit(exitCode);
            return StopExecution.Value;
        }

        public IgnoreResult inspectVars(object vars)
        {
            Inspect.vars(vars);
            return IgnoreResult.Value;
        }

        private static string check(string target) => 
            string.IsNullOrWhiteSpace(target?.Replace("\"","")) ? null : target;
        private static string winpath(string path) => path?.Replace('/', '\\');
        private static string unixpath(string path) => path?.Replace('\\', '/');

        public string mv(ScriptScopeContext scope, string from, string to)
        {
            _ = check(from) ?? throw new ArgumentNullException(nameof(from));
            _ = check(to) ?? throw new ArgumentNullException(nameof(to));
            return Env.IsWindows
                ? sh(scope, $"MOVE /Y {winpath(from)} {winpath(to)}")
                : sh(scope, $"mv -f {unixpath(from)} {unixpath(to)}");
        }
        public string cp(ScriptScopeContext scope, string from, string to)
        {
            _ = check(from) ?? throw new ArgumentNullException(nameof(from));
            _ = check(to) ?? throw new ArgumentNullException(nameof(to));
            return Env.IsWindows
                ? sh(scope, $"COPY /Y {winpath(from)} {winpath(to)}")
                : sh(scope, $"cp -f {unixpath(from)} {unixpath(to)}");
        }
        public string xcopy(ScriptScopeContext scope, string from, string to)
        {
            _ = check(from) ?? throw new ArgumentNullException(nameof(from));
            _ = check(to) ?? throw new ArgumentNullException(nameof(to));
            return Env.IsWindows
                ? sh(scope, $"XCOPY /E /H {winpath(from)} {winpath(to)}")
                : sh(scope, $"cp -R {unixpath(from)} {unixpath(to)}");
        }
        public string rm(ScriptScopeContext scope, string from, string to)
        {
            _ = check(from) ?? throw new ArgumentNullException(nameof(from));
            _ = check(to) ?? throw new ArgumentNullException(nameof(to));
            return Env.IsWindows
                ? sh(scope, $"DEL /y {winpath(from)} {winpath(to)}")
                : sh(scope, $"rm -f {unixpath(from)} {unixpath(to)}");
        }
        public string rmdir(ScriptScopeContext scope, string target)
        {
            _ = check(target) ?? throw new ArgumentNullException(nameof(target));
            return Env.IsWindows
                ? sh(scope, $"RMDIR /Q /S {winpath(target)}")
                : sh(scope, $"rm -rf {unixpath(target)}");
        }
        public string mkdir(ScriptScopeContext scope, string target)
        {
            _ = check(target) ?? throw new ArgumentNullException(nameof(target));
            return Env.IsWindows
                ? sh(scope, $"MKDIR {winpath(target)}")
                : sh(scope, $"mkdir -p {unixpath(target)}");
        }
        public string cat(ScriptScopeContext scope, string target)
        {
            _ = check(target) ?? throw new ArgumentNullException(nameof(target));
            return Env.IsWindows
                ? sh(scope, $"type {winpath(target)}")
                : sh(scope, $"cat {unixpath(target)}");
        }
        public string touch(ScriptScopeContext scope, string target)
        {
            _ = check(target) ?? throw new ArgumentNullException(nameof(target));
            return Env.IsWindows
                ? sh(scope, $"CALL >> {winpath(target)}")
                : sh(scope, $"touch {unixpath(target)}");
        }
        
        public FileScripts File() => new();
        public DirectoryScripts Directory() => new();

        static string HexHash(HashAlgorithm hash, string s) => HexHash(hash, s.ToUtf8Bytes());
        static string HexHash(HashAlgorithm hash, byte[] bytes)
        {
            using var _ = hash;
            return bytes == null || bytes.Length == 0 ? null : _.ComputeHash(bytes).ToHex();
        }
        public string sha1(object target) => target is string s
            ? HexHash(SHA1.Create(), s)
            : target is byte[] b
                ? HexHash(SHA1.Create(), b)
                : throw new NotSupportedException(target?.GetType().Name);
        public string sha256(object target) => target is string s
            ? HexHash(SHA256.Create(), s)
            : target is byte[] b
                ? HexHash(SHA256.Create(), b)
                : throw new NotSupportedException(target?.GetType().Name);
        public string sha512(object target) => target is string s
            ? HexHash(SHA512.Create(), s)
            : target is byte[] b
                ? HexHash(SHA512.Create(), b)
                : throw new NotSupportedException(target?.GetType().Name);
        
        public IgnoreResult Delete(string path) => System.IO.File.Exists(path)
            ? File().Delete(path)
            : System.IO.Directory.Exists(path)
                ? Directory().Delete(path)
                : IgnoreResult.Value;
        public IgnoreResult Delete(IOScript os, string path) => os.Delete(path);
        
        public bool Exists(string path) => System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        public bool Exists(IOScript os, string path) => os.Exists(path);

        public IgnoreResult Move(string from, string to) => System.IO.File.Exists(from)
            ? File().Move( from, to)
            : System.IO.Directory.Exists(from)
                ? Directory().Move( from, to)
                : IgnoreResult.Value;
        public IgnoreResult Move(IOScript os, string from, string to) => os.Move( from, to);

        public IgnoreResult Copy(string from, string to) => System.IO.File.Exists(from)
            ? File().Copy( from, to)
            : System.IO.Directory.Exists(from)
                ? Directory().Copy(from, to)
                : IgnoreResult.Value;
        public IgnoreResult Copy(IOScript os, string from, string to) => os.Copy( from, to);
        
        public IgnoreResult Create(string from, string to) => File().Copy( from, to);
        public IgnoreResult Create(FileScripts fs, string from, string to) => fs.Copy( from, to);
        
        public IgnoreResult Decrypt(string path) => File().Decrypt( path);
        public IgnoreResult Decrypt(FileScripts fs, string path) => fs.Decrypt( path);
        public IgnoreResult Encrypt(string path) => File().Encrypt( path);
        public IgnoreResult Encrypt(FileScripts fs, string path) => fs.Encrypt( path);
        
        public IgnoreResult Replace(string from, string to, string backup) => File().Replace( from, to, backup);
        public IgnoreResult Replace(FileScripts fs, string from, string to, string backup) => fs.Replace( from, to, backup);

        public byte[] ReadAllBytes(string path) => File().ReadAllBytes(path);
        public byte[] ReadAllBytes(FileScripts fs, string path) => fs.ReadAllBytes(path);
        public string[] ReadAllLines(string path) => File().ReadAllLines(path);
        public string[] ReadAllLines(FileScripts fs, string path) => fs.ReadAllLines(path);
        public string ReadAllText(string path) => File().ReadAllText(path);
        public string ReadAllText(FileScripts fs, string path) => fs.ReadAllText(path);

        public IgnoreResult WriteAllBytes(string path, byte[] bytes) => File().WriteAllBytes(path, bytes);
        public IgnoreResult WriteAllBytes(FileScripts fs, string path, byte[] bytes) => fs.WriteAllBytes(path, bytes);
        public IgnoreResult WriteAllLines(string path, string[] lines) => File().WriteAllLines(path, lines);
        public IgnoreResult WriteAllLines(FileScripts fs, string path, string[] lines) => fs.WriteAllLines(path, lines);
        public IgnoreResult WriteAllText(string path, string text) => File().WriteAllText(path, text);
        public IgnoreResult WriteAllText(FileScripts fs, string path, string text) => fs.WriteAllText(path, text);
        
        public IgnoreResult AppendAllLines(string path, string[] lines) => File().AppendAllLines(path, lines);
        public IgnoreResult AppendAllLines(FileScripts fs, string path, string[] lines) => fs.AppendAllLines(path, lines);
        public IgnoreResult AppendAllText(string path, string text) => File().AppendAllText(path, text);
        public IgnoreResult AppendAllText(FileScripts fs, string path, string text) => fs.AppendAllText(path, text);
        
        public IgnoreResult CreateDirectory(string path) => Directory().CreateDirectory(path);
        public IgnoreResult CreateDirectory(DirectoryScripts ds, string path) => ds.CreateDirectory(path);

        public string[] GetDirectories(string path) => Directory().GetDirectories(path);
        public string[] GetDirectories(DirectoryScripts ds, string path) => ds.GetDirectories(path);
        public string[] GetFiles(string path) => Directory().GetFiles(path);
        public string[] GetFiles(DirectoryScripts ds, string path) => ds.GetFiles(path);
        public string[] GetLogicalDrives() => Directory().GetLogicalDrives();
        public string[] GetLogicalDrives(DirectoryScripts ds) => ds.GetLogicalDrives();
        public string GetCurrentDirectory() => Directory().GetCurrentDirectory();
        public string GetCurrentDirectory(DirectoryScripts ds) => ds.GetCurrentDirectory();
        public string GetDirectoryRoot(string path) => Directory().GetDirectoryRoot(path);
        public string GetDirectoryRoot(DirectoryScripts ds, string path) => ds.GetDirectoryRoot(path);
    }

    public interface IOScript
    {
        IgnoreResult Delete(string path);
        bool Exists(string target);
        IgnoreResult Move(string from, string to);
        IgnoreResult Copy(string from, string to);
    }
    public class FileScripts : IOScript
    {
        public IgnoreResult Copy(string from, string to)
        {
            File.Copy(from, to);
            return IgnoreResult.Value;
        }
        public IgnoreResult Create(string path)
        {
            using var _ = File.Create(path);
            return IgnoreResult.Value;
        }
        public IgnoreResult Decrypt(string path)
        {
            File.Decrypt(path);
            return IgnoreResult.Value;
        }
        public IgnoreResult Delete(string path)
        {
            File.Delete(path);
            return IgnoreResult.Value;
        }
        public IgnoreResult Encrypt(string path)
        {
            File.Encrypt(path);
            return IgnoreResult.Value;
        }
        public bool Exists(string path) => File.Exists(path);
        public IgnoreResult Move(string from, string to)
        {
            File.Move(from, to);
            return IgnoreResult.Value;
        }
        public IgnoreResult Replace(string from, string to, string backup)
        {
            File.Replace(from, to, backup);
            return IgnoreResult.Value;
        }
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public string[] ReadAllLines(string path) => File.ReadAllLines(path);
        public string ReadAllText(string path) => File.ReadAllText(path);
        public IgnoreResult WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            return IgnoreResult.Value;
        }
        public IgnoreResult WriteAllLines(string path, string[] lines)
        {
            File.WriteAllLines(path, lines);
            return IgnoreResult.Value;
        }
        public IgnoreResult WriteAllText(string path, string text)
        {
            File.WriteAllText(path, text);
            return IgnoreResult.Value;
        }
        public IgnoreResult AppendAllLines(string path, string[] lines)
        {
            File.AppendAllLines(path, lines);
            return IgnoreResult.Value;
        }
        public IgnoreResult AppendAllText(string path, string text)
        {
            File.AppendAllText(path, text);
            return IgnoreResult.Value;
        }
    }
    public class DirectoryScripts : IOScript
    {
        public IgnoreResult CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return IgnoreResult.Value;
        }
        public IgnoreResult Delete(string path)
        {
            FileSystemVirtualFiles.DeleteDirectoryRecursive(path);
            return IgnoreResult.Value;
        }
        public bool Exists(string path) => Directory.Exists(path);
        public string[] GetDirectories(string path) => Directory.GetDirectories(path);
        public string[] GetFiles(string path) => Directory.GetFiles(path);
        public string[] GetLogicalDrives() => Directory.GetLogicalDrives();
        public string[] GetFileSystemEntries(string path) => Directory.GetFileSystemEntries(path);
        public DirectoryInfo GetParent(string path) => Directory.GetParent(path);
        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
        public string GetDirectoryRoot(string path) => Directory.GetDirectoryRoot(path);
        public IgnoreResult Move(string from, string to)
        {
            Directory.Move(from, to);
            return IgnoreResult.Value;
        }
        public IgnoreResult Copy(string from, string to)
        {
            CopyAllTo(from, to);
            return IgnoreResult.Value;
        }
        
        public static void CopyAllTo(string src, string dst, string[] excludePaths=null)
        {
            var d = Path.DirectorySeparatorChar;

            foreach (string dirPath in Directory.GetDirectories(src, "*.*", SearchOption.AllDirectories))
            {
                if (!excludePaths.IsEmpty() && excludePaths?.Any(x => dirPath.StartsWith(x)) == true)
                    continue;

                try { Directory.CreateDirectory(dirPath.Replace(src, dst)); } catch { }
            }

            foreach (string newPath in Directory.GetFiles(src, "*.*", SearchOption.AllDirectories))
            {
                if (!excludePaths.IsEmpty() && excludePaths?.Any(x => newPath.StartsWith(x)) == true)
                    continue;
                try
                {
                    File.Copy(newPath, newPath.Replace(src, dst), overwrite: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }        
    }
}