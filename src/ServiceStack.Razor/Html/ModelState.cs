using System;

namespace ServiceStack.Html
{
#if !NETCORE
    [Serializable]
#endif
    public class ModelState
    {
        private readonly ModelErrorCollection errors = new ModelErrorCollection();

        public ValueProviderResult Value { get; set; }

        public ModelErrorCollection Errors { get { return errors; } }
    }
}
