using ClosedXML.Excel;
using LabsDebts.Services;

namespace LabsDebts;

public partial class ExportDataPage : ContentPage
{
    private readonly DatabaseService _db;

    public ExportDataPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;

        FromDatePicker.Date = new DateTime(2026, 6, 1);
        ToDatePicker.Date = DateTime.Today;
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnExportClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            ExportButton.IsEnabled = false;

            var data =
                await _db.GetUnpaidTransactionsForExport(
                    FromDatePicker.Date,
                    ToDatePicker.Date);

            if (!data.Any())
            {
                await DisplayAlert(
                    "تنبيه",
                    "لا توجد بيانات للتصدير",
                    "OK");

                return;
            }

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Unpaid Debts");

            worksheet.Cell(1, 1).Value = "كود المعمل";
            worksheet.Cell(1, 2).Value = "اسم المعمل";
            worksheet.Cell(1, 3).Value = "المبلغ";
            worksheet.Cell(1, 5).Value = "تاريخ العملية";
            worksheet.Cell(1, 4).Value = "الملاحظة";
            //worksheet.Cell(1, 6).Value = "تاريخ السداد";

            int row = 2;

            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.LabCode;
                worksheet.Cell(row, 2).Value = item.LabName;
                worksheet.Cell(row, 3).Value = item.Amount;

                worksheet.Cell(row, 5).Value =
                    item.Date?.ToString("yyyy-MM-dd");

                worksheet.Cell(row, 4).Value = item.Note;

                //worksheet.Cell(row, 6).Value =
                //    item.DueDate?.ToString("yyyy-MM-dd");

                row++;
            }

            worksheet.Columns().AdjustToContents();

            var fileName =
                $"مديونيات معامل_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            var filePath =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    fileName);

            workbook.SaveAs(filePath);

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "تصدير المديونيات",
                    File = new ShareFile(filePath)
                });

            await DisplayAlert(
                "نجاح",
                "تم إنشاء ملف Excel بنجاح",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                ex.Message,
                "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            ExportButton.IsEnabled = true;
        }
    }
}