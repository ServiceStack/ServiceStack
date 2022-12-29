using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Funq;

namespace ServiceStack.ServiceHost.Tests.Examples
{
    /// <summary>
    /// Funq helper for easy registration.
    /// </summary>
    public static class FunqEasyRegistrationHelper
    {
        /// <summary>
        /// Register a service with the default, look-up-all_dependencies-from-the-container behavior.
        /// </summary>
        /// <typeparam name="interfaceT">interface type</typeparam>
        /// <typeparam name="implT">implementing type</typeparam>
        /// <param name="container">Funq container</param>
        public static void EasyRegister<interfaceT, implT>(this Container container) where implT : interfaceT
        {
            var lambdaParam = Expression.Parameter(typeof(Container), "ref_to_the_container_passed_into_the_lambda");

            var constructorExpression = BuildImplConstructorExpression<implT>(lambdaParam);
            var compiledExpression = CompileInterfaceConstructor<interfaceT>(lambdaParam, constructorExpression);

            container.Register(compiledExpression);
        }

        private static readonly MethodInfo FunqContainerResolveMethod;

        static FunqEasyRegistrationHelper()
        {
            FunqContainerResolveMethod = typeof(Container).GetMethod("Resolve", new Type[0]);
        }

        private static NewExpression BuildImplConstructorExpression<implT>(Expression lambdaParam)
        {
            var ctorWithMostParameters = GetConstructorWithMostParameters<implT>();

            var constructorParameterInfos = ctorWithMostParameters.GetParameters();
            var regParams = constructorParameterInfos.Select(pi => GetParameterCreationExpression(pi, lambdaParam));

            return Expression.New(ctorWithMostParameters, regParams.ToArray());
        }

        private static Func<Container, interfaceT> CompileInterfaceConstructor<interfaceT>(ParameterExpression lambdaParam, Expression constructorExpression)
        {
            var constructorLambda = Expression.Lambda<Func<Container, interfaceT>>(constructorExpression, lambdaParam);
            return constructorLambda.Compile();
        }

        private static ConstructorInfo GetConstructorWithMostParameters<implT>()
        {
            return typeof(implT)
                .GetConstructors()
                .OrderBy(x => x.GetParameters().Length)
                .Where(ctor => !ctor.IsStatic)
                .Last();
        }

        private static MethodCallExpression GetParameterCreationExpression(ParameterInfo pi, Expression lambdaParam)
        {
            var method = FunqContainerResolveMethod.MakeGenericMethod(pi.ParameterType);
            return Expression.Call(lambdaParam, method);
        }

    }
}