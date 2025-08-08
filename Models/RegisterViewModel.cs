using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
