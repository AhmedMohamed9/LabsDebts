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
    private const int PageSize = 20;

    private int _currentPage = 1;

    private bool _isLoading;

    private bool _hasMoreData = true;
    private bool _isSearchMode;

    private string? _searchText;

    private DateTime _searchFromDate;

    private DateTime _searchToDate;

    private bool _hasTransactionsOnly;

    private readonly ObservableCollection<Lab> _loadedLabs = new();
    public MainPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;
        LabsList.ItemsSource = _loadedLabs;
        FromDatePicker.Date = new DateTime(2026, 6, 1);
        ToDatePicker.Date = DateTime.Today;
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
        if (loadMore && !_hasMoreData)
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
    private async void OnSearchButtonClicked(
    object sender,
    EventArgs e)
    {
        _searchText =
            SearchEntry.Text?.Trim();

        _searchFromDate =
            FromDatePicker.Date?? DateTime.MinValue;

        _searchToDate =
            ToDatePicker.Date?? DateTime.MaxValue;

        _hasTransactionsOnly =
            HasTransactionsOnlyCheckBox.IsChecked;

        _isSearchMode = true;

        await LoadSearchResults();
    }
    private async Task LoadSearchResults(
    bool loadMore = false)
    {
        System.Diagnostics.Debug.WriteLine(
        $"LoadSearchResults - loadMore={loadMore}");

        if (_isLoading)
            return;
        if (loadMore && !_hasMoreData)
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

            var result =
                await _db.SearchLabsPaged(
                    _searchText,
                    _searchFromDate,
                    _searchToDate,
                    _hasTransactionsOnly,
                    _currentPage,
                    PageSize);

            System.Diagnostics.Debug.WriteLine(
    $"Page {_currentPage} returned {result.Count} rows");

            if (result.Count < PageSize)
                _hasMoreData = false;

            foreach (var lab in result)
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
        if (_isLoading || !_hasMoreData)
            return;
        if (_isSearchMode)
        {
            await LoadSearchResults(true);
        }
        else
        {
            await LoadLabs(true);
        }
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

        if (_isSearchMode)
        {
            await LoadSearchResults();
        }
        else
        {
            await LoadLabs();
        }

        NameEntry.Text = string.Empty;
        CodeEntry.Text = string.Empty;

    }

    private async void OnSearchChanged(
    object sender,TextChangedEventArgs e)
    {
        //var text = e.NewTextValue?.Trim();

        //if (string.IsNullOrWhiteSpace(text))
        //{
        //    LabsList.ItemsSource = _loadedLabs;
        //    await LoadLabs();
        //    return;
        //}

        //LabsList.ItemsSource = await _db.SearchLabs(text);
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

        if (_isSearchMode)
        {
            await LoadSearchResults();
        }
        else
        {
            await LoadLabs();
        }
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
