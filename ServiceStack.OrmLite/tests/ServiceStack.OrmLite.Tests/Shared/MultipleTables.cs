using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Shared
{
    public class Table1
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field1 { get; set; }
    }
    public class Table2
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field2 { get; set; }
    }
    public class Table3
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field3 { get; set; }
    }
    public class Table4
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field4 { get; set; }
    }
    public class Table5
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field5 { get; set; }
    }
    
    [Schema("Schema")]
    public class Schematable1
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field1 { get; set; }
    }
    [Schema("Schema")]
    public class Schematable2
    {
        public int Id { get; set; }
        public string String { get; set; }
        public string Field2 { get; set; }
    }
    
}