//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack
{
    /*
    * Useful collection DTO's that provide pretty Xml output for collection types, e.g.
    * 
    * ArrayOfIntId Ids { get; set; }		
    * ... =>
    * 
    * <Ids>
    *   <Id>1</Id>
    *   <Id>2</Id>
    *   <Id>3</Id>
    * <Ids>
    */

    [CollectionDataContract(ItemName = "String")]
    public partial class ArrayOfString : List<string>
    {
        public ArrayOfString()
        {
        }

        public ArrayOfString(IEnumerable<string> collection) : base(collection) { }
        public ArrayOfString(params string[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Id")]
    public partial class ArrayOfStringId : List<string>
    {
        public ArrayOfStringId()
        {
        }

        public ArrayOfStringId(IEnumerable<string> collection) : base(collection) { }
        public ArrayOfStringId(params string[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Guid")]
    public partial class ArrayOfGuid : List<Guid>
    {
        public ArrayOfGuid()
        {
        }

        public ArrayOfGuid(IEnumerable<Guid> collection) : base(collection) { }
        public ArrayOfGuid(params Guid[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Id")]
    public partial class ArrayOfGuidId : List<Guid>
    {
        public ArrayOfGuidId()
        {
        }

        public ArrayOfGuidId(IEnumerable<Guid> collection) : base(collection) { }
        public ArrayOfGuidId(params Guid[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Long")]
    public partial class ArrayOfLong : List<long>
    {
        public ArrayOfLong()
        {
        }

        public ArrayOfLong(IEnumerable<long> collection) : base(collection) { }
        public ArrayOfLong(params long[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Id")]
    public partial class ArrayOfLongId : List<long>
    {
        public ArrayOfLongId()
        {
        }

        public ArrayOfLongId(IEnumerable<long> collection) : base(collection) { }
        public ArrayOfLongId(params long[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Int")]
    public partial class ArrayOfInt : List<int>
    {
        public ArrayOfInt()
        {
        }

        public ArrayOfInt(IEnumerable<int> collection) : base(collection) { }
        public ArrayOfInt(params int[] args) : base(args) { }
    }

    [CollectionDataContract(ItemName = "Id")]
    public partial class ArrayOfIntId : List<int>
    {
        public ArrayOfIntId()
        {
        }

        public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
        public ArrayOfIntId(params int[] args) : base(args) { }
    }

}