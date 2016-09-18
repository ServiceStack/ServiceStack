// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack
{
    // The caching expression tree compiler was copied from MVC core to MVC Futures so that Futures code could benefit
    // from it and so that it could be exposed as a public API. This is the only public entry point into the system.
    // See the comments in the ExpressionUtil namespace for more information.
    //
    // The unit tests for the ExpressionUtil.* types are in the System.Web.Mvc.Test project.
    public static class CachedExpressionCompiler
    {
        private static readonly ParameterExpression _unusedParameterExpr = Expression.Parameter(typeof(object), "_unused");

        // Implements caching around LambdaExpression.Compile() so that equivalent expression trees only have to be
        // compiled once.
        public static Func<TModel, TValue> Compile<TModel, TValue>(this Expression<Func<TModel, TValue>> lambdaExpression)
        {
            if (lambdaExpression == null)
                throw new ArgumentNullException(nameof(lambdaExpression));

            return ExpressionUtil.CachedExpressionCompiler.Process(lambdaExpression);
        }

        // Evaluates an expression (not a LambdaExpression), e.g. 2 + 2.
        public static object Evaluate(Expression arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            Func<object, object> func = Wrap(arg);
            return func(null);
        }

        private static Func<object, object> Wrap(Expression arg)
        {
            Expression<Func<object, object>> lambdaExpr = Expression.Lambda<Func<object, object>>(Expression.Convert(arg, typeof(object)), _unusedParameterExpr);
            return ExpressionUtil.CachedExpressionCompiler.Process(lambdaExpr);
        }
    }
}

namespace ServiceStack.ExpressionUtil
{

    // BinaryExpression fingerprint class
    // Useful for things like array[index]

