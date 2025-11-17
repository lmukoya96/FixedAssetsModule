namespace TestModule.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
