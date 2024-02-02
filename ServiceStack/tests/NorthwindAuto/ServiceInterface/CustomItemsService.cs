using ServiceStack;

namespace MyApp.ServiceInterface;


public class Items
{
    public List<Item> Results { get; set; }
}
public class Item
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class GetItems : IReturn<Items> { }
public class GetNakedItems : IReturn<List<Item>> { }

public class AltQueryItems : IReturn<QueryResponseAlt<Item>>
{
    public string? Name { get; set; }
}
    
public class QueryResponseAlt<T> : IHasResponseStatus, IMeta
{
    public virtual int Offset { get; set; }
    public virtual int Total { get; set; }
    public virtual List<T> Results { get; set; }
    public virtual Dictionary<string, string> Meta { get; set; }
    public virtual ResponseStatus ResponseStatus { get; set; }
}

public class CustomItemsService : Service
{
    public object Any(AltQueryItems request) => new QueryResponseAlt<Item> {
        Results = Get(new GetNakedItems())
    };
        
    public Items Get(GetItems dto)
    {
        return new Items
        {
            Results =
            [
                new Item
                {
                    Name = "bar item 1",
                    Description = "item 1 description"
                },

                new Item
                {
                    Name = "bar item 2",
                    Description = "item 2 description"
                }
            ]
        };
    }

    public List<Item> Get(GetNakedItems request)
    {
        return
        [
            new Item
            {
                Name = "item 1",
                Description = "item 1 description"
            },

            new Item
            {
                Name = "item 2",
                Description = "item 2 description"
            }
        ];
    }
}
