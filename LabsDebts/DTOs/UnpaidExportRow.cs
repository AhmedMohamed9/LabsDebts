using System;
using System.Collections.Generic;
using System.Text;

namespace LabsDebts.DTOs
{
    public class UnpaidExportRow
    {
        public int LabCode { get; set; }

        public string LabName { get; set; } = string.Empty;

        public int Amount { get; set; }

        public string Note { get; set; } = string.Empty;

        public DateTime? Date { get; set; }

        //public DateTime? DueDate { get; set; }
    }
}
