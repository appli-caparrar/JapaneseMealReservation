using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JapaneseMealReservation.Models
{
    public class Login
    {
        [Required]
        public string? EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
