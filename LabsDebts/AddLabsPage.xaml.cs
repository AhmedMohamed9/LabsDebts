using ClosedXML.Excel;
using LabsDebts.Models;
using LabsDebts.Services;

namespace LabsDebts;

public partial class AddLabsPage : ContentPage
{
    private readonly DatabaseService _db;

    private FileResult? _selectedFile;

    public AddLabsPage(DatabaseService db)
    {
        InitializeComponent();

        _db = db;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(
    new PickOptions
    {
        PickerTitle = "اختر ملف Excel",
        FileTypes = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                {
                    DevicePlatform.Android,
                    new[]
                    {
                        "application/vnd.ms-excel",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    }
                }
            })
    });
            if (file == null)
                return;

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (extension != ".xlsx" &&
                extension != ".xls")
            {
                await DisplayAlert(
                    "خطأ",
                    "يرجى اختيار ملف Excel فقط (.xlsx أو .xls)",
                    "OK");

                return;
            }

            _selectedFile = file;

            SelectedFileLabel.Text = file.FileName;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                ex.Message,
                "OK");
        }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        ImportButton.IsEnabled = false;
        if (_selectedFile == null)
        {
            await DisplayAlert(
                "تنبيه",
                "يرجى اختيار ملف Excel أولاً",
                "OK");

            return;
        }

        try
        {

            int imported = 0;
            int skipped = 0;

            var existingLabs = await _db.GetLabs();

            using var stream =
                await _selectedFile.OpenReadAsync();

            using var workbook =
                new XLWorkbook(stream);

            var worksheet =
                workbook.Worksheet(1);
                
            var rows =
                worksheet.RowsUsed();

            var headerCode = worksheet.Cell(1, 1).GetString().Trim();
            var headerName = worksheet.Cell(1, 2).GetString().Trim();
            if (headerCode != "كود" ||
                headerName != "اسم المعمل")
            {
                await DisplayAlert(
                    "خطأ",
                    "صيغة الملف غير صحيحة",
                    "OK");

                return;
            }
            foreach (var row in rows.Skip(1))
            {
                try
                {
                    var codeText =
                        row.Cell(1).GetValue<string>().Trim();

                    var name =
                        row.Cell(2).GetValue<string>().Trim();

                    if (!int.TryParse(codeText, out int code))
                    {
                        skipped++;
                        continue;
                    }
                    if (code <= 0 || code > 99999)
                    {
                        skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        skipped++;
                        continue;
                    }
                    name = name.Trim();

                    if (name.Length < 3 || name.Length > 50)
                    {
                        skipped++;
                        continue;
                    }

                    bool exists = existingLabs.Any(x =>
                        x.Code == code ||
                        x.Name.ToLower() == name.ToLower());

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    var lab = new Lab
                    {
                        Code = code,
                        Name = name
                    };

                    await _db.AddLab(lab);

                    existingLabs.Add(lab);

                    imported++;
                }
                catch
                {
                    skipped++;
                }
            }

            ResultFrame.IsVisible = true;

            ImportedCountLabel.Text =
                $"✅ تم استيراد {imported} معمل";

            SkippedCountLabel.Text =
                $"⚠️ تم تجاهل {skipped} سجل";

            await DisplayAlert(
                "نجاح",
                $"تم استيراد {imported} معمل بنجاح",
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
            ImportButton.IsEnabled = true;
        }
    }
}