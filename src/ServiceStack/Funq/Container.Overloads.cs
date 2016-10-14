using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Funq
{
	public partial class Container
	{
		/* The following regions contain just the typed overloads
		 * that are just pass-through to the real implementations.
		 * They all have DebuggerStepThrough to ease debugging. */

		#region LazyResolve

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService}"]/*'/>
		[DebuggerStepThrough]
		public Func<TService> LazyResolve<TService>()
		{
			return LazyResolve<TService>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg, TService> LazyResolve<TService, TArg>()
		{
			return LazyResolve<TService, TArg>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TService> LazyResolve<TService, TArg1, TArg2>()
		{
			return LazyResolve<TService, TArg1, TArg2>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TService> LazyResolve<TService, TArg1, TArg2, TArg3>()
		{
			return LazyResolve<TService, TArg1, TArg2, TArg3>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>()
		{
			return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>()
		{
			return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>()
		{
			return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TService> LazyResolve<TService>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TService>>(name);
			return () => ResolveNamed<TService>(name);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg, TService> LazyResolve<TService, TArg>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg, TService>>(name);
			return arg => ResolveNamed<TService, TArg>(name, arg);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TService> LazyResolve<TService, TArg1, TArg2>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TService>>(name);
			return (arg1, arg2) => ResolveNamed<TService, TArg1, TArg2>(name, arg1, arg2);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TService> LazyResolve<TService, TArg1, TArg2, TArg3>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TService>>(name);
			return (arg1, arg2, arg3) => ResolveNamed<TService, TArg1, TArg2, TArg3>(name, arg1, arg2, arg3);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TService>>(name);
			return (arg1, arg2, arg3, arg4) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(name, arg1, arg2, arg3, arg4);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService>>(name);
			return (arg1, arg2, arg3, arg4, arg5) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, arg1, arg2, arg3, arg4, arg5);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.LazyResolve{TService,TArgs,name}"]/*'/>
		[DebuggerStepThrough]
		public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name)
		{
			ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService>>(name);
			return (arg1, arg2, arg3, arg4, arg5, arg6) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		#endregion

		#region Register

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService>(Func<Container, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg>(Func<Container, TArg, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2>(Func<Container, TArg1, TArg2, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3>(Func<Container, TArg1, TArg2, TArg3, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4>(Func<Container, TArg1, TArg2, TArg3, TArg4, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4,TArg5}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}(factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> factory)
		{
			return Register(null, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService>(string name, Func<Container, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg>(string name, Func<Container, TArg, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2>(string name, Func<Container, TArg1, TArg2, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3>(string name, Func<Container, TArg1, TArg2, TArg3, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4,TArg5}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService>>(name, factory);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Register{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}(name,factory)"]/*'/>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService>>(name, factory);
		}

		#endregion

		#region Resolve

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService>()
		{
			return ResolveNamed<TService>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg>(TArg arg)
		{
			return ResolveNamed<TService, TArg>(null, arg);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg1,TArg2}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg1, TArg2>(TArg1 arg1, TArg2 arg2)
		{
			return ResolveNamed<TService, TArg1, TArg2>(null, arg1, arg2);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg1,TArg2,TArg3}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3>(null, arg1, arg2, arg3);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg1,TArg2,TArg3,TArg4}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(null, arg1, arg2, arg3, arg4);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg1,TArg2,TArg3,TArg4,TArg5}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null, arg1, arg2, arg3, arg4, arg5);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.Resolve{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}"]/*'/>
		[DebuggerStepThrough]
		public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		#endregion

		#region ResolveNamed

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService>(string name)
		{
			return ResolveImpl<TService>(name, true);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg>(string name, TArg arg)
		{
			return ResolveImpl<TService, TArg>(name, true, arg);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg1,TArg2}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2)
		{
			return ResolveImpl<TService, TArg1, TArg2>(name, true, arg1, arg2);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg1,TArg2,TArg3}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3>(name, true, arg1, arg2, arg3);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg1,TArg2,TArg3,TArg4}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4>(name, true, arg1, arg2, arg3, arg4);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg1,TArg2,TArg3,TArg4,TArg5}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, true, arg1, arg2, arg3, arg4, arg5);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.ResolveNamed{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}"]/*'/>
		[DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, true, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		#endregion

		#region TryResolve

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService>()
		{
			return TryResolveNamed<TService>(null);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg>(TArg arg)
		{
			return TryResolveNamed<TService, TArg>(null, arg);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg1,TArg2}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2>(TArg1 arg1, TArg2 arg2)
		{
			return TryResolveNamed<TService, TArg1, TArg2>(null, arg1, arg2);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg1,TArg2,TArg3}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3>(null, arg1, arg2, arg3);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg1,TArg2,TArg3,TArg4}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(null, arg1, arg2, arg3, arg4);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg1,TArg2,TArg3,TArg4,TArg5}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null, arg1, arg2, arg3, arg4, arg5);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolve{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		#endregion

		#region TryResolveNamed

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService>(string name)
		{
			return ResolveImpl<TService>(name, false);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg>(string name, TArg arg)
		{
			return ResolveImpl<TService, TArg>(name, false, arg);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg1,TArg2}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2)
		{
			return ResolveImpl<TService, TArg1, TArg2>(name, false, arg1, arg2);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg1,TArg2,TArg3}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3>(name, false, arg1, arg2, arg3);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg1,TArg2,TArg3,TArg4}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4>(name, false, arg1, arg2, arg3, arg4);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg1,TArg2,TArg3,TArg4,TArg5}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, false, arg1, arg2, arg3, arg4, arg5);
		}

		/// <include file='Container.xdoc' path='docs/doc[@for="Container.TryResolveNamed{TService,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6}"]/*'/>
		[DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, false, arg1, arg2, arg3, arg4, arg5, arg6);
		}

		#endregion
	}
}
