using LabsDebts.Models;
using LabsDebts.Services;
using System.Collections.ObjectModel;
//using static Java.Util.Jar.Attributes;

namespace LabsDebts;

public partial class MainPage : ContentPage
{
    private const uint MenuAnimationDuration = 250;
    private const double MenuClosedOffset = 300;
    private bool _isNavigating;
    private bool _isMenuAnimating;
    private readonly DatabaseService _db;
    //private List<Lab> _allLabs = new();
    private const int PageSize = 20;

    private int _currentPage = 1;

    private bool _isLoading;

    private bool _hasMoreData = true;

    private readonly ObservableCollection<Lab> _loadedLabs = new();
    public MainPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;
        LabsList.ItemsSource = _loadedLabs;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadLabs();
    }

    private async Task LoadLabs(bool loadMore = false)
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            if (!loadMore)
            {
                _currentPage = 1;
                _loadedLabs.Clear();
                _hasMoreData = true;
            }

            if (!_hasMoreData)
                return;

            var pageData = await _db.GetLabsPaged(
                _currentPage,
                PageSize);

            if (pageData.Count < PageSize)
                _hasMoreData = false;

            foreach (var lab in pageData)
            {
                _loadedLabs.Add(lab);
            }

            _currentPage++;
        }
        finally
        {
            _isLoading = false;
        }
    }
    private async void OnLoadMore(
    object sender,
    EventArgs e)
    {
        await LoadLabs(true);
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

        bool exists = await _db.LabExists(code, name);

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

    private async void OnSearchChanged(
    object sender,TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            LabsList.ItemsSource = _loadedLabs;
            await LoadLabs();
            return;
        }

        LabsList.ItemsSource = await _db.SearchLabs(text);
    }
    
    private void OnShowAddForm(object sender, EventArgs e)
    {
        AddForm.IsVisible = true;
        SearchForm.IsVisible = false;
        CloseMenuImmediately();

        AddTabButton.BackgroundColor = Color.FromArgb("#6C63FF");
        SearchTabButton.BackgroundColor = Color.FromArgb("#333333");
    }

    private void OnShowSearchForm(object sender, EventArgs e)
    {
        AddForm.IsVisible = false;
        SearchForm.IsVisible = true;
        CloseMenuImmediately();

        SearchTabButton.BackgroundColor = Color.FromArgb("#6C63FF");
        AddTabButton.BackgroundColor = Color.FromArgb("#333333");
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        if (_isMenuAnimating)
            return;

        _isMenuAnimating = true;

        MenuOverlay.IsVisible = true;

        MenuOverlay.Opacity = 0;

        MainMenu.TranslationX = 230;

        await Task.WhenAll(
            MenuOverlay.FadeTo(1, 250),
            MainMenu.TranslateTo(0, 0, 250, Easing.CubicOut)
        );

        _isMenuAnimating = false;
    }

    private async void OnCloseMenuTapped(object sender, TappedEventArgs e)
    {
        await CloseMenuAsync();
    }

    private async void OnAddLabMenuClicked(object sender, EventArgs e)
    {
        await CloseMenuAsync();

        await Navigation.PushAsync(new AddLabsPage(_db));
    }

    private async Task CloseMenuAsync()
    {
        if (_isMenuAnimating || !MenuOverlay.IsVisible)
            return;

        _isMenuAnimating = true;

        await Task.WhenAll(
            MenuOverlay.FadeTo(0, 200),
            MainMenu.TranslateTo(230, 0, 250, Easing.CubicIn)
        );

        MenuOverlay.IsVisible = false;

        _isMenuAnimating = false;
    }

    private void CloseMenuImmediately()
    {
        MainMenu.TranslationX = MenuClosedOffset;
        MenuOverlay.IsVisible = false;
        _isMenuAnimating = false;
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
    private async void OnExportDataMenuClicked(
    object sender,
    EventArgs e)
    {
        await CloseMenuAsync();

        await Navigation.PushAsync(
            new ExportDataPage(_db));
    }
}
