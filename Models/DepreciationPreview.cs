namespace TestModule.Models
{
    public class DepreciationPreview
    {
        public string AssetCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssetGroup { get; set; } = string.Empty;
        public decimal CurrentCost { get; set; }
        public decimal AccumulatedDepreciationStart { get; set; }
        public decimal PeriodExpense { get; set; }
        public decimal AccumulatedDepreciationEnd { get; set; }
        public decimal BookValueEnd { get; set; }
        public short Year { get; set; }
        public byte PeriodNum { get; set; }

        // NEW PROPERTY: Capture the rate used in calculation
        public decimal DepreciationRate { get; set; }

        // NEW PROPERTY: For reporting skipped assets
        public string? Status { get; set; } // e.g., "Calculated", "Skipped - Not Started", "Skipped - No Rate"
    }
}