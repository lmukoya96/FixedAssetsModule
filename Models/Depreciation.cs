namespace TestModule.Models
{
    public class Depreciation
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DepreciationRate { get; set; }
        public string DepreciationMethod { get; set; } = string.Empty; // "EqualInstallments" or "ReducingBalance"
        public decimal TaxRate { get; set; }
        public string TaxMethod { get; set; } = string.Empty;
    }
}