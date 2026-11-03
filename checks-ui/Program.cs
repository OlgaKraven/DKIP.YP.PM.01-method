using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CatalogDesk;

internal static class WindowChecks
{
    [STAThread]
    public static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        string log = args[1];
        var results = new List<string>();
        try
        {
            // Exercise actual WPF controls and event handlers in the same STA,
            // without injecting input into the user's desktop.
            var app = new App(); app.InitializeComponent();
            var win = new MainWindow();
            var picker = (DatePicker)win.FindName("CalculationDate");
            var list = (ItemsControl)win.FindName("ProductList");
            var status = (TextBlock)win.FindName("Status");
            var load = typeof(MainWindow).GetMethod("LoadData",BindingFlags.Instance|BindingFlags.NonPublic)!;
            object[] Cards() => list.ItemsSource?.Cast<object>().ToArray() ?? [];
            ProductCard Card(int i) => (ProductCard)Cards()[i].GetType().GetProperty("Card")!.GetValue(Cards()[i])!;
            void Check(string name,bool value)
            {
                if(!value)throw new Exception(name);
                results.Add("PASS " + name);
            }
            load.Invoke(win,[Path.Combine(root,"data/demo/catalog.json")]);
            Check("Four initial cards",Cards().Length==4);
            Check("Initial prices",Card(0).Price==1000m && Card(1).Price==900m && Card(3).Price==7.52m);
            picker.SelectedDate=new DateTime(2027,2,1);
            Check("Date event recalculates",Card(0).Price==750m && Card(1).Price==1200m);
            picker.SelectedDate=new DateTime(2027,1,15);
            Check("Reset returns initial prices",Card(0).Price==1000m && Card(1).Price==900m);
            load.Invoke(win,[Path.Combine(root,"data/errors/missing-date.json")]);
            Check("Missing date clears stale results",Cards().Length==0 && status.Text.Contains("не загружены"));
            load.Invoke(win,[Path.Combine(root,"data/errors/negative-price.json")]);
            Check("Negative price rejected",Cards().Length==0 && status.Text.Contains("не загружены"));
            load.Invoke(win,[Path.Combine(root,"data/v30/catalog.json")]);
            Check("Recover with own variant",Cards().Length==4 && Card(0).Id=="V30-01");
            picker.SelectedDate=null;
            Check("Empty date clears stale results",Cards().Length==0 && status.Text.Contains("дату"));
            picker.SelectedDate=new DateTime(2027,1,15);
            Check("Date recovery",Cards().Length==4);
            picker.Text="31.02.2027";
            var recalculate=typeof(MainWindow).GetMethod("Recalculate_Click",BindingFlags.Instance|BindingFlags.NonPublic)!;
            recalculate.Invoke(win,[win,new RoutedEventArgs()]);
            Check("Invalid date text clears stale results",Cards().Length==0 && status.Text.Contains("дату"));
            picker.SelectedDate=null;
            picker.Text="15.01.2027";
            picker.SelectedDate=new DateTime(2027,1,15);
            recalculate.Invoke(win,[win,new RoutedEventArgs()]);
            decimal beforeRepeat=Card(1).Price;
            recalculate.Invoke(win,[win,new RoutedEventArgs()]);
            Check("Repeated button recalculation",Card(1).Price==beforeRepeat);
            Check("Low stock flag",(bool)Cards()[3].GetType().GetProperty("IsLowStock")!.GetValue(Cards()[3])!);
            Check("Four and five not highlighted",!(bool)Cards()[1].GetType().GetProperty("IsLowStock")!.GetValue(Cards()[1])! && !(bool)Cards()[2].GetType().GetProperty("IsLowStock")!.GetValue(Cards()[2])!);
            File.WriteAllLines(log,results);
            win.Close(); app.Shutdown();
            return 0;
        }
        catch(Exception e)
        {
            results.Add(e.ToString());File.WriteAllLines(log,results);return 1;
        }
    }
}
