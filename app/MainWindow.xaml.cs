using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace CatalogDesk;

public partial class MainWindow : Window
{
    private Catalog? catalog;
    private bool dateValidationFailed;
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    public MainWindow()
    {
        InitializeComponent();
        LoadData(Path.Combine(AppContext.BaseDirectory, "Data", "catalog.json"));
    }
    private void LoadData(string path)
    {
        try
        {
            catalog = DataLoader.Load(path);
            dateValidationFailed = false;
            OrganizationTitle.Text = catalog.Organization;
            Title = catalog.Organization + " — каталог и календарная цена";
            CalculationDate.SelectedDate = catalog.ControlDate.ToDateTime(TimeOnly.MinValue);
            RefreshCards();
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException or ArgumentException or OverflowException)
        {
            catalog = null;
            ProductList.ItemsSource = null;
            Status.Text = "Данные не загружены. " + ex.Message + " Открыть исправленный файл кнопкой «Открыть данные…».";
        }
    }
    private void RefreshCards()
    {
        if (catalog is null || Status is null) return;
        if (!IsDateValid()) return;
        try
        {
            var asOf = DateOnly.FromDateTime(CalculationDate.SelectedDate!.Value);
            ProductList.ItemsSource = catalog.Products.Select(p => new CardView(Pricing.CreateCard(p, catalog.Orders, asOf))).ToList();
            Status.Text = $"Показано товаров: {catalog.Products.Count}. Дата расчёта: {asOf:dd.MM.yyyy}. История заказов не изменена.";
        }
        catch (NotImplementedException)
        {
            ProductList.ItemsSource = null;
            Status.Text = "Данные загружены. Реализовать методы в Pricing.cs по методическим рекомендациям, затем запустить приложение снова.";
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            ProductList.ItemsSource = null;
            Status.Text = "Расчёт не выполнен. " + ex.Message;
        }
    }
    private bool IsDateValid()
    {
        if (dateValidationFailed || CalculationDate.SelectedDate is null || !DateTime.TryParse(CalculationDate.Text, Ru, DateTimeStyles.None, out var date)
            || date.Date != CalculationDate.SelectedDate.Value.Date)
        {
            ProductList.ItemsSource = null;
            Status.Text = "Указать действительную дату в формате ДД.ММ.ГГГГ или выбрать её в календаре.";
            return false;
        }
        return true;
    }
    private void Date_Changed(object sender, SelectionChangedEventArgs e)
    {
        dateValidationFailed = false;
        RefreshCards();
    }
    private void Date_CalendarClosed(object sender, RoutedEventArgs e)
    {
        // Повторный выбор того же дня не вызывает SelectedDateChanged.
        // Закрытие календаря подтверждает показанную действительную дату.
        if (CalculationDate.SelectedDate is not null
            && DateTime.TryParse(CalculationDate.Text, Ru, DateTimeStyles.None, out var date)
            && date.Date == CalculationDate.SelectedDate.Value.Date)
            dateValidationFailed = false;
        RefreshCards();
    }
    private void Date_Editing(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Tab && e.Key != System.Windows.Input.Key.Enter)
            dateValidationFailed = false;
    }
    private void Date_Invalid(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        dateValidationFailed = true;
        ProductList.ItemsSource = null;
        if (Status is not null) Status.Text = "Такой даты нет. Указать дату ДД.ММ.ГГГГ или выбрать её в календаре.";
    }
    private void Recalculate_Click(object sender, RoutedEventArgs e)
    {
        // Перевод фокуса завершает редактирование DatePicker перед чтением.
        KeyboardFocusHelper();
        RefreshCards();
    }
    private void KeyboardFocusHelper() => System.Windows.Input.Keyboard.Focus((IInputElement)FindName("ProductList"));
    private void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Данные каталога (*.json)|*.json", Title = "Открыть данные предметной области" };
        if (dialog.ShowDialog(this) == true) LoadData(dialog.FileName);
    }
    private sealed record CardView(ProductCard Card)
    {
        public string PlaceholderPath => Path.Combine(AppContext.BaseDirectory, "Assets", "picture.png");
        public bool IsLowStock => Card.TotalStock <= 3;
        public string Details => $"{Card.Category} · {Card.Manufacturer} · {Card.Material}";
        public string StockText => $"Остаток: {Card.TotalStock} — {Card.Availability}";
        public string PriceText => Card.Price.ToString("C2", Ru);
        public string BasePriceText => "Базовая: " + Card.BasePrice.ToString("C2", Ru);
        public string DiscountText => Card.DiscountApplied ? "Скидка 25%" : "Без скидки";
        public string PeriodText => "Проверяемый период: " + Card.Period;
    }
}