    internal sealed class BinaryExpressionFingerprint : ExpressionFingerprint
    {
        public BinaryExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
            : base(nodeType, type)
        {
            // Other properties on BinaryExpression (like IsLifted / IsLiftedToNull) are simply derived
            // from Type and NodeType, so they're not necessary for inclusion in the fingerprint.

            Method = method;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.binaryexpression.method.aspx
        public MethodInfo Method { get; private set; }

        public override bool Equals(object obj)
        {
            BinaryExpressionFingerprint other = obj as BinaryExpressionFingerprint;
            return (other != null)
                   && Equals(this.Method, other.Method)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Method);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal static class CachedExpressionCompiler
    {
        // This is the entry point to the cached expression compilation system. The system
        // will try to turn the expression into an actual delegate as quickly as possible,
        // relying on cache lookups and other techniques to save time if appropriate.
        // If the provided expression is particularly obscure and the system doesn't know
        // how to handle it, we'll just compile the expression as normal.
        public static Func<TModel, TValue> Process<TModel, TValue>(Expression<Func<TModel, TValue>> lambdaExpression)
        {
            return Compiler<TModel, TValue>.Compile(lambdaExpression);
        }

        private static class Compiler<TIn, TOut>
        {
            private static Func<TIn, TOut> _identityFunc;

            private static readonly ConcurrentDictionary<MemberInfo, Func<TIn, TOut>> _simpleMemberAccessDict =
                new ConcurrentDictionary<MemberInfo, Func<TIn, TOut>>();

            private static readonly ConcurrentDictionary<MemberInfo, Func<object, TOut>> _constMemberAccessDict =
                new ConcurrentDictionary<MemberInfo, Func<object, TOut>>();

            private static readonly ConcurrentDictionary<ExpressionFingerprintChain, Hoisted<TIn, TOut>> _fingerprintedCache =
                new ConcurrentDictionary<ExpressionFingerprintChain, Hoisted<TIn, TOut>>();

            public static Func<TIn, TOut> Compile(Expression<Func<TIn, TOut>> expr)
            {
                return CompileFromIdentityFunc(expr)
                       ?? CompileFromConstLookup(expr)
                       ?? CompileFromMemberAccess(expr)
                       ?? CompileFromFingerprint(expr)
                       ?? CompileSlow(expr);
            }

            private static Func<TIn, TOut> CompileFromConstLookup(Expression<Func<TIn, TOut>> expr)
            {
                ConstantExpression constExpr = expr.Body as ConstantExpression;
                if (constExpr != null)
                {
                    // model => {const}

                    TOut constantValue = (TOut)constExpr.Value;
                    return _ => constantValue;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromIdentityFunc(Expression<Func<TIn, TOut>> expr)
            {
                if (expr.Body == expr.Parameters[0])
                {
                    // model => model

                    // don't need to lock, as all identity funcs are identical
                    if (_identityFunc == null)
                    {
                        _identityFunc = expr.Compile();
                    }

                    return _identityFunc;
                }

                return null;
            }

            private static Func<TIn, TOut> CompileFromFingerprint(Expression<Func<TIn, TOut>> expr)
            {
                List<object> capturedConstants;
                ExpressionFingerprintChain fingerprint = FingerprintingExpressionVisitor.GetFingerprintChain(expr, out capturedConstants);

                if (fingerprint != null)
                {
                    var del = _fingerprintedCache.GetOrAdd(fingerprint, _ =>
                    {
                        // Fingerprinting succeeded, but there was a cache miss. Rewrite the expression
                        // and add the rewritten expression to the cache.

                        var hoistedExpr = HoistingExpressionVisitor<TIn, TOut>.Hoist(expr);
                        return hoistedExpr.Compile();
                    });
                    return model => del(model, capturedConstants);
                }

                // couldn't be fingerprinted
                return null;
            }

            private static Func<TIn, TOut> CompileFromMemberAccess(Expression<Func<TIn, TOut>> expr)
            {
                // Performance tests show that on the x64 platform, special-casing static member and
                // captured local variable accesses is faster than letting the fingerprinting system
                // handle them. On the x86 platform, the fingerprinting system is faster, but only
                // by around one microsecond, so it's not worth it to complicate the logic here with
                // an architecture check.

                MemberExpression memberExpr = expr.Body as MemberExpression;
                if (memberExpr != null)
                {
                    if (memberExpr.Expression == expr.Parameters[0] || memberExpr.Expression == null)
                    {
                        // model => model.Member or model => StaticMember
                        return _simpleMemberAccessDict.GetOrAdd(memberExpr.Member, _ => expr.Compile());
                    }

                    ConstantExpression constExpr = memberExpr.Expression as ConstantExpression;
                    if (constExpr != null)
                    {
                        // model => {const}.Member (captured local variable)
                        var del = _constMemberAccessDict.GetOrAdd(memberExpr.Member, _ =>
                        {
                            // rewrite as capturedLocal => ((TDeclaringType)capturedLocal).Member
                            var constParamExpr = Expression.Parameter(typeof(object), "capturedLocal");
                            var constCastExpr = Expression.Convert(constParamExpr, memberExpr.Member.DeclaringType);
                            var newMemberAccessExpr = memberExpr.Update(constCastExpr);
                            var newLambdaExpr = Expression.Lambda<Func<object, TOut>>(newMemberAccessExpr, constParamExpr);
                            return newLambdaExpr.Compile();
                        });

                        object capturedLocal = constExpr.Value;
                        return _ => del(capturedLocal);
                    }
                }

                return null;
            }

            private static Func<TIn, TOut> CompileSlow(Expression<Func<TIn, TOut>> expr)
            {
                // fallback compilation system - just compile the expression directly
                return expr.Compile();
            }
        }
    }

    internal sealed class ConditionalExpressionFingerprint : ExpressionFingerprint
    {
        public ConditionalExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on ConditionalExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            ConditionalExpressionFingerprint other = obj as ConditionalExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }

    internal sealed class ConstantExpressionFingerprint : ExpressionFingerprint
    {
        public ConstantExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on ConstantExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            ConstantExpressionFingerprint other = obj as ConstantExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }

    internal sealed class DefaultExpressionFingerprint : ExpressionFingerprint
    {
        public DefaultExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on DefaultExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            DefaultExpressionFingerprint other = obj as DefaultExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }

    internal abstract class ExpressionFingerprint
    {
        protected ExpressionFingerprint(ExpressionType nodeType, Type type)
        {
            NodeType = nodeType;
            Type = type;
        }

        // the type of expression node, e.g. OP_ADD, MEMBER_ACCESS, etc.
        public ExpressionType NodeType { get; private set; }

        // the CLR type resulting from this expression, e.g. int, string, etc.
        public Type Type { get; private set; }

        internal virtual void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddInt32((int)NodeType);
            combiner.AddObject(Type);
        }

        protected bool Equals(ExpressionFingerprint other)
        {
            return (other != null)
                   && (this.NodeType == other.NodeType)
                   && Equals(this.Type, other.Type);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExpressionFingerprint);
        }

        public override int GetHashCode()
        {
            HashCodeCombiner combiner = new HashCodeCombiner();
            AddToHashCodeCombiner(combiner);
            return combiner.CombinedHash;
        }
    }

    internal sealed class ExpressionFingerprintChain : IEquatable<ExpressionFingerprintChain>
    {
        public readonly List<ExpressionFingerprint> Elements = new List<ExpressionFingerprint>();

        public bool Equals(ExpressionFingerprintChain other)
        {
            // Two chains are considered equal if two elements appearing in the same index in
            // each chain are equal (value equality, not referential equality).

            if (other == null)
            {
                return false;
            }

            if (this.Elements.Count != other.Elements.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Elements.Count; i++)
            {
                if (!Equals(this.Elements[i], other.Elements[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExpressionFingerprintChain);
        }

        public override int GetHashCode()
        {
            HashCodeCombiner combiner = new HashCodeCombiner();
            Elements.ForEach(combiner.AddFingerprint);

            return combiner.CombinedHash;
        }
    }

    internal sealed class FingerprintingExpressionVisitor : ExpressionVisitor
    {
        private readonly List<object> _seenConstants = new List<object>();
        private readonly List<ParameterExpression> _seenParameters = new List<ParameterExpression>();
        private readonly ExpressionFingerprintChain _currentChain = new ExpressionFingerprintChain();
        private bool _gaveUp;

        private FingerprintingExpressionVisitor()
        {
        }

        private T GiveUp<T>(T node)
        {
            // We don't understand this node, so just quit.

            _gaveUp = true;
            return node;
        }

        // Returns the fingerprint chain + captured constants list for this expression, or null
        // if the expression couldn't be fingerprinted.
        public static ExpressionFingerprintChain GetFingerprintChain(Expression expr, out List<object> capturedConstants)
        {
            FingerprintingExpressionVisitor visitor = new FingerprintingExpressionVisitor();
            visitor.Visit(expr);

            if (visitor._gaveUp)
            {
                capturedConstants = null;
                return null;
            }
            else
            {
                capturedConstants = visitor._seenConstants;
                return visitor._currentChain;
            }
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                _currentChain.Elements.Add(null);
                return null;
            }
            else
            {
                return base.Visit(node);
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new BinaryExpressionFingerprint(node.NodeType, node.Type, node.Method));
            return base.VisitBinary(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            return GiveUp(node);
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new ConditionalExpressionFingerprint(node.NodeType, node.Type));
            return base.VisitConditional(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }

            _seenConstants.Add(node.Value);
            _currentChain.Elements.Add(new ConstantExpressionFingerprint(node.NodeType, node.Type));
            return base.VisitConstant(node);
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new DefaultExpressionFingerprint(node.NodeType, node.Type));
            return base.VisitDefault(node);
        }

        protected
#if !NETSTANDARD1_3
    	override
#endif
    	Expression VisitDynamic(DynamicExpression node)
        {
            return GiveUp(node);
        }

        protected override ElementInit VisitElementInit(ElementInit node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitExtension(Expression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new IndexExpressionFingerprint(node.NodeType, node.Type, node.Indexer));
            return base.VisitIndex(node);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitLabel(LabelExpression node)
        {
            return GiveUp(node);
        }

        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new LambdaExpressionFingerprint(node.NodeType, node.Type));
            return base.VisitLambda(node);
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new MemberExpressionFingerprint(node.NodeType, node.Type, node.Member));
            return base.VisitMember(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return GiveUp(node);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return GiveUp(node);
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            return GiveUp(node);
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new MethodCallExpressionFingerprint(node.NodeType, node.Type, node.Method));
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }

            int parameterIndex = _seenParameters.IndexOf(node);
            if (parameterIndex < 0)
            {
                // first time seeing this parameter
                parameterIndex = _seenParameters.Count;
                _seenParameters.Add(node);
            }

            _currentChain.Elements.Add(new ParameterExpressionFingerprint(node.NodeType, node.Type, parameterIndex));
            return base.VisitParameter(node);
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            return GiveUp(node);
        }

        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitTry(TryExpression node)
        {
            return GiveUp(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new TypeBinaryExpressionFingerprint(node.NodeType, node.Type, node.TypeOperand));
            return base.VisitTypeBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (_gaveUp)
            {
                return node;
            }
            _currentChain.Elements.Add(new UnaryExpressionFingerprint(node.NodeType, node.Type, node.Method));
            return base.VisitUnary(node);
        }
    }

