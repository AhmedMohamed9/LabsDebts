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

        public string Name { get; set; } = string.Empty;

        public  int Code { get; set; }
        [Ignore]
        public int UnpaidTotal { get; set; }
    }
}
