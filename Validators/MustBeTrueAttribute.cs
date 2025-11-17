using System.ComponentModel.DataAnnotations;

namespace TestModule.Validators
{
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is bool b)
            {
                return b;
            }
            return false;
        }
    }
}