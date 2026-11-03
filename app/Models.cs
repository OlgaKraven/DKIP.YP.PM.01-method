namespace CatalogDesk;

public sealed record StockItem(string Label, int Quantity);
public sealed record Product(string Id, string Name, string Category,
    string Manufacturer, string Material, string Description,
    decimal BasePrice, List<StockItem> Stocks);
public sealed record OrderLine(string ProductId, DateOnly Date);
public sealed record Catalog(string Organization, string Dimension,
    DateOnly ControlDate, List<Product> Products, List<OrderLine> Orders);
public sealed record PriceResult(decimal Price, bool DiscountApplied,
    DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record ProductCard(string Id, string Name, string Category,
    string Manufacturer, string Material, string Description,
    decimal BasePrice, decimal Price, bool DiscountApplied,
    int TotalStock, string Availability, string Period, string Reason);
