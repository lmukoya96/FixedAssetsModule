namespace TestModule.Models
{
    public class Ledger
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string SubAccount { get; set; } = string.Empty;

        // Convenience property → e.g. "6100-0010"
        public string DisplayValue => $"{Account}-{SubAccount}";
    }
}
