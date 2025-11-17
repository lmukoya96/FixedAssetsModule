using System.ComponentModel.DataAnnotations;

namespace TestModule.Models
{
    public class Period
    {
        public int PeriodID { get; set; }

        [Range(1, 12, ErrorMessage = "Period must be between 1 and 12")]
        public byte PeriodNum { get; set; }

        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public byte Month { get; set; }

        [Range(1900, 2100, ErrorMessage = "Year must be 4 digits")]
        public short Year { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; }
    }
}