    internal class HashCodeCombiner
    {
        private long _combinedHash64 = 0x1505L;

        public int CombinedHash
        {
            get { return _combinedHash64.GetHashCode(); }
        }

        public void AddFingerprint(ExpressionFingerprint fingerprint)
        {
            if (fingerprint != null)
            {
                fingerprint.AddToHashCodeCombiner(this);
            }
            else
            {
                AddInt32(0);
            }
        }

        public void AddEnumerable(IEnumerable e)
        {
            if (e == null)
            {
                AddInt32(0);
            }
            else
            {
                int count = 0;
                foreach (object o in e)
                {
                    AddObject(o);
                    count++;
                }
                AddInt32(count);
            }
        }

        public void AddInt32(int i)
        {
            _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ i;
        }

        public void AddObject(object o)
        {
            int hashCode = (o != null) ? o.GetHashCode() : 0;
            AddInt32(hashCode);
        }
    }

    internal delegate TValue Hoisted<in TModel, out TValue>(TModel model, List<object> capturedConstants);

    internal sealed class HoistingExpressionVisitor<TIn, TOut> : ExpressionVisitor
    {
        private static readonly ParameterExpression _hoistedConstantsParamExpr = Expression.Parameter(typeof(List<object>), "hoistedConstants");
        private int _numConstantsProcessed;

