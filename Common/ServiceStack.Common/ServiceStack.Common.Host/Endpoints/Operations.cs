using System;
using System.Collections.Generic;

namespace ServiceStack.Common.Host.Endpoints
{
    public class Operations
    {
        public Operations()
        {
            this.Names = new List<string>();
            this.Types = new List<Type>();
        }

        public List<string> Names { get; private set; }
        public List<Type> Types { get; private set; }
    }
}