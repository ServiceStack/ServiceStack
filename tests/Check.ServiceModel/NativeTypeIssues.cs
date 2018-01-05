﻿using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    [ExcludeMetadata]
    public abstract class Issue221Base<T>
    {
        public T Id { get; set; }

        protected Issue221Base(T id)
        {
            Id = id;
        }
    }

    [ExcludeMetadata]
    public class Issue221Long : Issue221Base<long>
    {
        public Issue221Long(long id) : base(id)
        {
        }
    }
}