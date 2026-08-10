using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

public class CoffeeShopServices : Service
{
    public async Task<object> Get(GetCoffeeShopMenu request)
    {
        var categories = await Db.SelectAsync<Category>();
        var products = await Db.SelectAsync<Product>();
        var options = await Db.SelectAsync<Option>();
        var links = await Db.SelectAsync<CategoryOption>();
        var quantities = await Db.SelectAsync<OptionQuantity>();

        return new GetCoffeeShopMenuResponse
        {
            Results = categories.Select(category => new MenuCategory
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Temperatures = category.Temperatures ?? [],
                DefaultTemperature = category.DefaultTemperature,
                Sizes = category.Sizes ?? [],
                DefaultSize = category.DefaultSize,
                ImageUrl = category.ImageUrl,
                Products = products.Where(x => x.CategoryId == category.Id).Select(x => new MenuProduct
                {
                    Id = x.Id, Name = x.Name, Cost = x.Cost, ImageUrl = x.ImageUrl,
                }).ToList(),
                Options = links.Where(x => x.CategoryId == category.Id)
                    .Join(options, x => x.OptionId, x => x.Id, (_, option) => new MenuOption
                    {
                        Type = option.Type,
                        Names = option.Names,
                        AllowQuantity = option.AllowQuantity == true,
                        QuantityLabel = option.QuantityLabel,
                    }).ToList(),
            }).ToList(),
            OptionQuantities = quantities.OrderBy(x => x.Value).Select(x => x.Name).ToList(),
        };
    }

    public async Task<object> Post(PreviewCoffeeShopOrder request) =>
        await PriceOrderAsync(request.CustomerName, request.Notes, request.Items);

    public async Task<object> Post(CreateCoffeeShopOrder request)
    {
        var preview = await PriceOrderAsync(request.CustomerName, request.Notes, request.Items);
        var session = await GetSessionAsync();
        using var transaction = Db.OpenTransaction();

        var order = new CoffeeShopOrder
        {
            OrderNumber = $"CS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerName = preview.CustomerName,
            CustomerUserId = session?.UserAuthId,
            Notes = preview.Notes,
            Status = "Submitted",
            Subtotal = preview.Subtotal,
            CreatedDate = DateTime.UtcNow,
        };
        order.Id = (int)await Db.InsertAsync(order, selectIdentity: true);

        order.Items = preview.Items.Select(x => new CoffeeShopOrderItem
        {
            CoffeeShopOrderId = order.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Quantity = x.Quantity,
            Size = x.Size,
            Temperature = x.Temperature,
            OptionsJson = x.Options.Count > 0 ? x.Options.ToJson() : null,
            UnitPrice = x.UnitPrice,
            LineTotal = x.LineTotal,
        }).ToList();
        await Db.InsertAllAsync(order.Items);
        transaction.Commit();

        return new CreateCoffeeShopOrderResponse { Result = order };
    }

    public async Task<object> Get(GetCoffeeShopOrder request)
    {
        var order = await Db.SingleByIdAsync<CoffeeShopOrder>(request.Id)
            ?? throw HttpError.NotFound($"Order {request.Id} was not found");
        order.Items = await Db.SelectAsync<CoffeeShopOrderItem>(x => x.CoffeeShopOrderId == order.Id);
        return new GetCoffeeShopOrderResponse { Result = order };
    }

    async Task<PreviewCoffeeShopOrderResponse> PriceOrderAsync(string customerName, string? notes,
        List<OrderItemRequest> requestedItems)
    {
        if (requestedItems.Count == 0)
            throw HttpError.BadRequest("Add at least one item to the order");

        var productIds = requestedItems.Select(x => x.ProductId).Distinct().ToList();
        var products = await Db.SelectAsync<Product>(x => productIds.Contains(x.Id));
        var categories = await Db.SelectAsync<Category>();
        var allOptions = await Db.SelectAsync<Option>();
        var links = await Db.SelectAsync<CategoryOption>();
        var validQuantities = (await Db.SelectAsync<OptionQuantity>()).Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = new List<PricedOrderItem>();

        foreach (var item in requestedItems)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId)
                ?? throw HttpError.BadRequest($"Product ID {item.ProductId} is not on the menu");
            if (item.Quantity < 1 || item.Quantity > 20)
                throw HttpError.BadRequest($"Quantity for {product.Name} must be between 1 and 20");

            var category = categories.First(x => x.Id == product.CategoryId);
            var size = NormalizeChoice(item.Size, category.Sizes, category.DefaultSize, "size", product.Name);
            var temperature = NormalizeChoice(item.Temperature, category.Temperatures,
                category.DefaultTemperature, "temperature", product.Name);
            var allowedOptions = links.Where(x => x.CategoryId == category.Id)
                .Join(allOptions, x => x.OptionId, x => x.Id, (_, option) => option).ToList();

            foreach (var selected in item.Options)
            {
                var option = allowedOptions.FirstOrDefault(x =>
                    x.Type.Equals(selected.Type, StringComparison.OrdinalIgnoreCase));
                if (option == null)
                    throw HttpError.BadRequest($"{selected.Type} is not available for {product.Name}");
                if (!option.Names.Any(x => x.Equals(selected.Name, StringComparison.OrdinalIgnoreCase)))
                    throw HttpError.BadRequest($"{selected.Name} is not a valid {option.Type} choice for {product.Name}");
                if (!string.IsNullOrEmpty(selected.Quantity) && option.AllowQuantity != true)
                    throw HttpError.BadRequest($"{option.Type} does not accept a quantity");
                if (!string.IsNullOrEmpty(selected.Quantity) && !validQuantities.Contains(selected.Quantity))
                    throw HttpError.BadRequest($"Option quantity '{selected.Quantity}' must be no, light, regular or extra");
            }

            var lineTotal = product.Cost * item.Quantity;
            var parts = new[] { temperature, size, product.Name }.Where(x => !string.IsNullOrEmpty(x)).ToList();
            if (item.Options.Count > 0)
                parts.Add("with " + string.Join(", ", item.Options.Select(x =>
                    string.IsNullOrEmpty(x.Quantity) ? x.Name : $"{x.Quantity} {x.Name}")));
            results.Add(new PricedOrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                Size = size,
                Temperature = temperature,
                Options = item.Options,
                UnitPrice = product.Cost,
                LineTotal = lineTotal,
                Summary = string.Join(" ", parts),
            });
        }

        return new PreviewCoffeeShopOrderResponse
        {
            CustomerName = customerName.Trim(),
            Notes = notes?.Trim(),
            Items = results,
            Subtotal = results.Sum(x => x.LineTotal),
        };
    }

    static string? NormalizeChoice(string? value, List<string>? choices, string? defaultValue,
        string label, string productName)
    {
        if (choices == null || choices.Count == 0)
        {
            if (!string.IsNullOrEmpty(value))
                throw HttpError.BadRequest($"{productName} does not support a {label}");
            return null;
        }
        value = string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        var normalized = choices.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw HttpError.BadRequest(
            $"Invalid {label} '{value}' for {productName}. Choose: {string.Join(", ", choices)}");
    }
}
