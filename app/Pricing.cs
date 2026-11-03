namespace CatalogDesk;

// Этот модуль не зависит от окна, JSON и системных часов.
public static class Pricing
{
    public static (DateOnly Start, DateOnly End) PreviousMonth(DateOnly asOf)
    {
        if (asOf.Year == 1 && asOf.Month == 1)
            throw new ArgumentOutOfRangeException(nameof(asOf), "Нет предыдущего месяца для января первого года.");
        var end = new DateOnly(asOf.Year, asOf.Month, 1);
        return (end.AddMonths(-1), end);
    }

    public static PriceResult Calculate(decimal basePrice, string productId,
        IEnumerable<OrderLine> orders, DateOnly asOf)
    {
        if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("Не указан код товара.", nameof(productId));
        ArgumentNullException.ThrowIfNull(orders);
        var (start, end) = PreviousMonth(asOf);
        bool hasOrders = orders.Any(o => o.ProductId == productId && o.Date >= start && o.Date < end);
        decimal factor = hasOrders ? 1m : 0.75m;
        decimal price = Math.Round(basePrice * factor, 2, MidpointRounding.AwayFromZero);
        return new PriceResult(price, !hasOrders, start, end);
    }

    public static (int Total, string Label) StockSummary(IEnumerable<StockItem> stocks)
    {
        ArgumentNullException.ThrowIfNull(stocks);
        var items = stocks.ToArray();
        if (items.Any(s => s.Quantity < 0)) throw new ArgumentException("Остаток не может быть отрицательным.");
        int total = items.Sum(s => s.Quantity);
        return (total, total > 5 ? "много" : "мало");
    }

    public static ProductCard CreateCard(Product product, IEnumerable<OrderLine> orders, DateOnly asOf)
    {
        var result = Calculate(product.BasePrice, product.Id, orders, asOf);
        var stock = StockSummary(product.Stocks);
        return new ProductCard(product.Id, product.Name, product.Category, product.Manufacturer,
            product.Material, product.Description, product.BasePrice, result.Price, result.DiscountApplied,
            stock.Total, stock.Label,
            $"{result.PeriodStart:dd.MM.yyyy}–{result.PeriodEnd.AddDays(-1):dd.MM.yyyy}",
            result.DiscountApplied ? "Заказов за прошлый месяц нет: скидка 25%." : "Есть заказ за прошлый месяц: базовая цена.");
    }
}
