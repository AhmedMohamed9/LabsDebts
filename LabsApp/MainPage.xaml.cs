using LabsApp.Models;
using LabsApp.Services;
//using static Java.Util.Jar.Attributes;

namespace LabsApp;

public partial class MainPage : ContentPage
{
    private bool _isNavigating;
    private readonly DatabaseService _db;
    private List<Lab> _allLabs = new();

    public MainPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadLabs();
    }

    private async Task LoadLabs()
    {
        _allLabs = await _db.GetLabsWithTotals();

        LabsList.ItemsSource = _allLabs;
    }
    private async void OnLabTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating)
            return;

        try
        {
            _isNavigating = true;

            if (e.Parameter is not Lab selectedLab)
                return;

            await Navigation.PushAsync(new LabDetailsPage(selectedLab,_db));
        }
        finally
        {
            await Task.Delay(500);

            _isNavigating = false;
        }
    }
    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
    string.IsNullOrWhiteSpace(CodeEntry.Text))
        {
            await DisplayAlert(
                "خطأ",
                "من فضلك أدخل جميع البيانات",
                "OK");

            return;
        }

        // Validate numeric code

        if (!int.TryParse(CodeEntry.Text, out int labCode))
        {
            await DisplayAlert(
                "خطأ",
                "كود المعمل يجب أن يكون رقم",
                "OK");

            return;
        }

        var name = NameEntry.Text.Trim();
        var code = int.Parse(CodeEntry.Text);
        // Check duplicates

        bool exists = _allLabs.Any(x =>
            x.Name.ToLower() == name.ToLower() ||
            x.Code == code);

        if (exists)
        {
            await DisplayAlert("تنبيه", "المعمل أو الكود موجود بالفعل", "OK");
            return;
        }
        var lab = new Lab
        {
            Name = name,
            Code = code
        };

        await _db.AddLab(lab);

        NameEntry.Text = string.Empty;
        CodeEntry.Text = string.Empty;

        await LoadLabs();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.ToLower() ?? "";

        var filtered = _allLabs.Where(x =>
            x.Name.ToLower().Contains(text) ||
            x.Code.ToString().Contains(text))
            .ToList();

        LabsList.ItemsSource = filtered;
    }

    private void OnShowAddForm(object sender, EventArgs e)
    {
        AddForm.IsVisible = true;
        SearchForm.IsVisible = false;

        AddTabButton.BackgroundColor = Color.FromArgb("#6C63FF");
        SearchTabButton.BackgroundColor = Color.FromArgb("#333333");
    }

    private void OnShowSearchForm(object sender, EventArgs e)
    {
        AddForm.IsVisible = false;
        SearchForm.IsVisible = true;

        SearchTabButton.BackgroundColor = Color.FromArgb("#6C63FF");
        AddTabButton.BackgroundColor = Color.FromArgb("#333333");
    }
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (button?.CommandParameter is not Lab lab)
            return;

        bool confirm = await DisplayAlert(
            "تأكيد",
            "هل تريد حذف هذا المعمل؟",
            "نعم",
            "إلغاء");

        if (!confirm)
            return;

        await _db.DeleteLab(lab);

        await LoadLabs();
    }
}