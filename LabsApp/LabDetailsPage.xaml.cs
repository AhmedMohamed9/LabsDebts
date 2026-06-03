using LabsApp.Models;
using LabsApp.Services;
using System.Collections.ObjectModel;

namespace LabsApp;

public partial class LabDetailsPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly Lab _lab;
    private LabTransaction? _selectedTransaction;
    private int _page = 0;
    private bool _hasMoreItems = true;
    private ObservableCollection<LabTransaction> _transactions = new();
    private const int PageSize = 10;

    private bool _isLoading;
    public LabDetailsPage(Lab lab, DatabaseService db)
    {
        InitializeComponent();

        _lab = lab;
        _db = db;

        NameLabel.Text = lab.Name;
        CodeLabel.Text = $"#{lab.Code}";
        TransactionsList.ItemsSource = _transactions;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTransactions();
    }

    private async Task LoadTransactions(bool loadMore = false)
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            // Reset pagination

            if (!loadMore)
            {
                _page = 0;

                _hasMoreItems = true;

                _transactions.Clear();
            }

            // Load next page

            var newTransactions =
                await _db.GetTransactionsPaged(
                    _lab.Id,
                    _page,
                    PageSize);

            // Append items

            foreach (var item in newTransactions)
            {
                _transactions.Add(item);
            }

            // Next page

            if (newTransactions.Any())
            {
                _page++;
            }
            else
            {
                _hasMoreItems = false;
            }

            // Total

            var totalUnpaid =
                await _db.GetUnpaidTotal(_lab.Id);

            TotalUnpaidLabel.Text =
                $"{totalUnpaid:N0} جنيه";
        }
        finally
        {
            _isLoading = false;
        }
    }
    private async void OnLoadMore(object sender, EventArgs e)
    {
        if (_isLoading || !_hasMoreItems)
            return;
        await LoadTransactions(true);
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnShowTransactionPopup(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = true;

        // Start below screen

        PopupFrame.TranslationY = 500;

        PopupFrame.Opacity = 0;

        // Animate in

        await Task.WhenAll(
            PopupFrame.TranslateTo(0, 0, 250, Easing.CubicOut),
            PopupFrame.FadeTo(1, 250)
        );
    }

    private async void OnClosePopup(object sender, EventArgs e)
    {
        // Animate out

        await Task.WhenAll(
            PopupFrame.TranslateTo(0, 500, 200, Easing.CubicIn),
            PopupFrame.FadeTo(0, 200)
        );

        PopupOverlay.IsVisible = false;
    }
    private void OnPaidCheckedChanged(
    object sender,
    CheckedChangedEventArgs e)
    {
        bool isPaid = e.Value;

        PaidDateLabel.IsVisible = isPaid;

        DueDatePicker.IsVisible = isPaid;

        // Reset date when unpaid

        if (!isPaid)
        {
            DueDatePicker.Date = DateTime.Now;
        }
    }
    private async void OnAddTransactionClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlert("خطأ", "أدخل المبلغ", "OK");
            return;
        }

        // =========================
        // UPDATE EXISTING
        // =========================

        if (_selectedTransaction != null)
        {
            _selectedTransaction.Amount =
                int.Parse(AmountEntry.Text);

            _selectedTransaction.Note =
                NoteEditor.Text ?? "";

            _selectedTransaction.Date =
                TransactionDatePicker.Date;

            _selectedTransaction.DueDate =
                IsPaidCheckBox.IsChecked
                    ? DueDatePicker.Date
                    : null;

            _selectedTransaction.IsPaid =
                IsPaidCheckBox.IsChecked;

            await _db.UpdateTransaction(_selectedTransaction);

            // Update UI item without reload

            var index =
    _transactions.IndexOf(_selectedTransaction);

            if (index >= 0)
            {
                // Remove old item

                _transactions.RemoveAt(index);

                // Insert updated item

                _transactions.Insert(index, _selectedTransaction);
            }

            await DisplayAlert(
                "نجاح",
                "تم تحديث المديونية",
                "OK");
        }

        // =========================
        // ADD NEW
        // =========================

        else
        {
            var transaction = new LabTransaction
            {
                LabId = _lab.Id,
                Amount = int.Parse(AmountEntry.Text),
                Note = NoteEditor.Text ?? "",
                Date = TransactionDatePicker.Date,
                DueDate = IsPaidCheckBox.IsChecked
                    ? DueDatePicker.Date
                    : null,
                IsPaid = IsPaidCheckBox.IsChecked
            };

            await _db.AddTransaction(transaction);

            // Add directly to UI

            _transactions.Insert(0, transaction);

            await DisplayAlert(
                "نجاح",
                "تم إضافة المديونية",
                "OK");
        }

        // =========================
        // UPDATE TOTAL ONLY
        // =========================

        var totalUnpaid =
            await _db.GetUnpaidTotal(_lab.Id);

        TotalUnpaidLabel.Text =
            $"{totalUnpaid:N0} جنيه";

        // =========================
        // RESET FORM
        // =========================

        _selectedTransaction = null;

        AmountEntry.Text = string.Empty;

        NoteEditor.Text = string.Empty;

        IsPaidCheckBox.IsChecked = false;

        DueDatePicker.IsVisible = false;
        PaidDateLabel.IsVisible = false;

        DueDatePicker.IsVisible = false;
        TransactionDatePicker.Date = DateTime.Now;

        DueDatePicker.Date = DateTime.Now;

        // =========================
        // CLOSE POPUP
        // =========================

        await Task.WhenAll(
            PopupFrame.TranslateTo(0, 500, 200, Easing.CubicIn),
            PopupFrame.FadeTo(0, 200)
        );

        PopupOverlay.IsVisible = false;
    }
    private void OnDueDateCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        DueDatePicker.IsVisible = e.Value;
    }
    private async void OnPaidClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (button?.CommandParameter is not LabTransaction transaction)
            return;

        // Already paid

        if (transaction.IsPaid)
        {
            await DisplayAlert(
                "تم الدفع",
                $"✔ تم دفع مبلغ {transaction.Amount} بالفعل",
                "OK");

            return;
        }

        // Build modern confirmation message

        string dueDate = transaction.DueDate.HasValue
            ? transaction.DueDate.Value.ToString("dd/MM/yyyy")
            : "غير محدد";

        string message =
    $"""
المبلغ: {transaction.Amount}

تاريخ الإنشاء:
{transaction.Date:dd/MM/yyyy}

تاريخ السداد:
{dueDate}

ملاحظات:
{transaction.Note}
""";

        bool confirm = await DisplayAlert(
            "تأكيد الدفع",
            message,
            "تأكيد",
            "إلغاء");

        if (!confirm)
            return;

        // Update status

        transaction.IsPaid = true;
        transaction.DueDate = transaction.DueDate ?? DateTime.Now;

        await _db.UpdateTransaction(transaction);

        // await LoadTransactions();
        // Refresh item visually

        var index = _transactions.IndexOf(transaction);

        if (index >= 0)
        {
            _transactions.RemoveAt(index);

            _transactions.Insert(index, transaction);
        }

        // Update total only

        var totalUnpaid =
            await _db.GetUnpaidTotal(_lab.Id);

        TotalUnpaidLabel.Text =
            $"{totalUnpaid:N0} جنيه";
        // Success message

        await DisplayAlert(
            "نجاح",
            $"✔ تم تسجيل دفع مبلغ {transaction.Amount}",
            "OK");
    }
    private async void OnTransactionDoubleTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not LabTransaction transaction)
            return;

        _selectedTransaction = transaction;

        // Fill Form

        AmountEntry.Text = transaction.Amount.ToString();

        NoteEditor.Text = transaction.Note;

        TransactionDatePicker.Date = transaction.Date;

        IsPaidCheckBox.IsChecked = transaction.IsPaid;

        if (transaction.IsPaid &&
            transaction.DueDate.HasValue)
        {
            PaidDateLabel.IsVisible = true;

            DueDatePicker.IsVisible = true;

            DueDatePicker.Date =
                transaction.DueDate.Value;
        }
        else
        {
            PaidDateLabel.IsVisible = false;

            DueDatePicker.IsVisible = false;
        }

        // SHOW POPUP WITH ANIMATION

        PopupOverlay.IsVisible = true;

        PopupFrame.TranslationY = 500;

        PopupFrame.Opacity = 0;

        await Task.WhenAll(
            PopupFrame.TranslateTo(0, 0, 250, Easing.CubicOut),
            PopupFrame.FadeTo(1, 250)
        );
    }
    private async void OnDeleteTransactionClicked(object sender, EventArgs e)
    {
        var button = sender as Button;

        if (button?.CommandParameter is not LabTransaction transaction)
            return;

        bool confirm = await DisplayAlert(
            "حذف المديونية",
            $"""
هل تريد حذف هذه المديونية؟

المبلغ:
{transaction.Amount}

الملاحظات:
{transaction.Note}
""",
            "حذف",
            "إلغاء");

        if (!confirm)
            return;

        await _db.DeleteTransaction(transaction);

        //await LoadTransactions();
        _transactions.Remove(transaction);

        // Update total

        var totalUnpaid =
            await _db.GetUnpaidTotal(_lab.Id);

        TotalUnpaidLabel.Text =
            $"{totalUnpaid:N0} جنيه";

        await DisplayAlert(
            "نجاح",
            "تم حذف المديونية",
            "OK");
    }
}