using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CatalogDesk;

internal static class VisualChecks
{
    [STAThread]
    static int Main(string[] args)
    {
        var root=Path.GetFullPath(args[0]);var target=Path.Combine(root,"assets/screens");Directory.CreateDirectory(target);
        var results=new List<string>();
        var app=new App();app.InitializeComponent();var win=new MainWindow();win.Show();
        var picker=(DatePicker)win.FindName("CalculationDate");var list=(ItemsControl)win.FindName("ProductList");var status=(TextBlock)win.FindName("Status");
        var load=typeof(MainWindow).GetMethod("LoadData",BindingFlags.Instance|BindingFlags.NonPublic)!;
        void Pump(){win.UpdateLayout();win.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);}
        void Load(string path){load.Invoke(win,[path]);Pump();}
        void Check(string name,bool value){if(!value)throw new Exception(name);results.Add("PASS "+name);}
        void Shot(string name){Pump();var content=(FrameworkElement)win.Content;var bmp=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32);var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){var rect=new Rect(0,0,content.ActualWidth,content.ActualHeight);dc.DrawRectangle(Brushes.White,null,rect);dc.DrawRectangle(new VisualBrush(content),null,rect);}bmp.Render(visual);var encoder=new JpegBitmapEncoder{QualityLevel=94};encoder.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(Path.Combine(target,name+".jpg"));encoder.Save(file);}
        T? Find<T>(DependencyObject o) where T:DependencyObject {if(o is T t)return t;for(int i=0;i<VisualTreeHelper.GetChildrenCount(o);i++){var f=Find<T>(VisualTreeHelper.GetChild(o,i));if(f!=null)return f;}return null;}
        ScrollViewer Scroll(){DependencyObject o=list;while(o is not ScrollViewer)o=VisualTreeHelper.GetParent(o);return (ScrollViewer)o;}
        try{
            if(args.Contains("--starter")){Shot("starter");Check("Starter explains unimplemented module",list.Items.Count==0&&status.Text.Contains("Реализовать"));}
            else{
                Load(Path.Combine(root,"data/demo/catalog.json"));Check("Demo has four cards",list.Items.Count==4);
                Scroll().ScrollToEnd();Shot("low-stock");Check("Last card is reachable",Scroll().VerticalOffset>0);
                Load(Path.Combine(root,"data/errors/missing-date.json"));Shot("missing-date");Check("Missing date clears cards",list.Items.Count==0&&status.Text.Contains("не загружены"));
                Load(Path.Combine(root,"data/errors/negative-price.json"));Shot("negative-price");Check("Negative price clears cards",list.Items.Count==0&&status.Text.Contains("цен"));
                var path=Path.Combine(root,"data/v07/catalog.json");Load(path);Scroll().ScrollToTop();Shot("recovery-v07");Check("Variant recovers",list.Items.Count==4&&win.Title.Contains("Синий лист"));
                var node=JsonNode.Parse(File.ReadAllText(path))!;node["controlDate"]="2028-03-01";node["orders"]=new JsonArray(new JsonObject{["productId"]="V07-01",["date"]="2028-02-29"});
                var qa=Path.Combine(root,"output/visual-input");Directory.CreateDirectory(qa);var leap=Path.Combine(qa,"leap.json");File.WriteAllText(leap,node.ToJsonString());Load(leap);Shot("leap-v07");
                var card=list.Items[0].GetType().GetProperty("Card")!.GetValue(list.Items[0])!;Check("Leap day in actual card",card.GetType().GetProperty("Price")!.GetValue(card)!.Equals(1700m));
                node["controlDate"]="2027-01-15";node["orders"]=new JsonArray();var empty=Path.Combine(qa,"empty-history.json");File.WriteAllText(empty,node.ToJsonString());Load(empty);Shot("empty-v07");
                card=list.Items[0].GetType().GetProperty("Card")!.GetValue(list.Items[0])!;Check("Empty history in actual card",card.GetType().GetProperty("Price")!.GetValue(card)!.Equals(1275m));
                Load(path);win.Width=880;win.Height=650;Pump();Scroll().ScrollToEnd();Shot("minimum-size");Check("Minimum-size bottom reached",Scroll().VerticalOffset>0);
            }
            File.WriteAllLines(Path.Combine(root,"output/visual-checks"+(args.Contains("--starter")?"-starter":"")+".txt"),results);return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{win.Close();app.Shutdown();}
    }
}
