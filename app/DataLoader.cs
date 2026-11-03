using System.IO;
using System.Text.Json;
namespace CatalogDesk;

public static class DataLoader
{
    public static Catalog Load(string path)
    {
        var catalog = JsonSerializer.Deserialize<Catalog>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("В файле нет каталога.");
        if (string.IsNullOrWhiteSpace(catalog.Organization) || string.IsNullOrWhiteSpace(catalog.Dimension)
            || catalog.ControlDate == default || catalog.Products is null || catalog.Products.Count == 0 || catalog.Orders is null)
            throw new InvalidDataException("Нужны организация, название характеристики, контрольная дата, товары и история заказов.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in catalog.Products)
        {
            if (p is null || string.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id)
                || string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Category)
                || string.IsNullOrWhiteSpace(p.Manufacturer) || string.IsNullOrWhiteSpace(p.Material)
                || p.Description is null || p.BasePrice < 0 || decimal.Round(p.BasePrice, 2) != p.BasePrice
                || p.Stocks is null || p.Stocks.Any(s => s is null || string.IsNullOrWhiteSpace(s.Label) || s.Quantity < 0))
                throw new InvalidDataException("Проверить поля товара, уникальность кодов, цену до копеек и неотрицательные остатки.");
        }
        if (catalog.Orders.Any(o => o is null || !ids.Contains(o.ProductId) || o.Date == default))
            throw new InvalidDataException("У каждой строки истории нужны существующий код товара и дата yyyy-MM-dd.");
        return catalog;
    }
}
