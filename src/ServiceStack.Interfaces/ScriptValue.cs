namespace ServiceStack
{
    /// <summary>
    /// Define a rich value that can either be a value, a constant JS Expression or a #Script Code script
    /// </summary>
    public interface IScriptValue
    {
        /// <summary>
        /// Use constant Value
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Create Value by Evaluating a #Script JS Expression. Lightweight, only evaluates an AST Token.
        /// Results are only evaluated *once* and cached globally in AppHost.ScriptContext.Cache
        /// </summary>
        string Expression { get; set; }

        /// <summary>
        /// Create Value by evaluating #Script Code, results of same expression are cached per request
        /// </summary>
        string Eval { get; set; }
        
        /// <summary>
        /// Whether to disable result caching for this Script Value
        /// </summary>
        bool NoCache { get; set; }
    }

    public struct ScriptValue : IScriptValue
    {
        public object Value { get; set; }
        public string Expression { get; set; }
        public string Eval { get; set; }
        public bool NoCache { get; set; }
    }

    public abstract class ScriptValueAttribute : AttributeBase, IScriptValue
    {
        public object Value { get; set; }
        public string Expression { get; set; }
        public string Eval { get; set; }
        public bool NoCache { get; set; }
    }
    
}