using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[NamedConnection("pgsql"), Schema("acme")]
public class Table1
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}

[NamedConnection("pgsql"), Schema("acme")]
public class Table2
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}