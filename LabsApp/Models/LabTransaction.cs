using SQLite;

namespace LabsApp.Models;

public class LabTransaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int LabId { get; set; }

    public int Amount { get; set; }

    public string Note { get; set; } = string.Empty;

    // تاريخ الإنشاء

    public DateTime? Date { get; set; }= DateTime.Now;

    // تاريخ السداد

    public DateTime? DueDate { get; set; }

    // تم الدفع

    public bool IsPaid { get; set; }
}