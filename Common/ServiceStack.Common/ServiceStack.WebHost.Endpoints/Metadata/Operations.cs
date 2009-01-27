using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    public class Operations
    {
		const string RESPONSE_SUFFIX = "Response";
		
		public Operations(IList<Type> types)
    	{
    		Types = types;
			var typeNames = types.Select(x => x.Name);
			Names = new List<string>();

			OneWayOperations = new Operations();
			ReplyOperations = new Operations();

			foreach (var type in types)
			{
				if (type.Name.EndsWith(RESPONSE_SUFFIX)) continue;

				Names.Add(type.Name);

				var hasResponse = typeNames.Contains(type.Name + RESPONSE_SUFFIX);
				if (hasResponse)
				{
					ReplyOperations.Add(type);
				}
				else
				{
					OneWayOperations.Add(type);
				}
			}
			Names.Sort();


    	}

		public void Add(Type type)
		{
			this.Types.Add(type);
			this.Names.Add(type.Name);
		}

    	public Operations()
        {
            this.Names = new List<string>();
            this.Types = new List<Type>();
        }

        public List<string> Names { get; private set; }
        public IList<Type> Types { get; private set; }

		public Operations ReplyOperations { get; private set; }
		public Operations OneWayOperations { get; private set; }	
	}
}