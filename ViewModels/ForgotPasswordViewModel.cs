using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
