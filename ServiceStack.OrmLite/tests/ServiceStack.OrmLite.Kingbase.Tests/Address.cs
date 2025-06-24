using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Kingbase.Tests;

public class Address
{
    [AutoIncrement, PrimaryKey] public long Id { get; set; }

    public string Street { get; set; }
    public string City { get; set; }
}