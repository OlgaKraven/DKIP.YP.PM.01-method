using CatalogDesk;
using System.Text.Json;

var root = Path.GetFullPath(args.Length == 0 ? "." : args[0]);
int checks = 0;
void Equal<T>(string name, T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: ожидалось {expected}; получено {actual}");
    checks++;
}
void Reject(string name, Action action)
{
    try { action(); } catch (Exception e) when (e is ArgumentException or InvalidDataException or JsonException) { checks++; return; }
    throw new Exception(name + ": ошибочные данные приняты");
}

// Ожидания зафиксированы отдельно от реализации Pricing.
foreach (var path in Directory.GetFiles(Path.Combine(root, "data"), "catalog.json", SearchOption.AllDirectories))
{
    var c = DataLoader.Load(path);
    var before = File.ReadAllBytes(path);
    var values = new[] { c.Products[0].BasePrice, c.Products[1].BasePrice * 0.75m, c.Products[2].BasePrice * 0.75m, 7.52m };
    for (int i = 0; i < 4; i++) Equal(path + " price " + i, values[i], Pricing.Calculate(c.Products[i].BasePrice, c.Products[i].Id, c.Orders, c.ControlDate).Price);
    var cards = c.Products.Select(p => Pricing.CreateCard(p, c.Orders, c.ControlDate)).ToArray();
    Equal("stock6", 6, cards[0].TotalStock); Equal("stock5", 5, cards[1].TotalStock);
    Equal("stock4", 4, cards[2].TotalStock); Equal("stock3", 3, cards[3].TotalStock);
    Equal("many", "много", cards[0].Availability); Equal("few5", "мало", cards[1].Availability);
    Equal("few4", "мало", cards[2].Availability); Equal("few3", "мало", cards[3].Availability);
    Equal("January boundary", c.Products[0].BasePrice, Pricing.Calculate(c.Products[0].BasePrice, c.Products[0].Id, c.Orders, new DateOnly(2027,1,1)).Price);
    Equal("February boundary", c.Products[1].BasePrice, Pricing.Calculate(c.Products[1].BasePrice, c.Products[1].Id, c.Orders, new DateOnly(2027,2,1)).Price);
    Equal("No mutation", true, before.SequenceEqual(File.ReadAllBytes(path)));
}
var h = new[] { new OrderLine("A", new DateOnly(2026,12,31)), new OrderLine("B", new DateOnly(2027,1,1)) };
Equal("last day included", 1000m, Pricing.Calculate(1000m,"A",h,new DateOnly(2027,1,1)).Price);
Equal("current day excluded", 750m, Pricing.Calculate(1000m,"B",h,new DateOnly(2027,1,1)).Price);
Equal("other product excluded", 750m, Pricing.Calculate(1000m,"C",h,new DateOnly(2027,1,1)).Price);
Equal("leap day", 1000m, Pricing.Calculate(1000m,"A",new[]{new OrderLine("A",new DateOnly(2028,2,29))},new DateOnly(2028,3,1)).Price);
Equal("round half", 7.52m, Pricing.Calculate(10.02m,"C",h,new DateOnly(2027,1,1)).Price);
Equal("zero price", 0m, Pricing.Calculate(0m,"C",h,new DateOnly(2027,1,1)).Price);
Equal("empty history", 750m, Pricing.Calculate(1000m,"A",Array.Empty<OrderLine>(),new DateOnly(2027,1,1)).Price);
Equal("repeat", 750m, Pricing.Calculate(1000m,"C",h,new DateOnly(2027,1,1)).Price);
Reject("negative price",()=>Pricing.Calculate(-1m,"A",h,new DateOnly(2027,1,1)));
Reject("negative stock",()=>Pricing.StockSummary(new[]{new StockItem("x",-1)}));
Reject("missing id",()=>Pricing.Calculate(100m,"",h,new DateOnly(2027,1,1)));
Reject("earliest date",()=>Pricing.PreviousMonth(new DateOnly(1,1,1)));
foreach (var path in Directory.GetFiles(Path.Combine(root,"data","errors"),"*.json")) Reject(path,()=>DataLoader.Load(path));
Console.WriteLine($"PASS: {checks} checks; all data sets and error fixtures.");
