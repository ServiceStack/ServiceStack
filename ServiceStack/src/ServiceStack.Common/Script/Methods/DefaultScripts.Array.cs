using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Script;

public partial class DefaultScripts
{
    public int push(IList list, object item)
    {
        list.Add(item);
        return list.Count;
    }

    public object pop(IList list)
    {
        if (list.Count > 0)
        {
            var ret = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return ret;
        }
        return null;
    }

    public object shift(IList list) => splice(list, 0);

    public object unshift(IList list, object item)
    {
        if (item is IList addItems)
        {
            for (var i = addItems.Count - 1; i >= 0; i--)
            {
                list.Insert(0, addItems[i]);
            }
        }
        else
        {
            list.Insert(0, item);
        }
        return list.Count;
    }

    public int indexOf(object target, object item)
    {
        if (target is string s)
            return item is char c
                ? s.IndexOf(c)
                : item is string str
                    ? s.IndexOf(str, (StringComparison)Context.Args[ScriptConstants.DefaultStringComparison])
                    : throw new NotSupportedException($"{item.GetType().Name} not supported in indexOf");
        if (target is IList list)
            return list.IndexOf(item);
            
        throw new NotSupportedException($"{target.GetType().Name} not supported in indexOf");
    }

    public int indexOf(object target, object item, int startIndex)
    {
        if (target is string s)
            return item is char c
                ? s.IndexOf(c, startIndex)
                : item is string str
                    ? s.IndexOf(str, startIndex, (StringComparison)Context.Args[ScriptConstants.DefaultStringComparison])
                    : throw new NotSupportedException($"{item.GetType().Name} not supported in indexOf");
        if (target is List<object> list)
            return list.IndexOf(item, startIndex);
            
        throw new NotSupportedException($"{target.GetType().Name} not supported in indexOf");
    }

    public int lastIndexOf(object target, object item)
    {
        if (target is string s)
            return item is char c
                ? s.LastIndexOf(c)
                : item is string str
                    ? s.LastIndexOf(str, (StringComparison)Context.Args[ScriptConstants.DefaultStringComparison])
                    : throw new NotSupportedException($"{item.GetType().Name} not supported in indexOf");
        if (target is List<object> list)
            return list.LastIndexOf(item);
        if (target is IList iList)
            return iList.Cast<object>().ToList().LastIndexOf(item);
            
        throw new NotSupportedException($"{target.GetType().Name} not supported in indexOf");
    }

    public int lastIndexOf(object target, object item, int startIndex)
    {
        if (target is string s)
            return item is char c
                ? s.LastIndexOf(c, startIndex)
                : item is string str
                    ? s.LastIndexOf(str, startIndex, (StringComparison)Context.Args[ScriptConstants.DefaultStringComparison])
                    : throw new NotSupportedException($"{item.GetType().Name} not supported in indexOf");
        if (target is List<object> list)
            return list.LastIndexOf(item, startIndex);
            
        throw new NotSupportedException($"{target.GetType().Name} not supported in indexOf");
    }
        
    public object splice(IList list, int removeAt)
    {
        if (list.Count > 0)
        {
            var ret = list[removeAt];
            list.RemoveAt(removeAt);
            return ret;
        }
        return null;
    }

    public List<object> splice(IList list, int removeAt, int deleteCount) =>
        splice(list, removeAt, deleteCount, null);

    public List<object> splice(IList list, int removeAt, int deleteCount, List<object> insertItems)
    {
        if (list.Count > 0)
        {
            var ret = new List<object>();
            for (var i = 0; i<deleteCount; i++)
            {
                ret.Add(list[removeAt]);
                list.RemoveAt(removeAt);
            }
            if (insertItems != null)
            {
                foreach (var item in insertItems.AsEnumerable().Reverse())
                {
                    list.Insert(removeAt, item);
                }
            }
            return ret;
        }
        return new List<object>();
    }

    public List<object> slice(IList list) => list.Map(x => x);

    public List<object> slice(IList list, int begin) => list.Map(x => x).Skip(begin).ToList();

    public List<object> slice(IList list, int begin, int end) => list.Map(x => x).Skip(begin).Take(end - begin).ToList();

    public IgnoreResult forEach(ScriptScopeContext scope, object target, JsArrowFunctionExpression arrowExpr)
    {
        var token = arrowExpr.Body;

        scope = scope.Clone();
        if (target is IList list)
        {
            var itemBinding = arrowExpr.Params[0].Name;
            var indexBinding = arrowExpr.Params.Length > 1 ? arrowExpr.Params[1].Name : ScriptConstants.Index;
            var arrayBinding = arrowExpr.Params.Length > 2 ? arrowExpr.Params[2].Name : null;

            for (var i = 0; i < list.Count; i++)
            {
                scope.ScopedParams[indexBinding] = i;
                if (arrayBinding != null)
                    scope.ScopedParams[arrayBinding] = list;
                
                scope = scope.AddItemToScope(itemBinding, list[i]);
                token.Evaluate(scope);
            }
        }
        else if (target is IDictionary d)
        {
            if (arrowExpr.Params.Length != 2)
                throw new NotSupportedException("Dictionary.forEach requires 2 lambda params");
                    
            var keyBinding = arrowExpr.Params[0].Name;
            var valueBinding = arrowExpr.Params[1].Name;

            foreach (var key in d.Keys)
            {
                scope.ScopedParams[keyBinding] = key;
                scope.ScopedParams[valueBinding] = d[key];
                token.Evaluate(scope);
            }
        }
        else throw new NotSupportedException("Can only use forEach on Lists or Dictionaries");

        return IgnoreResult.Value;
    }
        
    public bool every(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression) => 
        all(scope, list, expression, null);

    public bool some(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression) => 
        any(scope, list, expression, null);

    public object find(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression) => 
        first(scope, list, expression, null);

    public int findIndex(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression)
    {
        var items = list.AssertEnumerable(nameof(findIndex));
        var itemBinding = expression.Params[0].Name;
        var expr = expression.Body;

        var i = 0;
        foreach (var item in items)
        {
            scope.AddItemToScope(itemBinding, item, i);
            var result = expr.EvaluateToBool(scope);
            if (result)
                return i;
            i++;
        }

        return -1;
    }

    public List<object> filter(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression) => 
        where(scope, list, expression, null).ToList();

    public List<object> flat(IList list) => flatten(list, 1);
    public List<object> flat(IList list, int depth) => flatten(list, depth);

    public List<object> flatMap(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression) => 
        flat((IList)map(scope, list, expression, null));

    public List<object> flatMap(ScriptScopeContext scope, IList list, JsArrowFunctionExpression expression, int depth) => 
        flat((IList)map(scope, list, expression, null), depth);

    public bool includes(IList list, object item) =>
        includes(list, item, 0);

    public bool includes(IList list, object item, int fromIndex)
    {
        for (var i = fromIndex; i < list.Count; i++)
        {
            if (list[i].Equals(item))
                return true;
        }
        return false;
    }

    public List<object> sort(List<object> list)
    {
        list.Sort();
        return list;
    }
}