        // factory will create instance
        private HoistingExpressionVisitor()
        {
        }

        public static Expression<Hoisted<TIn, TOut>> Hoist(Expression<Func<TIn, TOut>> expr)
        {
            // rewrite Expression<Func<TIn, TOut>> as Expression<Hoisted<TIn, TOut>>

            var visitor = new HoistingExpressionVisitor<TIn, TOut>();
            var rewrittenBodyExpr = visitor.Visit(expr.Body);
            var rewrittenLambdaExpr = Expression.Lambda<Hoisted<TIn, TOut>>(rewrittenBodyExpr, expr.Parameters[0], _hoistedConstantsParamExpr);
            return rewrittenLambdaExpr;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            // rewrite the constant expression as (TConst)hoistedConstants[i];
            return Expression.Convert(Expression.Property(_hoistedConstantsParamExpr, "Item", Expression.Constant(_numConstantsProcessed++)), node.Type);
        }
    }

    internal sealed class IndexExpressionFingerprint : ExpressionFingerprint
    {
        public IndexExpressionFingerprint(ExpressionType nodeType, Type type, PropertyInfo indexer)
            : base(nodeType, type)
        {
            // Other properties on IndexExpression (like the argument count) are simply derived
            // from Type and Indexer, so they're not necessary for inclusion in the fingerprint.

            Indexer = indexer;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.indexexpression.indexer.aspx
        public PropertyInfo Indexer { get; private set; }

        public override bool Equals(object obj)
        {
            IndexExpressionFingerprint other = obj as IndexExpressionFingerprint;
            return (other != null)
                   && Equals(this.Indexer, other.Indexer)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Indexer);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal sealed class LambdaExpressionFingerprint : ExpressionFingerprint
    {
        public LambdaExpressionFingerprint(ExpressionType nodeType, Type type)
            : base(nodeType, type)
        {
            // There are no properties on LambdaExpression that are worth including in
            // the fingerprint.
        }

        public override bool Equals(object obj)
        {
            LambdaExpressionFingerprint other = obj as LambdaExpressionFingerprint;
            return (other != null)
                   && this.Equals(other);
        }
    }

    internal sealed class MemberExpressionFingerprint : ExpressionFingerprint
    {
        public MemberExpressionFingerprint(ExpressionType nodeType, Type type, MemberInfo member)
            : base(nodeType, type)
        {
            Member = member;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.memberexpression.member.aspx
        public MemberInfo Member { get; private set; }

        public override bool Equals(object obj)
        {
            MemberExpressionFingerprint other = obj as MemberExpressionFingerprint;
            return (other != null)
                   && Equals(this.Member, other.Member)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Member);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal sealed class MethodCallExpressionFingerprint : ExpressionFingerprint
    {
        public MethodCallExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
            : base(nodeType, type)
        {
            // Other properties on MethodCallExpression (like the argument count) are simply derived
            // from Type and Indexer, so they're not necessary for inclusion in the fingerprint.

            Method = method;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.methodcallexpression.method.aspx
        public MethodInfo Method { get; private set; }

        public override bool Equals(object obj)
        {
            MethodCallExpressionFingerprint other = obj as MethodCallExpressionFingerprint;
            return (other != null)
                   && Equals(this.Method, other.Method)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Method);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal sealed class ParameterExpressionFingerprint : ExpressionFingerprint
    {
        public ParameterExpressionFingerprint(ExpressionType nodeType, Type type, int parameterIndex)
            : base(nodeType, type)
        {
            ParameterIndex = parameterIndex;
        }

        // Parameter position within the overall expression, used to maintain alpha equivalence.
        public int ParameterIndex { get; private set; }

        public override bool Equals(object obj)
        {
            ParameterExpressionFingerprint other = obj as ParameterExpressionFingerprint;
            return (other != null)
                   && (this.ParameterIndex == other.ParameterIndex)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddInt32(ParameterIndex);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal sealed class TypeBinaryExpressionFingerprint : ExpressionFingerprint
    {
        public TypeBinaryExpressionFingerprint(ExpressionType nodeType, Type type, Type typeOperand)
            : base(nodeType, type)
        {
            TypeOperand = typeOperand;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.typebinaryexpression.typeoperand.aspx
        public Type TypeOperand { get; private set; }

        public override bool Equals(object obj)
        {
            TypeBinaryExpressionFingerprint other = obj as TypeBinaryExpressionFingerprint;
            return (other != null)
                   && Equals(this.TypeOperand, other.TypeOperand)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(TypeOperand);
            base.AddToHashCodeCombiner(combiner);
        }
    }

    internal sealed class UnaryExpressionFingerprint : ExpressionFingerprint
    {
        public UnaryExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
            : base(nodeType, type)
        {
            // Other properties on UnaryExpression (like IsLifted / IsLiftedToNull) are simply derived
            // from Type and NodeType, so they're not necessary for inclusion in the fingerprint.

            Method = method;
        }

        // http://msdn.microsoft.com/en-us/library/system.linq.expressions.unaryexpression.method.aspx
        public MethodInfo Method { get; private set; }

        public override bool Equals(object obj)
        {
            UnaryExpressionFingerprint other = obj as UnaryExpressionFingerprint;
            return (other != null)
                   && Equals(this.Method, other.Method)
                   && this.Equals(other);
        }

        internal override void AddToHashCodeCombiner(HashCodeCombiner combiner)
        {
            combiner.AddObject(Method);
            base.AddToHashCodeCombiner(combiner);
        }
    }
}