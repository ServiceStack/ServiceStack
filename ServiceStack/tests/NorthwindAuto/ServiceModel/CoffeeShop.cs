using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public static class CoffeeShopTags
{
    public const string CoffeeShop = "CoffeeShop";
}

public class Category
{
    [AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string>? Temperatures { get; set; }
    public string? DefaultTemperature { get; set; }
    public List<string>? Sizes { get; set; }
    public string? DefaultSize { get; set; }
    public string? ImageUrl { get; set; }
    [Reference] public List<Product> Products { get; set; } = [];
    [Reference] public List<CategoryOption> CategoryOptions { get; set; } = [];
}

public class Product
{
    [AutoIncrement] public int Id { get; set; }
    [References(typeof(Category))] public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? ImageUrl { get; set; }
}

public class Option
{
    [AutoIncrement] public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Names { get; set; } = [];
    public bool? AllowQuantity { get; set; }
    public string? QuantityLabel { get; set; }
}

public class OptionQuantity
{
    [AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class CategoryOption
{
    [AutoIncrement] public int Id { get; set; }
    [References(typeof(Category))] public int CategoryId { get; set; }
    [References(typeof(Option))] public int OptionId { get; set; }
}

public class CoffeeShopOrder
{
    [AutoIncrement] public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerUserId { get; set; }
    public string Status { get; set; } = "Submitted";
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime CreatedDate { get; set; }
    [Reference] public List<CoffeeShopOrderItem> Items { get; set; } = [];
}

public class CoffeeShopOrderItem
{
    [AutoIncrement] public int Id { get; set; }
    [References(typeof(CoffeeShopOrder))] public int CoffeeShopOrderId { get; set; }
    [References(typeof(Product))] public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Size { get; set; }
    public string? Temperature { get; set; }
    public string? OptionsJson { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class MenuCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Temperatures { get; set; } = [];
    public string? DefaultTemperature { get; set; }
    public List<string> Sizes { get; set; } = [];
    public string? DefaultSize { get; set; }
    public string? ImageUrl { get; set; }
    public List<MenuProduct> Products { get; set; } = [];
    public List<MenuOption> Options { get; set; } = [];
}

public class MenuProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? ImageUrl { get; set; }
}

public class MenuOption
{
    public string Type { get; set; } = string.Empty;
    public List<string> Names { get; set; } = [];
    public bool AllowQuantity { get; set; }
    public string? QuantityLabel { get; set; }
}

[Tag(CoffeeShopTags.CoffeeShop)]
[System.ComponentModel.Description("Returns the complete coffee shop menu with product IDs, prices, valid sizes, temperatures and customization options")]
[Tool("the user wants to browse the coffee shop menu, learn what can be ordered, check prices, or build an order", Safety = ToolSafety.ReadOnly, Keywords = ["coffee", "drink", "food", "bakery", "customizations"], FollowUps = [nameof(PreviewCoffeeShopOrder)], Take = 20)]
[Route("/coffee-shop/menu", "GET")]
public class GetCoffeeShopMenu : IGet, IReturn<GetCoffeeShopMenuResponse> { }

public class GetCoffeeShopMenuResponse
{
    public List<MenuCategory> Results { get; set; } = [];
    public List<string> OptionQuantities { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

public class OrderItemOption
{
    [System.ComponentModel.Description("Option group from the menu, e.g. Milks, Syrups, Sweeteners or Toppings")]
    [ValidateNotEmpty]
    public string Type { get; set; } = string.Empty;
    [System.ComponentModel.Description("Exact option name from that menu option group")]
    [ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;
    [System.ComponentModel.Description("Optional quantity label: no, light, regular or extra. Use only where the menu allows quantity")]
    public string? Quantity { get; set; }
}

public class OrderItemRequest
{
    [System.ComponentModel.Description("Product ID returned by GetCoffeeShopMenu")]
    [ValidateGreaterThan(0)]
    public int ProductId { get; set; }
    [System.ComponentModel.Description("Number of this configured item to order")]
    [ValidateGreaterThan(0)]
    public int Quantity { get; set; } = 1;
    [System.ComponentModel.Description("Exact size supported by the product category; omit to use its default")]
    public string? Size { get; set; }
    [System.ComponentModel.Description("Exact temperature supported by the product category; omit to use its default")]
    public string? Temperature { get; set; }
    [System.ComponentModel.Description("Requested customizations. Each option must be valid for the product category")]
    public List<OrderItemOption> Options { get; set; } = [];
}

[Tag(CoffeeShopTags.CoffeeShop)]
[System.ComponentModel.Description("Validates and prices a proposed order without saving it. Returns normalized defaults and actionable validation errors")]
[Tool("an order needs to be checked, normalized or priced before it is submitted", Safety = ToolSafety.ReadOnly, Keywords = ["preview", "quote", "total", "validate"], Prerequisites = [nameof(GetCoffeeShopMenu)], FollowUps = [nameof(CreateCoffeeShopOrder)])]
[Route("/coffee-shop/orders/preview", "POST")]
public class PreviewCoffeeShopOrder : IPost, IReturn<PreviewCoffeeShopOrderResponse>
{
    [System.ComponentModel.Description("Name to put on the order")]
    [ValidateNotEmpty]
    public string CustomerName { get; set; } = string.Empty;
    [System.ComponentModel.Description("Optional instructions applying to the whole order")]
    public string? Notes { get; set; }
    [System.ComponentModel.Description("One or more products from the current menu")]
    [ValidateNotEmpty]
    public List<OrderItemRequest> Items { get; set; } = [];
}

public class PricedOrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Size { get; set; }
    public string? Temperature { get; set; }
    public List<OrderItemOption> Options { get; set; } = [];
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class PreviewCoffeeShopOrderResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<PricedOrderItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[Tag(CoffeeShopTags.CoffeeShop)]
[System.ComponentModel.Description("Submits a validated coffee shop order. Product names and prices are always resolved from the database")]
[Tool("the user has finished choosing an order and wants to place or submit it", Safety = ToolSafety.Write, RequiresApproval = true, Keywords = ["buy", "checkout", "place order"], Prerequisites = [nameof(GetCoffeeShopMenu)], Preview = nameof(PreviewCoffeeShopOrder), FollowUps = [nameof(GetCoffeeShopOrder)], Aliases = ["PlaceCoffeeShopOrder"], Examples = ["{\"customerName\":\"Sam\",\"items\":[{\"productId\":5,\"quantity\":1,\"size\":\"Grande\",\"temperature\":\"Hot\",\"options\":[{\"type\":\"Milks\",\"name\":\"Oat Milk\"}]}]}"])]
[Route("/coffee-shop/orders", "POST")]
public class CreateCoffeeShopOrder : IPost, IReturn<CreateCoffeeShopOrderResponse>
{
    [System.ComponentModel.Description("Name to put on the order")]
    [ValidateNotEmpty]
    public string CustomerName { get; set; } = string.Empty;
    [System.ComponentModel.Description("Optional instructions applying to the whole order")]
    public string? Notes { get; set; }
    [System.ComponentModel.Description("Final order items. The approval form lets the user edit these before submission")]
    [ValidateNotEmpty]
    public List<OrderItemRequest> Items { get; set; } = [];
}

public class CreateCoffeeShopOrderResponse
{
    public CoffeeShopOrder Result { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[Tag(CoffeeShopTags.CoffeeShop)]
[System.ComponentModel.Description("Returns a previously submitted coffee shop order by ID")]
[Tool("the user asks for the details or status of a coffee shop order", Safety = ToolSafety.ReadOnly)]
[Route("/coffee-shop/orders/{Id}", "GET")]
public class GetCoffeeShopOrder : IGet, IReturn<GetCoffeeShopOrderResponse>
{
    [ValidateGreaterThan(0)] public int Id { get; set; }
}

public class GetCoffeeShopOrderResponse
{
    public CoffeeShopOrder Result { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}
