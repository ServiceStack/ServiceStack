using System;

namespace ServiceStack.Html
{
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public class ModelState
    {
        private readonly ModelErrorCollection errors = new ModelErrorCollection();

        public ValueProviderResult Value { get; set; }

        public ModelErrorCollection Errors { get { return errors; } }
    }
}
