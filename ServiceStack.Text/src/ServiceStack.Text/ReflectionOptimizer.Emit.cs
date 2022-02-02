#if NETFX || (NETCORE && !NETSTANDARD2_0)

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ServiceStack.Text
{
    public sealed class EmitReflectionOptimizer : ReflectionOptimizer
    {
        private static EmitReflectionOptimizer provider;
        public static EmitReflectionOptimizer Provider => provider ??= new EmitReflectionOptimizer();
        private EmitReflectionOptimizer() { }

        public override Type UseType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return DynamicProxy.GetInstanceFor(type).GetType();
            }

            return type;
        }

        internal static DynamicMethod CreateDynamicGetMethod<T>(MemberInfo memberInfo)
        {
            var memberType = memberInfo is FieldInfo ? "Field" : "Property";
            var name = $"_Get{memberType}[T]_{memberInfo.Name}_";
            var returnType = typeof(object);

            return !memberInfo.DeclaringType.IsInterface
                ? new DynamicMethod(name, returnType, new[] {typeof(T)}, memberInfo.DeclaringType, true)
                : new DynamicMethod(name, returnType, new[] {typeof(T)}, memberInfo.Module, true);
        }

        public override GetMemberDelegate CreateGetter(PropertyInfo propertyInfo)
        {
            var getter = CreateDynamicGetMethod(propertyInfo);

            var gen = getter.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);

            if (propertyInfo.DeclaringType.IsValueType)
            {
                gen.Emit(OpCodes.Unbox, propertyInfo.DeclaringType);
            }
            else
            {
                gen.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            }

            var mi = propertyInfo.GetGetMethod(true);
            if (mi == null)
                return null;
            gen.Emit(mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi);

            if (propertyInfo.PropertyType.IsValueType)
            {
                gen.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            gen.Emit(OpCodes.Ret);

            return (GetMemberDelegate) getter.CreateDelegate(typeof(GetMemberDelegate));
        }

        public override GetMemberDelegate<T> CreateGetter<T>(PropertyInfo propertyInfo)
        {
            var getter = CreateDynamicGetMethod<T>(propertyInfo);

            var gen = getter.GetILGenerator();
            var mi = propertyInfo.GetGetMethod(true);
            if (mi == null)
                return null;

            if (typeof(T).IsValueType)
            {
                gen.Emit(OpCodes.Ldarga_S, 0);

                if (typeof(T) != propertyInfo.DeclaringType)
                {
                    gen.Emit(OpCodes.Unbox, propertyInfo.DeclaringType);
                }
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);

                if (typeof(T) != propertyInfo.DeclaringType)
                {
                    gen.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                }
            }

            gen.Emit(mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi);

            if (propertyInfo.PropertyType.IsValueType)
            {
                gen.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            gen.Emit(OpCodes.Isinst, typeof(object));

            gen.Emit(OpCodes.Ret);

            return (GetMemberDelegate<T>) getter.CreateDelegate(typeof(GetMemberDelegate<T>));
        }

        public override SetMemberDelegate CreateSetter(PropertyInfo propertyInfo)
        {
            var mi = propertyInfo.GetSetMethod(true);
            if (mi == null)
                return null;

            var setter = CreateDynamicSetMethod(propertyInfo);

            var gen = setter.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);

            if (propertyInfo.DeclaringType.IsValueType)
            {
                gen.Emit(OpCodes.Unbox, propertyInfo.DeclaringType);
            }
            else
            {
                gen.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            }

            gen.Emit(OpCodes.Ldarg_1);

            if (propertyInfo.PropertyType.IsValueType)
            {
                gen.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
            }
            else
            {
                gen.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
            }

            gen.EmitCall(mi.IsFinal ? OpCodes.Call : OpCodes.Callvirt, mi, (Type[]) null);

            gen.Emit(OpCodes.Ret);

            return (SetMemberDelegate) setter.CreateDelegate(typeof(SetMemberDelegate));
        }

        public override SetMemberDelegate<T> CreateSetter<T>(PropertyInfo propertyInfo) =>
            ExpressionReflectionOptimizer.Provider.CreateSetter<T>(propertyInfo);


        public override GetMemberDelegate CreateGetter(FieldInfo fieldInfo)
        {
            var getter = CreateDynamicGetMethod(fieldInfo);

            var gen = getter.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);

            if (fieldInfo.DeclaringType.IsValueType)
            {
                gen.Emit(OpCodes.Unbox, fieldInfo.DeclaringType);
            }
            else
            {
                gen.Emit(OpCodes.Castclass, fieldInfo.DeclaringType);
            }

            gen.Emit(OpCodes.Ldfld, fieldInfo);

            if (fieldInfo.FieldType.IsValueType)
            {
                gen.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            gen.Emit(OpCodes.Ret);

            return (GetMemberDelegate) getter.CreateDelegate(typeof(GetMemberDelegate));
        }

        public override GetMemberDelegate<T> CreateGetter<T>(FieldInfo fieldInfo)
        {
            var getter = CreateDynamicGetMethod<T>(fieldInfo);

            var gen = getter.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);

            gen.Emit(OpCodes.Ldfld, fieldInfo);

            if (fieldInfo.FieldType.IsValueType)
            {
                gen.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            gen.Emit(OpCodes.Ret);

            return (GetMemberDelegate<T>) getter.CreateDelegate(typeof(GetMemberDelegate<T>));
        }

        public override SetMemberDelegate CreateSetter(FieldInfo fieldInfo)
        {
            var setter = CreateDynamicSetMethod(fieldInfo);

            var gen = setter.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);

            if (fieldInfo.DeclaringType.IsValueType)
            {
                gen.Emit(OpCodes.Unbox, fieldInfo.DeclaringType);
            }
            else
            {
                gen.Emit(OpCodes.Castclass, fieldInfo.DeclaringType);
            }

            gen.Emit(OpCodes.Ldarg_1);

            gen.Emit(fieldInfo.FieldType.IsClass
                    ? OpCodes.Castclass
                    : OpCodes.Unbox_Any,
                fieldInfo.FieldType);

            gen.Emit(OpCodes.Stfld, fieldInfo);
            gen.Emit(OpCodes.Ret);

            return (SetMemberDelegate) setter.CreateDelegate(typeof(SetMemberDelegate));
        }

        static readonly Type[] DynamicGetMethodArgs = {typeof(object)};

        internal static DynamicMethod CreateDynamicGetMethod(MemberInfo memberInfo)
        {
            var memberType = memberInfo is FieldInfo ? "Field" : "Property";
            var name = $"_Get{memberType}_{memberInfo.Name}_";
            var returnType = typeof(object);

            return !memberInfo.DeclaringType.IsInterface
                ? new DynamicMethod(name, returnType, DynamicGetMethodArgs, memberInfo.DeclaringType, true)
                : new DynamicMethod(name, returnType, DynamicGetMethodArgs, memberInfo.Module, true);
        }

        public override SetMemberDelegate<T> CreateSetter<T>(FieldInfo fieldInfo) =>
            ExpressionReflectionOptimizer.Provider.CreateSetter<T>(fieldInfo);

        public override SetMemberRefDelegate<T> CreateSetterRef<T>(FieldInfo fieldInfo) =>
            ExpressionReflectionOptimizer.Provider.CreateSetterRef<T>(fieldInfo);

        public override bool IsDynamic(Assembly assembly)
        {
            try
            {
                var isDynamic = assembly is AssemblyBuilder
                    || string.IsNullOrEmpty(assembly.Location);
                return isDynamic;
            }
            catch (NotSupportedException)
            {
                //Ignore assembly.Location not supported in a dynamic assembly.
                return true;
            }
        }

        public override EmptyCtorDelegate CreateConstructor(Type type)
        {
            var emptyCtor = type.GetConstructor(Type.EmptyTypes);
            if (emptyCtor != null)
            {
                var dm = new DynamicMethod("MyCtor", type, Type.EmptyTypes, typeof(ReflectionExtensions).Module, true);
                var ilgen = dm.GetILGenerator();
                ilgen.Emit(OpCodes.Nop);
                ilgen.Emit(OpCodes.Newobj, emptyCtor);
                ilgen.Emit(OpCodes.Ret);

                return (EmptyCtorDelegate) dm.CreateDelegate(typeof(EmptyCtorDelegate));
            }

            //Anonymous types don't have empty constructors
            return () => FormatterServices.GetUninitializedObject(type);
        }

        static readonly Type[] DynamicSetMethodArgs = {typeof(object), typeof(object)};

        internal static DynamicMethod CreateDynamicSetMethod(MemberInfo memberInfo)
        {
            var memberType = memberInfo is FieldInfo ? "Field" : "Property";
            var name = $"_Set{memberType}_{memberInfo.Name}_";
            var returnType = typeof(void);

            return !memberInfo.DeclaringType.IsInterface
                ? new DynamicMethod(name, returnType, DynamicSetMethodArgs, memberInfo.DeclaringType, true)
                : new DynamicMethod(name, returnType, DynamicSetMethodArgs, memberInfo.Module, true);
        }
    }
    

    public static class DynamicProxy
    {
        public static T GetInstanceFor<T>()
        {
            return (T)GetInstanceFor(typeof(T));
        }

        static readonly ModuleBuilder ModuleBuilder;
        static readonly AssemblyBuilder DynamicAssembly;
        static readonly Type[] EmptyTypes = new Type[0];

        public static object GetInstanceFor(Type targetType)
        {
            lock (DynamicAssembly)
            {
                var constructedType = DynamicAssembly.GetType(ProxyName(targetType)) ?? GetConstructedType(targetType);
                var instance = Activator.CreateInstance(constructedType);
                return instance;
            }
        }

        static string ProxyName(Type targetType)
        {
            return targetType.Name + "Proxy";
        }

        static DynamicProxy()
        {
            var assemblyName = new AssemblyName("DynImpl");
#if NETCORE
            DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
            DynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#endif
            ModuleBuilder = DynamicAssembly.DefineDynamicModule("DynImplModule");
        }

        static Type GetConstructedType(Type targetType)
        {
            var typeBuilder = ModuleBuilder.DefineType(targetType.Name + "Proxy", TypeAttributes.Public);

            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { });
            var ilGenerator = ctorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            IncludeType(targetType, typeBuilder);

            foreach (var face in targetType.GetInterfaces())
                IncludeType(face, typeBuilder);

#if NETCORE
            return typeBuilder.CreateTypeInfo().AsType();
#else
            return typeBuilder.CreateType();
#endif
        }

        static void IncludeType(Type typeOfT, TypeBuilder typeBuilder)
        {
            var methodInfos = typeOfT.GetMethods();
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.Name.StartsWith("set_", StringComparison.Ordinal)) continue; // we always add a set for a get.

                if (methodInfo.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    BindProperty(typeBuilder, methodInfo);
                }
                else
                {
                    BindMethod(typeBuilder, methodInfo);
                }
            }

            typeBuilder.AddInterfaceImplementation(typeOfT);
        }

        static void BindMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.GetType()).ToArray()
                );
            var methodILGen = methodBuilder.GetILGenerator();
            if (methodInfo.ReturnType == typeof(void))
            {
                methodILGen.Emit(OpCodes.Ret);
            }
            else
            {
                if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum)
                {
                    MethodInfo getMethod = typeof(Activator).GetMethod("CreateInstance", new[] { typeof(Type) });
                    LocalBuilder lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
                    methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
                    methodILGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                    methodILGen.Emit(OpCodes.Callvirt, getMethod);
                    methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
                }
                else
                {
                    methodILGen.Emit(OpCodes.Ldnull);
                }
                methodILGen.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        public static void BindProperty(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            // Backing Field
            string propertyName = methodInfo.Name.Replace("get_", "");
            Type propertyType = methodInfo.ReturnType;
            FieldBuilder backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            //Getter
            MethodBuilder backingGet = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, propertyType, EmptyTypes);
            ILGenerator getIl = backingGet.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, backingField);
            getIl.Emit(OpCodes.Ret);


            //Setter
            MethodBuilder backingSet = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, null, new[] { propertyType });

            ILGenerator setIl = backingSet.GetILGenerator();

            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, backingField);
            setIl.Emit(OpCodes.Ret);

            // Property
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
            propertyBuilder.SetGetMethod(backingGet);
            propertyBuilder.SetSetMethod(backingSet);
        }
    }
    
}

#endif