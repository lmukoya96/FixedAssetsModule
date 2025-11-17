namespace TestModule.Models
{
    public class Asset
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssetGroup { get; set; } = string.Empty;
        public string AssetCategory { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime DepreciationStartDate { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal Cost { get; set; }
        public string? TrackingCode { get; set; }
        public string? SerialNumber { get; set; }
    }
}
