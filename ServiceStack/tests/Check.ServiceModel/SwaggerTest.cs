using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel
{
    [Route("/{Version}/userdata", "GET")]
    public class SwaggerVersionTest
    {
        public string Version { get; set; }
    }

    [Route("/swagger/range")]
    public class SwaggerRangeTest
    {
        public string IntRange { get; set; }

        public string DoubleRange { get; set; }
    }

    public enum MyColorDesc
    {
        [Description("The color Red")]
        Red = 10,
        [Description("The color Green")]
        Green = 20,
        [Description("The color Blue")]
        Blue = 30,
    }

    public enum MyColorBasic
    {
        [Description("Basic color Red")]
        Red,
        [Description("Basic color Green")]
        Green,
        [Description("Basic color Blue")]
        Blue
    }

    [Flags]
    public enum MyColorFlags
    {
        [Description("Flag color Red")]
        Red = 10,
        [Description("Flag color Green")]
        Green = 20,
        [Description("Flag color Blue")]
        Blue = 30,
    }

    [Route("/swagger/desc")]
    public class SwaggerDescTest
    {
        [ApiMember(Description = "Color Description",
            ParameterType = "path", DataType = "string", IsRequired = true)]
        [ApiAllowableValues("Name", typeof(MyColorBasic))] //Enum
        [DataMember]
        public string Name { get; set; }

        [ApiMember]
        // [ApiAllowableValues("ColorBasic", typeof(MyColorDesc))] //Enum
        [DataMember]
        public MyColorBasic ColorBasic { get; set; }

        [ApiMember]
        // [ApiAllowableValues("NColorBasic", typeof(MyColorDesc))] //Enum
        [DataMember]
        public MyColorBasic? NColorBasic { get; set; }


        [ApiMember]
        [ApiAllowableValues("ColorDesc", typeof(MyColorDesc))] //Enum
        [DataMember]
        public MyColorDesc ColorDesc { get; set; }


        [ApiMember]
        [ApiAllowableValues("NColorDesc", typeof(MyColorDesc))] //Enum
        [DataMember]
        public MyColorDesc? NColorDesc { get; set; }


        [ApiMember]
        [ApiAllowableValues("ColorFlags", typeof(MyColorFlags))] //Enum
        [DataMember]
        public MyColorFlags ColorFlags { get; set; }
    }
    
    [Route("/swagger/search", "POST")]
    public class SwaggerSearch : IReturn<EmptyResponse>
    {
        public List<SearchFilter> Filters { get; set; }
    }

    public class SearchFilter
    {
        [ApiMember(Name = "Field")]
        public string Field { get; set; }

        [ApiMember(Name = "Values")]
        public List<string> Values { get; set; }

        [ApiMember(Name = "Type")]
        public string Type { get; set; }
    }
}