/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace ServiceStack.Html
{
	internal static class DynamicTypeGenerator
	{
		private static readonly ModuleBuilder _dynamicModule = CreateDynamicModule();

		private static ModuleBuilder CreateDynamicModule()
		{
			// DDB 226615 - since MVC is [SecurityTransparent], the dynamic assembly must declare itself likewise
			var builder = new CustomAttributeBuilder(
				typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			var assemblyAttributes = new[] { builder };
			var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName("System.Web.Mvc.{Dynamic}"), AssemblyBuilderAccess.Run, assemblyAttributes);
			var dynamicModule = dynamicAssembly.DefineDynamicModule("System.Web.Mvc.{Dynamic}.dll");
			return dynamicModule;
		}

		// Creates a new dynamic type that is a subclassed type of baseType and also implements methods of the specified
		// interfaces. The base type must already have method signatures that implicitly implement the given
		// interfaces. The signatures of all public (e.g. not private / internal) constructors from the baseType
		// will be duplicated for the subclassed type and the new constructors made public.
		public static Type GenerateType(string dynamicTypeName, Type baseType, IEnumerable<Type> interfaceTypes)
		{
			var newType = _dynamicModule.DefineType(
				"System.Web.Mvc.{Dynamic}." + dynamicTypeName,
				TypeAttributes.AutoLayout | TypeAttributes.Public | TypeAttributes.Class,
				baseType);

			foreach (Type interfaceType in interfaceTypes)
			{
				newType.AddInterfaceImplementation(interfaceType);
				foreach (MethodInfo interfaceMethod in interfaceType.GetMethods())
				{
					ImplementInterfaceMethod(newType, interfaceMethod);
				}
			}

			// generate new constructors for each accessible base constructor
			foreach (ConstructorInfo ctor in baseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				switch (ctor.Attributes & MethodAttributes.MemberAccessMask)
				{
					case MethodAttributes.Family:
					case MethodAttributes.Public:
					case MethodAttributes.FamORAssem:
						ImplementConstructor(newType, ctor);
						break;
				}
			}

			Type bakedType = newType.CreateType();
			return bakedType;
		}

		// generates this constructor:
		// public NewType(param0, param1, ...) : base(param0, param1, ...) { }
		private static void ImplementConstructor(TypeBuilder newType, ConstructorInfo baseCtor)
		{
			var parameters = baseCtor.GetParameters();
			var parameterTypes = (from p in parameters select p.ParameterType).ToArray();

			var newCtor = newType.DefineConstructor(
				(baseCtor.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public /* force public constructor */,
				baseCtor.CallingConvention, parameterTypes);

			// parameter 0 is 'this', so we start at index 1
			for (int i = 0; i < parameters.Length; i++)
			{
				newCtor.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
			}

			// load all arguments (including 'this') in proper order, then call and return
			ILGenerator ilGen = newCtor.GetILGenerator();
			for (int i = 0; i <= parameterTypes.Length; i++)
			{
				ilGen.Emit(OpCodes.Ldarg_S, (byte)i);
			}
			ilGen.Emit(OpCodes.Call, baseCtor);
			ilGen.Emit(OpCodes.Ret);
		}

		// generates this explicit interface method:
		// public new Interface.Method(param0, param1, ...) {
		//   return base.Method(param0, param1, ...);
		// }
		private static void ImplementInterfaceMethod(TypeBuilder newType, MethodInfo interfaceMethod)
		{
			var parameters = interfaceMethod.GetParameters();
			var parameterTypes = (from p in parameters select p.ParameterType).ToArray();

			// based on http://msdn.microsoft.com/en-us/library/system.reflection.emit.typebuilder.definemethodoverride.aspx
			var newMethod = newType.DefineMethod(interfaceMethod.DeclaringType.Name + "." + interfaceMethod.Name,
				MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				interfaceMethod.ReturnType, parameterTypes);

			var baseMethod = newType.BaseType.GetMethod(interfaceMethod.Name, parameterTypes);

			// parameter 0 is 'this', so we start at index 1
			for (int i = 0; i < parameters.Length; i++)
			{
				newMethod.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
			}

			// load all arguments (including 'this') in proper order, then call and return
			var ilGen = newMethod.GetILGenerator();
			for (int i = 0; i <= parameterTypes.Length; i++)
			{
				ilGen.Emit(OpCodes.Ldarg_S, (byte)i);
			}
			ilGen.Emit(OpCodes.Call, baseMethod);
			ilGen.Emit(OpCodes.Ret);

			// finally, hook the new method up to the interface mapping
			newType.DefineMethodOverride(newMethod, interfaceMethod);
		}
	}
}