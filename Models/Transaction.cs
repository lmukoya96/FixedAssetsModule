namespace TestModule.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Cost { get; set; }
    }
}