using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace LabsDebts.Models
{
    public class Lab
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        [Unique]
        public  int Code { get; set; }
        [Ignore]
        public int UnpaidTotal { get; set; }
    }
}
