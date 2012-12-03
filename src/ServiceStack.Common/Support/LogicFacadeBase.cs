using System;
using System.Collections.Generic;
using ServiceStack.DesignPatterns.Command;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;

namespace ServiceStack.Common.Support
{
    public abstract class LogicFacadeBase : ILogicFacade
    {
        private readonly ILog log = LogManager.GetLogger(typeof(LogicFacadeBase));

        internal class InitialisationContext : IInitContext
        {
            private readonly LogicFacadeBase logicFacade;

            /// <summary>
            /// Gets or sets the object that has been initialized only.
            /// </summary>
            public object InitialisedObject
            {
                get;
                set;
            }

            /// <summary>
            /// Determines whether this context is initialise only or not
            /// </summary>
            internal readonly InitOptions initOptions;

            /// <summary>
            /// Constructs a new InitialiseOnlyContext
            /// </summary>
            internal InitialisationContext(LogicFacadeBase logicFacade, InitOptions options)
            {
                this.logicFacade = logicFacade;
                this.initOptions = options;
            }

            /// <summary>
            /// Call to remove this current context and reveal the previous context (if any).
            /// </summary>
            public virtual void Dispose()
            {
                this.logicFacade.contexts.Pop();
            }
        }

        /// <summary>
        /// Gets the current context (or null if none).
        /// </summary>
        private InitialisationContext CurrentContext
        {
            get
            {
                //TODO: check if '|| this.contexts.Count == 0)' is intended as it was throwing an exception
                if (this.contexts == null || this.contexts.Count == 0)
                {
                    return null;
                }

                return this.contexts.Peek();
            }
        }

        [ThreadStatic]
        internal Stack<InitialisationContext> contexts;

        /// <summary>
        /// Checks if the current context is set to "initialize only".
        /// </summary>
        public bool IsCurrentlyInitializeOnly
        {
            get
            {
                return this.CurrentContext != null
                       && ((int)(this.CurrentContext.initOptions & InitOptions.InitialiseOnly) != 0);
            }
        }

        public IInitContext AcquireInitContext(InitOptions initOptions)
        {
            if (this.contexts == null)
            {
                this.contexts = new Stack<InitialisationContext>();
            }

            var context = new InitialisationContext(this, initOptions);

            this.contexts.Push(context);

            return context;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected T Execute<T>(ICommand<T> action)
        {
            try
            {
                DateTime before = DateTime.UtcNow;

                this.log.DebugFormat("Executing action '{0}'", action.GetType().Name);

                Init(action);

                if (this.CurrentContext != null)
                {
                    this.CurrentContext.InitialisedObject = action;
                }

                if (this.IsCurrentlyInitializeOnly)
                {
                    this.log.DebugFormat("Action '{0}' not executed (InitializedOnlyContext).", action.GetType().Name);
                    return default(T);
                }
                else
                {
                    T result = action.Execute();

                    TimeSpan timeTaken = DateTime.UtcNow - before;
                    this.log.DebugFormat("Action '{0}' executed. Took {1} ms.", action.GetType().Name, timeTaken.TotalMilliseconds);

                    return result;
                }

            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error executing action", ex);
                throw;
            }
        }

        protected abstract void Init<T>(ICommand<T> action);

        public virtual void Dispose() { }
    }
}